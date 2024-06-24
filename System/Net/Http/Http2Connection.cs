using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http;

internal sealed class Http2Connection : HttpConnectionBase
{
	internal enum KeepAliveState
	{
		None,
		PingSent
	}

	private sealed class NopHeadersHandler : IHttpStreamHeadersHandler
	{
		public static readonly NopHeadersHandler Instance = new NopHeadersHandler();

		void IHttpStreamHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
		}

		void IHttpStreamHeadersHandler.OnHeadersComplete(bool endStream)
		{
		}

		void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index)
		{
		}

		void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
		{
		}

		void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
		}
	}

	private abstract class WriteQueueEntry : TaskCompletionSource
	{
		private readonly CancellationTokenRegistration _cancellationRegistration;

		public int WriteBytes { get; }

		public WriteQueueEntry(int writeBytes, CancellationToken cancellationToken)
			: base(TaskCreationOptions.RunContinuationsAsynchronously)
		{
			WriteBytes = writeBytes;
			_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
			{
				bool flag = ((WriteQueueEntry)s).TrySetCanceled(cancellationToken);
			}, this);
		}

		public bool TryDisableCancellation()
		{
			_cancellationRegistration.Dispose();
			return !base.Task.IsCanceled;
		}

		public abstract bool InvokeWriteAction(Memory<byte> writeBuffer);
	}

	private sealed class WriteQueueEntry<T> : WriteQueueEntry
	{
		private readonly T _state;

		private readonly Func<T, Memory<byte>, bool> _writeAction;

		public WriteQueueEntry(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken)
			: base(writeBytes, cancellationToken)
		{
			_state = state;
			_writeAction = writeAction;
		}

		public override bool InvokeWriteAction(Memory<byte> writeBuffer)
		{
			return _writeAction(_state, writeBuffer);
		}
	}

	private enum FrameType : byte
	{
		Data = 0,
		Headers = 1,
		Priority = 2,
		RstStream = 3,
		Settings = 4,
		PushPromise = 5,
		Ping = 6,
		GoAway = 7,
		WindowUpdate = 8,
		Continuation = 9,
		AltSvc = 10,
		Last = 10
	}

	private readonly struct FrameHeader
	{
		public readonly int PayloadLength;

		public readonly FrameType Type;

		public readonly FrameFlags Flags;

		public readonly int StreamId;

		public bool PaddedFlag => (Flags & FrameFlags.Padded) != 0;

		public bool AckFlag => (Flags & FrameFlags.EndStream) != 0;

		public bool EndHeadersFlag => (Flags & FrameFlags.EndHeaders) != 0;

		public bool EndStreamFlag => (Flags & FrameFlags.EndStream) != 0;

		public bool PriorityFlag => (Flags & FrameFlags.Priority) != 0;

		public FrameHeader(int payloadLength, FrameType type, FrameFlags flags, int streamId)
		{
			PayloadLength = payloadLength;
			Type = type;
			Flags = flags;
			StreamId = streamId;
		}

		public static FrameHeader ReadFrom(ReadOnlySpan<byte> buffer)
		{
			FrameFlags flags = (FrameFlags)buffer[4];
			int payloadLength = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
			FrameType type = (FrameType)buffer[3];
			int streamId = (int)(BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(5)) & 0x7FFFFFFF);
			return new FrameHeader(payloadLength, type, flags, streamId);
		}

		public static void WriteTo(Span<byte> destination, int payloadLength, FrameType type, FrameFlags flags, int streamId)
		{
			BinaryPrimitives.WriteInt32BigEndian(destination.Slice(5), streamId);
			destination[4] = (byte)flags;
			destination[0] = (byte)((payloadLength & 0xFF0000) >> 16);
			destination[1] = (byte)((payloadLength & 0xFF00) >> 8);
			destination[2] = (byte)((uint)payloadLength & 0xFFu);
			destination[3] = (byte)type;
		}

		public override string ToString()
		{
			return $"StreamId={StreamId}; Type={Type}; Flags={Flags}; PayloadLength={PayloadLength}";
		}
	}

	[Flags]
	private enum FrameFlags : byte
	{
		None = 0,
		EndStream = 1,
		Ack = 1,
		EndHeaders = 4,
		Padded = 8,
		Priority = 0x20,
		ValidBits = 0x2D
	}

	private enum SettingId : ushort
	{
		HeaderTableSize = 1,
		EnablePush = 2,
		MaxConcurrentStreams = 3,
		InitialWindowSize = 4,
		MaxFrameSize = 5,
		MaxHeaderListSize = 6,
		EnableConnect = 8
	}

	private sealed class Http2Stream : IValueTaskSource, IHttpStreamHeadersHandler, IHttpTrace
	{
		private enum ResponseProtocolState : byte
		{
			ExpectingStatus,
			ExpectingIgnoredHeaders,
			ExpectingHeaders,
			ExpectingData,
			ExpectingTrailingHeaders,
			Complete,
			Aborted
		}

		private enum StreamCompletionState : byte
		{
			InProgress,
			Completed,
			Failed
		}

		private sealed class Http2ReadStream : Http2ReadWriteStream
		{
			public override bool CanWrite => false;

			public Http2ReadStream(Http2Stream http2Stream)
				: base(http2Stream, closeResponseBodyOnDispose: true)
			{
			}

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				throw new NotSupportedException(System.SR.net_http_content_readonly_stream);
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken)
			{
				return ValueTask.FromException(new NotSupportedException(System.SR.net_http_content_readonly_stream));
			}
		}

		private sealed class Http2WriteStream : Http2ReadWriteStream
		{
			public long BytesWritten { get; private set; }

			public long ContentLength { get; }

			public override bool CanRead => false;

			public Http2WriteStream(Http2Stream http2Stream, long contentLength)
				: base(http2Stream)
			{
				ContentLength = contentLength;
			}

			public override int Read(Span<byte> buffer)
			{
				throw new NotSupportedException(System.SR.net_http_content_writeonly_stream);
			}

			public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
			{
				return ValueTask.FromException<int>(new NotSupportedException(System.SR.net_http_content_writeonly_stream));
			}

			public override void CopyTo(Stream destination, int bufferSize)
			{
				throw new NotSupportedException(System.SR.net_http_content_writeonly_stream);
			}

			public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
			{
				return Task.FromException(new NotSupportedException(System.SR.net_http_content_writeonly_stream));
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
			{
				BytesWritten += buffer.Length;
				if ((ulong)BytesWritten > (ulong)ContentLength)
				{
					return ValueTask.FromException(new HttpRequestException(System.SR.net_http_content_write_larger_than_content_length));
				}
				return base.WriteAsync(buffer, cancellationToken);
			}
		}

		public class Http2ReadWriteStream : HttpBaseStream
		{
			private Http2Stream _http2Stream;

			private readonly HttpResponseMessage _responseMessage;

			protected bool CloseResponseBodyOnDispose { get; private init; }

			public override bool CanRead => _http2Stream != null;

			public override bool CanWrite => _http2Stream != null;

			public Http2ReadWriteStream(Http2Stream http2Stream, bool closeResponseBodyOnDispose = false)
			{
				_http2Stream = http2Stream;
				_responseMessage = _http2Stream._response;
				CloseResponseBodyOnDispose = closeResponseBodyOnDispose;
			}

			~Http2ReadWriteStream()
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					_http2Stream?.Trace("", "Finalize");
				}
				try
				{
					Dispose(disposing: false);
				}
				catch (Exception value)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						_http2Stream?.Trace($"Error: {value}", "Finalize");
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				Http2Stream http2Stream = Interlocked.Exchange(ref _http2Stream, null);
				if (http2Stream != null)
				{
					if (CloseResponseBodyOnDispose)
					{
						http2Stream.CloseResponseBody();
					}
					base.Dispose(disposing);
				}
			}

			public override int Read(Span<byte> destination)
			{
				Http2Stream http2Stream = _http2Stream;
				ObjectDisposedException.ThrowIf(http2Stream == null, this);
				return http2Stream.ReadData(destination, _responseMessage);
			}

			public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
			{
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream == null)
				{
					return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream")));
				}
				if (cancellationToken.IsCancellationRequested)
				{
					return ValueTask.FromCanceled<int>(cancellationToken);
				}
				return http2Stream.ReadDataAsync(destination, _responseMessage, cancellationToken);
			}

			public override void CopyTo(Stream destination, int bufferSize)
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				Http2Stream http2Stream = _http2Stream ?? throw ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream"));
				http2Stream.CopyTo(_responseMessage, destination, bufferSize);
			}

			public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream != null)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						return http2Stream.CopyToAsync(_responseMessage, destination, bufferSize, cancellationToken);
					}
					return Task.FromCanceled<int>(cancellationToken);
				}
				return Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException("Http2ReadStream")));
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
			{
				return _http2Stream?.SendDataAsync(buffer, cancellationToken) ?? ValueTask.FromException(new ObjectDisposedException("Http2WriteStream"));
			}

			public override Task FlushAsync(CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled(cancellationToken);
				}
				Http2Stream http2Stream = _http2Stream;
				if (http2Stream == null)
				{
					return Task.CompletedTask;
				}
				return http2Stream._connection.FlushAsync(cancellationToken);
			}
		}

		private readonly Http2Connection _connection;

		private readonly HttpRequestMessage _request;

		private HttpResponseMessage _response;

		private HttpResponseHeaders _trailers;

		private System.Net.MultiArrayBuffer _responseBuffer;

		private Http2StreamWindowManager _windowManager;

		private CreditWaiter _creditWaiter;

		private int _availableCredit;

		private readonly object _creditSyncObject = new object();

		private StreamCompletionState _requestCompletionState;

		private StreamCompletionState _responseCompletionState;

		private ResponseProtocolState _responseProtocolState;

		private bool _responseHeadersReceived;

		private Exception _resetException;

		private bool _canRetry;

		private bool _requestBodyAbandoned;

		private ManualResetValueTaskSourceCore<bool> _waitSource = new ManualResetValueTaskSourceCore<bool>
		{
			RunContinuationsAsynchronously = true
		};

		private CancellationTokenRegistration _waitSourceCancellation;

		private bool _hasWaiter;

		private readonly CancellationTokenSource _requestBodyCancellationSource;

		private readonly TaskCompletionSource<bool> _expect100ContinueWaiter;

		private int _headerBudgetRemaining;

		private bool _sendRstOnResponseClose;

		private static readonly (HeaderDescriptor descriptor, byte[] value)[] s_hpackStaticHeaderTable = new(HeaderDescriptor, byte[])[47]
		{
			(KnownHeaders.AcceptCharset.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AcceptEncoding.Descriptor, "gzip, deflate"u8.ToArray()),
			(KnownHeaders.AcceptLanguage.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AcceptRanges.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Accept.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.AccessControlAllowOrigin.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Age.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Allow.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Authorization.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.CacheControl.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentDisposition.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentEncoding.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLanguage.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLength.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentLocation.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentRange.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ContentType.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Cookie.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Date.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ETag.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Expect.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Expires.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.From.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Host.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfMatch.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfModifiedSince.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfNoneMatch.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfRange.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.IfUnmodifiedSince.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.LastModified.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Link.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Location.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.MaxForwards.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ProxyAuthenticate.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.ProxyAuthorization.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Range.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Referer.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Refresh.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.RetryAfter.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Server.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.SetCookie.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.StrictTransportSecurity.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.TransferEncoding.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.UserAgent.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Vary.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.Via.Descriptor, Array.Empty<byte>()),
			(KnownHeaders.WWWAuthenticate.Descriptor, Array.Empty<byte>())
		};

		private static ReadOnlySpan<byte> StatusHeaderName => ":status"u8;

		private object SyncObject => this;

		public int StreamId { get; private set; }

		public bool SendRequestFinished => _requestCompletionState != StreamCompletionState.InProgress;

		public bool ExpectResponseData => _responseProtocolState == ResponseProtocolState.ExpectingData;

		public Http2Connection Connection => _connection;

		public bool ConnectProtocolEstablished { get; private set; }

		private static ReadOnlySpan<int> HpackStaticStatusCodeTable => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		public Http2Stream(HttpRequestMessage request, Http2Connection connection)
		{
			_request = request;
			_connection = connection;
			_requestCompletionState = StreamCompletionState.InProgress;
			_responseCompletionState = StreamCompletionState.InProgress;
			_responseProtocolState = ResponseProtocolState.ExpectingStatus;
			_responseBuffer = new System.Net.MultiArrayBuffer(1024);
			_windowManager = new Http2StreamWindowManager(connection, this);
			_headerBudgetRemaining = connection._pool.Settings.MaxResponseHeadersByteLength;
			if (_request.Content == null || _request.IsExtendedConnectRequest)
			{
				_requestCompletionState = StreamCompletionState.Completed;
				if (_request.IsExtendedConnectRequest)
				{
					_requestBodyCancellationSource = new CancellationTokenSource();
				}
			}
			else
			{
				_requestBodyCancellationSource = new CancellationTokenSource();
				if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
				{
					_expect100ContinueWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				}
			}
			_response = new HttpResponseMessage
			{
				Version = HttpVersion.Version20,
				RequestMessage = _request,
				Content = new HttpConnectionResponseContent()
			};
		}

		public void Initialize(int streamId, int initialWindowSize)
		{
			StreamId = streamId;
			_availableCredit = initialWindowSize;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"initialWindowSize"}={initialWindowSize}", "Initialize");
			}
		}

		public HttpResponseMessage GetAndClearResponse()
		{
			HttpResponseMessage response = _response;
			_response = null;
			return response;
		}

		public async Task SendRequestBodyAsync(CancellationToken cancellationToken)
		{
			if (_request.Content == null || _request.IsExtendedConnectRequest)
			{
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{_request.Content}", "SendRequestBodyAsync");
			}
			CancellationTokenRegistration linkedRegistration = default(CancellationTokenRegistration);
			bool sendRequestContent = true;
			try
			{
				if (_expect100ContinueWaiter != null)
				{
					linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
					sendRequestContent = await WaitFor100ContinueAsync(_requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (sendRequestContent)
				{
					using Http2WriteStream writeStream = new Http2WriteStream(this, _request.Content.Headers.ContentLength ?? (-1));
					if (HttpTelemetry.Log.IsEnabled())
					{
						HttpTelemetry.Log.RequestContentStart();
					}
					ValueTask valueTask = _request.Content.InternalCopyToAsync(writeStream, null, _requestBodyCancellationSource.Token);
					if (valueTask.IsCompleted)
					{
						valueTask.GetAwaiter().GetResult();
					}
					else
					{
						if (linkedRegistration.Equals(default(CancellationTokenRegistration)))
						{
							linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
						}
						await valueTask.ConfigureAwait(continueOnCapturedContext: false);
					}
					if (writeStream.BytesWritten < writeStream.ContentLength)
					{
						throw new HttpRequestException(System.SR.Format(System.SR.net_http_request_content_length_mismatch, writeStream.BytesWritten, writeStream.ContentLength));
					}
					if (HttpTelemetry.Log.IsEnabled())
					{
						HttpTelemetry.Log.RequestContentStop(writeStream.BytesWritten);
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Finished sending request body.", "SendRequestBodyAsync");
				}
			}
			catch (Exception value)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Failed to send request body: {value}", "SendRequestBodyAsync");
				}
				bool flag;
				lock (SyncObject)
				{
					if (_requestBodyAbandoned)
					{
						_requestCompletionState = StreamCompletionState.Completed;
						Complete();
						return;
					}
					(bool signalWaiter, bool sendReset) tuple = CancelResponseBody();
					(flag, _) = tuple;
					_ = tuple.sendReset;
					_requestCompletionState = StreamCompletionState.Failed;
					SendReset();
					Complete();
				}
				if (flag)
				{
					_waitSource.SetResult(result: true);
				}
				throw;
			}
			finally
			{
				linkedRegistration.Dispose();
			}
			bool flag2 = false;
			lock (SyncObject)
			{
				_requestCompletionState = StreamCompletionState.Completed;
				bool flag3 = false;
				if (_responseCompletionState != 0)
				{
					flag2 = _responseCompletionState == StreamCompletionState.Failed;
					flag3 = true;
				}
				if (flag2)
				{
					SendReset();
				}
				else if (!sendRequestContent)
				{
					_sendRstOnResponseClose = true;
				}
				else
				{
					_connection.LogExceptions(_connection.SendEndStreamAsync(StreamId));
				}
				if (flag3)
				{
					Complete();
				}
			}
		}

		public async ValueTask<bool> WaitFor100ContinueAsync(CancellationToken cancellationToken)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Waiting to send request body content for 100-Continue.", "WaitFor100ContinueAsync");
			}
			TaskCompletionSource<bool> expect100ContinueWaiter = _expect100ContinueWaiter;
			using (cancellationToken.UnsafeRegister(delegate(object s)
			{
				((TaskCompletionSource<bool>)s).TrySetResult(result: false);
			}, expect100ContinueWaiter))
			{
				ConfiguredAsyncDisposable configuredAsyncDisposable = new Timer(delegate(object s)
				{
					Http2Stream http2Stream = (Http2Stream)s;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						http2Stream.Trace("100-Continue timer expired.", "WaitFor100ContinueAsync");
					}
					http2Stream._expect100ContinueWaiter?.TrySetResult(result: true);
				}, this, _connection._pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan).ConfigureAwait(continueOnCapturedContext: false);
				bool result;
				try
				{
					bool flag = await expect100ContinueWaiter.Task.ConfigureAwait(continueOnCapturedContext: false);
					CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
					result = flag;
				}
				finally
				{
					IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
					if (asyncDisposable != null)
					{
						await asyncDisposable.DisposeAsync();
					}
				}
				return result;
			}
		}

		private void SendReset()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Stream reset. Request={_requestCompletionState}, Response={_responseCompletionState}.", "SendReset");
			}
			if (_resetException == null)
			{
				_connection.LogExceptions(_connection.SendRstStreamAsync(StreamId, Http2ProtocolErrorCode.Cancel));
			}
		}

		private void Complete()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Stream complete. Request={_requestCompletionState}, Response={_responseCompletionState}.", "Complete");
			}
			_connection.RemoveStream(this);
			lock (_creditSyncObject)
			{
				CreditWaiter creditWaiter = _creditWaiter;
				if (creditWaiter != null)
				{
					creditWaiter.Dispose();
					_creditWaiter = null;
				}
			}
		}

		private void Cancel()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("", "Cancel");
			}
			CancellationTokenSource cancellationTokenSource = null;
			bool flag = false;
			bool flag2 = false;
			lock (SyncObject)
			{
				if (_requestCompletionState == StreamCompletionState.InProgress)
				{
					cancellationTokenSource = _requestBodyCancellationSource;
				}
				(flag, flag2) = CancelResponseBody();
			}
			cancellationTokenSource?.Cancel();
			lock (SyncObject)
			{
				if (flag2)
				{
					SendReset();
					if (!ConnectProtocolEstablished)
					{
						Complete();
					}
				}
			}
			if (flag)
			{
				_waitSource.SetResult(result: true);
			}
		}

		private (bool signalWaiter, bool sendReset) CancelResponseBody()
		{
			bool item = _sendRstOnResponseClose;
			if (_responseCompletionState == StreamCompletionState.InProgress)
			{
				_responseCompletionState = StreamCompletionState.Failed;
				if (_requestCompletionState != 0)
				{
					item = true;
				}
			}
			_responseBuffer.DiscardAll();
			_responseProtocolState = ResponseProtocolState.Aborted;
			bool hasWaiter = _hasWaiter;
			_hasWaiter = false;
			return (signalWaiter: hasWaiter, sendReset: item);
		}

		public void OnWindowUpdate(int amount)
		{
			lock (_creditSyncObject)
			{
				checked
				{
					_availableCredit += amount;
				}
				if (_availableCredit > 0 && _creditWaiter != null)
				{
					int num = Math.Min(_availableCredit, _creditWaiter.Amount);
					if (_creditWaiter.TrySetResult(num))
					{
						_availableCredit -= num;
					}
				}
			}
		}

		void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index)
		{
			if (index <= 7)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Invalid request pseudo-header ID {index}.", "OnStaticIndexedHeader");
				}
				throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
			}
			if (index <= 14)
			{
				int statusCode = HpackStaticStatusCodeTable[index - 8];
				OnStatus(statusCode);
			}
			else
			{
				var (descriptor, array) = s_hpackStaticHeaderTable[index - 15];
				OnHeader(descriptor, array);
			}
		}

		void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
		{
			if (index <= 7)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Invalid request pseudo-header ID {index}.", "OnStaticIndexedHeader");
				}
				throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
			}
			if (index <= 14)
			{
				int statusCode = HttpConnectionBase.ParseStatusCode(value);
				OnStatus(statusCode);
			}
			else
			{
				HeaderDescriptor item = s_hpackStaticHeaderTable[index - 15].descriptor;
				OnHeader(item, value);
			}
		}

		void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
			OnHeader(name, value);
		}

		private void AdjustHeaderBudget(int amount)
		{
			_headerBudgetRemaining -= amount;
			if (_headerBudgetRemaining < 0)
			{
				throw new HttpRequestException(HttpRequestError.ConfigurationLimitExceeded, System.SR.Format(System.SR.net_http_response_headers_exceeded_length, _connection._pool.Settings.MaxResponseHeadersByteLength));
			}
		}

		private void OnStatus(int statusCode)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Status code is {statusCode}", "OnStatus");
			}
			AdjustHeaderBudget(10);
			lock (SyncObject)
			{
				if (_responseProtocolState == ResponseProtocolState.Aborted)
				{
					return;
				}
				if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Received extra status header.", "OnStatus");
					}
					throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response_multiple_status_codes);
				}
				if (_responseProtocolState != 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Status pseudo-header received in {_responseProtocolState} state.", "OnStatus");
					}
					throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response_pseudo_header_in_trailer);
				}
				_response.StatusCode = (HttpStatusCode)statusCode;
				if (statusCode < 200)
				{
					_responseProtocolState = ResponseProtocolState.ExpectingIgnoredHeaders;
					if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueWaiter != null)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Received 100-Continue status.", "OnStatus");
						}
						_expect100ContinueWaiter.TrySetResult(result: true);
					}
					return;
				}
				if (statusCode == 200 && _response.RequestMessage.IsExtendedConnectRequest)
				{
					ConnectProtocolEstablished = true;
				}
				_responseProtocolState = ResponseProtocolState.ExpectingHeaders;
				if (_expect100ContinueWaiter != null)
				{
					bool result = statusCode < 300;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Expecting 100 Continue but received final status {statusCode}.", "OnStatus");
					}
					_expect100ContinueWaiter.TrySetResult(result);
				}
			}
		}

		private void OnHeader(HeaderDescriptor descriptor, ReadOnlySpan<byte> value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace(descriptor.Name + ": " + Encoding.ASCII.GetString(value), "OnHeader");
			}
			AdjustHeaderBudget(descriptor.Name.Length + value.Length);
			lock (SyncObject)
			{
				if (_responseProtocolState == ResponseProtocolState.Aborted || _responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders)
				{
					return;
				}
				if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingTrailingHeaders)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Received header before status.", "OnHeader");
					}
					throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
				}
				Encoding valueEncoding = _connection._pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);
				if (_responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
				{
					string headerValue = descriptor.GetHeaderValue(value, valueEncoding);
					_trailers.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, headerValue);
				}
				else if ((descriptor.HeaderType & HttpHeaderType.Content) == HttpHeaderType.Content)
				{
					string headerValue2 = descriptor.GetHeaderValue(value, valueEncoding);
					_response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue2);
				}
				else
				{
					string responseHeaderValueWithCaching = _connection.GetResponseHeaderValueWithCaching(descriptor, value, valueEncoding);
					_response.Headers.TryAddWithoutValidation(((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, responseHeaderValueWithCaching);
				}
			}
		}

		public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
		{
			if (name[0] == 58)
			{
				if (!name.SequenceEqual(StatusHeaderName))
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("Invalid response pseudo-header '" + Encoding.ASCII.GetString(name) + "'.", "OnHeader");
					}
					throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
				}
				int statusCode = HttpConnectionBase.ParseStatusCode(value);
				OnStatus(statusCode);
			}
			else
			{
				if (!HeaderDescriptor.TryGet(name, out var descriptor))
				{
					throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
				}
				OnHeader(descriptor, value);
			}
		}

		public void OnHeadersStart()
		{
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.ExpectingData:
					_responseProtocolState = ResponseProtocolState.ExpectingTrailingHeaders;
					if (_trailers == null)
					{
						_trailers = new HttpResponseHeaders(containsTrailingHeaders: true);
					}
					break;
				default:
					ThrowProtocolError();
					break;
				case ResponseProtocolState.ExpectingStatus:
				case ResponseProtocolState.Aborted:
					break;
				}
			}
		}

		public void OnHeadersComplete(bool endStream)
		{
			bool hasWaiter;
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.Aborted:
					return;
				case ResponseProtocolState.ExpectingHeaders:
					_responseProtocolState = (endStream ? ResponseProtocolState.Complete : ResponseProtocolState.ExpectingData);
					_responseHeadersReceived = true;
					break;
				case ResponseProtocolState.ExpectingTrailingHeaders:
					if (!endStream)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Trailing headers received without endStream", "OnHeadersComplete");
						}
						ThrowProtocolError();
					}
					_responseProtocolState = ResponseProtocolState.Complete;
					break;
				case ResponseProtocolState.ExpectingIgnoredHeaders:
					if (endStream)
					{
						ThrowProtocolError();
					}
					_responseProtocolState = ResponseProtocolState.ExpectingStatus;
					return;
				default:
					ThrowProtocolError();
					break;
				}
				if (endStream)
				{
					_responseCompletionState = StreamCompletionState.Completed;
					if (_requestCompletionState == StreamCompletionState.Completed && !ConnectProtocolEstablished)
					{
						Complete();
					}
				}
				if (_responseProtocolState == ResponseProtocolState.ExpectingData)
				{
					_windowManager.Start();
				}
				hasWaiter = _hasWaiter;
				_hasWaiter = false;
			}
			if (hasWaiter)
			{
				_waitSource.SetResult(result: true);
			}
		}

		public void OnResponseData(ReadOnlySpan<byte> buffer, bool endStream)
		{
			bool hasWaiter;
			lock (SyncObject)
			{
				switch (_responseProtocolState)
				{
				case ResponseProtocolState.Aborted:
					return;
				default:
					ThrowProtocolError();
					break;
				case ResponseProtocolState.ExpectingData:
					break;
				}
				if (_responseBuffer.ActiveMemory.Length + buffer.Length > _windowManager.StreamWindowSize)
				{
					ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
				}
				_responseBuffer.EnsureAvailableSpace(buffer.Length);
				_responseBuffer.AvailableMemory.CopyFrom(buffer);
				_responseBuffer.Commit(buffer.Length);
				if (endStream)
				{
					_responseProtocolState = ResponseProtocolState.Complete;
					_responseCompletionState = StreamCompletionState.Completed;
					if (_requestCompletionState == StreamCompletionState.Completed && !ConnectProtocolEstablished)
					{
						Complete();
					}
				}
				hasWaiter = _hasWaiter;
				_hasWaiter = false;
			}
			if (hasWaiter)
			{
				_waitSource.SetResult(result: true);
			}
		}

		public void OnReset(Exception resetException, Http2ProtocolErrorCode? resetStreamErrorCode = null, bool canRetry = false)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"resetException"}={resetException}, {"resetStreamErrorCode"}={resetStreamErrorCode}", "OnReset");
			}
			bool flag = false;
			CancellationTokenSource cancellationTokenSource = null;
			lock (SyncObject)
			{
				if ((_requestCompletionState == StreamCompletionState.Completed && _responseCompletionState == StreamCompletionState.Completed) || _resetException != null)
				{
					return;
				}
				if (canRetry && _responseProtocolState != 0)
				{
					canRetry = false;
				}
				if (resetStreamErrorCode == Http2ProtocolErrorCode.NoError && _responseCompletionState == StreamCompletionState.Completed)
				{
					if (_requestCompletionState == StreamCompletionState.InProgress)
					{
						_requestBodyAbandoned = true;
						cancellationTokenSource = _requestBodyCancellationSource;
					}
				}
				else
				{
					_resetException = resetException;
					_canRetry = canRetry;
					flag = true;
				}
			}
			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
			}
			else
			{
				Cancel();
			}
		}

		private void CheckResponseBodyState()
		{
			Exception resetException = _resetException;
			if (resetException != null)
			{
				if (_canRetry)
				{
					ThrowRetry(System.SR.net_http_request_aborted, resetException);
				}
				ThrowRequestAborted(resetException);
			}
			if (_responseProtocolState == ResponseProtocolState.Aborted)
			{
				ThrowRequestAborted();
			}
		}

		private (bool wait, bool isEmptyResponse) TryEnsureHeaders()
		{
			lock (SyncObject)
			{
				if (!_responseHeadersReceived)
				{
					CheckResponseBodyState();
					_hasWaiter = true;
					_waitSource.Reset();
					return (wait: true, isEmptyResponse: false);
				}
				return (wait: false, isEmptyResponse: _responseProtocolState == ResponseProtocolState.Complete && _responseBuffer.IsEmpty);
			}
		}

		public async Task ReadResponseHeadersAsync(CancellationToken cancellationToken)
		{
			bool flag2;
			try
			{
				if (HttpTelemetry.Log.IsEnabled())
				{
					HttpTelemetry.Log.ResponseHeadersStart();
				}
				bool flag;
				(flag, flag2) = TryEnsureHeaders();
				if (flag)
				{
					await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					(bool wait, bool isEmptyResponse) tuple2 = TryEnsureHeaders();
					_ = tuple2.wait;
					flag2 = tuple2.isEmptyResponse;
				}
				if (HttpTelemetry.Log.IsEnabled())
				{
					HttpTelemetry.Log.ResponseHeadersStop((int)_response.StatusCode);
				}
			}
			catch
			{
				Cancel();
				throw;
			}
			HttpConnectionResponseContent httpConnectionResponseContent = (HttpConnectionResponseContent)_response.Content;
			if (ConnectProtocolEstablished)
			{
				httpConnectionResponseContent.SetStream(new Http2ReadWriteStream(this, closeResponseBodyOnDispose: true));
			}
			else if (flag2)
			{
				MoveTrailersToResponseMessage(_response);
				httpConnectionResponseContent.SetStream(EmptyReadStream.Instance);
			}
			else
			{
				httpConnectionResponseContent.SetStream(new Http2ReadStream(this));
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received response: {_response}", "ReadResponseHeadersAsync");
			}
			if (_connection._pool.Settings._useCookies)
			{
				CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer);
			}
		}

		private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer, bool partOfSyncRead = false)
		{
			lock (SyncObject)
			{
				CheckResponseBodyState();
				if (!_responseBuffer.IsEmpty)
				{
					System.Net.MultiMemory activeMemory = _responseBuffer.ActiveMemory;
					int num = Math.Min(buffer.Length, activeMemory.Length);
					activeMemory.Slice(0, num).CopyTo(buffer);
					_responseBuffer.Discard(num);
					return (wait: false, bytesRead: num);
				}
				if (_responseProtocolState == ResponseProtocolState.Complete)
				{
					return (wait: false, bytesRead: 0);
				}
				_hasWaiter = true;
				_waitSource.Reset();
				_waitSource.RunContinuationsAsynchronously = !partOfSyncRead;
				return (wait: true, bytesRead: 0);
			}
		}

		public int ReadData(Span<byte> buffer, HttpResponseMessage responseMessage)
		{
			int num;
			bool flag;
			(flag, num) = TryReadFromBuffer(buffer, partOfSyncRead: true);
			if (flag)
			{
				WaitForData();
				(flag, num) = TryReadFromBuffer(buffer, partOfSyncRead: true);
			}
			if (num != 0)
			{
				_windowManager.AdjustWindow(num, this);
			}
			else if (buffer.Length != 0)
			{
				MoveTrailersToResponseMessage(responseMessage);
			}
			return num;
		}

		public async ValueTask<int> ReadDataAsync(Memory<byte> buffer, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
		{
			var (flag, num) = TryReadFromBuffer(buffer.Span);
			if (flag)
			{
				await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				(bool wait, int bytesRead) tuple2 = TryReadFromBuffer(buffer.Span);
				_ = tuple2.wait;
				num = tuple2.bytesRead;
			}
			if (num != 0)
			{
				_windowManager.AdjustWindow(num, this);
			}
			else if (buffer.Length != 0)
			{
				MoveTrailersToResponseMessage(responseMessage);
			}
			return num;
		}

		public void CopyTo(HttpResponseMessage responseMessage, Stream destination, int bufferSize)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(bufferSize);
			try
			{
				while (true)
				{
					int num;
					bool flag;
					(flag, num) = TryReadFromBuffer(array, partOfSyncRead: true);
					if (flag)
					{
						WaitForData();
						(flag, num) = TryReadFromBuffer(array, partOfSyncRead: true);
					}
					if (num == 0)
					{
						break;
					}
					_windowManager.AdjustWindow(num, this);
					destination.Write(new ReadOnlySpan<byte>(array, 0, num));
				}
				MoveTrailersToResponseMessage(responseMessage);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}

		public async Task CopyToAsync(HttpResponseMessage responseMessage, Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
			try
			{
				while (true)
				{
					var (flag, num) = TryReadFromBuffer(buffer);
					if (flag)
					{
						await WaitForDataAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						(bool wait, int bytesRead) tuple2 = TryReadFromBuffer(buffer);
						_ = tuple2.wait;
						num = tuple2.bytesRead;
					}
					if (num == 0)
					{
						break;
					}
					_windowManager.AdjustWindow(num, this);
					await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				MoveTrailersToResponseMessage(responseMessage);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		private void MoveTrailersToResponseMessage(HttpResponseMessage responseMessage)
		{
			if (_trailers != null)
			{
				responseMessage.StoreReceivedTrailingHeaders(_trailers);
			}
		}

		private async ValueTask SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration linkedRegistration = ((cancellationToken.CanBeCanceled && cancellationToken != _requestBodyCancellationSource.Token) ? RegisterRequestBodyCancellation(cancellationToken) : default(CancellationTokenRegistration));
			try
			{
				while (buffer.Length > 0)
				{
					int num = -1;
					bool flush = false;
					lock (_creditSyncObject)
					{
						if (_availableCredit > 0)
						{
							num = Math.Min(buffer.Length, _availableCredit);
							_availableCredit -= num;
							if (_availableCredit == 0)
							{
								flush = true;
							}
						}
						else
						{
							if (_creditWaiter == null)
							{
								_creditWaiter = new CreditWaiter(_requestBodyCancellationSource.Token);
							}
							else
							{
								_creditWaiter.ResetForAwait(_requestBodyCancellationSource.Token);
							}
							_creditWaiter.Amount = buffer.Length;
						}
					}
					if (num == -1)
					{
						num = await _creditWaiter.AsValueTask().ConfigureAwait(continueOnCapturedContext: false);
						lock (_creditSyncObject)
						{
							if (_availableCredit == 0)
							{
								flush = true;
							}
						}
					}
					ReadOnlyMemory<byte> buffer2;
					(buffer2, buffer) = SplitBuffer(buffer, num);
					await _connection.SendStreamDataAsync(StreamId, buffer2, flush, _requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == _requestBodyCancellationSource.Token)
			{
				lock (SyncObject)
				{
					Exception resetException = _resetException;
					if (resetException != null)
					{
						if (_canRetry)
						{
							ThrowRetry(System.SR.net_http_request_aborted, resetException);
						}
						ThrowRequestAborted(resetException);
					}
				}
				throw;
			}
			finally
			{
				linkedRegistration.Dispose();
			}
		}

		private void CloseResponseBody()
		{
			if (ConnectProtocolEstablished && _resetException == null)
			{
				_connection.LogExceptions(_connection.SendEndStreamAsync(StreamId));
			}
			bool flag = false;
			lock (SyncObject)
			{
				if (_responseBuffer.IsEmpty && _responseProtocolState == ResponseProtocolState.Complete)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				Cancel();
			}
			else if (_sendRstOnResponseClose)
			{
				_connection.LogExceptions(_connection.SendRstStreamAsync(StreamId, Http2ProtocolErrorCode.Cancel));
			}
			lock (SyncObject)
			{
				if (ConnectProtocolEstablished)
				{
					Complete();
				}
				_responseBuffer.Dispose();
			}
		}

		private CancellationTokenRegistration RegisterRequestBodyCancellation(CancellationToken cancellationToken)
		{
			return cancellationToken.UnsafeRegister(delegate(object s)
			{
				((CancellationTokenSource)s).Cancel();
			}, _requestBodyCancellationSource);
		}

		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
		{
			return _waitSource.GetStatus(token);
		}

		void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_waitSource.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			_waitSourceCancellation.Dispose();
			_waitSourceCancellation = default(CancellationTokenRegistration);
			_waitSource.GetResult(token);
		}

		private void WaitForData()
		{
			new ValueTask(this, _waitSource.Version).AsTask().GetAwaiter().GetResult();
		}

		private ValueTask WaitForDataAsync(CancellationToken cancellationToken)
		{
			_waitSourceCancellation = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
			{
				Http2Stream http2Stream = (Http2Stream)s;
				bool hasWaiter;
				lock (http2Stream.SyncObject)
				{
					hasWaiter = http2Stream._hasWaiter;
					http2Stream._hasWaiter = false;
				}
				if (hasWaiter)
				{
					http2Stream._waitSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(CancellationHelper.CreateOperationCanceledException(null, cancellationToken)));
				}
			}, this);
			return new ValueTask(this, _waitSource.Version);
		}

		public void Trace(string message, [CallerMemberName] string memberName = null)
		{
			_connection.Trace(StreamId, message, memberName);
		}
	}

	private struct Http2StreamWindowManager
	{
		private int _deliveredBytes;

		private int _streamWindowSize;

		private long _lastWindowUpdate;

		private static double WindowScaleThresholdMultiplier => GlobalHttpSettings.SocketsHttpHandler.Http2StreamWindowScaleThresholdMultiplier;

		private static int MaxStreamWindowSize => GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize;

		private static bool WindowScalingEnabled => !GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing;

		internal int StreamWindowThreshold => _streamWindowSize / 8;

		internal int StreamWindowSize => _streamWindowSize;

		public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
		{
			HttpConnectionSettings settings = connection._pool.Settings;
			_streamWindowSize = settings._initialHttp2StreamWindowSize;
			_deliveredBytes = 0;
			_lastWindowUpdate = 0L;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				stream.Trace($"[FlowControl] InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, WindowScaleThresholdMultiplier: {WindowScaleThresholdMultiplier}", ".ctor");
			}
		}

		public void Start()
		{
			_lastWindowUpdate = Stopwatch.GetTimestamp();
		}

		public void AdjustWindow(int bytesConsumed, Http2Stream stream)
		{
			if (stream.ExpectResponseData)
			{
				if (WindowScalingEnabled)
				{
					AdjustWindowDynamic(bytesConsumed, stream);
				}
				else
				{
					AjdustWindowStatic(bytesConsumed, stream);
				}
			}
		}

		private void AjdustWindowStatic(int bytesConsumed, Http2Stream stream)
		{
			_deliveredBytes += bytesConsumed;
			if (_deliveredBytes >= StreamWindowThreshold)
			{
				int deliveredBytes = _deliveredBytes;
				_deliveredBytes = 0;
				Http2Connection connection = stream.Connection;
				Task task = connection.SendWindowUpdateAsync(stream.StreamId, deliveredBytes);
				connection.LogExceptions(task);
			}
		}

		private void AdjustWindowDynamic(int bytesConsumed, Http2Stream stream)
		{
			_deliveredBytes += bytesConsumed;
			if (_deliveredBytes < StreamWindowThreshold)
			{
				return;
			}
			int num = _deliveredBytes;
			long timestamp = Stopwatch.GetTimestamp();
			Http2Connection connection = stream.Connection;
			TimeSpan minRtt = connection._rttEstimator.MinRtt;
			if (minRtt > TimeSpan.Zero && _streamWindowSize < MaxStreamWindowSize)
			{
				TimeSpan elapsedTime = Stopwatch.GetElapsedTime(_lastWindowUpdate, timestamp);
				if ((double)_deliveredBytes * (double)minRtt.Ticks > (double)(_streamWindowSize * elapsedTime.Ticks) * WindowScaleThresholdMultiplier)
				{
					int num2 = Math.Min(MaxStreamWindowSize, _streamWindowSize * 2);
					num += num2 - _streamWindowSize;
					_streamWindowSize = num2;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						stream.Trace($"[FlowControl] Updated Stream Window. StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}", "AdjustWindowDynamic");
					}
					if (_streamWindowSize == MaxStreamWindowSize && System.Net.NetEventSource.Log.IsEnabled())
					{
						stream.Trace($"[FlowControl] StreamWindowSize reached the configured maximum of {MaxStreamWindowSize}.", "AdjustWindowDynamic");
					}
				}
			}
			_deliveredBytes = 0;
			Task task = connection.SendWindowUpdateAsync(stream.StreamId, num);
			connection.LogExceptions(task);
			_lastWindowUpdate = timestamp;
		}
	}

	private struct RttEstimator
	{
		private enum State
		{
			Disabled,
			Init,
			Waiting,
			PingSent,
			TerminatingMayReceivePingAck
		}

		private static readonly long PingIntervalInTicks = (long)(2.0 * (double)Stopwatch.Frequency);

		private State _state;

		private long _pingSentTimestamp;

		private long _pingCounter;

		private int _initialBurst;

		private long _minRtt;

		public TimeSpan MinRtt => new TimeSpan(_minRtt);

		public static RttEstimator Create()
		{
			RttEstimator result = default(RttEstimator);
			result._state = ((!GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing) ? State.Init : State.Disabled);
			result._initialBurst = 4;
			return result;
		}

		internal void OnInitialSettingsSent()
		{
			if (_state != 0)
			{
				_pingSentTimestamp = Stopwatch.GetTimestamp();
			}
		}

		internal void OnInitialSettingsAckReceived(Http2Connection connection)
		{
			if (_state != 0)
			{
				RefreshRtt(connection);
				_state = State.Waiting;
			}
		}

		internal void OnDataOrHeadersReceived(Http2Connection connection)
		{
			if (_state != State.Waiting)
			{
				return;
			}
			long timestamp = Stopwatch.GetTimestamp();
			bool flag = _initialBurst > 0;
			if (flag || timestamp - _pingSentTimestamp > PingIntervalInTicks)
			{
				if (flag)
				{
					_initialBurst--;
				}
				_pingCounter--;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Sending RTT PING with payload {_pingCounter}", "OnDataOrHeadersReceived");
				}
				connection.LogExceptions(connection.SendPingAsync(_pingCounter));
				_pingSentTimestamp = timestamp;
				_state = State.PingSent;
			}
		}

		internal void OnPingAckReceived(long payload, Http2Connection connection)
		{
			if (_state != State.PingSent && _state != State.TerminatingMayReceivePingAck)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Unexpected PING ACK in state {_state}", "OnPingAckReceived");
				}
				ThrowProtocolError();
			}
			if (_state == State.TerminatingMayReceivePingAck)
			{
				_state = State.Disabled;
				return;
			}
			if (_pingCounter != payload)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"[FlowControl] Unexpected RTT PING ACK payload {payload}, should be {_pingCounter}.", "OnPingAckReceived");
				}
				ThrowProtocolError();
			}
			RefreshRtt(connection);
			_state = State.Waiting;
		}

		internal void OnGoAwayReceived()
		{
			if (_state == State.PingSent)
			{
				_state = State.TerminatingMayReceivePingAck;
			}
			else
			{
				_state = State.Disabled;
			}
		}

		private void RefreshRtt(Http2Connection connection)
		{
			long val = ((_minRtt == 0L) ? long.MaxValue : _minRtt);
			long value = Math.Min(val, Stopwatch.GetElapsedTime(_pingSentTimestamp).Ticks);
			Interlocked.Exchange(ref _minRtt, value);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"[FlowControl] Updated MinRtt: {MinRtt.TotalMilliseconds} ms", "RefreshRtt");
			}
		}
	}

	private static readonly TaskCompletionSourceWithCancellation<bool> s_settingsReceivedSingleton = CreateSuccessfullyCompletedTcs();

	private TaskCompletionSourceWithCancellation<bool> _initialSettingsReceived;

	private readonly HttpConnectionPool _pool;

	private readonly Stream _stream;

	private System.Net.ArrayBuffer _incomingBuffer;

	private System.Net.ArrayBuffer _outgoingBuffer;

	[ThreadStatic]
	private static string[] t_headerValues;

	private readonly HPackDecoder _hpackDecoder;

	private readonly Dictionary<int, Http2Stream> _httpStreams;

	private readonly CreditManager _connectionWindow;

	private RttEstimator _rttEstimator;

	private int _nextStream;

	private bool _receivedSettingsAck;

	private int _initialServerStreamWindowSize;

	private int _pendingWindowUpdate;

	private uint _maxConcurrentStreams;

	private uint _streamsInUse;

	private TaskCompletionSource<bool> _availableStreamsWaiter;

	private readonly Channel<WriteQueueEntry> _writeChannel;

	private bool _lastPendingWriterShouldFlush;

	private uint _maxHeaderListSize = uint.MaxValue;

	private bool _shutdown;

	private Exception _abortException;

	private static readonly UnboundedChannelOptions s_channelOptions = new UnboundedChannelOptions
	{
		SingleReader = true
	};

	private readonly long _keepAlivePingDelay;

	private readonly long _keepAlivePingTimeout;

	private readonly HttpKeepAlivePingPolicy _keepAlivePingPolicy;

	private long _keepAlivePingPayload;

	private long _nextPingRequestTimestamp;

	private long _keepAlivePingTimeoutTimestamp;

	private volatile KeepAliveState _keepAliveState;

	private static ReadOnlySpan<byte> ProtocolLiteralHeaderBytes => new byte[11]
	{
		0, 9, 58, 112, 114, 111, 116, 111, 99, 111,
		108
	};

	private static ReadOnlySpan<byte> Http2ConnectionPreface => "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8;

	private object SyncObject => _httpStreams;

	internal TaskCompletionSourceWithCancellation<bool> InitialSettingsReceived => _initialSettingsReceived ?? Interlocked.CompareExchange(ref _initialSettingsReceived, new TaskCompletionSourceWithCancellation<bool>(), null) ?? _initialSettingsReceived;

	internal bool IsConnectEnabled { get; private set; }

	public Http2Connection(HttpConnectionPool pool, Stream stream, IPEndPoint remoteEndPoint)
		: base(pool, remoteEndPoint)
	{
		_pool = pool;
		_stream = stream;
		_incomingBuffer = new System.Net.ArrayBuffer(0, usePool: true);
		_outgoingBuffer = new System.Net.ArrayBuffer(0, usePool: true);
		_hpackDecoder = new HPackDecoder(4096, pool.Settings.MaxResponseHeadersByteLength);
		_httpStreams = new Dictionary<int, Http2Stream>();
		_connectionWindow = new CreditManager(this, "_connectionWindow", 65535);
		_rttEstimator = RttEstimator.Create();
		_writeChannel = Channel.CreateUnbounded<WriteQueueEntry>(s_channelOptions);
		_nextStream = 1;
		_initialServerStreamWindowSize = 65535;
		_maxConcurrentStreams = 100u;
		_streamsInUse = 0u;
		_pendingWindowUpdate = 0;
		_keepAlivePingDelay = TimeSpanToMs(_pool.Settings._keepAlivePingDelay);
		_keepAlivePingTimeout = TimeSpanToMs(_pool.Settings._keepAlivePingTimeout);
		_nextPingRequestTimestamp = Environment.TickCount64 + _keepAlivePingDelay;
		_keepAlivePingPolicy = _pool.Settings._keepAlivePingPolicy;
		uint lastSeenHttp2MaxHeaderListSize = _pool._lastSeenHttp2MaxHeaderListSize;
		if (lastSeenHttp2MaxHeaderListSize != 0)
		{
			_maxHeaderListSize = lastSeenHttp2MaxHeaderListSize;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			TraceConnection(_stream);
		}
		static long TimeSpanToMs(TimeSpan value)
		{
			double totalMilliseconds = value.TotalMilliseconds;
			return (long)((totalMilliseconds > 2147483647.0) ? 2147483647.0 : totalMilliseconds);
		}
	}

	~Http2Connection()
	{
		Dispose();
	}

	public async ValueTask SetupAsync(CancellationToken cancellationToken)
	{
		try
		{
			_outgoingBuffer.EnsureAvailableSpace(Http2ConnectionPreface.Length + 9 + 6 + 9 + 4);
			Http2ConnectionPreface.CopyTo(_outgoingBuffer.AvailableSpan);
			_outgoingBuffer.Commit(Http2ConnectionPreface.Length);
			FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, 12, FrameType.Settings, FrameFlags.None, 0);
			_outgoingBuffer.Commit(9);
			BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, 2);
			_outgoingBuffer.Commit(2);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, 0u);
			_outgoingBuffer.Commit(4);
			BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, 4);
			_outgoingBuffer.Commit(2);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, (uint)_pool.Settings._initialHttp2StreamWindowSize);
			_outgoingBuffer.Commit(4);
			uint value = 67043329u;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Initial connection-level WINDOW_UPDATE, windowUpdateAmount={value}", "SetupAsync");
			}
			FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, 4, FrameType.WindowUpdate, FrameFlags.None, 0);
			_outgoingBuffer.Commit(9);
			BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, value);
			_outgoingBuffer.Commit(4);
			ProcessIncomingFramesAsync();
			await _stream.WriteAsync(_outgoingBuffer.ActiveMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_rttEstimator.OnInitialSettingsSent();
			_outgoingBuffer.ClearAndReturnBuffer();
		}
		catch (Exception ex)
		{
			_outgoingBuffer.Dispose();
			Dispose();
			if (ex is OperationCanceledException ex2 && ex2.CancellationToken == cancellationToken)
			{
				throw;
			}
			throw new IOException(System.SR.net_http_http2_connection_not_established, ex);
		}
		ProcessOutgoingFramesAsync();
	}

	private void Shutdown()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"_shutdown"}={_shutdown}, {"_abortException"}={_abortException}", "Shutdown");
		}
		if (!_shutdown)
		{
			_shutdown = true;
			_pool.InvalidateHttp2Connection(this);
			SignalAvailableStreamsWaiter(result: false);
			if (_streamsInUse == 0)
			{
				FinalTeardown();
			}
		}
	}

	public bool TryReserveStream()
	{
		lock (SyncObject)
		{
			if (_shutdown)
			{
				return false;
			}
			if (_streamsInUse < _maxConcurrentStreams)
			{
				if (_streamsInUse == 0)
				{
					MarkConnectionAsNotIdle();
				}
				_streamsInUse++;
				return true;
			}
		}
		return false;
	}

	public void ReleaseStream()
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"_streamsInUse"}={_streamsInUse}", "ReleaseStream");
			}
			_streamsInUse--;
			if (_streamsInUse < _maxConcurrentStreams)
			{
				SignalAvailableStreamsWaiter(result: true);
			}
			if (_streamsInUse == 0)
			{
				MarkConnectionAsIdle();
				if (_shutdown)
				{
					FinalTeardown();
				}
			}
		}
	}

	public Task<bool> WaitForAvailableStreamsAsync()
	{
		lock (SyncObject)
		{
			if (_shutdown)
			{
				return Task.FromResult(result: false);
			}
			if (_streamsInUse < _maxConcurrentStreams)
			{
				return Task.FromResult(result: true);
			}
			_availableStreamsWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			return _availableStreamsWaiter.Task;
		}
	}

	private void SignalAvailableStreamsWaiter(bool result)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"result"}={result}, {"_availableStreamsWaiter"}?={_availableStreamsWaiter != null}", "SignalAvailableStreamsWaiter");
		}
		if (_availableStreamsWaiter != null)
		{
			_availableStreamsWaiter.SetResult(result);
			_availableStreamsWaiter = null;
		}
	}

	private async Task FlushOutgoingBytesAsync()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"ActiveLength"}={_outgoingBuffer.ActiveLength}", "FlushOutgoingBytesAsync");
		}
		if (_outgoingBuffer.ActiveLength > 0)
		{
			try
			{
				await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception abortException)
			{
				Abort(abortException);
			}
			_lastPendingWriterShouldFlush = false;
			_outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
		}
	}

	private async ValueTask<FrameHeader> ReadFrameAsync(bool initialFrame = false)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"initialFrame"}={initialFrame}", "ReadFrameAsync");
		}
		if (_incomingBuffer.ActiveLength < 9)
		{
			do
			{
				await _stream.ReadAsync(Memory<byte>.Empty).ConfigureAwait(continueOnCapturedContext: false);
				_incomingBuffer.EnsureAvailableSpace(9);
				int num = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
				_incomingBuffer.Commit(num);
				if (num == 0)
				{
					if (_incomingBuffer.ActiveLength == 0)
					{
						ThrowMissingFrame();
					}
					else
					{
						ThrowPrematureEOF(9);
					}
				}
			}
			while (_incomingBuffer.ActiveLength < 9);
		}
		FrameHeader frameHeader = FrameHeader.ReadFrom(_incomingBuffer.ActiveSpan);
		if (frameHeader.PayloadLength > 16384)
		{
			if (initialFrame && System.Net.NetEventSource.Log.IsEnabled())
			{
				string @string = Encoding.ASCII.GetString(_incomingBuffer.ActiveSpan.Slice(0, Math.Min(20, _incomingBuffer.ActiveLength)));
				Trace("HTTP/2 handshake failed. Server returned " + @string, "ReadFrameAsync");
			}
			_incomingBuffer.Discard(9);
			ThrowProtocolError(initialFrame ? Http2ProtocolErrorCode.ProtocolError : Http2ProtocolErrorCode.FrameSizeError);
		}
		_incomingBuffer.Discard(9);
		if (_incomingBuffer.ActiveLength < frameHeader.PayloadLength)
		{
			_incomingBuffer.EnsureAvailableSpace(frameHeader.PayloadLength - _incomingBuffer.ActiveLength);
			do
			{
				await _stream.ReadAsync(Memory<byte>.Empty).ConfigureAwait(continueOnCapturedContext: false);
				int num2 = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
				_incomingBuffer.Commit(num2);
				if (num2 == 0)
				{
					ThrowPrematureEOF(frameHeader.PayloadLength);
				}
			}
			while (_incomingBuffer.ActiveLength < frameHeader.PayloadLength);
		}
		return frameHeader;
		static void ThrowMissingFrame()
		{
			throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.net_http_invalid_response_missing_frame);
		}
		void ThrowPrematureEOF(int requiredBytes)
		{
			throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, requiredBytes - _incomingBuffer.ActiveLength));
		}
	}

	private async Task ProcessIncomingFramesAsync()
	{
		_ = 4;
		try
		{
			try
			{
				FrameHeader frameHeader = await ReadFrameAsync(initialFrame: true).ConfigureAwait(continueOnCapturedContext: false);
				if (frameHeader.Type != FrameType.Settings || frameHeader.AckFlag)
				{
					if (frameHeader.Type == FrameType.GoAway)
					{
						Http2ProtocolErrorCode item = ReadGoAwayFrame(frameHeader).errorCode;
						ThrowProtocolError(item, System.SR.net_http_http2_connection_close);
					}
					else
					{
						ThrowProtocolError();
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Frame 0: {frameHeader}.", "ProcessIncomingFramesAsync");
				}
				ProcessSettingsFrame(frameHeader, initialFrame: true);
			}
			catch (HttpProtocolException exception)
			{
				InitialSettingsReceived.TrySetException(exception);
				throw;
			}
			catch (Exception innerException)
			{
				InitialSettingsReceived.TrySetException(new HttpIOException(HttpRequestError.InvalidResponse, System.SR.net_http_http2_connection_not_established, innerException));
				throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.net_http_http2_connection_not_established, innerException);
			}
			long frameNum = 1L;
			while (true)
			{
				if (_incomingBuffer.ActiveLength < 9)
				{
					int num;
					do
					{
						ValueTask<int> valueTask = _stream.ReadAsync(Memory<byte>.Empty);
						if (!valueTask.IsCompletedSuccessfully && _incomingBuffer.ActiveLength == 0)
						{
							_incomingBuffer.ClearAndReturnBuffer();
						}
						await valueTask.ConfigureAwait(continueOnCapturedContext: false);
						_incomingBuffer.EnsureAvailableSpace(16393);
						num = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
						_incomingBuffer.Commit(num);
					}
					while (num != 0 && _incomingBuffer.ActiveLength < 9);
				}
				FrameHeader frameHeader = await ReadFrameAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Frame {frameNum}: {frameHeader}.", "ProcessIncomingFramesAsync");
				}
				RefreshPingTimestamp();
				switch (frameHeader.Type)
				{
				case FrameType.Headers:
					await ProcessHeadersFrame(frameHeader).ConfigureAwait(continueOnCapturedContext: false);
					break;
				case FrameType.Data:
					ProcessDataFrame(frameHeader);
					break;
				case FrameType.Settings:
					ProcessSettingsFrame(frameHeader);
					break;
				case FrameType.Priority:
					ProcessPriorityFrame(frameHeader);
					break;
				case FrameType.Ping:
					ProcessPingFrame(frameHeader);
					break;
				case FrameType.WindowUpdate:
					ProcessWindowUpdateFrame(frameHeader);
					break;
				case FrameType.RstStream:
					ProcessRstStreamFrame(frameHeader);
					break;
				case FrameType.GoAway:
					ProcessGoAwayFrame(frameHeader);
					break;
				case FrameType.AltSvc:
					ProcessAltSvcFrame(frameHeader);
					break;
				default:
					ThrowProtocolError();
					break;
				}
				frameNum++;
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("ProcessIncomingFramesAsync: " + ex.Message, "ProcessIncomingFramesAsync");
			}
			Abort(ex);
		}
		finally
		{
			_incomingBuffer.Dispose();
		}
	}

	private Http2Stream GetStream(int streamId)
	{
		if (streamId <= 0 || streamId >= _nextStream)
		{
			ThrowProtocolError();
		}
		lock (SyncObject)
		{
			if (!_httpStreams.TryGetValue(streamId, out var value))
			{
				return null;
			}
			return value;
		}
	}

	private async ValueTask ProcessHeadersFrame(FrameHeader frameHeader)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}", "ProcessHeadersFrame");
		}
		bool endStream = frameHeader.EndStreamFlag;
		int streamId = frameHeader.StreamId;
		Http2Stream http2Stream = GetStream(streamId);
		IHttpStreamHeadersHandler headersHandler;
		if (http2Stream != null)
		{
			http2Stream.OnHeadersStart();
			_rttEstimator.OnDataOrHeadersReceived(this);
			headersHandler = http2Stream;
		}
		else
		{
			headersHandler = NopHeadersHandler.Instance;
		}
		_hpackDecoder.Decode(GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.PaddedFlag, frameHeader.PriorityFlag), frameHeader.EndHeadersFlag, headersHandler);
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		while (!frameHeader.EndHeadersFlag)
		{
			frameHeader = await ReadFrameAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (frameHeader.Type != FrameType.Continuation || frameHeader.StreamId != streamId)
			{
				ThrowProtocolError();
			}
			_hpackDecoder.Decode(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.EndHeadersFlag, headersHandler);
			_incomingBuffer.Discard(frameHeader.PayloadLength);
		}
		_hpackDecoder.CompleteDecode();
		http2Stream?.OnHeadersComplete(endStream);
	}

	private static ReadOnlySpan<byte> GetFrameData(ReadOnlySpan<byte> frameData, bool hasPad, bool hasPriority)
	{
		if (hasPad)
		{
			if (frameData.Length == 0)
			{
				ThrowProtocolError();
			}
			int num = frameData[0];
			frameData = frameData.Slice(1);
			if (frameData.Length < num)
			{
				ThrowProtocolError();
			}
			frameData = frameData.Slice(0, frameData.Length - num);
		}
		if (hasPriority)
		{
			if (frameData.Length < 5)
			{
				ThrowProtocolError();
			}
			frameData = frameData.Slice(5);
		}
		return frameData;
	}

	private void ProcessAltSvcFrame(FrameHeader frameHeader)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}", "ProcessAltSvcFrame");
		}
		ReadOnlySpan<byte> readOnlySpan = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);
		if (BinaryPrimitives.TryReadUInt16BigEndian(readOnlySpan, out var value))
		{
			readOnlySpan = readOnlySpan.Slice(2);
			if ((frameHeader.StreamId != 0 && value == 0) || (frameHeader.StreamId == 0 && readOnlySpan.Length >= value && readOnlySpan.Slice(0, value).SequenceEqual(_pool.Http2AltSvcOriginUri)))
			{
				readOnlySpan = readOnlySpan.Slice(value);
				string @string = Encoding.ASCII.GetString(readOnlySpan);
				_pool.HandleAltSvc(new string[1] { @string }, null);
			}
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessDataFrame(FrameHeader frameHeader)
	{
		Http2Stream stream = GetStream(frameHeader.StreamId);
		ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.PaddedFlag, hasPriority: false);
		if (stream != null)
		{
			bool endStreamFlag = frameHeader.EndStreamFlag;
			if (frameData.Length > 0 || endStreamFlag)
			{
				stream.OnResponseData(frameData, endStreamFlag);
			}
			if (!endStreamFlag && frameData.Length > 0)
			{
				_rttEstimator.OnDataOrHeadersReceived(this);
			}
		}
		if (frameData.Length > 0)
		{
			ExtendWindow(frameData.Length);
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessSettingsFrame(FrameHeader frameHeader, bool initialFrame = false)
	{
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		if (frameHeader.AckFlag)
		{
			if (frameHeader.PayloadLength != 0)
			{
				ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
			}
			if (_receivedSettingsAck)
			{
				ThrowProtocolError();
			}
			_receivedSettingsAck = true;
			_rttEstimator.OnInitialSettingsAckReceived(this);
			return;
		}
		if (frameHeader.PayloadLength % 6 != 0)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		ReadOnlySpan<byte> source = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);
		bool flag = false;
		while (source.Length > 0)
		{
			ushort num = BinaryPrimitives.ReadUInt16BigEndian(source);
			source = source.Slice(2);
			uint num2 = BinaryPrimitives.ReadUInt32BigEndian(source);
			source = source.Slice(4);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Applying setting {num}={num2}", "ProcessSettingsFrame");
			}
			switch ((SettingId)num)
			{
			case SettingId.MaxConcurrentStreams:
				ChangeMaxConcurrentStreams(num2);
				flag = true;
				break;
			case SettingId.InitialWindowSize:
				if (num2 > int.MaxValue)
				{
					ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
				}
				ChangeInitialWindowSize((int)num2);
				break;
			case SettingId.MaxFrameSize:
				if (num2 < 16384 || num2 > 16777215)
				{
					ThrowProtocolError();
				}
				break;
			case SettingId.EnableConnect:
				switch (num2)
				{
				case 1u:
					IsConnectEnabled = true;
					break;
				case 0u:
					if (IsConnectEnabled)
					{
						ThrowProtocolError();
					}
					break;
				}
				break;
			case SettingId.MaxHeaderListSize:
				_maxHeaderListSize = num2;
				_pool._lastSeenHttp2MaxHeaderListSize = _maxHeaderListSize;
				break;
			}
		}
		if (initialFrame)
		{
			if (!flag)
			{
				ChangeMaxConcurrentStreams(2147483647u);
			}
			if (_initialSettingsReceived == null)
			{
				Interlocked.CompareExchange(ref _initialSettingsReceived, s_settingsReceivedSingleton, null);
			}
			InitialSettingsReceived.TrySetResult(result: true);
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		LogExceptions(SendSettingsAckAsync());
	}

	private void ChangeMaxConcurrentStreams(uint newValue)
	{
		lock (SyncObject)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"{"newValue"}={newValue}, {"_streamsInUse"}={_streamsInUse}, {"_availableStreamsWaiter"}?={_availableStreamsWaiter != null}", "ChangeMaxConcurrentStreams");
			}
			_maxConcurrentStreams = newValue;
			if (_streamsInUse < _maxConcurrentStreams)
			{
				SignalAvailableStreamsWaiter(result: true);
			}
		}
	}

	private void ChangeInitialWindowSize(int newSize)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"newSize"}={newSize}", "ChangeInitialWindowSize");
		}
		lock (SyncObject)
		{
			int amount = newSize - _initialServerStreamWindowSize;
			_initialServerStreamWindowSize = newSize;
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				httpStream.Value.OnWindowUpdate(amount);
			}
		}
	}

	private void ProcessPriorityFrame(FrameHeader frameHeader)
	{
		if (frameHeader.StreamId == 0 || frameHeader.PayloadLength != 5)
		{
			ThrowProtocolError();
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessPingFrame(FrameHeader frameHeader)
	{
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		if (frameHeader.PayloadLength != 8)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		ReadOnlySpan<byte> source = _incomingBuffer.ActiveSpan.Slice(0, 8);
		long num = BinaryPrimitives.ReadInt64BigEndian(source);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received PING frame, content:{num} ack: {frameHeader.AckFlag}", "ProcessPingFrame");
		}
		if (frameHeader.AckFlag)
		{
			ProcessPingAck(num);
		}
		else
		{
			LogExceptions(SendPingAsync(num, isAck: true));
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
	}

	private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength != 4)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		int num = BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{frameHeader}. {"amount"}={num}", "ProcessWindowUpdateFrame");
		}
		if (num == 0)
		{
			ThrowProtocolError();
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		if (frameHeader.StreamId == 0)
		{
			_connectionWindow.AdjustCredit(num);
		}
		else
		{
			GetStream(frameHeader.StreamId)?.OnWindowUpdate(num);
		}
	}

	private void ProcessRstStreamFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength != 4)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		if (frameHeader.StreamId == 0)
		{
			ThrowProtocolError();
		}
		Http2Stream stream = GetStream(frameHeader.StreamId);
		if (stream == null)
		{
			_incomingBuffer.Discard(frameHeader.PayloadLength);
			return;
		}
		Http2ProtocolErrorCode http2ProtocolErrorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(frameHeader.StreamId, $"{"protocolError"}={http2ProtocolErrorCode}", "ProcessRstStreamFrame");
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		bool canRetry = http2ProtocolErrorCode == Http2ProtocolErrorCode.RefusedStream;
		stream.OnReset(HttpProtocolException.CreateHttp2StreamException(http2ProtocolErrorCode), http2ProtocolErrorCode, canRetry);
	}

	private void ProcessGoAwayFrame(FrameHeader frameHeader)
	{
		(int lastStreamId, Http2ProtocolErrorCode errorCode) tuple = ReadGoAwayFrame(frameHeader);
		int item = tuple.lastStreamId;
		Http2ProtocolErrorCode item2 = tuple.errorCode;
		Exception resetException = HttpProtocolException.CreateHttp2ConnectionException(item2, System.SR.net_http_http2_connection_close);
		_rttEstimator.OnGoAwayReceived();
		List<Http2Stream> list = new List<Http2Stream>();
		lock (SyncObject)
		{
			Shutdown();
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				int key = httpStream.Key;
				if (key > item)
				{
					list.Add(httpStream.Value);
				}
			}
		}
		foreach (Http2Stream item3 in list)
		{
			item3.OnReset(resetException, null, canRetry: true);
		}
	}

	private (int lastStreamId, Http2ProtocolErrorCode errorCode) ReadGoAwayFrame(FrameHeader frameHeader)
	{
		if (frameHeader.PayloadLength < 8)
		{
			ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
		}
		if (frameHeader.StreamId != 0)
		{
			ThrowProtocolError();
		}
		int num = (int)(BinaryPrimitives.ReadUInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF);
		Http2ProtocolErrorCode http2ProtocolErrorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan.Slice(4));
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(frameHeader.StreamId, $"{"lastStreamId"}={num}, {"errorCode"}={http2ProtocolErrorCode}", "ReadGoAwayFrame");
		}
		_incomingBuffer.Discard(frameHeader.PayloadLength);
		return (lastStreamId: num, errorCode: http2ProtocolErrorCode);
	}

	internal Task FlushAsync(CancellationToken cancellationToken)
	{
		return PerformWriteAsync(0, 0, (int _, Memory<byte> __) => true, cancellationToken);
	}

	private Task PerformWriteAsync<T>(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken = default(CancellationToken))
	{
		WriteQueueEntry writeQueueEntry = new WriteQueueEntry<T>(writeBytes, state, writeAction, cancellationToken);
		if (!_writeChannel.Writer.TryWrite(writeQueueEntry))
		{
			if (_abortException != null)
			{
				return Task.FromException(GetRequestAbortedException(_abortException));
			}
			return Task.FromException(new ObjectDisposedException("Http2Connection"));
		}
		return writeQueueEntry.Task;
	}

	private async Task ProcessOutgoingFramesAsync()
	{
		_ = 2;
		try
		{
			while (await _writeChannel.Reader.WaitToReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				WriteQueueEntry writeEntry;
				while (_writeChannel.Reader.TryRead(out writeEntry))
				{
					if (_abortException != null)
					{
						if (writeEntry.TryDisableCancellation())
						{
							writeEntry.SetException(_abortException);
						}
						continue;
					}
					int writeBytes = writeEntry.WriteBytes;
					int capacity = _outgoingBuffer.Capacity;
					if (capacity >= 32768)
					{
						int activeLength = _outgoingBuffer.ActiveLength;
						if (writeBytes >= capacity - activeLength)
						{
							await FlushOutgoingBytesAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					if (!writeEntry.TryDisableCancellation())
					{
						continue;
					}
					_outgoingBuffer.EnsureAvailableSpace(writeBytes);
					try
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace($"{"writeBytes"}={writeBytes}", "ProcessOutgoingFramesAsync");
						}
						bool flag = writeEntry.InvokeWriteAction(_outgoingBuffer.AvailableMemorySliced(writeBytes));
						writeEntry.SetResult();
						_outgoingBuffer.Commit(writeBytes);
						_lastPendingWriterShouldFlush |= flag;
					}
					catch (Exception exception)
					{
						writeEntry.SetException(exception);
					}
				}
				if (_lastPendingWriterShouldFlush)
				{
					await FlushOutgoingBytesAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				if (_outgoingBuffer.ActiveLength == 0)
				{
					_outgoingBuffer.ClearAndReturnBuffer();
				}
			}
		}
		catch (Exception value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Unexpected exception in {"ProcessOutgoingFramesAsync"}: {value}", "ProcessOutgoingFramesAsync");
			}
		}
		finally
		{
			_outgoingBuffer.Dispose();
		}
	}

	private Task SendSettingsAckAsync()
	{
		return PerformWriteAsync(9, this, delegate(Http2Connection thisRef, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				thisRef.Trace("Started writing.", "SendSettingsAckAsync");
			}
			FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Settings, FrameFlags.EndStream, 0);
			return true;
		});
	}

	private Task SendPingAsync(long pingContent, bool isAck = false)
	{
		return PerformWriteAsync(17, (this, pingContent, isAck), delegate((Http2Connection thisRef, long pingContent, bool isAck) state, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				state.thisRef.Trace($"Started writing. {"pingContent"}={state.pingContent}", "SendPingAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 8, FrameType.Ping, state.isAck ? FrameFlags.EndStream : FrameFlags.None, 0);
			BinaryPrimitives.WriteInt64BigEndian(span.Slice(9), state.pingContent);
			return true;
		});
	}

	private Task SendRstStreamAsync(int streamId, Http2ProtocolErrorCode errorCode)
	{
		return PerformWriteAsync(13, (this, streamId, errorCode), delegate((Http2Connection thisRef, int streamId, Http2ProtocolErrorCode errorCode) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, $"Started writing. {"errorCode"}={s.errorCode}", "SendRstStreamAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 4, FrameType.RstStream, FrameFlags.None, s.streamId);
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(9), (int)s.errorCode);
			return true;
		});
	}

	internal void HeartBeat()
	{
		if (_shutdown)
		{
			return;
		}
		try
		{
			VerifyKeepAlive();
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("HeartBeat: " + ex.Message, "HeartBeat");
			}
			Abort(ex);
		}
	}

	private static (ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) SplitBuffer(ReadOnlyMemory<byte> buffer, int maxSize)
	{
		if (buffer.Length <= maxSize)
		{
			return (first: buffer, rest: Memory<byte>.Empty);
		}
		return (first: buffer.Slice(0, maxSize), rest: buffer.Slice(maxSize));
	}

	private void WriteIndexedHeader(int index, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"index"}={index}", "WriteIndexedHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeIndexedHeaderField(index, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.Grow();
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteIndexedHeader(int index, string value, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"index"}={index}, {"value"}={value}", "WriteIndexedHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, null, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.Grow();
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeader(string name, ReadOnlySpan<string> values, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"name"}={name}, {"values"}={string.Join(", ", values.ToArray())}", "WriteLiteralHeader");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, ", ", valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.Grow();
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeaderValues(ReadOnlySpan<string> values, string separator, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("values=" + string.Join(separator, values.ToArray()), "WriteLiteralHeaderValues");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeStringLiterals(values, separator, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.Grow();
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteLiteralHeaderValue(string value, Encoding valueEncoding, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("value=" + value, "WriteLiteralHeaderValue");
		}
		int bytesWritten;
		while (!HPackEncoder.EncodeStringLiteral(value, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
		{
			headerBuffer.Grow();
		}
		headerBuffer.Commit(bytesWritten);
	}

	private void WriteBytes(ReadOnlySpan<byte> bytes, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"Length"}={bytes.Length}", "WriteBytes");
		}
		headerBuffer.EnsureAvailableSpace(bytes.Length);
		bytes.CopyTo(headerBuffer.AvailableSpan);
		headerBuffer.Commit(bytes.Length);
	}

	private int WriteHeaderCollection(HttpRequestMessage request, HttpHeaders headers, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "WriteHeaderCollection");
		}
		HeaderEncodingSelector<HttpRequestMessage> requestHeaderEncodingSelector = _pool.Settings._requestHeaderEncodingSelector;
		ReadOnlySpan<HeaderEntry> entries = headers.GetEntries();
		int num = entries.Length * 32;
		ReadOnlySpan<HeaderEntry> readOnlySpan = entries;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			HeaderEntry headerEntry = readOnlySpan[i];
			int storeValuesIntoStringArray = HttpHeaders.GetStoreValuesIntoStringArray(headerEntry.Key, headerEntry.Value, ref t_headerValues);
			ReadOnlySpan<string> readOnlySpan2 = t_headerValues.AsSpan(0, storeValuesIntoStringArray);
			Encoding valueEncoding = requestHeaderEncodingSelector?.Invoke(headerEntry.Key.Name, request);
			KnownHeader knownHeader = headerEntry.Key.KnownHeader;
			if (knownHeader != null)
			{
				if (knownHeader == KnownHeaders.Host || knownHeader == KnownHeaders.Connection || knownHeader == KnownHeaders.Upgrade || knownHeader == KnownHeaders.ProxyConnection)
				{
					continue;
				}
				num += knownHeader.Name.Length;
				if (knownHeader == KnownHeaders.TE)
				{
					ReadOnlySpan<string> readOnlySpan3 = readOnlySpan2;
					for (int j = 0; j < readOnlySpan3.Length; j++)
					{
						string text = readOnlySpan3[j];
						if (string.Equals(text, "trailers", StringComparison.OrdinalIgnoreCase))
						{
							WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
							WriteLiteralHeaderValue(text, valueEncoding, ref headerBuffer);
							break;
						}
					}
				}
				else if (knownHeader != KnownHeaders.ContentLength || !request.IsExtendedConnectRequest)
				{
					WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
					string separator = null;
					if (readOnlySpan2.Length > 1)
					{
						HttpHeaderParser parser = headerEntry.Key.Parser;
						separator = ((parser == null || !parser.SupportsMultipleValues) ? ", " : parser.Separator);
					}
					WriteLiteralHeaderValues(readOnlySpan2, separator, valueEncoding, ref headerBuffer);
				}
			}
			else
			{
				WriteLiteralHeader(headerEntry.Key.Name, readOnlySpan2, valueEncoding, ref headerBuffer);
			}
		}
		return num;
	}

	private void WriteHeaders(HttpRequestMessage request, ref System.Net.ArrayBuffer headerBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "WriteHeaders");
		}
		if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
		{
			request.Headers.TransferEncodingChunked = false;
		}
		HttpMethod httpMethod = HttpMethod.Normalize(request.Method);
		if ((object)httpMethod == HttpMethod.Get)
		{
			WriteIndexedHeader(2, ref headerBuffer);
		}
		else if ((object)httpMethod == HttpMethod.Post)
		{
			WriteIndexedHeader(3, ref headerBuffer);
		}
		else
		{
			WriteIndexedHeader(2, httpMethod.Method, ref headerBuffer);
		}
		WriteIndexedHeader(_pool.IsSecure ? 7 : 6, ref headerBuffer);
		if (request.HasHeaders)
		{
			string host = request.Headers.Host;
			if (host != null)
			{
				WriteIndexedHeader(1, host, ref headerBuffer);
				goto IL_00e9;
			}
		}
		WriteBytes(_pool._http2EncodedAuthorityHostHeader, ref headerBuffer);
		goto IL_00e9;
		IL_00e9:
		string pathAndQuery = request.RequestUri.PathAndQuery;
		if (pathAndQuery == "/")
		{
			WriteIndexedHeader(4, ref headerBuffer);
		}
		else
		{
			WriteIndexedHeader(4, pathAndQuery, ref headerBuffer);
		}
		int num = 96;
		if (request.HasHeaders)
		{
			string protocol = request.Headers.Protocol;
			if (protocol != null)
			{
				WriteBytes(ProtocolLiteralHeaderBytes, ref headerBuffer);
				Encoding valueEncoding = _pool.Settings._requestHeaderEncodingSelector?.Invoke(":protocol", request);
				WriteLiteralHeaderValue(protocol, valueEncoding, ref headerBuffer);
				num += 32;
			}
			num += WriteHeaderCollection(request, request.Headers, ref headerBuffer);
		}
		if (_pool.Settings._useCookies)
		{
			string cookieHeader = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
			if (cookieHeader != string.Empty)
			{
				WriteBytes(KnownHeaders.Cookie.Http2EncodedName, ref headerBuffer);
				Encoding valueEncoding2 = _pool.Settings._requestHeaderEncodingSelector?.Invoke(KnownHeaders.Cookie.Name, request);
				WriteLiteralHeaderValue(cookieHeader, valueEncoding2, ref headerBuffer);
				num += "Cookie".Length + 32;
			}
		}
		if (request.Content == null)
		{
			if (httpMethod.MustHaveRequestBody)
			{
				WriteBytes(KnownHeaders.ContentLength.Http2EncodedName, ref headerBuffer);
				WriteLiteralHeaderValue("0", null, ref headerBuffer);
				num += "Content-Length".Length + 32;
			}
		}
		else
		{
			num += WriteHeaderCollection(request, request.Content.Headers, ref headerBuffer);
		}
		num += headerBuffer.ActiveLength;
		uint maxHeaderListSize = _maxHeaderListSize;
		if ((uint)num > maxHeaderListSize)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_request_headers_exceeded_length, maxHeaderListSize));
		}
	}

	private void AddStream(Http2Stream http2Stream)
	{
		lock (SyncObject)
		{
			if (_nextStream == int.MaxValue)
			{
				Shutdown();
			}
			if (_abortException != null)
			{
				throw GetRequestAbortedException(_abortException);
			}
			if (_shutdown)
			{
				ThrowRetry(System.SR.net_http_server_shutdown);
			}
			if (_streamsInUse > _maxConcurrentStreams)
			{
				ThrowRetry(System.SR.net_http_request_aborted);
			}
			http2Stream.Initialize(_nextStream, _initialServerStreamWindowSize);
			_nextStream += 2;
			_httpStreams.Add(http2Stream.StreamId, http2Stream);
		}
	}

	private async ValueTask<Http2Stream> SendHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool mustFlush)
	{
		System.Net.ArrayBuffer headerBuffer = default(System.Net.ArrayBuffer);
		try
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStart(base.Id);
			}
			headerBuffer = new System.Net.ArrayBuffer(16393, usePool: true);
			WriteHeaders(request, ref headerBuffer);
			ReadOnlyMemory<byte> item = headerBuffer.ActiveMemory;
			int num = (item.Length - 1) / 16384 + 1;
			int writeBytes = item.Length + num * 9;
			Http2Stream http2Stream = new Http2Stream(request, this);
			await PerformWriteAsync(writeBytes, (this, http2Stream, item, request.Content == null && !request.IsExtendedConnectRequest, mustFlush), delegate((Http2Connection thisRef, Http2Stream http2Stream, ReadOnlyMemory<byte> headerBytes, bool endStream, bool mustFlush) s, Memory<byte> writeBuffer)
			{
				s.thisRef.AddStream(s.http2Stream);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					s.thisRef.Trace(s.http2Stream.StreamId, $"Started writing. Total header bytes={s.headerBytes.Length}", "SendHeadersAsync");
				}
				Span<byte> destination = writeBuffer.Span;
				(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) tuple = SplitBuffer(s.headerBytes, 16384);
				ReadOnlyMemory<byte> item2 = tuple.first;
				ReadOnlyMemory<byte> item3 = tuple.rest;
				FrameFlags frameFlags = ((item3.Length == 0) ? FrameFlags.EndHeaders : FrameFlags.None);
				frameFlags |= (s.endStream ? FrameFlags.EndStream : FrameFlags.None);
				FrameHeader.WriteTo(destination, item2.Length, FrameType.Headers, frameFlags, s.http2Stream.StreamId);
				destination = destination.Slice(9);
				item2.Span.CopyTo(destination);
				destination = destination.Slice(item2.Length);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote HEADERS frame. Length={item2.Length}, flags={frameFlags}", "SendHeadersAsync");
				}
				while (item3.Length > 0)
				{
					(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) tuple2 = SplitBuffer(item3, 16384);
					item2 = tuple2.first;
					item3 = tuple2.rest;
					frameFlags = ((item3.Length == 0) ? FrameFlags.EndHeaders : FrameFlags.None);
					FrameHeader.WriteTo(destination, item2.Length, FrameType.Continuation, frameFlags, s.http2Stream.StreamId);
					destination = destination.Slice(9);
					item2.Span.CopyTo(destination);
					destination = destination.Slice(item2.Length);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote CONTINUATION frame. Length={item2.Length}, flags={frameFlags}", "SendHeadersAsync");
					}
				}
				return s.mustFlush || s.endStream;
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStop();
			}
			return http2Stream;
		}
		catch
		{
			ReleaseStream();
			throw;
		}
		finally
		{
			headerBuffer.Dispose();
		}
	}

	private async Task SendStreamDataAsync(int streamId, ReadOnlyMemory<byte> buffer, bool finalFlush, CancellationToken cancellationToken)
	{
		ReadOnlyMemory<byte> remaining = buffer;
		while (remaining.Length > 0)
		{
			int frameSize = Math.Min(remaining.Length, 16384);
			frameSize = await _connectionWindow.RequestCreditAsync(frameSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>) tuple = SplitBuffer(remaining, frameSize);
			ReadOnlyMemory<byte> item = tuple.Item1;
			remaining = tuple.Item2;
			bool item2 = false;
			if (finalFlush && remaining.Length == 0)
			{
				item2 = true;
			}
			if (!_connectionWindow.IsCreditAvailable)
			{
				item2 = true;
			}
			try
			{
				await PerformWriteAsync(9 + item.Length, (this, streamId, item, item2), delegate((Http2Connection thisRef, int streamId, ReadOnlyMemory<byte> current, bool flush) s, Memory<byte> writeBuffer)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						s.thisRef.Trace(s.streamId, $"Started writing. {"Length"}={writeBuffer.Length}", "SendStreamDataAsync");
					}
					FrameHeader.WriteTo(writeBuffer.Span, s.current.Length, FrameType.Data, FrameFlags.None, s.streamId);
					s.current.CopyTo(writeBuffer.Slice(9));
					return s.flush;
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				_connectionWindow.AdjustCredit(frameSize);
				throw;
			}
		}
	}

	private Task SendEndStreamAsync(int streamId)
	{
		return PerformWriteAsync(9, (this, streamId), delegate((Http2Connection thisRef, int streamId) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, "Started writing.", "SendEndStreamAsync");
			}
			FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Data, FrameFlags.EndStream, s.streamId);
			return true;
		});
	}

	private Task SendWindowUpdateAsync(int streamId, int amount)
	{
		return PerformWriteAsync(13, (this, streamId, amount), delegate((Http2Connection thisRef, int streamId, int amount) s, Memory<byte> writeBuffer)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				s.thisRef.Trace(s.streamId, $"Started writing. {"amount"}={s.amount}", "SendWindowUpdateAsync");
			}
			Span<byte> span = writeBuffer.Span;
			FrameHeader.WriteTo(span, 4, FrameType.WindowUpdate, FrameFlags.None, s.streamId);
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(9), s.amount);
			return true;
		});
	}

	private void ExtendWindow(int amount)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"amount"}={amount}", "ExtendWindow");
		}
		int pendingWindowUpdate;
		lock (SyncObject)
		{
			_pendingWindowUpdate += amount;
			if (_pendingWindowUpdate < 8388608)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"{"_pendingWindowUpdate"} {_pendingWindowUpdate} < {8388608}.", "ExtendWindow");
				}
				return;
			}
			pendingWindowUpdate = _pendingWindowUpdate;
			_pendingWindowUpdate = 0;
		}
		LogExceptions(SendWindowUpdateAsync(0, pendingWindowUpdate));
	}

	public override long GetIdleTicks(long nowTicks)
	{
		if (_streamsInUse != 0)
		{
			return 0L;
		}
		return base.GetIdleTicks(nowTicks);
	}

	private void Abort(Exception abortException)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{"abortException"}=={abortException}", "Abort");
		}
		List<Http2Stream> list = new List<Http2Stream>();
		lock (SyncObject)
		{
			if (_abortException != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Abort called while already aborting. {"abortException"}={abortException}", "Abort");
				}
				return;
			}
			_abortException = abortException;
			Shutdown();
			foreach (KeyValuePair<int, Http2Stream> httpStream in _httpStreams)
			{
				int key = httpStream.Key;
				list.Add(httpStream.Value);
			}
		}
		foreach (Http2Stream item in list)
		{
			item.OnReset(_abortException);
		}
	}

	private void FinalTeardown()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("", "FinalTeardown");
		}
		GC.SuppressFinalize(this);
		_stream.Dispose();
		_connectionWindow.Dispose();
		bool flag = _writeChannel.Writer.TryComplete();
		MarkConnectionAsClosed();
	}

	public override void Dispose()
	{
		lock (SyncObject)
		{
			Shutdown();
		}
	}

	private static TaskCompletionSourceWithCancellation<bool> CreateSuccessfullyCompletedTcs()
	{
		TaskCompletionSourceWithCancellation<bool> taskCompletionSourceWithCancellation = new TaskCompletionSourceWithCancellation<bool>();
		taskCompletionSourceWithCancellation.TrySetResult(result: true);
		return taskCompletionSourceWithCancellation;
	}

	public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Sending request: {request}", "SendAsync");
		}
		try
		{
			bool flag = request.Content != null && request.HasHeaders && request.Headers.ExpectContinue == true;
			Http2Stream http2Stream = await SendHeadersAsync(request, cancellationToken, flag || request.IsExtendedConnectRequest).ConfigureAwait(continueOnCapturedContext: false);
			bool flag2 = request.Content != null && request.Content.AllowDuplex;
			CancellationToken cancellationToken2 = (flag2 ? CancellationToken.None : cancellationToken);
			Task requestBodyTask = http2Stream.SendRequestBodyAsync(cancellationToken2);
			Task responseHeadersTask = http2Stream.ReadResponseHeadersAsync(cancellationToken);
			bool flag3 = requestBodyTask.IsCompleted || !flag2;
			bool flag4 = flag3;
			if (!flag4)
			{
				flag4 = await Task.WhenAny(requestBodyTask, responseHeadersTask).ConfigureAwait(continueOnCapturedContext: false) == requestBodyTask;
			}
			if (flag4 || requestBodyTask.IsCompleted || http2Stream.SendRequestFinished)
			{
				try
				{
					await requestBodyTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception value)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Sending request content failed: {value}", "SendAsync");
					}
					LogExceptions(responseHeadersTask);
					throw;
				}
			}
			else
			{
				LogExceptions(requestBodyTask);
			}
			await responseHeadersTask.ConfigureAwait(continueOnCapturedContext: false);
			return http2Stream.GetAndClearResponse();
		}
		catch (HttpIOException ex)
		{
			throw new HttpRequestException(ex.HttpRequestError, ex.Message, ex);
		}
		catch (Exception ex2) when (ex2 is IOException || ex2 is ObjectDisposedException || ex2 is InvalidOperationException)
		{
			throw new HttpRequestException(HttpRequestError.Unknown, System.SR.net_http_client_execution_error, ex2);
		}
	}

	private void RemoveStream(Http2Stream http2Stream)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace(http2Stream.StreamId, "", "RemoveStream");
		}
		lock (SyncObject)
		{
			if (!_httpStreams.Remove(http2Stream.StreamId))
			{
				return;
			}
		}
		ReleaseStream();
	}

	private void RefreshPingTimestamp()
	{
		_nextPingRequestTimestamp = Environment.TickCount64 + _keepAlivePingDelay;
	}

	private void ProcessPingAck(long payload)
	{
		if (payload < 0)
		{
			_rttEstimator.OnPingAckReceived(payload, this);
			return;
		}
		if (_keepAliveState != KeepAliveState.PingSent)
		{
			ThrowProtocolError();
		}
		if (Interlocked.Read(ref _keepAlivePingPayload) != payload)
		{
			ThrowProtocolError();
		}
		_keepAliveState = KeepAliveState.None;
	}

	private void VerifyKeepAlive()
	{
		if (_keepAlivePingPolicy == HttpKeepAlivePingPolicy.WithActiveRequests)
		{
			lock (SyncObject)
			{
				if (_streamsInUse == 0)
				{
					return;
				}
			}
		}
		long tickCount = Environment.TickCount64;
		switch (_keepAliveState)
		{
		case KeepAliveState.None:
			if (tickCount > _nextPingRequestTimestamp)
			{
				_keepAliveState = KeepAliveState.PingSent;
				_keepAlivePingTimeoutTimestamp = tickCount + _keepAlivePingTimeout;
				long pingContent = Interlocked.Increment(ref _keepAlivePingPayload);
				LogExceptions(SendPingAsync(pingContent));
			}
			break;
		case KeepAliveState.PingSent:
			if (tickCount > _keepAlivePingTimeoutTimestamp)
			{
				ThrowProtocolError();
			}
			break;
		}
	}

	public sealed override string ToString()
	{
		return $"{"Http2Connection"}({_pool})";
	}

	public override void Trace(string message, [CallerMemberName] string memberName = null)
	{
		Trace(0, message, memberName);
	}

	internal void Trace(int streamId, string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(_pool?.GetHashCode() ?? 0, GetHashCode(), streamId, memberName, message);
	}

	[DoesNotReturn]
	private static void ThrowRetry(string message, Exception innerException = null)
	{
		throw new HttpRequestException((innerException as HttpIOException)?.HttpRequestError ?? HttpRequestError.Unknown, message, innerException, RequestRetryType.RetryOnConnectionFailure);
	}

	private static Exception GetRequestAbortedException(Exception innerException = null)
	{
		return (innerException as HttpIOException) ?? new IOException(System.SR.net_http_request_aborted, innerException);
	}

	[DoesNotReturn]
	private static void ThrowRequestAborted(Exception innerException = null)
	{
		throw GetRequestAbortedException(innerException);
	}

	[DoesNotReturn]
	private static void ThrowProtocolError()
	{
		ThrowProtocolError(Http2ProtocolErrorCode.ProtocolError);
	}

	[DoesNotReturn]
	private static void ThrowProtocolError(Http2ProtocolErrorCode errorCode, string message = null)
	{
		throw HttpProtocolException.CreateHttp2ConnectionException(errorCode, message);
	}
}
