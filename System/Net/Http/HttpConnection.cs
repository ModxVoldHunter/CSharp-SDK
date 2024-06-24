using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnection : HttpConnectionBase, IDisposable
{
	private sealed class ChunkedEncodingReadStream : HttpContentReadStream
	{
		private enum ParsingState : byte
		{
			ExpectChunkHeader,
			ExpectChunkData,
			ExpectChunkTerminator,
			ConsumeTrailers,
			Done
		}

		private ulong _chunkBytesRemaining;

		private ParsingState _state;

		private readonly HttpResponseMessage _response;

		public override bool NeedsDrain => base.CanReadFromConnection;

		public ChunkedEncodingReadStream(HttpConnection connection, HttpResponseMessage response)
			: base(connection)
		{
			_response = response;
		}

		public override int Read(Span<byte> buffer)
		{
			if (_connection == null)
			{
				return 0;
			}
			if (buffer.Length == 0)
			{
				if (PeekChunkFromConnectionBuffer())
				{
					return 0;
				}
			}
			else
			{
				int num = ReadChunksFromConnectionBuffer(buffer, default(CancellationTokenRegistration));
				if (num > 0)
				{
					return num;
				}
			}
			int num3;
			while (true)
			{
				if (_connection == null)
				{
					return 0;
				}
				if (_state == ParsingState.ExpectChunkData && buffer.Length >= _connection.ReadBufferSize && _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
				{
					int num2 = _connection.Read(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining)));
					if (num2 == 0)
					{
						throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
					}
					_chunkBytesRemaining -= (ulong)num2;
					if (_chunkBytesRemaining == 0L)
					{
						_state = ParsingState.ExpectChunkTerminator;
					}
					return num2;
				}
				if (buffer.Length == 0)
				{
					_connection.Read(buffer);
				}
				Fill();
				if (buffer.Length == 0)
				{
					if (PeekChunkFromConnectionBuffer())
					{
						return 0;
					}
					continue;
				}
				num3 = ReadChunksFromConnectionBuffer(buffer, default(CancellationTokenRegistration));
				if (num3 > 0)
				{
					break;
				}
			}
			return num3;
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return ValueTask.FromCanceled<int>(cancellationToken);
			}
			if (_connection == null)
			{
				return new ValueTask<int>(0);
			}
			if (buffer.Length == 0)
			{
				if (PeekChunkFromConnectionBuffer())
				{
					return new ValueTask<int>(0);
				}
			}
			else
			{
				int num = ReadChunksFromConnectionBuffer(buffer.Span, default(CancellationTokenRegistration));
				if (num > 0)
				{
					return new ValueTask<int>(num);
				}
			}
			if (_connection == null)
			{
				return new ValueTask<int>(0);
			}
			return ReadAsyncCore(buffer, cancellationToken);
		}

		private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				int num2;
				while (true)
				{
					if (_connection == null)
					{
						return 0;
					}
					if (_state == ParsingState.ExpectChunkData && buffer.Length >= _connection.ReadBufferSize && _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
					{
						int num = await _connection.ReadAsync(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining))).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
						}
						_chunkBytesRemaining -= (ulong)num;
						if (_chunkBytesRemaining == 0L)
						{
							_state = ParsingState.ExpectChunkTerminator;
						}
						return num;
					}
					if (buffer.Length == 0)
					{
						await _connection.ReadAsync(buffer).ConfigureAwait(continueOnCapturedContext: false);
					}
					await FillAsync().ConfigureAwait(continueOnCapturedContext: false);
					if (buffer.Length == 0)
					{
						if (PeekChunkFromConnectionBuffer())
						{
							return 0;
						}
						continue;
					}
					num2 = ReadChunksFromConnectionBuffer(buffer.Span, ctr);
					if (num2 > 0)
					{
						break;
					}
				}
				return num2;
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (!cancellationToken.IsCancellationRequested)
			{
				if (_connection != null)
				{
					return CopyToAsyncCore(destination, cancellationToken);
				}
				return Task.CompletedTask;
			}
			return Task.FromCanceled(cancellationToken);
		}

		private async Task CopyToAsyncCore(Stream destination, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				while (true)
				{
					ReadOnlyMemory<byte>? readOnlyMemory = ReadChunkFromConnectionBuffer(int.MaxValue, ctr);
					if (readOnlyMemory.HasValue)
					{
						ReadOnlyMemory<byte> valueOrDefault = readOnlyMemory.GetValueOrDefault();
						if (valueOrDefault.Length != 0)
						{
							await destination.WriteAsync(valueOrDefault, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
							continue;
						}
					}
					if (_connection == null)
					{
						break;
					}
					await FillAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}

		private bool PeekChunkFromConnectionBuffer()
		{
			return ReadChunkFromConnectionBuffer(0, default(CancellationTokenRegistration)).HasValue;
		}

		private int ReadChunksFromConnectionBuffer(Span<byte> buffer, CancellationTokenRegistration cancellationRegistration)
		{
			int num = 0;
			while (buffer.Length > 0)
			{
				ReadOnlyMemory<byte>? readOnlyMemory = ReadChunkFromConnectionBuffer(buffer.Length, cancellationRegistration);
				if (!readOnlyMemory.HasValue)
				{
					break;
				}
				ReadOnlyMemory<byte> valueOrDefault = readOnlyMemory.GetValueOrDefault();
				if (valueOrDefault.Length == 0)
				{
					break;
				}
				num += valueOrDefault.Length;
				valueOrDefault.Span.CopyTo(buffer);
				buffer = buffer.Slice(valueOrDefault.Length);
			}
			return num;
		}

		private ReadOnlyMemory<byte>? ReadChunkFromConnectionBuffer(int maxBytesToRead, CancellationTokenRegistration cancellationRegistration)
		{
			try
			{
				ReadOnlySpan<byte> line;
				switch (_state)
				{
				case ParsingState.ExpectChunkHeader:
				{
					if (!_connection.TryReadNextChunkedLine(out line))
					{
						return null;
					}
					if (!Utf8Parser.TryParse(line, out ulong value, out int bytesConsumed, 'X'))
					{
						throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_chunk_header_invalid, BitConverter.ToString(line.ToArray())));
					}
					_chunkBytesRemaining = value;
					if (bytesConsumed != line.Length)
					{
						ValidateChunkExtension(line.Slice(bytesConsumed));
					}
					if (value != 0)
					{
						_state = ParsingState.ExpectChunkData;
						goto case ParsingState.ExpectChunkData;
					}
					_state = ParsingState.ConsumeTrailers;
					goto case ParsingState.ConsumeTrailers;
				}
				case ParsingState.ExpectChunkData:
				{
					ReadOnlyMemory<byte> remainingBuffer = _connection.RemainingBuffer;
					if (remainingBuffer.Length == 0)
					{
						return null;
					}
					int num = Math.Min(maxBytesToRead, (int)Math.Min((ulong)remainingBuffer.Length, _chunkBytesRemaining));
					_connection.ConsumeFromRemainingBuffer(num);
					_chunkBytesRemaining -= (ulong)num;
					if (_chunkBytesRemaining == 0L)
					{
						_state = ParsingState.ExpectChunkTerminator;
					}
					return remainingBuffer.Slice(0, num);
				}
				case ParsingState.ExpectChunkTerminator:
					if (!_connection.TryReadNextChunkedLine(out line))
					{
						return null;
					}
					if (line.Length != 0)
					{
						throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_chunk_terminator_invalid, Encoding.ASCII.GetString(line)));
					}
					_state = ParsingState.ExpectChunkHeader;
					goto case ParsingState.ExpectChunkHeader;
				case ParsingState.ConsumeTrailers:
					if (_connection.ParseHeaders(base.IsDisposed ? null : _response, isFromTrailer: true))
					{
						cancellationRegistration.Dispose();
						CancellationHelper.ThrowIfCancellationRequested(cancellationRegistration.Token);
						_state = ParsingState.Done;
						_connection.CompleteResponse();
						_connection = null;
					}
					return null;
				default:
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, $"Unexpected state: {_state}", "ReadChunkFromConnectionBuffer");
					}
					return null;
				}
			}
			catch (Exception)
			{
				_connection.Dispose();
				_connection = null;
				throw;
			}
		}

		private static void ValidateChunkExtension(ReadOnlySpan<byte> lineAfterChunkSize)
		{
			for (int i = 0; i < lineAfterChunkSize.Length; i++)
			{
				switch (lineAfterChunkSize[i])
				{
				case 9:
				case 32:
					continue;
				case 59:
					return;
				}
				throw new HttpIOException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_chunk_extension_invalid, BitConverter.ToString(lineAfterChunkSize.ToArray())));
			}
		}

		public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			CancellationTokenSource cts = null;
			CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
			try
			{
				int drainedBytes = 0;
				while (true)
				{
					drainedBytes += _connection.RemainingBuffer.Length;
					ReadOnlyMemory<byte>? readOnlyMemory;
					do
					{
						readOnlyMemory = ReadChunkFromConnectionBuffer(int.MaxValue, ctr);
					}
					while (readOnlyMemory.HasValue && readOnlyMemory.GetValueOrDefault().Length != 0);
					if (_connection == null)
					{
						return true;
					}
					if (drainedBytes >= maxDrainBytes)
					{
						return false;
					}
					if (cts == null)
					{
						TimeSpan maxResponseDrainTime = _connection._pool.Settings._maxResponseDrainTime;
						if (maxResponseDrainTime == TimeSpan.Zero)
						{
							break;
						}
						if (maxResponseDrainTime != Timeout.InfiniteTimeSpan)
						{
							cts = new CancellationTokenSource((int)maxResponseDrainTime.TotalMilliseconds);
							ctr = cts.Token.Register(delegate(object s)
							{
								((HttpConnection)s).Dispose();
							}, _connection);
						}
					}
					await FillAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				return false;
			}
			finally
			{
				ctr.Dispose();
				cts?.Dispose();
			}
		}

		private void Fill()
		{
			((_state == ParsingState.ConsumeTrailers) ? _connection.FillForHeadersAsync(async: false) : _connection.FillAsync(async: false)).GetAwaiter().GetResult();
		}

		private ValueTask FillAsync()
		{
			if (_state != ParsingState.ConsumeTrailers)
			{
				return _connection.FillAsync(async: true);
			}
			return _connection.FillForHeadersAsync(async: true);
		}
	}

	private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
	{
		private static readonly byte[] s_crlfBytes = "\r\n"u8.ToArray();

		private static readonly byte[] s_finalChunkBytes = "0\r\n\r\n"u8.ToArray();

		public ChunkedEncodingWriteStream(HttpConnection connection)
			: base(connection)
		{
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			if (buffer.Length == 0)
			{
				connectionOrThrow.Flush();
				return;
			}
			connectionOrThrow.WriteHexInt32Async(buffer.Length, async: false).GetAwaiter().GetResult();
			connectionOrThrow.Write(s_crlfBytes);
			connectionOrThrow.Write(buffer);
			connectionOrThrow.Write(s_crlfBytes);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored)
		{
			base.BytesWritten += buffer.Length;
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			return (buffer.Length == 0) ? connectionOrThrow.FlushAsync(async: true) : WriteChunkAsync(connectionOrThrow, buffer);
			static async ValueTask WriteChunkAsync(HttpConnection connection, ReadOnlyMemory<byte> buffer)
			{
				await connection.WriteHexInt32Async(buffer.Length, async: true).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteAsync(s_crlfBytes).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteAsync(buffer).ConfigureAwait(continueOnCapturedContext: false);
				await connection.WriteAsync(s_crlfBytes).ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		public override Task FinishAsync(bool async)
		{
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			_connection = null;
			if (async)
			{
				return connectionOrThrow.WriteAsync(s_finalChunkBytes).AsTask();
			}
			connectionOrThrow.Write(s_finalChunkBytes);
			return Task.CompletedTask;
		}
	}

	private sealed class ConnectionCloseReadStream : HttpContentReadStream
	{
		public ConnectionCloseReadStream(HttpConnection connection)
			: base(connection)
		{
		}

		public override int Read(Span<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return 0;
			}
			int num = connection.Read(buffer);
			if (num == 0 && buffer.Length != 0)
			{
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return 0;
			}
			ValueTask<int> valueTask = connection.ReadAsync(buffer);
			int num;
			if (valueTask.IsCompletedSuccessfully)
			{
				num = valueTask.Result;
			}
			else
			{
				CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
				try
				{
					num = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
				{
					throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
				}
				finally
				{
					ctr.Dispose();
				}
			}
			if (num == 0 && buffer.Length != 0)
			{
				CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = connection.CopyToUntilEofAsync(destination, async: true, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish(connection);
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, connection, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Finish(connection);
		}

		private void Finish(HttpConnection connection)
		{
			_connection = null;
			connection.Dispose();
		}
	}

	private sealed class ContentLengthReadStream : HttpContentReadStream
	{
		private ulong _contentBytesRemaining;

		public override bool NeedsDrain => base.CanReadFromConnection;

		public ContentLengthReadStream(HttpConnection connection, ulong contentLength)
			: base(connection)
		{
			_contentBytesRemaining = contentLength;
		}

		public override int Read(Span<byte> buffer)
		{
			if (_connection == null)
			{
				return 0;
			}
			if ((ulong)buffer.Length > _contentBytesRemaining)
			{
				buffer = buffer.Slice(0, (int)_contentBytesRemaining);
			}
			int num = _connection.Read(buffer);
			if (num <= 0 && buffer.Length != 0)
			{
				throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
			}
			_contentBytesRemaining -= (ulong)num;
			if (_contentBytesRemaining == 0L)
			{
				_connection.CompleteResponse();
				_connection = null;
			}
			return num;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			if (_connection == null)
			{
				return 0;
			}
			if ((ulong)buffer.Length > _contentBytesRemaining)
			{
				buffer = buffer.Slice(0, (int)_contentBytesRemaining);
			}
			ValueTask<int> valueTask = _connection.ReadAsync(buffer);
			int num;
			if (valueTask.IsCompletedSuccessfully)
			{
				num = valueTask.Result;
			}
			else
			{
				CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
				try
				{
					num = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
				{
					throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
				}
				finally
				{
					ctr.Dispose();
				}
			}
			if (num == 0 && buffer.Length != 0)
			{
				CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
				throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.Format(System.SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
			}
			_contentBytesRemaining -= (ulong)num;
			if (_contentBytesRemaining == 0L)
			{
				_connection.CompleteResponse();
				_connection = null;
			}
			return num;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			if (_connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = _connection.CopyToContentLengthAsync(destination, async: true, _contentBytesRemaining, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish();
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			Finish();
		}

		private void Finish()
		{
			_contentBytesRemaining = 0uL;
			_connection.CompleteResponse();
			_connection = null;
		}

		private ReadOnlyMemory<byte> ReadFromConnectionBuffer(int maxBytesToRead)
		{
			ReadOnlyMemory<byte> remainingBuffer = _connection.RemainingBuffer;
			if (remainingBuffer.Length == 0)
			{
				return default(ReadOnlyMemory<byte>);
			}
			int num = Math.Min(maxBytesToRead, (int)Math.Min((ulong)remainingBuffer.Length, _contentBytesRemaining));
			_connection.ConsumeFromRemainingBuffer(num);
			_contentBytesRemaining -= (ulong)num;
			return remainingBuffer.Slice(0, num);
		}

		public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			ReadFromConnectionBuffer(int.MaxValue);
			if (_contentBytesRemaining == 0L)
			{
				Finish();
				return true;
			}
			if (_contentBytesRemaining > (ulong)maxDrainBytes)
			{
				return false;
			}
			CancellationTokenSource cts = null;
			CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
			TimeSpan maxResponseDrainTime = _connection._pool.Settings._maxResponseDrainTime;
			if (maxResponseDrainTime == TimeSpan.Zero)
			{
				return false;
			}
			if (maxResponseDrainTime != Timeout.InfiniteTimeSpan)
			{
				cts = new CancellationTokenSource((int)maxResponseDrainTime.TotalMilliseconds);
				ctr = cts.Token.Register(delegate(object s)
				{
					((HttpConnection)s).Dispose();
				}, _connection);
			}
			try
			{
				do
				{
					await _connection.FillAsync(async: true).ConfigureAwait(continueOnCapturedContext: false);
					ReadFromConnectionBuffer(int.MaxValue);
				}
				while (_contentBytesRemaining != 0L);
				ctr.Dispose();
				CancellationHelper.ThrowIfCancellationRequested(ctr.Token);
				Finish();
				return true;
			}
			finally
			{
				ctr.Dispose();
				cts?.Dispose();
			}
		}
	}

	private sealed class ContentLengthWriteStream : HttpContentWriteStream
	{
		private readonly long _contentLength;

		public ContentLengthWriteStream(HttpConnection connection, long contentLength)
			: base(connection)
		{
			_contentLength = contentLength;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			base.BytesWritten += buffer.Length;
			if (base.BytesWritten > _contentLength)
			{
				throw new HttpRequestException(System.SR.net_http_content_write_larger_than_content_length);
			}
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			connectionOrThrow.Write(buffer);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored)
		{
			base.BytesWritten += buffer.Length;
			if (base.BytesWritten > _contentLength)
			{
				return ValueTask.FromException(new HttpRequestException(System.SR.net_http_content_write_larger_than_content_length));
			}
			HttpConnection connectionOrThrow = GetConnectionOrThrow();
			return connectionOrThrow.WriteAsync(buffer);
		}

		public override Task FinishAsync(bool async)
		{
			if (base.BytesWritten != _contentLength)
			{
				return Task.FromException(new HttpRequestException(System.SR.Format(System.SR.net_http_request_content_length_mismatch, base.BytesWritten, _contentLength)));
			}
			_connection = null;
			return Task.CompletedTask;
		}
	}

	internal abstract class HttpContentReadStream : HttpContentStream
	{
		private int _disposed;

		public sealed override bool CanRead => _disposed == 0;

		public sealed override bool CanWrite => false;

		public virtual bool NeedsDrain => false;

		protected bool IsDisposed => _disposed == 1;

		protected bool CanReadFromConnection
		{
			get
			{
				HttpConnection connection = _connection;
				if (connection != null)
				{
					return connection._disposed != 1;
				}
				return false;
			}
		}

		public HttpContentReadStream(HttpConnection connection)
			: base(connection)
		{
		}

		public sealed override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotSupportedException(System.SR.net_http_content_readonly_stream);
		}

		public sealed override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public virtual ValueTask<bool> DrainAsync(int maxDrainBytes)
		{
			return new ValueTask<bool>(result: false);
		}

		protected override void Dispose(bool disposing)
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				if (disposing && NeedsDrain)
				{
					DrainOnDisposeAsync();
				}
				else
				{
					base.Dispose(disposing);
				}
			}
		}

		private async Task DrainOnDisposeAsync()
		{
			HttpConnection connection = _connection;
			try
			{
				bool flag = await DrainAsync(connection._pool.Settings._maxResponseDrainSize).ConfigureAwait(continueOnCapturedContext: false);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace(flag ? "Connection drain succeeded" : $"Connection drain failed when MaxResponseDrainSize={connection._pool.Settings._maxResponseDrainSize} bytes or MaxResponseDrainTime=={connection._pool.Settings._maxResponseDrainTime} exceeded", "DrainOnDisposeAsync");
				}
			}
			catch (Exception value)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace($"Connection drain failed due to exception: {value}", "DrainOnDisposeAsync");
				}
			}
			base.Dispose(disposing: true);
		}
	}

	private abstract class HttpContentWriteStream : HttpContentStream
	{
		public long BytesWritten { get; protected set; }

		public sealed override bool CanRead => false;

		public sealed override bool CanWrite => _connection != null;

		public HttpContentWriteStream(HttpConnection connection)
			: base(connection)
		{
		}

		public sealed override void Flush()
		{
			_connection?.Flush();
		}

		public sealed override Task FlushAsync(CancellationToken ignored)
		{
			return _connection?.FlushAsync(async: true).AsTask();
		}

		public sealed override int Read(Span<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public sealed override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public sealed override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public abstract Task FinishAsync(bool async);
	}

	private sealed class RawConnectionStream : HttpContentStream
	{
		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _003CReadAsync_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public PoolingAsyncValueTaskMethodBuilder<int> _003C_003Et__builder;

			public CancellationToken cancellationToken;

			public RawConnectionStream _003C_003E4__this;

			public Memory<byte> buffer;

			private HttpConnection _003Cconnection_003E5__2;

			private CancellationTokenRegistration _003Cctr_003E5__3;

			private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _003C_003Eu__1;

			private void MoveNext()
			{
				int num = _003C_003E1__state;
				RawConnectionStream rawConnectionStream = _003C_003E4__this;
				int result;
				try
				{
					if (num == 0)
					{
						goto IL_0078;
					}
					CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
					_003Cconnection_003E5__2 = rawConnectionStream._connection;
					Unsafe.SkipInit(out ValueTask<int> valueTask);
					int num2;
					if (_003Cconnection_003E5__2 != null)
					{
						valueTask = _003Cconnection_003E5__2.ReadBufferedAsync(buffer);
						if (valueTask.IsCompletedSuccessfully)
						{
							num2 = valueTask.Result;
							goto IL_0134;
						}
						_003Cctr_003E5__3 = _003Cconnection_003E5__2.RegisterCancellation(cancellationToken);
						goto IL_0078;
					}
					result = 0;
					goto end_IL_000e;
					IL_0134:
					if (num2 == 0 && buffer.Length != 0)
					{
						CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
						rawConnectionStream._connection = null;
						_003Cconnection_003E5__2.Dispose();
					}
					result = num2;
					goto end_IL_000e;
					IL_0078:
					try
					{
						ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter;
						if (num != 0)
						{
							awaiter = valueTask.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
							if (!awaiter.IsCompleted)
							{
								num = (_003C_003E1__state = 0);
								_003C_003Eu__1 = awaiter;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
								return;
							}
						}
						else
						{
							awaiter = _003C_003Eu__1;
							_003C_003Eu__1 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
						}
						int result2 = awaiter.GetResult();
						num2 = result2;
					}
					catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
					{
						throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
					}
					finally
					{
						if (num < 0)
						{
							_003Cctr_003E5__3.Dispose();
						}
					}
					_003Cctr_003E5__3 = default(CancellationTokenRegistration);
					goto IL_0134;
					end_IL_000e:;
				}
				catch (Exception exception)
				{
					_003C_003E1__state = -2;
					_003Cconnection_003E5__2 = null;
					_003C_003Et__builder.SetException(exception);
					return;
				}
				_003C_003E1__state = -2;
				_003Cconnection_003E5__2 = null;
				_003C_003Et__builder.SetResult(result);
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_003C_003Et__builder.SetStateMachine(stateMachine);
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		public sealed override bool CanRead => _connection != null;

		public sealed override bool CanWrite => _connection != null;

		public RawConnectionStream(HttpConnection connection)
			: base(connection)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, null, ".ctor");
			}
		}

		public override int Read(Span<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return 0;
			}
			int num = connection.ReadBuffered(buffer);
			if (num == 0 && buffer.Length != 0)
			{
				_connection = null;
				connection.Dispose();
			}
			return num;
		}

		[AsyncStateMachine(typeof(_003CReadAsync_003Ed__6))]
		[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			Unsafe.SkipInit(out _003CReadAsync_003Ed__6 stateMachine);
			stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder<int>.Create();
			stateMachine._003C_003E4__this = this;
			stateMachine.buffer = buffer;
			stateMachine.cancellationToken = cancellationToken;
			stateMachine._003C_003E1__state = -1;
			stateMachine._003C_003Et__builder.Start(ref stateMachine);
			return stateMachine._003C_003Et__builder.Task;
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.ValidateCopyToArguments(destination, bufferSize);
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			Task task = connection.CopyToUntilEofAsync(destination, async: true, bufferSize, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				Finish(connection);
				return Task.CompletedTask;
			}
			return CompleteCopyToAsync(task, connection, cancellationToken);
		}

		private async Task CompleteCopyToAsync(Task copyTask, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Finish(connection);
		}

		private void Finish(HttpConnection connection)
		{
			connection.Dispose();
			_connection = null;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			HttpConnection connection = _connection;
			if (connection == null)
			{
				throw new IOException(System.SR.ObjectDisposed_StreamClosed);
			}
			if (buffer.Length != 0)
			{
				connection.WriteWithoutBuffering(buffer);
			}
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return ValueTask.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(System.SR.ObjectDisposed_StreamClosed)));
			}
			if (buffer.Length == 0)
			{
				return default(ValueTask);
			}
			ValueTask valueTask = connection.WriteWithoutBufferingAsync(buffer, async: true);
			if (!valueTask.IsCompleted)
			{
				return new ValueTask(WaitWithConnectionCancellationAsync(valueTask, connection, cancellationToken));
			}
			return valueTask;
		}

		public override void Flush()
		{
			_connection?.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			HttpConnection connection = _connection;
			if (connection == null)
			{
				return Task.CompletedTask;
			}
			ValueTask task = connection.FlushAsync(async: true);
			if (!task.IsCompleted)
			{
				return WaitWithConnectionCancellationAsync(task, connection, cancellationToken);
			}
			return task.AsTask();
		}

		private static async Task WaitWithConnectionCancellationAsync(ValueTask task, HttpConnection connection, CancellationToken cancellationToken)
		{
			CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
			try
			{
				await task.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			finally
			{
				ctr.Dispose();
			}
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CReadBufferedAsyncCore_003Ed__90 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder<int> _003C_003Et__builder;

		public HttpConnection _003C_003E4__this;

		public Memory<byte> destination;

		private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _003C_003Eu__1;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			HttpConnection httpConnection = _003C_003E4__this;
			int result2;
			try
			{
				ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter;
				if (num != 0)
				{
					if (httpConnection._readBuffer.ActiveLength != 0)
					{
						goto IL_0100;
					}
					awaiter = httpConnection._stream.ReadAsync(httpConnection._readBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
				}
				else
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
					num = (_003C_003E1__state = -1);
				}
				int result = awaiter.GetResult();
				int num2 = result;
				httpConnection._readBuffer.Commit(num2);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					httpConnection.Trace($"Received {num2} bytes.", "ReadBufferedAsyncCore");
				}
				goto IL_0100;
				IL_0100:
				result2 = httpConnection.ReadFromBuffer(destination.Span);
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult(result2);
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	private static readonly ulong s_http10Bytes = BitConverter.ToUInt64("HTTP/1.0"u8);

	private static readonly ulong s_http11Bytes = BitConverter.ToUInt64("HTTP/1.1"u8);

	private readonly HttpConnectionPool _pool;

	internal readonly Stream _stream;

	private readonly TransportContext _transportContext;

	private HttpRequestMessage _currentRequest;

	private System.Net.ArrayBuffer _writeBuffer;

	private int _allowedReadLineBytes;

	[ThreadStatic]
	private static string[] t_headerValues;

	private int _readAheadTaskStatus;

	private ValueTask<int> _readAheadTask;

	private System.Net.ArrayBuffer _readBuffer;

	private int _keepAliveTimeoutSeconds;

	private bool _inUse;

	private bool _detachedFromPool;

	private bool _canRetry;

	private bool _connectionClose;

	private int _disposed;

	private bool ReadAheadTaskHasStarted => _readAheadTaskStatus != 0;

	public TransportContext TransportContext => _transportContext;

	public HttpConnectionKind Kind => _pool.Kind;

	private int ReadBufferSize => _readBuffer.Capacity;

	private ReadOnlyMemory<byte> RemainingBuffer => _readBuffer.ActiveMemory;

	public HttpConnection(HttpConnectionPool pool, Stream stream, TransportContext transportContext, IPEndPoint remoteEndPoint)
		: base(pool, remoteEndPoint)
	{
		_pool = pool;
		_stream = stream;
		_transportContext = transportContext;
		_writeBuffer = new System.Net.ArrayBuffer(4096);
		_readBuffer = new System.Net.ArrayBuffer(4096);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			TraceConnection(_stream);
		}
	}

	~HttpConnection()
	{
		Dispose(disposing: false);
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
	}

	private void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 1)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Connection closing.", "Dispose");
			}
			MarkConnectionAsClosed();
			if (!_detachedFromPool)
			{
				_pool.InvalidateHttp11Connection(this, disposing);
			}
			if (disposing)
			{
				GC.SuppressFinalize(this);
				_stream.Dispose();
			}
		}
	}

	public bool PrepareForReuse(bool async)
	{
		if (CheckKeepAliveTimeoutExceeded())
		{
			return false;
		}
		if (ReadAheadTaskHasStarted)
		{
			return TryOwnReadAheadTaskCompletion();
		}
		if (!async && _stream is NetworkStream networkStream)
		{
			try
			{
				return !networkStream.Socket.Poll(0, SelectMode.SelectRead);
			}
			catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
			{
				return false;
			}
		}
		_readAheadTaskStatus = 2;
		try
		{
			_readAheadTask = _stream.ReadAsync(_readBuffer.AvailableMemory);
			return !_readAheadTask.IsCompleted;
		}
		catch (Exception value)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Error performing read ahead: {value}", "PrepareForReuse");
			}
			return false;
		}
	}

	public override bool CheckUsabilityOnScavenge()
	{
		if (CheckKeepAliveTimeoutExceeded())
		{
			return false;
		}
		EnsureReadAheadTaskHasStarted();
		return !_readAheadTask.IsCompleted;
	}

	private bool TryOwnReadAheadTaskCompletion()
	{
		return Interlocked.CompareExchange(ref _readAheadTaskStatus, 2, 1) == 1;
	}

	private void EnsureReadAheadTaskHasStarted()
	{
		if (_readAheadTaskStatus == 0)
		{
			_readAheadTaskStatus = 1;
			_readAheadTask = ReadAheadWithZeroByteReadAsync();
		}
		async ValueTask<int> ReadAheadWithZeroByteReadAsync()
		{
			_ = 1;
			try
			{
				await _stream.ReadAsync(Memory<byte>.Empty).ConfigureAwait(continueOnCapturedContext: false);
				int result = await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false);
				if (TryOwnReadAheadTaskCompletion() && System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Read-ahead task observed data before the request was sent.", "EnsureReadAheadTaskHasStarted");
				}
				return result;
			}
			catch (Exception value) when (TryOwnReadAheadTaskCompletion())
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Error performing read ahead: {value}", "EnsureReadAheadTaskHasStarted");
				}
				return 0;
			}
		}
	}

	private bool CheckKeepAliveTimeoutExceeded()
	{
		if (_keepAliveTimeoutSeconds != 0)
		{
			return GetIdleTicks(Environment.TickCount64) >= _keepAliveTimeoutSeconds * 1000;
		}
		return false;
	}

	private void ConsumeFromRemainingBuffer(int bytesToConsume)
	{
		_readBuffer.Discard(bytesToConsume);
	}

	private void WriteHeaders(HttpRequestMessage request, HttpMethod normalizedMethod)
	{
		WriteAsciiString(normalizedMethod.Method);
		_writeBuffer.EnsureAvailableSpace(1);
		_writeBuffer.AvailableSpan[0] = 32;
		_writeBuffer.Commit(1);
		if ((object)normalizedMethod == HttpMethod.Connect)
		{
			if (request.HasHeaders)
			{
				string host = request.Headers.Host;
				if (host != null)
				{
					WriteAsciiString(host);
					goto IL_00a8;
				}
			}
			throw new HttpRequestException(System.SR.net_http_request_no_host);
		}
		if (Kind == HttpConnectionKind.Proxy)
		{
			WriteBytes("http://"u8);
			WriteHost(request.RequestUri);
		}
		WriteAsciiString(request.RequestUri.PathAndQuery);
		goto IL_00a8;
		IL_00a8:
		bool flag = request.Version.Minor == 0 && request.Version.Major == 1;
		WriteBytes(flag ? " HTTP/1.0\r\n"u8 : " HTTP/1.1\r\n"u8);
		if (!request.HasHeaders || request.Headers.Host == null)
		{
			byte[] hostHeaderLineBytes = _pool.HostHeaderLineBytes;
			if (hostHeaderLineBytes != null)
			{
				WriteBytes(hostHeaderLineBytes);
			}
			else
			{
				WriteBytes(KnownHeaders.Host.AsciiBytesWithColonSpace);
				WriteHost(request.RequestUri);
				WriteCRLF();
			}
		}
		string text = null;
		if (_pool.Settings._useCookies)
		{
			text = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
			if (text == "")
			{
				text = null;
			}
		}
		if (request.HasHeaders || text != null)
		{
			WriteHeaderCollection(request.Headers, text);
		}
		HttpContent content = request.Content;
		if (content != null)
		{
			WriteHeaderCollection(content.Headers);
		}
		else if (normalizedMethod.MustHaveRequestBody)
		{
			WriteBytes("Content-Length: 0\r\n"u8);
		}
		WriteCRLF();
		void WriteHost(Uri requestUri)
		{
			string s = ((requestUri.HostNameType == UriHostNameType.IPv6) ? requestUri.Host : requestUri.IdnHost);
			WriteAsciiString(s);
			if (!requestUri.IsDefaultPort)
			{
				_writeBuffer.EnsureAvailableSpace(6);
				Span<byte> availableSpan = _writeBuffer.AvailableSpan;
				availableSpan[0] = 58;
				int bytesWritten;
				bool flag2 = ((uint)requestUri.Port).TryFormat(availableSpan.Slice(1), out bytesWritten);
				_writeBuffer.Commit(bytesWritten + 1);
			}
		}
	}

	private void WriteHeaderCollection(HttpHeaders headers, string cookiesFromContainer = null)
	{
		HeaderEncodingSelector<HttpRequestMessage> requestHeaderEncodingSelector = _pool.Settings._requestHeaderEncodingSelector;
		ReadOnlySpan<HeaderEntry> entries = headers.GetEntries();
		for (int i = 0; i < entries.Length; i++)
		{
			HeaderEntry headerEntry = entries[i];
			KnownHeader knownHeader = headerEntry.Key.KnownHeader;
			if (knownHeader != null)
			{
				WriteBytes(knownHeader.AsciiBytesWithColonSpace);
			}
			else
			{
				WriteAsciiString(headerEntry.Key.Name);
				WriteBytes(": "u8);
			}
			int storeValuesIntoStringArray = HttpHeaders.GetStoreValuesIntoStringArray(headerEntry.Key, headerEntry.Value, ref t_headerValues);
			Encoding encoding = requestHeaderEncodingSelector?.Invoke(headerEntry.Key.Name, _currentRequest);
			WriteString(t_headerValues[0], encoding);
			if (cookiesFromContainer != null && headerEntry.Key.Equals(KnownHeaders.Cookie))
			{
				WriteBytes("; "u8);
				WriteString(cookiesFromContainer, encoding);
				cookiesFromContainer = null;
			}
			if (storeValuesIntoStringArray > 1)
			{
				HttpHeaderParser parser = headerEntry.Key.Parser;
				string s = ", ";
				if (parser != null && parser.SupportsMultipleValues)
				{
					s = parser.Separator;
				}
				for (int j = 1; j < storeValuesIntoStringArray; j++)
				{
					WriteAsciiString(s);
					WriteString(t_headerValues[j], encoding);
				}
			}
			WriteCRLF();
		}
		if (cookiesFromContainer != null)
		{
			WriteBytes(KnownHeaders.Cookie.AsciiBytesWithColonSpace);
			WriteString(cookiesFromContainer, requestHeaderEncodingSelector?.Invoke("Cookie", _currentRequest));
			WriteCRLF();
		}
	}

	private void WriteCRLF()
	{
		_writeBuffer.EnsureAvailableSpace(2);
		Span<byte> availableSpan = _writeBuffer.AvailableSpan;
		availableSpan[1] = 10;
		availableSpan[0] = 13;
		_writeBuffer.Commit(2);
	}

	private void WriteBytes(ReadOnlySpan<byte> bytes)
	{
		_writeBuffer.EnsureAvailableSpace(bytes.Length);
		bytes.CopyTo(_writeBuffer.AvailableSpan);
		_writeBuffer.Commit(bytes.Length);
	}

	private void WriteAsciiString(string s)
	{
		_writeBuffer.EnsureAvailableSpace(s.Length);
		int bytes = Encoding.ASCII.GetBytes(s, _writeBuffer.AvailableSpan);
		_writeBuffer.Commit(bytes);
	}

	private void WriteString(string s, Encoding encoding)
	{
		if (encoding == null)
		{
			_writeBuffer.EnsureAvailableSpace(s.Length);
			Span<byte> availableSpan = _writeBuffer.AvailableSpan;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (!char.IsAscii(c))
				{
					ThrowForInvalidCharEncoding();
				}
				availableSpan[i] = (byte)c;
			}
			_writeBuffer.Commit(s.Length);
		}
		else
		{
			_writeBuffer.EnsureAvailableSpace(encoding.GetMaxByteCount(s.Length));
			int bytes = encoding.GetBytes(s, _writeBuffer.AvailableSpan);
			_writeBuffer.Commit(bytes);
		}
		static void ThrowForInvalidCharEncoding()
		{
			throw new HttpRequestException(System.SR.net_http_request_invalid_char_encoding);
		}
	}

	public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		MarkConnectionAsNotIdle();
		TaskCompletionSource<bool> allowExpect100ToContinue = null;
		Task sendRequestContentTask = null;
		_currentRequest = request;
		HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);
		_canRetry = false;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Sending request: {request}", "SendAsync");
		}
		CancellationTokenRegistration cancellationRegistration = RegisterCancellation(cancellationToken);
		Unsafe.SkipInit(out HttpResponseMessage result);
		try
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStart(base.Id);
			}
			WriteHeaders(request, normalizedMethod);
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.RequestHeadersStop();
			}
			if (request.Content == null)
			{
				await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				bool flag = request.HasHeaders && request.Headers.ExpectContinue == true;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Request content is not null, start processing it. hasExpectContinueHeader = {flag}", "SendAsync");
				}
				if (!flag)
				{
					await SendRequestContentAsync(request, CreateRequestContentStream(request), async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
					allowExpect100ToContinue = new TaskCompletionSource<bool>();
					Timer expect100Timer = new Timer(delegate(object s)
					{
						((TaskCompletionSource<bool>)s).TrySetResult(result: true);
					}, allowExpect100ToContinue, _pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
					sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(request, allowExpect100ToContinue.Task, CreateRequestContentStream(request), expect100Timer, async, cancellationToken);
				}
			}
			_allowedReadLineBytes = _pool.Settings.MaxResponseHeadersByteLength;
			if (!ReadAheadTaskHasStarted)
			{
				await InitialFillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				ValueTask<int> readAheadTask = _readAheadTask;
				_readAheadTask = default(ValueTask<int>);
				int num;
				if (readAheadTask.IsCompleted)
				{
					num = readAheadTask.Result;
				}
				else
				{
					if (System.Net.NetEventSource.Log.IsEnabled() && !async)
					{
						Trace("Pre-emptive read completed asynchronously for a synchronous request.", "SendAsync");
					}
					num = await readAheadTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				_readBuffer.Commit(num);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Received {num} bytes.", "SendAsync");
				}
				_readAheadTaskStatus = 0;
			}
			if (_readBuffer.ActiveLength == 0)
			{
				if (request.Content == null || allowExpect100ToContinue != null)
				{
					_canRetry = true;
				}
				throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.net_http_invalid_response_premature_eof);
			}
			HttpResponseMessage response = new HttpResponseMessage
			{
				RequestMessage = request,
				Content = new HttpConnectionResponseContent()
			};
			while (!ParseStatusLine(response))
			{
				await FillForHeadersAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.ResponseHeadersStart();
			}
			while ((uint)(response.StatusCode - 100) <= 99u)
			{
				if (allowExpect100ToContinue != null && response.StatusCode == HttpStatusCode.Continue)
				{
					allowExpect100ToContinue.TrySetResult(result: true);
					allowExpect100ToContinue = null;
				}
				else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
				{
					break;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Current {response.StatusCode} response is an interim response or not expected, need to read for a final response.", "SendAsync");
				}
				while (!ParseHeaders(null, isFromTrailer: false))
				{
					await FillForHeadersAsync(async).ConfigureAwait(continueOnCapturedContext: false);
				}
				while (!ParseStatusLine(response))
				{
					await FillForHeadersAsync(async).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			while (!ParseHeaders(response, isFromTrailer: false))
			{
				await FillForHeadersAsync(async).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.ResponseHeadersStop((int)response.StatusCode);
			}
			if (allowExpect100ToContinue != null)
			{
				if (response.StatusCode >= HttpStatusCode.MultipleChoices && request.Content != null && (!request.Content.Headers.ContentLength.HasValue || request.Content.Headers.ContentLength.GetValueOrDefault() > 1024) && !AuthenticationHelper.IsSessionAuthenticationChallenge(response))
				{
					allowExpect100ToContinue.TrySetResult(result: false);
					if (!allowExpect100ToContinue.Task.Result)
					{
						_connectionClose = true;
					}
				}
				else
				{
					allowExpect100ToContinue.TrySetResult(result: true);
				}
			}
			if (response.Headers.ConnectionClose.GetValueOrDefault())
			{
				_connectionClose = true;
			}
			if (sendRequestContentTask != null)
			{
				Task task = sendRequestContentTask;
				sendRequestContentTask = null;
				await task.ConfigureAwait(continueOnCapturedContext: false);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Request is fully sent.", "SendAsync");
			}
			cancellationRegistration.Dispose();
			CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
			Stream stream;
			if ((object)normalizedMethod == HttpMethod.Head || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotModified)
			{
				stream = EmptyReadStream.Instance;
				CompleteResponse();
			}
			else if ((object)normalizedMethod == HttpMethod.Connect && response.StatusCode == HttpStatusCode.OK)
			{
				stream = new RawConnectionStream(this);
				_connectionClose = true;
				_pool.InvalidateHttp11Connection(this);
				_detachedFromPool = true;
			}
			else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
			{
				stream = new RawConnectionStream(this);
				_connectionClose = true;
				_pool.InvalidateHttp11Connection(this);
				_detachedFromPool = true;
			}
			else if (response.Headers.TransferEncodingChunked == true)
			{
				stream = new ChunkedEncodingReadStream(this, response);
			}
			else if (response.Content.Headers.ContentLength.HasValue)
			{
				long valueOrDefault = response.Content.Headers.ContentLength.GetValueOrDefault();
				if (valueOrDefault <= 0)
				{
					stream = EmptyReadStream.Instance;
					CompleteResponse();
				}
				else
				{
					stream = new ContentLengthReadStream(this, (ulong)valueOrDefault);
				}
			}
			else
			{
				stream = new ConnectionCloseReadStream(this);
			}
			((HttpConnectionResponseContent)response.Content).SetStream(stream);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received response: {response}", "SendAsync");
			}
			if (_pool.Settings._useCookies)
			{
				CookieHelper.ProcessReceivedCookies(response, _pool.Settings._cookieContainer);
			}
			result = response;
			return result;
		}
		catch (Exception ex)
		{
			Exception error = ex;
			cancellationRegistration.Dispose();
			if (allowExpect100ToContinue != null && !allowExpect100ToContinue.TrySetResult(result: false))
			{
				_canRetry = false;
			}
			if (_readAheadTask != default(ValueTask<int>))
			{
				LogExceptions(_readAheadTask.AsTask());
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Error sending request: {error}", "SendAsync");
			}
			if (sendRequestContentTask != null && !sendRequestContentTask.IsCompletedSuccessfully)
			{
				if (Volatile.Read(ref _disposed) == 1)
				{
					Exception mappedException;
					try
					{
						await sendRequestContentTask.ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (Exception exception) when (MapSendException(exception, cancellationToken, out mappedException))
					{
						throw mappedException;
					}
				}
				LogExceptions(sendRequestContentTask);
			}
			Dispose();
			if (MapSendException(error, cancellationToken, out var mappedException2))
			{
				throw mappedException2;
			}
			if (!(ex is Exception source))
			{
				throw ex;
			}
			ExceptionDispatchInfo.Capture(source).Throw();
		}
		return result;
	}

	private bool MapSendException(Exception exception, CancellationToken cancellationToken, out Exception mappedException)
	{
		if (CancellationHelper.ShouldWrapInOperationCanceledException(exception, cancellationToken))
		{
			mappedException = CancellationHelper.CreateOperationCanceledException(exception, cancellationToken);
			return true;
		}
		if (exception is InvalidOperationException)
		{
			mappedException = new HttpRequestException(System.SR.net_http_client_execution_error, exception);
			return true;
		}
		if (exception is IOException ex)
		{
			HttpRequestError httpRequestError = ((ex is HttpIOException ex2) ? ex2.HttpRequestError : HttpRequestError.Unknown);
			mappedException = new HttpRequestException(httpRequestError, System.SR.net_http_client_execution_error, ex, _canRetry ? RequestRetryType.RetryOnConnectionFailure : RequestRetryType.NoRetry);
			return true;
		}
		mappedException = exception;
		return false;
	}

	private HttpContentWriteStream CreateRequestContentStream(HttpRequestMessage request)
	{
		return (request.HasHeaders && request.Headers.TransferEncodingChunked == true) ? ((HttpContentWriteStream)new ChunkedEncodingWriteStream(this)) : ((HttpContentWriteStream)new ContentLengthWriteStream(this, request.Content.Headers.ContentLength.GetValueOrDefault()));
	}

	private CancellationTokenRegistration RegisterCancellation(CancellationToken cancellationToken)
	{
		return cancellationToken.Register(delegate(object s)
		{
			HttpConnection httpConnection = (HttpConnection)s;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				httpConnection.Trace("Cancellation requested. Disposing of the connection.", "RegisterCancellation");
			}
			httpConnection.Dispose();
		}, this);
	}

	private async ValueTask SendRequestContentAsync(HttpRequestMessage request, HttpContentWriteStream stream, bool async, CancellationToken cancellationToken)
	{
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestContentStart();
		}
		if (async)
		{
			await request.Content.CopyToAsync(stream, _transportContext, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			request.Content.CopyTo(stream, _transportContext, cancellationToken);
		}
		await stream.FinishAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestContentStop(stream.BytesWritten);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Finished sending request content.", "SendRequestContentAsync");
		}
	}

	private async Task SendRequestContentWithExpect100ContinueAsync(HttpRequestMessage request, Task<bool> allowExpect100ToContinueTask, HttpContentWriteStream stream, Timer expect100Timer, bool async, CancellationToken cancellationToken)
	{
		bool flag = await allowExpect100ToContinueTask.ConfigureAwait(continueOnCapturedContext: false);
		expect100Timer.Dispose();
		if (flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Sending request content for Expect: 100-continue.", "SendRequestContentWithExpect100ContinueAsync");
			}
			try
			{
				await SendRequestContentAsync(request, stream, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Canceling request content for Expect: 100-continue.", "SendRequestContentWithExpect100ContinueAsync");
		}
	}

	private bool ParseStatusLine(HttpResponseMessage response)
	{
		Span<byte> activeSpan = _readBuffer.ActiveSpan;
		int num = activeSpan.IndexOf<byte>(10);
		if (num >= 0)
		{
			int num2 = num + 1;
			_readBuffer.Discard(num2);
			_allowedReadLineBytes -= num2;
			int num3 = num - 1;
			ParseStatusLineCore(activeSpan[..(((uint)num3 < (uint)activeSpan.Length && activeSpan[num3] == 13) ? num3 : num)], response);
			return true;
		}
		if (_allowedReadLineBytes <= activeSpan.Length)
		{
			ThrowExceededAllowedReadLineBytes();
		}
		return false;
	}

	private static void ParseStatusLineCore(Span<byte> line, HttpResponseMessage response)
	{
		if (line.Length < 12 || line[8] != 32)
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
		}
		ulong num = BitConverter.ToUInt64(line);
		if (num == s_http11Bytes)
		{
			response.SetVersionWithoutValidation(HttpVersion.Version11);
		}
		else if (num == s_http10Bytes)
		{
			response.SetVersionWithoutValidation(HttpVersion.Version10);
		}
		else
		{
			byte b = line[7];
			if (!HttpConnectionBase.IsDigit(b) || !line.StartsWith("HTTP/1."u8))
			{
				throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
			}
			response.SetVersionWithoutValidation(new Version(1, b - 48));
		}
		byte b2 = line[9];
		byte b3 = line[10];
		byte b4 = line[11];
		if (!HttpConnectionBase.IsDigit(b2) || !HttpConnectionBase.IsDigit(b3) || !HttpConnectionBase.IsDigit(b4))
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(line.Slice(9, 3))));
		}
		response.SetStatusCodeWithoutValidation((HttpStatusCode)(100 * (b2 - 48) + 10 * (b3 - 48) + (b4 - 48)));
		if (line.Length == 12)
		{
			response.SetReasonPhraseWithoutValidation(string.Empty);
			return;
		}
		if (line[12] == 32)
		{
			ReadOnlySpan<byte> readOnlySpan = line.Slice(13);
			string text = HttpStatusDescription.Get(response.StatusCode);
			if (text != null && Ascii.Equals(readOnlySpan, text))
			{
				response.SetReasonPhraseWithoutValidation(text);
				return;
			}
			try
			{
				response.ReasonPhrase = HttpRuleParser.DefaultHttpEncoding.GetString(readOnlySpan);
				return;
			}
			catch (FormatException inner)
			{
				throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_reason, Encoding.ASCII.GetString(readOnlySpan.ToArray())), inner);
			}
		}
		throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
	}

	private bool ParseHeaders(HttpResponseMessage response, bool isFromTrailer)
	{
		Span<byte> activeSpan = _readBuffer.ActiveSpan;
		(bool finished, int bytesConsumed) tuple = ParseHeadersCore(activeSpan, response, isFromTrailer);
		bool item = tuple.finished;
		int item2 = tuple.bytesConsumed;
		int num = (item ? item2 : activeSpan.Length);
		if (_allowedReadLineBytes < num)
		{
			ThrowExceededAllowedReadLineBytes();
		}
		_readBuffer.Discard(item2);
		_allowedReadLineBytes -= item2;
		return item;
	}

	private (bool finished, int bytesConsumed) ParseHeadersCore(Span<byte> buffer, HttpResponseMessage response, bool isFromTrailer)
	{
		int length = buffer.Length;
		while (true)
		{
			int num = buffer.IndexOfAny<byte>(58, 10);
			if (num < 0)
			{
				return (finished: false, bytesConsumed: length - buffer.Length);
			}
			if (buffer[num] == 10)
			{
				if ((num == 1 && buffer[0] == 13) || num == 0)
				{
					return (finished: true, bytesConsumed: length - buffer.Length + num + 1);
				}
				ThrowForInvalidHeaderLine(buffer, num);
			}
			int num2 = num + 1;
			if ((uint)num2 >= (uint)buffer.Length)
			{
				break;
			}
			Span<byte> span = buffer.Slice(num2);
			int num5;
			int num6;
			while (true)
			{
				int num3 = span.IndexOf<byte>(10);
				if ((uint)num3 >= (uint)span.Length)
				{
					return (finished: false, bytesConsumed: length - buffer.Length);
				}
				int num4 = num3 - 1;
				num5 = (((uint)num4 < (uint)span.Length && span[num4] == 13) ? num4 : num3);
				num6 = num3 + 1;
				if ((uint)num6 >= (uint)span.Length)
				{
					return (finished: false, bytesConsumed: length - buffer.Length);
				}
				byte b = span[num6];
				if (b != 9 && b != 32)
				{
					break;
				}
				span[num5] = 32;
				span[num3] = 32;
				span = span.Slice(num6 + 1);
			}
			if (response != null)
			{
				ReadOnlySpan<byte> name = buffer.Slice(0, num2 - 1);
				ReadOnlySpan<byte> value = buffer.Slice(num2, buffer.Length - span.Length + num5 - num2);
				AddResponseHeader(name, value, response, isFromTrailer);
			}
			buffer = buffer.Slice(buffer.Length - span.Length + num6);
		}
		return (finished: false, bytesConsumed: length - buffer.Length);
		static void ThrowForInvalidHeaderLine(ReadOnlySpan<byte> buffer, int newLineIndex)
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(buffer.Slice(0, newLineIndex))));
		}
	}

	private void AddResponseHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, HttpResponseMessage response, bool isFromTrailer)
	{
		while (true)
		{
			int num = name.Length - 1;
			if ((uint)num < (uint)name.Length)
			{
				if (name[num] != 32)
				{
					break;
				}
				name = name.Slice(0, num);
			}
			else
			{
				ThrowForEmptyHeaderName();
			}
		}
		while (true)
		{
			bool flag = value.Length != 0;
			bool flag2 = flag;
			if (flag2)
			{
				byte b = value[0];
				bool flag3 = ((b == 9 || b == 32) ? true : false);
				flag2 = flag3;
			}
			if (!flag2)
			{
				break;
			}
			value = value.Slice(1);
		}
		while (true)
		{
			int num2 = value.Length - 1;
			bool flag4 = (uint)num2 >= (uint)value.Length;
			bool flag5 = flag4;
			if (!flag5)
			{
				byte b = value[num2];
				bool flag3 = ((b == 9 || b == 32) ? true : false);
				flag5 = !flag3;
			}
			if (flag5)
			{
				break;
			}
			value = value.Slice(0, num2);
		}
		if (!HeaderDescriptor.TryGet(name, out var descriptor))
		{
			ThrowForInvalidHeaderName(name);
		}
		Encoding valueEncoding = _pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _currentRequest);
		HttpHeaderType headerType = descriptor.HeaderType;
		if ((headerType & HttpHeaderType.Request) != 0)
		{
			descriptor = descriptor.AsCustomHeader();
		}
		string text;
		HttpHeaders httpHeaders;
		if (isFromTrailer)
		{
			if ((headerType & HttpHeaderType.NonTrailing) != 0)
			{
				return;
			}
			text = descriptor.GetHeaderValue(value, valueEncoding);
			httpHeaders = response.TrailingHeaders;
		}
		else if ((headerType & HttpHeaderType.Content) != 0)
		{
			text = descriptor.GetHeaderValue(value, valueEncoding);
			httpHeaders = response.Content.Headers;
		}
		else
		{
			text = GetResponseHeaderValueWithCaching(descriptor, value, valueEncoding);
			httpHeaders = response.Headers;
			if (descriptor.Equals(KnownHeaders.KeepAlive))
			{
				ProcessKeepAliveHeader(text);
			}
		}
		bool flag6 = httpHeaders.TryAddWithoutValidation(descriptor, text);
		static void ThrowForEmptyHeaderName()
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_header_name, ""));
		}
		static void ThrowForInvalidHeaderName(ReadOnlySpan<byte> name)
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
		}
	}

	private void ThrowExceededAllowedReadLineBytes()
	{
		throw new HttpRequestException(HttpRequestError.ConfigurationLimitExceeded, System.SR.Format(System.SR.net_http_response_headers_exceeded_length, _pool.Settings.MaxResponseHeadersByteLength));
	}

	private void ProcessKeepAliveHeader(string keepAlive)
	{
		UnvalidatedObjectCollection<NameValueHeaderValue> unvalidatedObjectCollection = new UnvalidatedObjectCollection<NameValueHeaderValue>();
		if (NameValueHeaderValue.GetNameValueListLength(keepAlive, 0, ',', unvalidatedObjectCollection) != keepAlive.Length)
		{
			return;
		}
		foreach (NameValueHeaderValue item in unvalidatedObjectCollection)
		{
			if (string.Equals(item.Name, "timeout", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrEmpty(item.Value) && HeaderUtilities.TryParseInt32(item.Value, out var result) && result >= 0)
				{
					if (result <= 1)
					{
						_connectionClose = true;
					}
					else
					{
						_keepAliveTimeoutSeconds = result - 1;
					}
				}
			}
			else if (string.Equals(item.Name, "max", StringComparison.OrdinalIgnoreCase) && item.Value == "0")
			{
				_connectionClose = true;
			}
		}
	}

	private void WriteToBuffer(ReadOnlySpan<byte> source)
	{
		source.CopyTo(_writeBuffer.AvailableSpan);
		_writeBuffer.Commit(source.Length);
	}

	private void Write(ReadOnlySpan<byte> source)
	{
		int availableLength = _writeBuffer.AvailableLength;
		if (source.Length <= availableLength)
		{
			WriteToBuffer(source);
			return;
		}
		if (_writeBuffer.ActiveLength != 0)
		{
			WriteToBuffer(source.Slice(0, availableLength));
			source = source.Slice(availableLength);
			Flush();
		}
		if (source.Length >= _writeBuffer.Capacity)
		{
			WriteToStream(source);
		}
		else
		{
			WriteToBuffer(source);
		}
	}

	private ValueTask WriteAsync(ReadOnlyMemory<byte> source)
	{
		int availableLength = _writeBuffer.AvailableLength;
		if (source.Length <= availableLength)
		{
			WriteToBuffer(source.Span);
			return default(ValueTask);
		}
		if (_writeBuffer.ActiveLength != 0)
		{
			WriteToBuffer(source.Span.Slice(0, availableLength));
			source = source.Slice(availableLength);
			ValueTask flushTask2 = FlushAsync(async: true);
			if (!flushTask2.IsCompletedSuccessfully)
			{
				return AwaitFlushAndWriteAsync(flushTask2, source);
			}
			flushTask2.GetAwaiter().GetResult();
			if (source.Length <= _writeBuffer.Capacity)
			{
				WriteToBuffer(source.Span);
				return default(ValueTask);
			}
		}
		return WriteToStreamAsync(source, async: true);
		async ValueTask AwaitFlushAndWriteAsync(ValueTask flushTask, ReadOnlyMemory<byte> source)
		{
			await flushTask.ConfigureAwait(continueOnCapturedContext: false);
			if (source.Length <= _writeBuffer.Capacity)
			{
				WriteToBuffer(source.Span);
			}
			else
			{
				await WriteToStreamAsync(source, async: true).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private void WriteWithoutBuffering(ReadOnlySpan<byte> source)
	{
		if (_writeBuffer.ActiveLength != 0)
		{
			if (source.Length <= _writeBuffer.AvailableLength)
			{
				WriteToBuffer(source);
				Flush();
				return;
			}
			Flush();
		}
		WriteToStream(source);
	}

	private ValueTask WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
	{
		if (_writeBuffer.ActiveLength == 0)
		{
			return WriteToStreamAsync(source, async);
		}
		if (source.Length <= _writeBuffer.AvailableLength)
		{
			WriteToBuffer(source.Span);
			return FlushAsync(async);
		}
		return FlushThenWriteWithoutBufferingAsync(source, async);
	}

	private async ValueTask FlushThenWriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
	{
		await FlushAsync(async).ConfigureAwait(continueOnCapturedContext: false);
		await WriteToStreamAsync(source, async).ConfigureAwait(continueOnCapturedContext: false);
	}

	private ValueTask WriteHexInt32Async(int value, bool async)
	{
		if (value.TryFormat(_writeBuffer.AvailableSpan, out var bytesWritten, "X"))
		{
			_writeBuffer.Commit(bytesWritten);
			return default(ValueTask);
		}
		if (async)
		{
			Span<byte> utf8Destination = stackalloc byte[8];
			bool flag = value.TryFormat(utf8Destination, out bytesWritten, "X");
			return WriteAsync(utf8Destination.Slice(0, bytesWritten).ToArray());
		}
		Flush();
		return WriteHexInt32Async(value, async: false);
	}

	private void Flush()
	{
		ReadOnlySpan<byte> source = _writeBuffer.ActiveSpan;
		if (source.Length > 0)
		{
			_writeBuffer.Discard(source.Length);
			WriteToStream(source);
		}
	}

	private ValueTask FlushAsync(bool async)
	{
		ReadOnlyMemory<byte> source = _writeBuffer.ActiveMemory;
		if (source.Length > 0)
		{
			_writeBuffer.Discard(source.Length);
			return WriteToStreamAsync(source, async);
		}
		return default(ValueTask);
	}

	private void WriteToStream(ReadOnlySpan<byte> source)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Writing {source.Length} bytes.", "WriteToStream");
		}
		_stream.Write(source);
	}

	private ValueTask WriteToStreamAsync(ReadOnlyMemory<byte> source, bool async)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Writing {source.Length} bytes.", "WriteToStreamAsync");
		}
		if (async)
		{
			return _stream.WriteAsync(source);
		}
		_stream.Write(source.Span);
		return default(ValueTask);
	}

	private bool TryReadNextChunkedLine(out ReadOnlySpan<byte> line)
	{
		ReadOnlySpan<byte> activeReadOnlySpan = _readBuffer.ActiveReadOnlySpan;
		int num = activeReadOnlySpan.IndexOf<byte>(10);
		if (num < 0)
		{
			if (activeReadOnlySpan.Length < 16384)
			{
				line = default(ReadOnlySpan<byte>);
				return false;
			}
		}
		else
		{
			int num2 = num + 1;
			if (num2 <= 16384)
			{
				_readBuffer.Discard(num2);
				int num3 = num - 1;
				line = activeReadOnlySpan[..(((uint)num3 < (uint)activeReadOnlySpan.Length && activeReadOnlySpan[num3] == 13) ? num3 : num)];
				return true;
			}
		}
		throw new HttpRequestException(System.SR.net_http_chunk_too_large);
	}

	private async ValueTask InitialFillAsync(bool async)
	{
		int num = ((!async) ? _stream.Read(_readBuffer.AvailableSpan) : (await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false)));
		int num2 = num;
		_readBuffer.Commit(num2);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "InitialFillAsync");
		}
	}

	private async ValueTask FillAsync(bool async)
	{
		_readBuffer.EnsureAvailableSpace(1);
		int num = ((!async) ? _stream.Read(_readBuffer.AvailableSpan) : (await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(continueOnCapturedContext: false)));
		int num2 = num;
		_readBuffer.Commit(num2);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num2} bytes.", "FillAsync");
		}
		if (num2 == 0)
		{
			throw new HttpIOException(HttpRequestError.ResponseEnded, System.SR.net_http_invalid_response_premature_eof);
		}
	}

	private ValueTask FillForHeadersAsync(bool async)
	{
		if (_readBuffer.ActiveStartOffset != 0)
		{
			return FillAsync(async);
		}
		return ReadUntilEndOfHeaderAsync(async);
		async ValueTask ReadUntilEndOfHeaderAsync(bool async)
		{
			int searchOffset = _readBuffer.ActiveLength;
			if (searchOffset > 0)
			{
				searchOffset--;
			}
			while (true)
			{
				await FillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
				if (TryFindEndOfLine(_readBuffer.ActiveReadOnlySpan.Slice(searchOffset), out var searchOffset2))
				{
					break;
				}
				searchOffset += searchOffset2;
				int activeLength = _readBuffer.ActiveLength;
				if (searchOffset != activeLength && activeLength <= 2)
				{
					break;
				}
				if (activeLength >= _allowedReadLineBytes)
				{
					ThrowExceededAllowedReadLineBytes();
				}
			}
		}
		static bool TryFindEndOfLine(ReadOnlySpan<byte> buffer, out int searchOffset)
		{
			int length = buffer.Length;
			while (true)
			{
				int num = buffer.IndexOf<byte>(10);
				if (num < 0)
				{
					searchOffset = length;
					return false;
				}
				int num2 = num + 1;
				if (num2 == buffer.Length)
				{
					searchOffset = length - 1;
					return false;
				}
				byte b = buffer[num2];
				if (b != 9 && b != 32)
				{
					break;
				}
				buffer = buffer.Slice(num2 + 1);
			}
			searchOffset = 0;
			return true;
		}
	}

	private int ReadFromBuffer(Span<byte> buffer)
	{
		ReadOnlySpan<byte> readOnlySpan = _readBuffer.ActiveSpan;
		int num = Math.Min(readOnlySpan.Length, buffer.Length);
		readOnlySpan.Slice(0, num).CopyTo(buffer);
		_readBuffer.Discard(num);
		return num;
	}

	private int Read(Span<byte> destination)
	{
		if (_readBuffer.ActiveLength > 0)
		{
			return ReadFromBuffer(destination);
		}
		int num = _stream.Read(destination);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num} bytes.", "Read");
		}
		return num;
	}

	private async ValueTask<int> ReadAsync(Memory<byte> destination)
	{
		if (_readBuffer.ActiveLength > 0)
		{
			return ReadFromBuffer(destination.Span);
		}
		int num = await _stream.ReadAsync(destination).ConfigureAwait(continueOnCapturedContext: false);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Received {num} bytes.", "ReadAsync");
		}
		return num;
	}

	private int ReadBuffered(Span<byte> destination)
	{
		if (_readBuffer.ActiveLength == 0)
		{
			if (destination.Length == 0)
			{
				return _stream.Read(Array.Empty<byte>());
			}
			int num = _stream.Read(_readBuffer.AvailableSpan);
			_readBuffer.Commit(num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Received {num} bytes.", "ReadBuffered");
			}
		}
		return ReadFromBuffer(destination);
	}

	private ValueTask<int> ReadBufferedAsync(Memory<byte> destination)
	{
		if (destination.Length < _readBuffer.Capacity && destination.Length != 0)
		{
			return ReadBufferedAsyncCore(destination);
		}
		return ReadAsync(destination);
	}

	[AsyncStateMachine(typeof(_003CReadBufferedAsyncCore_003Ed__90))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
	private ValueTask<int> ReadBufferedAsyncCore(Memory<byte> destination)
	{
		Unsafe.SkipInit(out _003CReadBufferedAsyncCore_003Ed__90 stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder<int>.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.destination = destination;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	private ValueTask CopyFromBufferAsync(Stream destination, bool async, int count, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Copying {count} bytes to stream.", "CopyFromBufferAsync");
		}
		ReadOnlyMemory<byte> buffer = _readBuffer.ActiveMemory.Slice(0, count);
		_readBuffer.Discard(count);
		if (async)
		{
			return destination.WriteAsync(buffer, cancellationToken);
		}
		destination.Write(buffer.Span);
		return default(ValueTask);
	}

	private Task CopyToUntilEofAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
	{
		if (_readBuffer.ActiveLength > 0)
		{
			return CopyToUntilEofWithExistingBufferedDataAsync(destination, async, bufferSize, cancellationToken);
		}
		if (async)
		{
			return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
		}
		_stream.CopyTo(destination, bufferSize);
		return Task.CompletedTask;
	}

	private async Task CopyToUntilEofWithExistingBufferedDataAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
	{
		int activeLength = _readBuffer.ActiveLength;
		await CopyFromBufferAsync(destination, async, activeLength, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (async)
		{
			await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			_stream.CopyTo(destination, bufferSize);
		}
	}

	private async Task CopyToContentLengthAsync(Stream destination, bool async, ulong length, int bufferSize, CancellationToken cancellationToken)
	{
		int remaining = _readBuffer.ActiveLength;
		if (remaining > 0)
		{
			if ((ulong)remaining > length)
			{
				remaining = (int)length;
			}
			await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			length -= (ulong)remaining;
			if (length == 0L)
			{
				return;
			}
		}
		byte[] origReadBuffer = null;
		try
		{
			while (true)
			{
				await FillAsync(async).ConfigureAwait(continueOnCapturedContext: false);
				remaining = (int)Math.Min((ulong)_readBuffer.ActiveLength, length);
				await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				length -= (ulong)remaining;
				if (length == 0L)
				{
					break;
				}
				if (origReadBuffer != null)
				{
					continue;
				}
				int capacity = _readBuffer.Capacity;
				if (remaining == capacity)
				{
					int num = (int)Math.Min((ulong)bufferSize, length);
					if (num > capacity)
					{
						origReadBuffer = _readBuffer.DangerousGetUnderlyingBuffer();
						byte[] buffer = ArrayPool<byte>.Shared.Rent(num);
						_readBuffer = new System.Net.ArrayBuffer(buffer);
					}
				}
			}
		}
		finally
		{
			if (origReadBuffer != null)
			{
				bool flag = _readBuffer.ActiveLength > 0;
				byte[] array = _readBuffer.DangerousGetUnderlyingBuffer();
				_readBuffer = new System.Net.ArrayBuffer(origReadBuffer);
				ArrayPool<byte>.Shared.Return(array);
				if (flag)
				{
					_readBuffer.Commit(1);
				}
			}
		}
	}

	internal void Acquire()
	{
		_inUse = true;
	}

	internal void Release()
	{
		_inUse = false;
		if (_currentRequest == null)
		{
			ReturnConnectionToPool();
		}
	}

	internal void DetachFromPool()
	{
		_detachedFromPool = true;
	}

	private void CompleteResponse()
	{
		_currentRequest = null;
		if (_readBuffer.ActiveLength > 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Unexpected data on connection after response read.", "CompleteResponse");
			}
			_readBuffer.Discard(_readBuffer.ActiveLength);
			_connectionClose = true;
		}
		if (!_inUse)
		{
			ReturnConnectionToPool();
		}
	}

	public async ValueTask DrainResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (_connectionClose)
		{
			throw new HttpRequestException(HttpRequestError.UserAuthenticationError, System.SR.net_http_authconnectionfailure);
		}
		Stream stream = response.Content.ReadAsStream(cancellationToken);
		if (stream is HttpContentReadStream { NeedsDrain: not false } httpContentReadStream && (!(await httpContentReadStream.DrainAsync(_pool.Settings._maxResponseDrainSize).ConfigureAwait(continueOnCapturedContext: false)) || _connectionClose))
		{
			throw new HttpRequestException(HttpRequestError.UserAuthenticationError, System.SR.net_http_authconnectionfailure);
		}
		response.Dispose();
	}

	private void ReturnConnectionToPool()
	{
		if (_connectionClose)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Connection will not be reused.", "ReturnConnectionToPool");
			}
			Dispose();
		}
		else
		{
			_pool.RecycleHttp11Connection(this);
		}
	}

	public sealed override string ToString()
	{
		return $"{"HttpConnection"}({_pool})";
	}

	public sealed override void Trace(string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(_pool?.GetHashCode() ?? 0, GetHashCode(), _currentRequest?.GetHashCode() ?? 0, memberName, message);
	}
}
