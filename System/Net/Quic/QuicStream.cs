using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;

namespace System.Net.Quic;

public sealed class QuicStream : Stream
{
	private readonly MsQuicContextSafeHandle _handle;

	private int _disposed;

	private readonly ValueTaskSource _startedTcs = new ValueTaskSource();

	private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

	private readonly ResettableValueTaskSource _receiveTcs = new ResettableValueTaskSource
	{
		CancellationAction = delegate(object target)
		{
			try
			{
				if (target is QuicStream quicStream2)
				{
					quicStream2.Abort(QuicAbortDirection.Read, quicStream2._defaultErrorCode);
				}
			}
			catch (ObjectDisposedException)
			{
			}
		}
	};

	private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();

	private int _receivedNeedsEnable;

	private readonly ResettableValueTaskSource _sendTcs = new ResettableValueTaskSource
	{
		CancellationAction = delegate(object target)
		{
			try
			{
				if (target is QuicStream quicStream)
				{
					quicStream.Abort(QuicAbortDirection.Write, quicStream._defaultErrorCode);
				}
			}
			catch (ObjectDisposedException)
			{
			}
		}
	};

	private MsQuicBuffers _sendBuffers = new MsQuicBuffers();

	private readonly object _sendBuffersLock = new object();

	private readonly long _defaultErrorCode;

	private readonly bool _canRead;

	private readonly bool _canWrite;

	private long _id = -1L;

	private readonly QuicStreamType _type;

	private TimeSpan _readTimeout = Timeout.InfiniteTimeSpan;

	private TimeSpan _writeTimeout = Timeout.InfiniteTimeSpan;

	public long Id => _id;

	public QuicStreamType Type => _type;

	public Task ReadsClosed => _receiveTcs.GetFinalTask();

	public Task WritesClosed => _sendTcs.GetFinalTask();

	public override bool CanSeek => false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException();
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public override bool CanTimeout => true;

	public override int ReadTimeout
	{
		get
		{
			ObjectDisposedException.ThrowIf(_disposed == 1, this);
			return (int)_readTimeout.TotalMilliseconds;
		}
		set
		{
			ObjectDisposedException.ThrowIf(_disposed == 1, this);
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_quic_timeout_use_gt_zero);
			}
			_readTimeout = TimeSpan.FromMilliseconds(value);
		}
	}

	public override int WriteTimeout
	{
		get
		{
			ObjectDisposedException.ThrowIf(_disposed == 1, this);
			return (int)_writeTimeout.TotalMilliseconds;
		}
		set
		{
			ObjectDisposedException.ThrowIf(_disposed == 1, this);
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_quic_timeout_use_gt_zero);
			}
			_writeTimeout = TimeSpan.FromMilliseconds(value);
		}
	}

	public override bool CanRead
	{
		get
		{
			if (Volatile.Read(ref _disposed) == 0)
			{
				return _canRead;
			}
			return false;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (Volatile.Read(ref _disposed) == 0)
			{
				return _canWrite;
			}
			return false;
		}
	}

	public override string ToString()
	{
		return _handle.ToString();
	}

	internal unsafe QuicStream(MsQuicContextSafeHandle connectionHandle, QuicStreamType type, long defaultErrorCode)
	{
		GCHandle gCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		try
		{
			Unsafe.SkipInit(out QUIC_HANDLE* handle);
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamOpen(connectionHandle, (type == QuicStreamType.Unidirectional) ? QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.NONE, (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>)(delegate*<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>)(&NativeCallback), (void*)GCHandle.ToIntPtr(gCHandle), &handle), "StreamOpen failed");
			_handle = new MsQuicContextSafeHandle(handle, gCHandle, SafeHandleType.Stream, connectionHandle);
		}
		catch
		{
			gCHandle.Free();
			throw;
		}
		_defaultErrorCode = defaultErrorCode;
		_canRead = type == QuicStreamType.Bidirectional;
		_canWrite = true;
		if (!_canRead)
		{
			_receiveTcs.TrySetResult(final: true);
		}
		_type = type;
	}

	internal unsafe QuicStream(MsQuicContextSafeHandle connectionHandle, QUIC_HANDLE* handle, QUIC_STREAM_OPEN_FLAGS flags, long defaultErrorCode)
	{
		GCHandle gCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		try
		{
			_handle = new MsQuicContextSafeHandle(handle, gCHandle, SafeHandleType.Stream, connectionHandle);
			delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> callback = (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>)(delegate*<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>)(&NativeCallback);
			MsQuicApi.Api.SetCallbackHandler(_handle, callback, (void*)GCHandle.ToIntPtr(gCHandle));
		}
		catch
		{
			gCHandle.Free();
			throw;
		}
		_defaultErrorCode = defaultErrorCode;
		_canRead = true;
		_canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
		if (!_canWrite)
		{
			_sendTcs.TrySetResult(final: true);
		}
		_id = (long)MsQuicHelpers.GetMsQuicParameter<ulong>(_handle, 134217728u);
		_type = ((!flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL)) ? QuicStreamType.Bidirectional : QuicStreamType.Unidirectional);
		_startedTcs.TrySetResult();
	}

	internal ValueTask StartAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		_startedTcs.TryInitialize(out var valueTask, this, cancellationToken);
		int status = MsQuicApi.Api.StreamStart(_handle, QUIC_STREAM_START_FLAGS.SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.INDICATE_PEER_ACCEPT);
		if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(status, out var exception))
		{
			_startedTcs.TrySetException(exception);
		}
		return valueTask;
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		if (!_canRead)
		{
			throw new InvalidOperationException(System.SR.net_quic_reading_notallowed);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{this} Stream reading into memory of '{buffer.Length}' bytes.", "ReadAsync");
		}
		if (_receiveTcs.IsCompleted)
		{
			cancellationToken.ThrowIfCancellationRequested();
		}
		int totalCopied = 0;
		bool complete;
		do
		{
			if (!_receiveTcs.TryGetValueTask(out var valueTask, this, cancellationToken))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, "read"));
			}
			bool isEmpty;
			int num = _receiveBuffers.CopyTo(buffer, out complete, out isEmpty);
			buffer = buffer.Slice(num);
			totalCopied += num;
			if (complete)
			{
				_receiveTcs.TrySetResult(final: true);
			}
			if (totalCopied > 0 || !isEmpty)
			{
				_receiveTcs.TrySetResult();
			}
			await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		while (!complete && !buffer.IsEmpty && totalCopied == 0);
		if (totalCopied > 0 && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
		{
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamReceiveSetEnabled(_handle, 1), "StreamReceivedSetEnabled failed");
		}
		return totalCopied;
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAsync(buffer, completeWrites: false, cancellationToken);
	}

	public unsafe ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		if (!_canWrite)
		{
			throw new InvalidOperationException(System.SR.net_quic_writing_notallowed);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("{0} Stream writing memory of '{1}' bytes while {2} writes.", this, buffer.Length, completeWrites ? "completing" : "not completing"), "WriteAsync");
		}
		if (_sendTcs.IsCompleted && cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		if (!_sendTcs.TryGetValueTask(out var valueTask, this, cancellationToken))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, "write"));
		}
		if (valueTask.IsCompleted)
		{
			return valueTask;
		}
		if (buffer.IsEmpty)
		{
			_sendTcs.TrySetResult();
			if (completeWrites)
			{
				CompleteWrites();
			}
			return valueTask;
		}
		lock (_sendBuffersLock)
		{
			ObjectDisposedException.ThrowIf(_disposed == 1, this);
			if (_sendBuffers.Count > 0 && _sendBuffers.Buffers->Buffer != null)
			{
				_sendTcs.TrySetException(ThrowHelper.GetOperationAbortedException(System.SR.net_quic_writing_aborted));
				return valueTask;
			}
			_sendBuffers.Initialize(buffer);
			int status = MsQuicApi.Api.StreamSend(_handle, _sendBuffers.Buffers, (uint)_sendBuffers.Count, completeWrites ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE, null);
			if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(status, out var exception))
			{
				_sendBuffers.Reset();
				_sendTcs.TrySetException(exception, final: true);
			}
		}
		return valueTask;
	}

	public void Abort(QuicAbortDirection abortDirection, long errorCode)
	{
		if (_disposed == 1)
		{
			return;
		}
		QUIC_STREAM_SHUTDOWN_FLAGS qUIC_STREAM_SHUTDOWN_FLAGS = QUIC_STREAM_SHUTDOWN_FLAGS.NONE;
		if (abortDirection.HasFlag(QuicAbortDirection.Read) && _receiveTcs.TrySetException(ThrowHelper.GetOperationAbortedException(System.SR.net_quic_reading_aborted), final: true))
		{
			qUIC_STREAM_SHUTDOWN_FLAGS |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
		}
		if (abortDirection.HasFlag(QuicAbortDirection.Write) && _sendTcs.TrySetException(ThrowHelper.GetOperationAbortedException(System.SR.net_quic_writing_aborted), final: true))
		{
			qUIC_STREAM_SHUTDOWN_FLAGS |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND;
		}
		if (qUIC_STREAM_SHUTDOWN_FLAGS != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"{this} Aborting {abortDirection} with {errorCode}", "Abort");
			}
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamShutdown(_handle, qUIC_STREAM_SHUTDOWN_FLAGS, (ulong)errorCode), "StreamShutdown failed");
		}
	}

	public void CompleteWrites()
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		if (_shutdownTcs.TryInitialize(out var _, this))
		{
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamShutdown(_handle, QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0uL), "StreamShutdown failed");
		}
	}

	private int HandleEventStartComplete(ref QUIC_STREAM_EVENT._Anonymous_e__Union._START_COMPLETE_e__Struct data)
	{
		_id = (long)data.ID;
		Exception exception;
		if (MsQuic.StatusSucceeded(data.Status))
		{
			if (data.PeerAccepted != 0)
			{
				_startedTcs.TrySetResult();
			}
		}
		else if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(data.Status, out exception))
		{
			_startedTcs.TrySetException(exception);
		}
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private unsafe int HandleEventReceive(ref QUIC_STREAM_EVENT._Anonymous_e__Union._RECEIVE_e__Struct data)
	{
		ulong num = (ulong)_receiveBuffers.CopyFrom(new ReadOnlySpan<QUIC_BUFFER>(data.Buffers, (int)data.BufferCount), (int)data.TotalBufferLength, data.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN));
		if (num < data.TotalBufferLength)
		{
			Volatile.Write(ref _receivedNeedsEnable, 1);
		}
		_receiveTcs.TrySetResult();
		data.TotalBufferLength = num;
		if (!_receiveBuffers.HasCapacity() || Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) != 1)
		{
			return MsQuic.QUIC_STATUS_SUCCESS;
		}
		return MsQuic.QUIC_STATUS_CONTINUE;
	}

	private int HandleEventSendComplete(ref QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_COMPLETE_e__Struct data)
	{
		lock (_sendBuffersLock)
		{
			_sendBuffers.Reset();
		}
		if (data.Canceled == 0)
		{
			_sendTcs.TrySetResult();
		}
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventPeerSendShutdown()
	{
		_receiveBuffers.SetFinal();
		_receiveTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventPeerSendAborted(ref QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct data)
	{
		_receiveTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventPeerReceiveAborted(ref QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct data)
	{
		_sendTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventSendShutdownComplete(ref QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct data)
	{
		if (data.Graceful != 0)
		{
			_sendTcs.TrySetResult(final: true);
		}
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventShutdownComplete(ref QUIC_STREAM_EVENT._Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct data)
	{
		if (data.ConnectionShutdown != 0)
		{
			bool flag = data.ConnectionShutdownByApp != 0;
			bool flag2 = data.ConnectionClosedRemotely != 0;
			Exception ex = (flag ? ((!flag2) ? ThrowHelper.GetOperationAbortedException() : ThrowHelper.GetConnectionAbortedException((long)data.ConnectionErrorCode)) : ((!flag2) ? ThrowHelper.GetExceptionForMsQuicStatus(data.ConnectionCloseStatus, (long)data.ConnectionErrorCode) : ThrowHelper.GetExceptionForMsQuicStatus(data.ConnectionCloseStatus, (long)data.ConnectionErrorCode, $"Shutdown by transport {data.ConnectionErrorCode}")));
			Exception exception = ex;
			_startedTcs.TrySetException(exception);
			_receiveTcs.TrySetException(exception, final: true);
			_sendTcs.TrySetException(exception, final: true);
		}
		_shutdownTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventPeerAccepted()
	{
		_startedTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent)
	{
		return streamEvent.Type switch
		{
			QUIC_STREAM_EVENT_TYPE.START_COMPLETE => HandleEventStartComplete(ref streamEvent.START_COMPLETE), 
			QUIC_STREAM_EVENT_TYPE.RECEIVE => HandleEventReceive(ref streamEvent.RECEIVE), 
			QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE => HandleEventSendComplete(ref streamEvent.SEND_COMPLETE), 
			QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN => HandleEventPeerSendShutdown(), 
			QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED => HandleEventPeerSendAborted(ref streamEvent.PEER_SEND_ABORTED), 
			QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED => HandleEventPeerReceiveAborted(ref streamEvent.PEER_RECEIVE_ABORTED), 
			QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE => HandleEventSendShutdownComplete(ref streamEvent.SEND_SHUTDOWN_COMPLETE), 
			QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(ref streamEvent.SHUTDOWN_COMPLETE), 
			QUIC_STREAM_EVENT_TYPE.PEER_ACCEPTED => HandleEventPeerAccepted(), 
			_ => MsQuic.QUIC_STATUS_SUCCESS, 
		};
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
	private unsafe static int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_STREAM_EVENT* streamEvent)
	{
		GCHandle gCHandle = GCHandle.FromIntPtr((nint)context);
		if (!gCHandle.IsAllocated || !(gCHandle.Target is QuicStream quicStream))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"Received event {streamEvent->Type} while connection is already disposed", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INVALID_STATE;
		}
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(quicStream, $"{quicStream} Received event {streamEvent->Type} {streamEvent->ToString()}", "NativeCallback");
			}
			return quicStream.HandleStreamEvent(ref *streamEvent);
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(quicStream, $"{quicStream} Exception while processing event {streamEvent->Type}: {ex}", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
		}
	}

	public override async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		ValueTask valueTask;
		if (!_startedTcs.IsCompletedSuccessfully)
		{
			if (_shutdownTcs.TryInitialize(out valueTask, this))
			{
				StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT | QUIC_STREAM_SHUTDOWN_FLAGS.IMMEDIATE, _defaultErrorCode);
			}
		}
		else
		{
			if (_receiveTcs.TrySetException(ThrowHelper.GetOperationAbortedException(), final: true))
			{
				StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, _defaultErrorCode);
			}
			if (_shutdownTcs.TryInitialize(out valueTask, this))
			{
				StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
			}
		}
		await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		_handle.Dispose();
		lock (_sendBuffersLock)
		{
			_sendBuffers.Dispose();
		}
		void StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
		{
			int status = MsQuicApi.Api.StreamShutdown(_handle, flags, (ulong)errorCode);
			if (MsQuic.StatusFailed(status) && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"{this} StreamShutdown({flags}) failed: {ThrowHelper.GetErrorMessageForStatus(status)}.", "DisposeAsync");
			}
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, default(CancellationToken)), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return Read(buffer.AsSpan(offset, count));
	}

	public override int ReadByte()
	{
		byte reference = 0;
		if (Read(MemoryMarshal.CreateSpan(ref reference, 1)) == 0)
		{
			return -1;
		}
		return reference;
	}

	public override int Read(Span<byte> buffer)
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		CancellationTokenSource cancellationTokenSource = null;
		try
		{
			if (_readTimeout > TimeSpan.Zero)
			{
				cancellationTokenSource = new CancellationTokenSource(_readTimeout);
			}
			int result = ReadAsync(new Memory<byte>(array, 0, buffer.Length), cancellationTokenSource?.Token ?? default(CancellationToken)).AsTask().GetAwaiter().GetResult();
			array.AsSpan(0, result).CopyTo(buffer);
			return result;
		}
		catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested ?? false)
		{
			throw new IOException(System.SR.net_quic_timeout);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
			cancellationTokenSource?.Dispose();
		}
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, default(CancellationToken)), callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		Write(buffer.AsSpan(offset, count));
	}

	public override void WriteByte(byte value)
	{
		Write(new ReadOnlySpan<byte>(ref value));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		CancellationTokenSource cancellationTokenSource = null;
		if (_writeTimeout > TimeSpan.Zero)
		{
			cancellationTokenSource = new CancellationTokenSource(_writeTimeout);
		}
		try
		{
			WriteAsync(buffer.ToArray(), cancellationTokenSource?.Token ?? default(CancellationToken)).AsTask().GetAwaiter().GetResult();
		}
		catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested ?? false)
		{
			throw new IOException(System.SR.net_quic_timeout);
		}
		finally
		{
			cancellationTokenSource?.Dispose();
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override void Flush()
	{
		FlushAsync().GetAwaiter().GetResult();
	}

	public override Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing)
	{
		DisposeAsync().AsTask().GetAwaiter().GetResult();
		base.Dispose(disposing);
	}
}
