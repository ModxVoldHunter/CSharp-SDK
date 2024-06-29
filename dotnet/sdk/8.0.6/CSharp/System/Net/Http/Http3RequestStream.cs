using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class Http3RequestStream : IHttpStreamHeadersHandler, IAsyncDisposable, IDisposable
{
	private sealed class Http3ReadStream : HttpBaseStream
	{
		private Http3RequestStream _stream;

		private HttpResponseMessage _response;

		public override bool CanRead => _stream != null;

		public override bool CanWrite => false;

		public Http3ReadStream(Http3RequestStream stream)
		{
			_stream = stream;
			_response = stream._response;
		}

		~Http3ReadStream()
		{
			Dispose(disposing: false);
		}

		protected override void Dispose(bool disposing)
		{
			Http3RequestStream http3RequestStream = Interlocked.Exchange(ref _stream, null);
			if (http3RequestStream != null)
			{
				if (disposing)
				{
					http3RequestStream.Dispose();
				}
				else
				{
					http3RequestStream.AbortStream();
					http3RequestStream._connection.RemoveStream(http3RequestStream._stream);
					http3RequestStream._connection = null;
				}
				_response = null;
				base.Dispose(disposing);
			}
		}

		public override async ValueTask DisposeAsync()
		{
			Http3RequestStream http3RequestStream = Interlocked.Exchange(ref _stream, null);
			if (http3RequestStream != null)
			{
				await http3RequestStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				_response = null;
				await base.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		public override int Read(Span<byte> buffer)
		{
			Http3RequestStream stream = _stream;
			ObjectDisposedException.ThrowIf(stream == null, this);
			return stream.ReadResponseContent(_response, buffer);
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			return _stream?.ReadResponseContentAsync(_response, buffer, cancellationToken) ?? ValueTask.FromException<int>(new ObjectDisposedException("Http3RequestStream"));
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}

	private sealed class Http3WriteStream : HttpBaseStream
	{
		private Http3RequestStream _stream;

		public long BytesWritten { get; private set; }

		public override bool CanRead => false;

		public override bool CanWrite => _stream != null;

		public Http3WriteStream(Http3RequestStream stream)
		{
			_stream = stream;
		}

		protected override void Dispose(bool disposing)
		{
			_stream = null;
			base.Dispose(disposing);
		}

		public override int Read(Span<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			BytesWritten += buffer.Length;
			return _stream?.WriteRequestContentAsync(buffer, cancellationToken) ?? ValueTask.FromException(new ObjectDisposedException("Http3WriteStream"));
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			Http3RequestStream stream = _stream;
			if (stream == null)
			{
				return Task.FromException(new ObjectDisposedException("Http3WriteStream"));
			}
			return stream.FlushSendBufferAsync(endStream: false, cancellationToken).AsTask();
		}
	}

	private enum HeaderState
	{
		StatusHeader,
		SkipExpect100Headers,
		ResponseHeaders,
		TrailingHeaders
	}

	private readonly HttpRequestMessage _request;

	private Http3Connection _connection;

	private long _streamId = -1L;

	private readonly QuicStream _stream;

	private System.Net.ArrayBuffer _sendBuffer;

	private System.Net.ArrayBuffer _recvBuffer;

	private TaskCompletionSource<bool> _expect100ContinueCompletionSource;

	private bool _disposed;

	private readonly CancellationTokenSource _requestBodyCancellationSource;

	private HttpResponseMessage _response;

	private readonly QPackDecoder _headerDecoder;

	private HeaderState _headerState;

	private int _headerBudgetRemaining;

	private string[] _headerValues = Array.Empty<string>();

	private List<(HeaderDescriptor name, string value)> _trailingHeaders;

	private long _responseDataPayloadRemaining;

	private long _requestContentLengthRemaining;

	private bool _singleDataFrameWritten;

	private bool _requestSendCompleted;

	private bool _responseRecvCompleted;

	public long StreamId
	{
		get
		{
			return Volatile.Read(ref _streamId);
		}
		set
		{
			Volatile.Write(ref _streamId, value);
		}
	}

	public Http3RequestStream(HttpRequestMessage request, Http3Connection connection, QuicStream stream)
	{
		_request = request;
		_connection = connection;
		_stream = stream;
		_sendBuffer = new System.Net.ArrayBuffer(64, usePool: true);
		_recvBuffer = new System.Net.ArrayBuffer(64, usePool: true);
		_headerBudgetRemaining = connection.Pool.Settings.MaxResponseHeadersByteLength;
		_headerDecoder = new QPackDecoder(Math.Min(int.MaxValue, _headerBudgetRemaining));
		_requestBodyCancellationSource = new CancellationTokenSource();
		_requestSendCompleted = _request.Content == null;
		_responseRecvCompleted = false;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			AbortStream();
			_stream.Dispose();
			DisposeSyncHelper();
		}
	}

	private void RemoveFromConnectionIfDone()
	{
		if (_responseRecvCompleted && _requestSendCompleted)
		{
			_connection.RemoveStream(_stream);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			AbortStream();
			await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			DisposeSyncHelper();
		}
	}

	private void DisposeSyncHelper()
	{
		_connection.RemoveStream(_stream);
		_sendBuffer.Dispose();
		_recvBuffer.Dispose();
	}

	public void GoAway()
	{
		_requestBodyCancellationSource.Cancel();
	}

	public async Task<HttpResponseMessage> SendAsync(CancellationToken cancellationToken)
	{
		bool disposeSelf = true;
		if (_request.Content != null)
		{
			_ = _request.Content.AllowDuplex;
		}
		CancellationTokenRegistration linkedTokenRegistration = cancellationToken.UnsafeRegister(delegate(object cts)
		{
			((CancellationTokenSource)cts).Cancel();
		}, _requestBodyCancellationSource);
		bool shouldCancelBody = true;
		HttpResponseMessage result;
		try
		{
			_ = 4;
			try
			{
				BufferHeaders(_request);
				if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
				{
					_expect100ContinueCompletionSource = new TaskCompletionSource<bool>();
				}
				if (_expect100ContinueCompletionSource != null || _request.Content == null)
				{
					await FlushSendBufferAsync(_request.Content == null, _requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				Task sendContentTask = ((_request.Content == null) ? Task.CompletedTask : SendContentAsync(_request.Content, _requestBodyCancellationSource.Token));
				Task readResponseTask = ReadResponseAsync(_requestBodyCancellationSource.Token);
				bool sendContentObserved = false;
				int num;
				if (!sendContentTask.IsCompleted)
				{
					HttpContent? content = _request.Content;
					num = ((content == null || !content.AllowDuplex) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				bool flag = (byte)num != 0;
				bool flag2 = flag;
				if (!flag2)
				{
					flag2 = await Task.WhenAny(sendContentTask, readResponseTask).ConfigureAwait(continueOnCapturedContext: false) == sendContentTask;
				}
				if (flag2 || sendContentTask.IsCompleted)
				{
					try
					{
						await sendContentTask.ConfigureAwait(continueOnCapturedContext: false);
						sendContentObserved = true;
					}
					catch
					{
						_connection.LogExceptions(readResponseTask);
						throw;
					}
				}
				else
				{
					_connection.LogExceptions(sendContentTask);
				}
				await readResponseTask.ConfigureAwait(continueOnCapturedContext: false);
				HttpConnectionResponseContent responseContent = (HttpConnectionResponseContent)_response.Content;
				bool useEmptyResponseContent = responseContent.Headers.ContentLength == 0 && sendContentObserved;
				if (useEmptyResponseContent)
				{
					await DrainContentLength0Frames(_requestBodyCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false);
					responseContent.SetStream(EmptyReadStream.Instance);
				}
				else
				{
					responseContent.SetStream(new Http3ReadStream(this));
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Received response: {_response}", "SendAsync");
				}
				if (_connection.Pool.Settings._useCookies)
				{
					CookieHelper.ProcessReceivedCookies(_response, _connection.Pool.Settings._cookieContainer);
				}
				HttpResponseMessage response = _response;
				_response = null;
				disposeSelf = useEmptyResponseContent;
				shouldCancelBody = false;
				result = response;
			}
			catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted)
			{
				Http3ErrorCode value = (Http3ErrorCode)ex.ApplicationErrorCode.Value;
				switch (value)
				{
				case Http3ErrorCode.VersionFallback:
					throw new HttpRequestException(HttpRequestError.Unknown, System.SR.net_http_retry_on_older_version, ex, RequestRetryType.RetryOnLowerHttpVersion);
				case Http3ErrorCode.RequestRejected:
				{
					HttpProtocolException inner = HttpProtocolException.CreateHttp3StreamException(value, ex);
					throw new HttpRequestException(HttpRequestError.HttpProtocolError, System.SR.net_http_request_aborted, inner, RequestRetryType.RetryOnConnectionFailure);
				}
				default:
				{
					Exception ex2 = _connection.AbortException ?? HttpProtocolException.CreateHttp3StreamException(value, ex);
					HttpRequestError httpRequestError = ((ex2 is HttpProtocolException) ? HttpRequestError.HttpProtocolError : HttpRequestError.Unknown);
					throw new HttpRequestException(httpRequestError, System.SR.net_http_client_execution_error, ex2);
				}
				}
			}
			catch (QuicException ex3) when (ex3.QuicError == QuicError.ConnectionAborted)
			{
				Http3ErrorCode value2 = (Http3ErrorCode)ex3.ApplicationErrorCode.Value;
				Exception inner2 = _connection.Abort(HttpProtocolException.CreateHttp3ConnectionException(value2, System.SR.net_http_http3_connection_close));
				throw new HttpRequestException(HttpRequestError.HttpProtocolError, System.SR.net_http_client_execution_error, inner2);
			}
			catch (QuicException ex4) when (ex4.QuicError == QuicError.OperationAborted && _connection.AbortException != null)
			{
				throw new HttpRequestException(HttpRequestError.Unknown, System.SR.net_http_client_execution_error, _connection.AbortException);
			}
			catch (OperationCanceledException ex5) when (ex5.CancellationToken == _requestBodyCancellationSource.Token || ex5.CancellationToken == cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_stream.Abort(QuicAbortDirection.Write, 268L);
					throw new TaskCanceledException(ex5.Message, ex5, cancellationToken);
				}
				throw new HttpRequestException(HttpRequestError.Unknown, System.SR.net_http_request_aborted, ex5, RequestRetryType.RetryOnConnectionFailure);
			}
			catch (HttpIOException ex6)
			{
				_connection.Abort(ex6);
				throw new HttpRequestException(ex6.HttpRequestError, System.SR.net_http_client_execution_error, ex6);
			}
			catch (Exception ex7)
			{
				_stream.Abort(QuicAbortDirection.Write, 258L);
				if (ex7 is HttpRequestException)
				{
					throw;
				}
				throw new HttpRequestException(HttpRequestError.Unknown, System.SR.net_http_client_execution_error, ex7);
			}
		}
		finally
		{
			if (shouldCancelBody)
			{
				_requestBodyCancellationSource.Cancel();
			}
			linkedTokenRegistration.Dispose();
			if (disposeSelf)
			{
				await DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		return result;
	}

	private async Task ReadResponseAsync(CancellationToken cancellationToken)
	{
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.ResponseHeadersStart();
		}
		do
		{
			_headerState = HeaderState.StatusHeader;
			var (http3FrameType, headersLength) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (http3FrameType != Http3FrameType.Headers)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Expected HEADERS as first response frame; received {http3FrameType}.", "ReadResponseAsync");
				}
				throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
			}
			await ReadHeadersAsync(headersLength, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (_response.StatusCode < HttpStatusCode.OK);
		_headerState = HeaderState.TrailingHeaders;
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.ResponseHeadersStop((int)_response.StatusCode);
		}
	}

	private async Task SendContentAsync(HttpContent content, CancellationToken cancellationToken)
	{
		_ = 3;
		try
		{
			if (_expect100ContinueCompletionSource != null)
			{
				Timer timer = null;
				try
				{
					if (_connection.Pool.Settings._expect100ContinueTimeout != Timeout.InfiniteTimeSpan)
					{
						timer = new Timer(delegate(object o)
						{
							((Http3RequestStream)o)._expect100ContinueCompletionSource.TrySetResult(result: true);
						}, this, _connection.Pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
					}
					if (!(await _expect100ContinueCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false)))
					{
						return;
					}
				}
				finally
				{
					if (timer != null)
					{
						await timer.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestContentStart();
			}
			_requestContentLengthRemaining = content.Headers.ContentLength ?? (-1);
			long bytesWritten;
			using (Http3WriteStream writeStream = new Http3WriteStream(this))
			{
				await content.CopyToAsync(writeStream, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				bytesWritten = writeStream.BytesWritten;
			}
			if (_requestContentLengthRemaining > 0)
			{
				long valueOrDefault = content.Headers.ContentLength.GetValueOrDefault();
				long num = valueOrDefault - _requestContentLengthRemaining;
				throw new HttpRequestException(System.SR.Format(System.SR.net_http_request_content_length_mismatch, num, valueOrDefault));
			}
			_requestContentLengthRemaining = 0L;
			if (_sendBuffer.ActiveLength != 0)
			{
				await FlushSendBufferAsync(endStream: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				_stream.CompleteWrites();
			}
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestContentStop(bytesWritten);
			}
		}
		finally
		{
			_requestSendCompleted = true;
			RemoveFromConnectionIfDone();
		}
	}

	private async ValueTask WriteRequestContentAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		if (buffer.Length == 0)
		{
			return;
		}
		long requestContentLengthRemaining = _requestContentLengthRemaining;
		if (requestContentLengthRemaining != -1)
		{
			if (buffer.Length > _requestContentLengthRemaining)
			{
				throw new HttpRequestException(System.SR.net_http_content_write_larger_than_content_length);
			}
			_requestContentLengthRemaining -= buffer.Length;
			if (!_singleDataFrameWritten)
			{
				BufferFrameEnvelope(Http3FrameType.Data, requestContentLengthRemaining);
				await _stream.WriteAsync(_sendBuffer.ActiveMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_sendBuffer.Discard(_sendBuffer.ActiveLength);
				_singleDataFrameWritten = true;
			}
			else
			{
				await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		else
		{
			BufferFrameEnvelope(Http3FrameType.Data, buffer.Length);
			await _stream.WriteAsync(_sendBuffer.ActiveMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_sendBuffer.Discard(_sendBuffer.ActiveLength);
		}
	}

	private async ValueTask FlushSendBufferAsync(bool endStream, CancellationToken cancellationToken)
	{
		await _stream.WriteAsync(_sendBuffer.ActiveMemory, endStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_sendBuffer.Discard(_sendBuffer.ActiveLength);
		await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async ValueTask DrainContentLength0Frames(CancellationToken cancellationToken)
	{
		while (true)
		{
			var (http3FrameType, num) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (http3FrameType.HasValue)
			{
				Http3FrameType valueOrDefault = http3FrameType.GetValueOrDefault();
				if (valueOrDefault == Http3FrameType.Data)
				{
					if (num != 0L)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							Trace("Response content exceeded Content-Length.", "DrainContentLength0Frames");
						}
						throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.net_http_invalid_response);
					}
					continue;
				}
				if (valueOrDefault != Http3FrameType.Headers)
				{
					break;
				}
				_trailingHeaders = new List<(HeaderDescriptor, string)>();
				await ReadHeadersAsync(num, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			CopyTrailersToResponseMessage(_response);
			_responseDataPayloadRemaining = -1L;
			break;
		}
	}

	private void CopyTrailersToResponseMessage(HttpResponseMessage responseMessage)
	{
		List<(HeaderDescriptor name, string value)> trailingHeaders = _trailingHeaders;
		if (trailingHeaders == null || trailingHeaders.Count <= 0)
		{
			return;
		}
		foreach (var (descriptor, value) in _trailingHeaders)
		{
			responseMessage.TrailingHeaders.TryAddWithoutValidation(descriptor, value);
		}
		_trailingHeaders.Clear();
	}

	private void BufferHeaders(HttpRequestMessage request)
	{
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestHeadersStart(_connection.Id);
		}
		_sendBuffer.Commit(9);
		_sendBuffer.EnsureAvailableSpace(2);
		_sendBuffer.AvailableSpan[0] = 0;
		_sendBuffer.AvailableSpan[1] = 0;
		_sendBuffer.Commit(2);
		HttpMethod httpMethod = HttpMethod.Normalize(request.Method);
		BufferBytes(httpMethod.Http3EncodedBytes);
		BufferIndexedHeader(23);
		if (request.HasHeaders)
		{
			string host = request.Headers.Host;
			if (host != null)
			{
				BufferLiteralHeaderWithStaticNameReference(0, host);
				goto IL_00d6;
			}
		}
		BufferBytes(_connection.Pool._http3EncodedAuthorityHostHeader);
		goto IL_00d6;
		IL_00d6:
		string pathAndQuery = request.RequestUri.PathAndQuery;
		if (pathAndQuery == "/")
		{
			BufferIndexedHeader(1);
		}
		else
		{
			BufferLiteralHeaderWithStaticNameReference(1, pathAndQuery);
		}
		BufferBytes(_connection.AltUsedEncodedHeaderBytes);
		int num = 128;
		if (request.HasHeaders)
		{
			if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
			{
				request.Headers.TransferEncodingChunked = false;
			}
			num += BufferHeaderCollection(request.Headers);
		}
		if (_connection.Pool.Settings._useCookies)
		{
			string cookieHeader = _connection.Pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
			if (cookieHeader != string.Empty)
			{
				Encoding valueEncoding = _connection.Pool.Settings._requestHeaderEncodingSelector?.Invoke("Cookie", request);
				BufferLiteralHeaderWithStaticNameReference(5, cookieHeader, valueEncoding);
				num += "Cookie".Length + 32;
			}
		}
		if (request.Content == null)
		{
			if (httpMethod.MustHaveRequestBody)
			{
				BufferIndexedHeader(4);
				num += "Content-Length".Length + 32;
			}
		}
		else
		{
			num += BufferHeaderCollection(request.Content.Headers);
		}
		int num2 = _sendBuffer.ActiveLength - 9;
		int byteCount = VariableLengthIntegerHelper.GetByteCount(num2);
		_sendBuffer.Discard(9 - byteCount - 1);
		_sendBuffer.ActiveSpan[0] = 1;
		int num3 = VariableLengthIntegerHelper.WriteInteger(_sendBuffer.ActiveSpan.Slice(1, byteCount), num2);
		num += num2;
		uint maxHeaderListSize = _connection.MaxHeaderListSize;
		if ((uint)num > maxHeaderListSize)
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_request_headers_exceeded_length, maxHeaderListSize));
		}
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestHeadersStop();
		}
	}

	private int BufferHeaderCollection(HttpHeaders headers)
	{
		HeaderEncodingSelector<HttpRequestMessage> requestHeaderEncodingSelector = _connection.Pool.Settings._requestHeaderEncodingSelector;
		ReadOnlySpan<HeaderEntry> entries = headers.GetEntries();
		int num = entries.Length * 32;
		ReadOnlySpan<HeaderEntry> readOnlySpan = entries;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			HeaderEntry headerEntry = readOnlySpan[i];
			int storeValuesIntoStringArray = HttpHeaders.GetStoreValuesIntoStringArray(headerEntry.Key, headerEntry.Value, ref _headerValues);
			ReadOnlySpan<string> readOnlySpan2 = _headerValues.AsSpan(0, storeValuesIntoStringArray);
			Encoding valueEncoding = requestHeaderEncodingSelector?.Invoke(headerEntry.Key.Name, _request);
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
							BufferLiteralHeaderWithoutNameReference("TE", text, valueEncoding);
							break;
						}
					}
				}
				else
				{
					BufferBytes(knownHeader.Http3EncodedName);
					string separator = null;
					if (readOnlySpan2.Length > 1)
					{
						HttpHeaderParser parser = headerEntry.Key.Parser;
						separator = ((parser == null || !parser.SupportsMultipleValues) ? ", " : parser.Separator);
					}
					BufferLiteralHeaderValues(readOnlySpan2, separator, valueEncoding);
				}
			}
			else
			{
				BufferLiteralHeaderWithoutNameReference(headerEntry.Key.Name, readOnlySpan2, ", ", valueEncoding);
			}
		}
		return num;
	}

	private void BufferIndexedHeader(int index)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeStaticIndexedHeaderField(index, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithStaticNameReference(int nameIndex, string value, Encoding valueEncoding = null)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReference(nameIndex, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithoutNameReference(string name, ReadOnlySpan<string> values, string separator, Encoding valueEncoding)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, values, separator, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderWithoutNameReference(string name, string value, Encoding valueEncoding)
	{
		int bytesWritten;
		while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferLiteralHeaderValues(ReadOnlySpan<string> values, string separator, Encoding valueEncoding)
	{
		int length;
		while (!QPackEncoder.EncodeValueString(values, separator, valueEncoding, _sendBuffer.AvailableSpan, out length))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(length);
	}

	private void BufferFrameEnvelope(Http3FrameType frameType, long payloadLength)
	{
		int bytesWritten;
		while (!Http3Frame.TryWriteFrameEnvelope(frameType, payloadLength, _sendBuffer.AvailableSpan, out bytesWritten))
		{
			_sendBuffer.Grow();
		}
		_sendBuffer.Commit(bytesWritten);
	}

	private void BufferBytes(ReadOnlySpan<byte> span)
	{
		_sendBuffer.EnsureAvailableSpace(span.Length);
		span.CopyTo(_sendBuffer.AvailableSpan);
		_sendBuffer.Commit(span.Length);
	}

	private async ValueTask<(Http3FrameType? frameType, long payloadLength)> ReadFrameEnvelopeAsync(CancellationToken cancellationToken)
	{
		while (true)
		{
			if (!Http3Frame.TryReadIntegerPair(_recvBuffer.ActiveSpan, out var a, out var b, out var bytesRead))
			{
				_recvBuffer.EnsureAvailableSpace(16);
				bytesRead = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (bytesRead == 0)
				{
					break;
				}
				_recvBuffer.Commit(bytesRead);
				continue;
			}
			_recvBuffer.Discard(bytesRead);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received frame {a} of length {b}.", "ReadFrameEnvelopeAsync");
			}
			Http3FrameType http3FrameType = (Http3FrameType)a;
			if ((ulong)http3FrameType <= 13uL)
			{
				switch (http3FrameType)
				{
				case Http3FrameType.Data:
				case Http3FrameType.Headers:
					return ((Http3FrameType)a, b);
				case Http3FrameType.ReservedHttp2Priority:
				case Http3FrameType.Settings:
				case Http3FrameType.ReservedHttp2Ping:
				case Http3FrameType.GoAway:
				case Http3FrameType.ReservedHttp2WindowUpdate:
				case Http3FrameType.ReservedHttp2Continuation:
				case Http3FrameType.MaxPushId:
					throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.UnexpectedFrame);
				case Http3FrameType.CancelPush:
				case Http3FrameType.PushPromise:
					throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.IdError);
				}
			}
			await SkipUnknownPayloadAsync(b, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (_recvBuffer.ActiveLength == 0)
		{
			return (null, 0L);
		}
		throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.net_http_invalid_response_premature_eof);
	}

	private async ValueTask ReadHeadersAsync(long headersLength, CancellationToken cancellationToken)
	{
		if (headersLength > _headerBudgetRemaining)
		{
			_stream.Abort(QuicAbortDirection.Read, 263L);
			throw new HttpRequestException(HttpRequestError.ConfigurationLimitExceeded, System.SR.Format(System.SR.net_http_response_headers_exceeded_length, _connection.Pool.Settings.MaxResponseHeadersByteLength));
		}
		_headerBudgetRemaining -= (int)headersLength;
		while (headersLength != 0L)
		{
			if (_recvBuffer.ActiveLength == 0)
			{
				_recvBuffer.EnsureAvailableSpace(1);
				int num = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"Server closed response stream before entire header payload could be read. {headersLength:N0} bytes remaining.", "ReadHeadersAsync");
					}
					throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.net_http_invalid_response_premature_eof);
				}
				_recvBuffer.Commit(num);
			}
			int num2 = (int)Math.Min(headersLength, _recvBuffer.ActiveLength);
			bool endHeaders = headersLength == num2;
			_headerDecoder.Decode(_recvBuffer.ActiveSpan.Slice(0, num2), endHeaders, this);
			_recvBuffer.Discard(num2);
			headersLength -= num2;
		}
		_headerDecoder.Reset();
	}

	void IHttpStreamHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
	{
		if (!HeaderDescriptor.TryGet(name, out var descriptor))
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
		}
		OnHeader(null, descriptor, null, value);
	}

	void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index)
	{
		GetStaticQPackHeader(index, out var descriptor, out var knownValue);
		OnHeader(index, descriptor, knownValue, default(ReadOnlySpan<byte>));
	}

	void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
	{
		GetStaticQPackHeader(index, out var descriptor, out var _);
		OnHeader(index, descriptor, null, value);
	}

	void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
	{
		((IHttpStreamHeadersHandler)this).OnHeader(name, value);
	}

	private void GetStaticQPackHeader(int index, out HeaderDescriptor descriptor, out string knownValue)
	{
		if (!HeaderDescriptor.TryGetStaticQPackHeader(index, out descriptor, out knownValue))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Response contains invalid static header index '{index}'.", "GetStaticQPackHeader");
			}
			throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.ProtocolError);
		}
	}

	private void OnHeader(int? staticIndex, HeaderDescriptor descriptor, string staticValue, ReadOnlySpan<byte> literalValue)
	{
		if (descriptor.Name[0] == ':')
		{
			if (!descriptor.Equals(KnownHeaders.PseudoStatus))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received unknown pseudo-header '" + descriptor.Name + "'.", "OnHeader");
				}
				throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.ProtocolError);
			}
			if (_headerState != 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received extra status header.", "OnHeader");
				}
				throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.ProtocolError);
			}
			int num = ((staticValue == null) ? HttpConnectionBase.ParseStatusCode(literalValue) : (staticIndex switch
			{
				24 => 103, 
				25 => 200, 
				26 => 304, 
				27 => 404, 
				28 => 503, 
				63 => 100, 
				64 => 204, 
				65 => 206, 
				66 => 302, 
				67 => 400, 
				68 => 403, 
				69 => 421, 
				70 => 425, 
				71 => 500, 
				_ => ParseStatusCode(staticIndex, staticValue), 
			}));
			_response = new HttpResponseMessage
			{
				Version = HttpVersion.Version30,
				RequestMessage = _request,
				Content = new HttpConnectionResponseContent(),
				StatusCode = (HttpStatusCode)num
			};
			if (num < 200)
			{
				_headerState = HeaderState.SkipExpect100Headers;
				if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueCompletionSource != null)
				{
					_expect100ContinueCompletionSource.TrySetResult(result: true);
				}
				return;
			}
			_headerState = HeaderState.ResponseHeaders;
			if (_expect100ContinueCompletionSource != null)
			{
				bool result = num < 300;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Expecting 100 Continue but received final status {num}.", "OnHeader");
				}
				_expect100ContinueCompletionSource.TrySetResult(result);
			}
		}
		else
		{
			if (_headerState == HeaderState.SkipExpect100Headers)
			{
				return;
			}
			string text = staticValue;
			if (text == null)
			{
				Encoding valueEncoding = _connection.Pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);
				text = _connection.GetResponseHeaderValueWithCaching(descriptor, literalValue, valueEncoding);
			}
			switch (_headerState)
			{
			case HeaderState.StatusHeader:
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Received headers without :status.", "OnHeader");
				}
				throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.ProtocolError);
			case HeaderState.ResponseHeaders:
				if (descriptor.HeaderType.HasFlag(HttpHeaderType.Content))
				{
					_response.Content.Headers.TryAddWithoutValidation(descriptor, text);
				}
				else
				{
					_response.Headers.TryAddWithoutValidation(descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, text);
				}
				break;
			case HeaderState.TrailingHeaders:
				_trailingHeaders.Add((descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, text));
				break;
			case HeaderState.SkipExpect100Headers:
				break;
			}
		}
		int ParseStatusCode(int? index, string value)
		{
			string message = $"Unexpected QPACK table reference for Status code: index={index} value='{value}'";
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace(message, "OnHeader");
			}
			return HttpConnectionBase.ParseStatusCode(Encoding.ASCII.GetBytes(value));
		}
	}

	void IHttpStreamHeadersHandler.OnHeadersComplete(bool endStream)
	{
	}

	private async ValueTask SkipUnknownPayloadAsync(long payloadLength, CancellationToken cancellationToken)
	{
		while (payloadLength != 0L)
		{
			if (_recvBuffer.ActiveLength == 0)
			{
				_recvBuffer.EnsureAvailableSpace(1);
				int num = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					throw HttpProtocolException.CreateHttp3ConnectionException(Http3ErrorCode.FrameError);
				}
				_recvBuffer.Commit(num);
			}
			long num2 = Math.Min(payloadLength, _recvBuffer.ActiveLength);
			_recvBuffer.Discard((int)num2);
			payloadLength -= num2;
		}
	}

	private int ReadResponseContent(HttpResponseMessage response, Span<byte> buffer)
	{
		try
		{
			int num = 0;
			do
			{
				if (_responseDataPayloadRemaining <= 0 && !ReadNextDataFrameAsync(response, CancellationToken.None).AsTask().GetAwaiter().GetResult())
				{
					_responseRecvCompleted = true;
					RemoveFromConnectionIfDone();
					break;
				}
				if (_recvBuffer.ActiveLength != 0)
				{
					int num2 = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
					Span<byte> span = _recvBuffer.ActiveSpan;
					span = span.Slice(0, num2);
					span.CopyTo(buffer);
					num += num2;
					_responseDataPayloadRemaining -= num2;
					_recvBuffer.Discard(num2);
					buffer = buffer.Slice(num2);
					continue;
				}
				int length = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
				int num3 = _stream.Read(buffer.Slice(0, length));
				if (num3 == 0 && buffer.Length != 0)
				{
					throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
				}
				num += num3;
				_responseDataPayloadRemaining -= num3;
				buffer = buffer.Slice(num3);
				break;
			}
			while ((_responseDataPayloadRemaining != 0L || _recvBuffer.ActiveLength != 0) && buffer.Length != 0);
			return num;
		}
		catch (Exception ex)
		{
			HandleReadResponseContentException(ex, CancellationToken.None);
			return 0;
		}
	}

	private async ValueTask<int> ReadResponseContentAsync(HttpResponseMessage response, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			int totalBytesRead = 0;
			do
			{
				bool flag = _responseDataPayloadRemaining <= 0;
				bool flag2 = flag;
				if (flag2)
				{
					flag2 = !(await ReadNextDataFrameAsync(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
				}
				if (flag2)
				{
					_responseRecvCompleted = true;
					RemoveFromConnectionIfDone();
					break;
				}
				if (_recvBuffer.ActiveLength != 0)
				{
					int num = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
					Span<byte> span = _recvBuffer.ActiveSpan;
					span = span.Slice(0, num);
					span.CopyTo(buffer.Span);
					totalBytesRead += num;
					_responseDataPayloadRemaining -= num;
					_recvBuffer.Discard(num);
					buffer = buffer.Slice(num);
					continue;
				}
				int length = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
				int num2 = await _stream.ReadAsync(buffer.Slice(0, length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num2 == 0 && buffer.Length != 0)
				{
					throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
				}
				totalBytesRead += num2;
				_responseDataPayloadRemaining -= num2;
				buffer = buffer.Slice(num2);
				break;
			}
			while ((_responseDataPayloadRemaining != 0L || _recvBuffer.ActiveLength != 0) && buffer.Length != 0);
			return totalBytesRead;
		}
		catch (Exception ex)
		{
			HandleReadResponseContentException(ex, cancellationToken);
			return 0;
		}
	}

	[DoesNotReturn]
	private void HandleReadResponseContentException(Exception ex, CancellationToken cancellationToken)
	{
		_responseRecvCompleted = true;
		RemoveFromConnectionIfDone();
		if (!(ex is QuicException ex2))
		{
			if (ex is HttpIOException)
			{
				_connection.Abort(ex);
				ExceptionDispatchInfo.Throw(ex);
				return;
			}
			if (ex is OperationCanceledException ex3 && ex3.CancellationToken == cancellationToken)
			{
				_stream.Abort(QuicAbortDirection.Read, 268L);
				ExceptionDispatchInfo.Throw(ex);
				return;
			}
		}
		else
		{
			if (ex2.QuicError == QuicError.StreamAborted)
			{
				throw HttpProtocolException.CreateHttp3StreamException((Http3ErrorCode)ex2.ApplicationErrorCode.Value, ex2);
			}
			QuicException ex4 = ex2;
			if (ex4.QuicError == QuicError.ConnectionAborted)
			{
				HttpProtocolException ex5 = HttpProtocolException.CreateHttp3ConnectionException((Http3ErrorCode)ex4.ApplicationErrorCode.Value, System.SR.net_http_http3_connection_close);
				_connection.Abort(ex5);
				throw ex5;
			}
		}
		_stream.Abort(QuicAbortDirection.Read, 258L);
		throw new HttpIOException(HttpRequestError.Unknown, System.SR.net_http_client_execution_error, new HttpRequestException(System.SR.net_http_client_execution_error, ex));
	}

	private async ValueTask<bool> ReadNextDataFrameAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (_responseDataPayloadRemaining == -1)
		{
			return false;
		}
		while (true)
		{
			var (http3FrameType, num) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			switch (http3FrameType)
			{
			default:
				continue;
			case Http3FrameType.Data:
				goto IL_00d6;
			case Http3FrameType.Headers:
				_trailingHeaders = new List<(HeaderDescriptor, string)>();
				await ReadHeadersAsync(num, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case null:
				break;
			}
			break;
			IL_00d6:
			if (num != 0L)
			{
				_responseDataPayloadRemaining = num;
				return true;
			}
		}
		CopyTrailersToResponseMessage(response);
		_responseDataPayloadRemaining = -1L;
		return false;
	}

	public void Trace(string message, [CallerMemberName] string memberName = null)
	{
		_connection.Trace(StreamId, message, memberName);
	}

	private void AbortStream()
	{
		if (_requestContentLengthRemaining != 0L)
		{
			_stream.Abort(QuicAbortDirection.Write, 268L);
		}
		if (_responseDataPayloadRemaining != -1)
		{
			_stream.Abort(QuicAbortDirection.Read, 268L);
		}
	}
}
