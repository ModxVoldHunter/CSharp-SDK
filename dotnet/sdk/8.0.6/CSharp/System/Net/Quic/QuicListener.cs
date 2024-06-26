using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;

namespace System.Net.Quic;

public sealed class QuicListener : IAsyncDisposable
{
	private readonly MsQuicContextSafeHandle _handle;

	private int _disposed;

	private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

	private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

	private readonly Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> _connectionOptionsCallback;

	private readonly Channel<object> _acceptQueue;

	private int _pendingConnectionsCapacity;

	public static bool IsSupported => MsQuicApi.IsQuicSupported;

	public IPEndPoint LocalEndPoint { get; }

	public static ValueTask<QuicListener> ListenAsync(QuicListenerOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException(System.SR.Format(System.SR.SystemNetQuic_PlatformNotSupported, MsQuicApi.NotSupportedReason));
		}
		options.Validate("options");
		QuicListener quicListener = new QuicListener(options);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(quicListener, $"{quicListener} Listener listens on {quicListener.LocalEndPoint}", "ListenAsync");
		}
		return ValueTask.FromResult(quicListener);
	}

	public override string ToString()
	{
		return _handle.ToString();
	}

	private unsafe QuicListener(QuicListenerOptions options)
	{
		GCHandle gCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		try
		{
			Unsafe.SkipInit(out QUIC_HANDLE* handle);
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ListenerOpen(MsQuicApi.Api.Registration, (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int>)(delegate*<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int>)(&NativeCallback), (void*)GCHandle.ToIntPtr(gCHandle), &handle), "ListenerOpen failed");
			_handle = new MsQuicContextSafeHandle(handle, gCHandle, SafeHandleType.Listener);
		}
		catch
		{
			gCHandle.Free();
			throw;
		}
		_connectionOptionsCallback = options.ConnectionOptionsCallback;
		_acceptQueue = Channel.CreateUnbounded<object>();
		_pendingConnectionsCapacity = options.ListenBacklog;
		using MsQuicBuffers msQuicBuffers = new MsQuicBuffers();
		msQuicBuffers.Initialize(options.ApplicationProtocols, (SslApplicationProtocol applicationProtocol) => applicationProtocol.Protocol);
		QuicAddr quicAddr = options.ListenEndPoint.ToQuicAddr();
		if (options.ListenEndPoint.Address.Equals(IPAddress.IPv6Any))
		{
			quicAddr.Family = MsQuic.QUIC_ADDRESS_FAMILY_UNSPEC;
		}
		ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ListenerStart(_handle, msQuicBuffers.Buffers, (uint)msQuicBuffers.Count, &quicAddr), "ListenerStart failed");
		quicAddr = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, 67108864u);
		LocalEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&quicAddr, options.ListenEndPoint.AddressFamily);
	}

	public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		GCHandle keepObject = GCHandle.Alloc(this);
		try
		{
			object obj = await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Interlocked.Increment(ref _pendingConnectionsCapacity);
			if (obj is QuicConnection result)
			{
				return result;
			}
			ExceptionDispatchInfo.Throw((Exception)obj);
			throw null;
		}
		catch (ChannelClosedException ex) when (ex.InnerException != null)
		{
			ExceptionDispatchInfo.Throw(ex.InnerException);
			throw;
		}
		finally
		{
			keepObject.Free();
		}
	}

	private async void StartConnectionHandshake(QuicConnection connection, SslClientHelloInfo clientHello)
	{
		bool wrapException = false;
		CancellationToken cancellationToken = default(CancellationToken);
		try
		{
			using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
			linkedCts.CancelAfter(QuicDefaults.HandshakeTimeout);
			cancellationToken = linkedCts.Token;
			wrapException = true;
			QuicServerConnectionOptions quicServerConnectionOptions = await _connectionOptionsCallback(connection, clientHello, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			wrapException = false;
			quicServerConnectionOptions.Validate("options");
			await connection.FinishHandshakeAsync(quicServerConnectionOptions, clientHello.ServerName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (!_acceptQueue.Writer.TryWrite(connection))
			{
				await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(connection, $"{connection} Connection handshake stopped by listener", "StartConnectionHandshake");
			}
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException ex3) when (cancellationToken.IsCancellationRequested)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(connection, $"{connection} Connection handshake timed out: {ex3}", "StartConnectionHandshake");
			}
			Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace(new QuicException(QuicError.ConnectionTimeout, null, System.SR.Format(System.SR.net_quic_handshake_timeout, QuicDefaults.HandshakeTimeout), ex3));
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			_acceptQueue.Writer.TryWrite(ex);
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(connection, $"{connection} Connection handshake failed: {ex}", "StartConnectionHandshake");
			}
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			_acceptQueue.Writer.TryWrite(wrapException ? ExceptionDispatchInfo.SetCurrentStackTrace(new QuicException(QuicError.CallbackError, null, System.SR.net_quic_callback_error, ex)) : ex);
		}
	}

	private unsafe int HandleEventNewConnection(ref QUIC_LISTENER_EVENT._Anonymous_e__Union._NEW_CONNECTION_e__Struct data)
	{
		if (Interlocked.Decrement(ref _pendingConnectionsCapacity) < 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"{this} Refusing connection from {MsQuicHelpers.QuicAddrToIPEndPoint(data.Info->RemoteAddress)} due to backlog limit", "HandleEventNewConnection");
			}
			Interlocked.Increment(ref _pendingConnectionsCapacity);
			return MsQuic.QUIC_STATUS_CONNECTION_REFUSED;
		}
		QuicConnection connection = new QuicConnection(data.Connection, data.Info);
		SslClientHelloInfo clientHello = new SslClientHelloInfo((data.Info->ServerNameLength > 0) ? Marshal.PtrToStringUTF8((nint)data.Info->ServerName, data.Info->ServerNameLength) : "", SslProtocols.Tls13);
		StartConnectionHandshake(connection, clientHello);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventStopComplete()
	{
		_shutdownTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleListenerEvent(ref QUIC_LISTENER_EVENT listenerEvent)
	{
		return listenerEvent.Type switch
		{
			QUIC_LISTENER_EVENT_TYPE.NEW_CONNECTION => HandleEventNewConnection(ref listenerEvent.NEW_CONNECTION), 
			QUIC_LISTENER_EVENT_TYPE.STOP_COMPLETE => HandleEventStopComplete(), 
			_ => MsQuic.QUIC_STATUS_SUCCESS, 
		};
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
	private unsafe static int NativeCallback(QUIC_HANDLE* listener, void* context, QUIC_LISTENER_EVENT* listenerEvent)
	{
		GCHandle gCHandle = GCHandle.FromIntPtr((nint)context);
		if (!gCHandle.IsAllocated || !(gCHandle.Target is QuicListener quicListener))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"Received event {listenerEvent->Type} while listener is already disposed", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INVALID_STATE;
		}
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(quicListener, $"{quicListener} Received event {listenerEvent->Type} {listenerEvent->ToString()}", "NativeCallback");
			}
			return quicListener.HandleListenerEvent(ref *listenerEvent);
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(quicListener, $"{quicListener} Exception while processing event {listenerEvent->Type}: {ex}", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		if (_shutdownTcs.TryInitialize(out var valueTask, this))
		{
			MsQuicApi.Api.ListenerStop(_handle);
		}
		await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		_handle.Dispose();
		_disposeCts.Cancel();
		_acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetOperationAbortedException()));
		object item;
		while (_acceptQueue.Reader.TryRead(out item))
		{
			if (item is QuicConnection quicConnection)
			{
				await quicConnection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
