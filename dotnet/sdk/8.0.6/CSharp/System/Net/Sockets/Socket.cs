using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

public class Socket : IDisposable
{
	private sealed class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
	{
		internal AsyncTaskMethodBuilder<TResult> _builder;

		internal bool _accessed;

		internal bool _wrapExceptionsInIOExceptions;

		internal TaskSocketAsyncEventArgs()
			: base(unsafeSuppressExecutionContextFlow: true)
		{
		}

		internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
		{
			lock (this)
			{
				responsibleForReturningToPool = _accessed;
				_accessed = true;
				_ = _builder.Task;
				return _builder;
			}
		}
	}

	internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>, IValueTaskSource<Socket>, IValueTaskSource<SocketReceiveFromResult>, IValueTaskSource<SocketReceiveMessageFromResult>
	{
		private readonly Socket _owner;

		private readonly bool _isReadForCaching;

		private ManualResetValueTaskSourceCore<bool> _mrvtsc;

		private CancellationToken _cancellationToken;

		public bool WrapExceptionsForNetworkStream { get; set; }

		public AwaitableSocketAsyncEventArgs(Socket owner, bool isReceiveForCaching)
			: base(unsafeSuppressExecutionContextFlow: true)
		{
			_owner = owner;
			_isReadForCaching = isReceiveForCaching;
		}

		private void ReleaseForAsyncCompletion()
		{
			_cancellationToken = default(CancellationToken);
			_mrvtsc.Reset();
			ReleaseForSyncCompletion();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ReleaseForSyncCompletion()
		{
			if (Interlocked.CompareExchange(ref _isReadForCaching ? ref _owner._singleBufferReceiveEventArgs : ref _owner._singleBufferSendEventArgs, this, null) != null)
			{
				Dispose();
			}
		}

		protected override void OnCompleted(SocketAsyncEventArgs _)
		{
			_mrvtsc.SetResult(result: true);
		}

		public ValueTask<Socket> AcceptAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.AcceptAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<Socket>(this, _mrvtsc.Version);
			}
			Socket acceptSocket = base.AcceptSocket;
			SocketError socketError = base.SocketError;
			base.AcceptSocket = null;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<Socket>(CreateException(socketError));
			}
			return new ValueTask<Socket>(acceptSocket);
		}

		public ValueTask<int> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveFromAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<SocketReceiveFromResult>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<SocketReceiveFromResult>(CreateException(socketError));
			}
			SocketReceiveFromResult result = default(SocketReceiveFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			return new ValueTask<SocketReceiveFromResult>(result);
		}

		internal ValueTask<int> ReceiveFromSocketAddressAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveFromAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveMessageFromAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<SocketReceiveMessageFromResult>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketFlags socketFlags = base.SocketFlags;
			IPPacketInformation receiveMessageFromPacketInfo = base.ReceiveMessageFromPacketInfo;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<SocketReceiveMessageFromResult>(CreateException(socketError));
			}
			SocketReceiveMessageFromResult result = default(SocketReceiveMessageFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			result.SocketFlags = socketFlags;
			result.PacketInformation = receiveMessageFromPacketInfo;
			return new ValueTask<SocketReceiveMessageFromResult>(result);
		}

		public ValueTask<int> SendAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask SendAsyncForNetworkStream(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _mrvtsc.Version);
			}
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask SendPacketsAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendPacketsAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _mrvtsc.Version);
			}
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask<int> SendToAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendToAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _mrvtsc.Version);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask ConnectAsync(Socket socket)
		{
			try
			{
				if (socket.ConnectAsync(this, userSocket: true, saeaCancelable: false))
				{
					return new ValueTask(this, _mrvtsc.Version);
				}
			}
			catch
			{
				ReleaseForSyncCompletion();
				throw;
			}
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask DisconnectAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.DisconnectAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _mrvtsc.Version);
			}
			SocketError socketError = base.SocketError;
			ReleaseForSyncCompletion();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return ValueTask.CompletedTask;
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _mrvtsc.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_mrvtsc.OnCompleted(continuation, state, token, flags);
		}

		int IValueTaskSource<int>.GetResult(short token)
		{
			if (token != _mrvtsc.Version)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			CancellationToken cancellationToken = _cancellationToken;
			ReleaseForAsyncCompletion();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			return bytesTransferred;
		}

		void IValueTaskSource.GetResult(short token)
		{
			if (token != _mrvtsc.Version)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			CancellationToken cancellationToken = _cancellationToken;
			ReleaseForAsyncCompletion();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
		}

		Socket IValueTaskSource<Socket>.GetResult(short token)
		{
			if (token != _mrvtsc.Version)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			Socket acceptSocket = base.AcceptSocket;
			CancellationToken cancellationToken = _cancellationToken;
			base.AcceptSocket = null;
			ReleaseForAsyncCompletion();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			return acceptSocket;
		}

		SocketReceiveFromResult IValueTaskSource<SocketReceiveFromResult>.GetResult(short token)
		{
			if (token != _mrvtsc.Version)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			CancellationToken cancellationToken = _cancellationToken;
			ReleaseForAsyncCompletion();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			SocketReceiveFromResult result = default(SocketReceiveFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			return result;
		}

		SocketReceiveMessageFromResult IValueTaskSource<SocketReceiveMessageFromResult>.GetResult(short token)
		{
			if (token != _mrvtsc.Version)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketFlags socketFlags = base.SocketFlags;
			IPPacketInformation receiveMessageFromPacketInfo = base.ReceiveMessageFromPacketInfo;
			CancellationToken cancellationToken = _cancellationToken;
			ReleaseForAsyncCompletion();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			SocketReceiveMessageFromResult result = default(SocketReceiveMessageFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			result.SocketFlags = socketFlags;
			result.PacketInformation = receiveMessageFromPacketInfo;
			return result;
		}

		private static void ThrowIncorrectTokenException()
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_IncorrectToken);
		}

		private void ThrowException(SocketError error, CancellationToken cancellationToken)
		{
			if ((error == SocketError.OperationAborted || error == SocketError.ConnectionAborted) ? true : false)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			throw CreateException(error, forAsyncThrow: false);
		}

		private Exception CreateException(SocketError error, bool forAsyncThrow = true)
		{
			Exception ex = new SocketException((int)error);
			if (forAsyncThrow)
			{
				ex = ExceptionDispatchInfo.SetCurrentStackTrace(ex);
			}
			if (!WrapExceptionsForNetworkStream)
			{
				return ex;
			}
			return new IOException(System.SR.Format(_isReadForCaching ? System.SR.net_io_readfailure : System.SR.net_io_writefailure, ex.Message), ex);
		}
	}

	private sealed class CachedSerializedEndPoint
	{
		public readonly IPEndPoint IPEndPoint;

		public readonly SocketAddress SocketAddress;

		public CachedSerializedEndPoint(IPAddress address)
		{
			IPEndPoint = new IPEndPoint(address, 0);
			SocketAddress = IPEndPoint.Serialize();
		}
	}

	private static readonly IPAddress s_IPAddressAnyMapToIPv6 = IPAddress.Any.MapToIPv6();

	private static readonly IPEndPoint s_IPEndPointIPv6 = new IPEndPoint(s_IPAddressAnyMapToIPv6, 0);

	private SafeSocketHandle _handle;

	internal EndPoint _rightEndPoint;

	internal EndPoint _remoteEndPoint;

	private EndPoint _localEndPoint;

	private bool _isConnected;

	private bool _isDisconnected;

	private bool _willBlock = true;

	private bool _willBlockInternal = true;

	private bool _isListening;

	private bool _nonBlockingConnectInProgress;

	private EndPoint _pendingConnectRightEndPoint;

	private AddressFamily _addressFamily;

	private SocketType _socketType;

	private ProtocolType _protocolType;

	private bool _receivingPacketInformation;

	private int _closeTimeout = -1;

	private int _disposed;

	private AwaitableSocketAsyncEventArgs _singleBufferReceiveEventArgs;

	private AwaitableSocketAsyncEventArgs _singleBufferSendEventArgs;

	private TaskSocketAsyncEventArgs<int> _multiBufferReceiveEventArgs;

	private TaskSocketAsyncEventArgs<int> _multiBufferSendEventArgs;

	private static CachedSerializedEndPoint s_cachedAnyEndPoint;

	private static CachedSerializedEndPoint s_cachedAnyV6EndPoint;

	private static CachedSerializedEndPoint s_cachedMappedAnyV6EndPoint;

	private DynamicWinsockMethods _dynamicWinsockMethods;

	[Obsolete("SupportsIPv4 has been deprecated. Use OSSupportsIPv4 instead.")]
	public static bool SupportsIPv4 => OSSupportsIPv4;

	[Obsolete("SupportsIPv6 has been deprecated. Use OSSupportsIPv6 instead.")]
	public static bool SupportsIPv6 => OSSupportsIPv6;

	public static bool OSSupportsIPv4 => System.Net.SocketProtocolSupportPal.OSSupportsIPv4;

	public static bool OSSupportsIPv6 => System.Net.SocketProtocolSupportPal.OSSupportsIPv6;

	public static bool OSSupportsUnixDomainSockets => System.Net.SocketProtocolSupportPal.OSSupportsUnixDomainSockets;

	public int Available
	{
		get
		{
			ThrowIfDisposed();
			int available;
			SocketError available2 = SocketPal.GetAvailable(_handle, out available);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"GetAvailable returns errorCode:{available2}", "Available");
			}
			if (available2 != 0)
			{
				UpdateStatusAfterSocketErrorAndThrowException(available2, disconnectOnFailure: true, "Available");
			}
			return available;
		}
	}

	public unsafe EndPoint? LocalEndPoint
	{
		get
		{
			ThrowIfDisposed();
			CheckNonBlockingConnectCompleted();
			if (_rightEndPoint == null)
			{
				return null;
			}
			if (_localEndPoint == null)
			{
				Span<byte> span = stackalloc byte[SocketAddress.GetMaximumAddressSize(_addressFamily)];
				int length = span.Length;
				fixed (byte* buffer = &MemoryMarshal.GetReference(span))
				{
					SocketError sockName = SocketPal.GetSockName(_handle, buffer, &length);
					if (sockName != 0)
					{
						UpdateStatusAfterSocketErrorAndThrowException(sockName, disconnectOnFailure: true, "LocalEndPoint");
					}
				}
				if (_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6)
				{
					_localEndPoint = System.Net.Sockets.IPEndPointExtensions.CreateIPEndPoint(span.Slice(0, length));
				}
				else
				{
					SocketAddress socketAddress = new SocketAddress(_rightEndPoint.AddressFamily, length);
					span.Slice(0, length).CopyTo(socketAddress.Buffer.Span);
					_localEndPoint = _rightEndPoint.Create(socketAddress);
				}
			}
			return _localEndPoint;
		}
	}

	public EndPoint? RemoteEndPoint
	{
		get
		{
			ThrowIfDisposed();
			if (_remoteEndPoint == null)
			{
				CheckNonBlockingConnectCompleted();
				if (_rightEndPoint == null || !_isConnected)
				{
					return null;
				}
				Span<byte> buffer = stackalloc byte[SocketAddress.GetMaximumAddressSize(_addressFamily)];
				int nameLen = buffer.Length;
				SocketError peerName = SocketPal.GetPeerName(_handle, buffer, ref nameLen);
				if (peerName != 0)
				{
					UpdateStatusAfterSocketErrorAndThrowException(peerName, disconnectOnFailure: true, "RemoteEndPoint");
				}
				try
				{
					if (_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6)
					{
						_remoteEndPoint = System.Net.Sockets.IPEndPointExtensions.CreateIPEndPoint(buffer.Slice(0, nameLen));
					}
					else
					{
						SocketAddress socketAddress = new SocketAddress(_rightEndPoint.AddressFamily, nameLen);
						buffer.Slice(0, nameLen).CopyTo(socketAddress.Buffer.Span);
						_remoteEndPoint = _rightEndPoint.Create(socketAddress);
					}
				}
				catch
				{
				}
			}
			return _remoteEndPoint;
		}
	}

	public nint Handle => SafeHandle.DangerousGetHandle();

	public SafeSocketHandle SafeHandle
	{
		get
		{
			_handle.SetExposed();
			return _handle;
		}
	}

	internal SafeSocketHandle InternalSafeHandle => _handle;

	public bool Blocking
	{
		get
		{
			return _willBlock;
		}
		set
		{
			ThrowIfDisposed();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"value:{value} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}", "Blocking");
			}
			bool current;
			SocketError socketError = InternalSetBlocking(value, out current);
			if (socketError != 0)
			{
				UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Blocking");
			}
			_willBlock = current;
		}
	}

	[Obsolete("UseOnlyOverlappedIO has been deprecated and is not supported.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool UseOnlyOverlappedIO
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public bool Connected
	{
		get
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_isConnected:{_isConnected}", "Connected");
			}
			CheckNonBlockingConnectCompleted();
			return _isConnected;
		}
	}

	public AddressFamily AddressFamily => _addressFamily;

	public SocketType SocketType => _socketType;

	public ProtocolType ProtocolType => _protocolType;

	public bool IsBound => _rightEndPoint != null;

	public bool ExclusiveAddressUse
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse) != 0;
		}
		set
		{
			if (IsBound)
			{
				throw new InvalidOperationException(System.SR.net_sockets_mustnotbebound);
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, value ? 1 : 0);
		}
	}

	public int ReceiveBufferSize
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
		}
	}

	public int SendBufferSize
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
		}
	}

	public int ReceiveTimeout
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, -1, "value");
			if (value == -1)
			{
				value = 0;
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
		}
	}

	public int SendTimeout
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, -1, "value");
			if (value == -1)
			{
				value = 0;
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
		}
	}

	public LingerOption? LingerState
	{
		get
		{
			return (LingerOption)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger);
		}
		[param: DisallowNull]
		set
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, value);
		}
	}

	public bool NoDelay
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug) != 0;
		}
		set
		{
			SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, value ? 1 : 0);
		}
	}

	public short Ttl
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				return (short)(int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress);
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				return (short)(int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress);
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 255, "value");
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, value);
				return;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress, value);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool DontFragment
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				return (int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment) != 0;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, value ? 1 : 0);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool MulticastLoopback
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				return (int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback) != 0;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				return (int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback) != 0;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, value ? 1 : 0);
				return;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, value ? 1 : 0);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool EnableBroadcast
	{
		get
		{
			if (SocketType == SocketType.Stream)
			{
				return false;
			}
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast) != 0;
		}
		set
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, value ? 1 : 0);
		}
	}

	public bool DualMode
	{
		get
		{
			if (AddressFamily != AddressFamily.InterNetworkV6)
			{
				return false;
			}
			return (int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only) == 0;
		}
		set
		{
			if (AddressFamily != AddressFamily.InterNetworkV6)
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, (!value) ? 1 : 0);
		}
	}

	private bool IsDualMode
	{
		get
		{
			if (AddressFamily == AddressFamily.InterNetworkV6)
			{
				return DualMode;
			}
			return false;
		}
	}

	internal bool Disposed => _disposed != 0;

	private bool IsConnectionOriented => _socketType == SocketType.Stream;

	public Socket(SocketType socketType, ProtocolType protocolType)
		: this(OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, socketType, protocolType)
	{
		if (OSSupportsIPv6)
		{
			DualMode = true;
		}
	}

	public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, addressFamily, ".ctor");
		}
		SocketError socketError = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
		_addressFamily = addressFamily;
		_socketType = socketType;
		_protocolType = protocolType;
	}

	public Socket(SafeSocketHandle handle)
		: this(ValidateHandle(handle), loadPropertiesFromHandle: true)
	{
	}

	private unsafe Socket(SafeSocketHandle handle, bool loadPropertiesFromHandle)
	{
		_handle = handle;
		_addressFamily = AddressFamily.Unknown;
		_socketType = SocketType.Unknown;
		_protocolType = ProtocolType.Unknown;
		if (!loadPropertiesFromHandle)
		{
			return;
		}
		try
		{
			LoadSocketTypeFromHandle(handle, out _addressFamily, out _socketType, out _protocolType, out _willBlockInternal, out _isListening, out var isSocket);
			if (!isSocket)
			{
				return;
			}
			Span<byte> span = stackalloc byte[SocketPal.MaximumAddressSize];
			int nameLen = span.Length;
			fixed (byte* buffer = span)
			{
				if (SocketPal.GetSockName(handle, buffer, &nameLen) != 0)
				{
					return;
				}
			}
			switch (_addressFamily)
			{
			case AddressFamily.InterNetwork:
				_rightEndPoint = new IPEndPoint(new IPAddress((long)System.Net.SocketAddressPal.GetIPv4Address(span.Slice(0, nameLen)) & 0xFFFFFFFFL), System.Net.SocketAddressPal.GetPort(span));
				break;
			case AddressFamily.InterNetworkV6:
			{
				Span<byte> span2 = stackalloc byte[16];
				System.Net.SocketAddressPal.GetIPv6Address(span.Slice(0, nameLen), span2, out var scope);
				_rightEndPoint = new IPEndPoint(new IPAddress(span2, scope), System.Net.SocketAddressPal.GetPort(span));
				break;
			}
			case AddressFamily.Unix:
				_rightEndPoint = new UnixDomainSocketEndPoint(span.Slice(0, nameLen));
				break;
			}
			if (_rightEndPoint == null)
			{
				return;
			}
			try
			{
				nameLen = span.Length;
				switch (SocketPal.GetPeerName(handle, span, ref nameLen))
				{
				case SocketError.Success:
					switch (_addressFamily)
					{
					case AddressFamily.InterNetwork:
						_remoteEndPoint = new IPEndPoint(new IPAddress((long)System.Net.SocketAddressPal.GetIPv4Address(span.Slice(0, nameLen)) & 0xFFFFFFFFL), System.Net.SocketAddressPal.GetPort(span));
						break;
					case AddressFamily.InterNetworkV6:
					{
						Span<byte> span3 = stackalloc byte[16];
						System.Net.SocketAddressPal.GetIPv6Address(span.Slice(0, nameLen), span3, out var scope2);
						_remoteEndPoint = new IPEndPoint(new IPAddress(span3, scope2), System.Net.SocketAddressPal.GetPort(span));
						break;
					}
					case AddressFamily.Unix:
						_remoteEndPoint = new UnixDomainSocketEndPoint(span.Slice(0, nameLen));
						break;
					}
					_isConnected = true;
					break;
				case SocketError.InvalidArgument:
					_isConnected = true;
					break;
				}
			}
			catch
			{
			}
		}
		catch
		{
			_handle = null;
			GC.SuppressFinalize(this);
			throw;
		}
	}

	private static SafeSocketHandle ValidateHandle(SafeSocketHandle handle)
	{
		ArgumentNullException.ThrowIfNull(handle, "handle");
		if (handle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Arg_InvalidHandle, "handle");
		}
		return handle;
	}

	internal bool CanTryAddressFamily(AddressFamily family)
	{
		if (family != _addressFamily)
		{
			if (family == AddressFamily.InterNetwork)
			{
				return IsDualMode;
			}
			return false;
		}
		return true;
	}

	public void Bind(EndPoint localEP)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, localEP, "Bind");
		}
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(localEP, "localEP");
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"localEP:{localEP}", "Bind");
		}
		SocketAddress socketAddress = Serialize(ref localEP);
		DoBind(localEP, socketAddress);
	}

	private void DoBind(EndPoint endPointSnapshot, SocketAddress socketAddress)
	{
		IPEndPoint iPEndPoint = endPointSnapshot as IPEndPoint;
		if (!OSSupportsIPv4 && iPEndPoint != null && iPEndPoint.Address.IsIPv4MappedToIPv6)
		{
			UpdateStatusAfterSocketErrorAndThrowException(SocketError.InvalidArgument, disconnectOnFailure: true, "DoBind");
		}
		SocketError socketError = SocketPal.Bind(_handle, _protocolType, socketAddress.Buffer.Span.Slice(0, socketAddress.Size));
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "DoBind");
		}
		_rightEndPoint = ((endPointSnapshot is UnixDomainSocketEndPoint unixDomainSocketEndPoint) ? unixDomainSocketEndPoint.CreateBoundEndPoint() : endPointSnapshot);
	}

	public void Connect(EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		if (_isDisconnected)
		{
			throw new InvalidOperationException(System.SR.net_sockets_disconnectedConnect);
		}
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		ThrowIfConnectedStreamSocket();
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"DST:{remoteEP}", "Connect");
		}
		if (remoteEP is DnsEndPoint dnsEndPoint)
		{
			if (dnsEndPoint.AddressFamily != 0 && !CanTryAddressFamily(dnsEndPoint.AddressFamily))
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			Connect(dnsEndPoint.Host, dnsEndPoint.Port);
		}
		else
		{
			SocketAddress socketAddress = Serialize(ref remoteEP);
			_pendingConnectRightEndPoint = remoteEP;
			_nonBlockingConnectInProgress = !Blocking;
			DoConnect(remoteEP, socketAddress);
		}
	}

	public void Connect(IPAddress address, int port)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(address, "address");
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		ThrowIfConnectedStreamSocket();
		if (!CanTryAddressFamily(address.AddressFamily))
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		IPEndPoint remoteEP = new IPEndPoint(address, port);
		Connect(remoteEP);
	}

	public void Connect(string host, int port)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(host, "host");
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		if (IPAddress.TryParse(host, out IPAddress address))
		{
			Connect(address, port);
			return;
		}
		IPAddress[] hostAddresses = Dns.GetHostAddresses(host);
		Connect(hostAddresses, port);
	}

	public void Connect(IPAddress[] addresses, int port)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(addresses, "addresses");
		if (addresses.Length == 0)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_ipaddress_length, "addresses");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		ThrowIfConnectedStreamSocket();
		ExceptionDispatchInfo exceptionDispatchInfo = null;
		foreach (IPAddress iPAddress in addresses)
		{
			if (CanTryAddressFamily(iPAddress.AddressFamily))
			{
				try
				{
					Connect(new IPEndPoint(iPAddress, port));
					exceptionDispatchInfo = null;
				}
				catch (Exception ex) when (!ExceptionCheck.IsFatal(ex))
				{
					exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
					continue;
				}
				break;
			}
		}
		exceptionDispatchInfo?.Throw();
		if (!Connected)
		{
			throw new ArgumentException(System.SR.net_invalidAddressList, "addresses");
		}
	}

	public void Close()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Close");
		}
		Dispose();
	}

	public void Close(int timeout)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(timeout, -1, "timeout");
		_closeTimeout = timeout;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Close");
		}
		Dispose();
	}

	public void Listen()
	{
		Listen(int.MaxValue);
	}

	public void Listen(int backlog)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, backlog, "Listen");
		}
		ThrowIfDisposed();
		SocketError socketError = SocketPal.Listen(_handle, backlog);
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Listen");
		}
		_isListening = true;
	}

	public Socket Accept()
	{
		ThrowIfDisposed();
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		if (!_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustlisten);
		}
		if (_isDisconnected)
		{
			throw new InvalidOperationException(System.SR.net_sockets_disconnectedAccept);
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint}", "Accept");
		}
		SocketAddress socketAddress = new SocketAddress(_addressFamily);
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AcceptStart(socketAddress);
		}
		SocketError socketError;
		SafeSocketHandle socket;
		try
		{
			socketError = SocketPal.Accept(_handle, socketAddress.Buffer, out var socketAddressSize, out socket);
			socketAddress.Size = socketAddressSize;
		}
		catch (Exception ex)
		{
			SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);
			throw;
		}
		if (socketError != 0)
		{
			UpdateAcceptSocketErrorForDisposed(ref socketError);
			SocketsTelemetry.Log.AfterAccept(socketError);
			socket.Dispose();
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Accept");
		}
		SocketsTelemetry.Log.AfterAccept(SocketError.Success);
		Socket socket2 = CreateAcceptSocket(socket, _rightEndPoint.Create(socketAddress));
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Accepted(socket2, socket2.RemoteEndPoint, socket2.LocalEndPoint);
		}
		return socket2;
	}

	public int Send(byte[] buffer, int size, SocketFlags socketFlags)
	{
		return Send(buffer, 0, size, socketFlags);
	}

	public int Send(byte[] buffer, SocketFlags socketFlags)
	{
		return Send(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags);
	}

	public int Send(byte[] buffer)
	{
		return Send(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None);
	}

	public int Send(IList<ArraySegment<byte>> buffers)
	{
		return Send(buffers, SocketFlags.None);
	}

	public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffers, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(buffers, "buffers");
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}", "Send");
		}
		errorCode = SocketPal.Send(_handle, buffers, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		return bytesTransferred;
	}

	public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffer, offset, size, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}", "Send");
		}
		errorCode = SocketPal.Send(_handle, buffer, offset, size, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Send returns:{bytesTransferred}", "Send");
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred, "Send");
		}
		return bytesTransferred;
	}

	public int Send(ReadOnlySpan<byte> buffer)
	{
		return Send(buffer, SocketFlags.None);
	}

	public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffer, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBlockingMode();
		errorCode = SocketPal.Send(_handle, buffer, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		return bytesTransferred;
	}

	public void SendFile(string? fileName)
	{
		SendFile(fileName, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread);
	}

	public void SendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags)
	{
		SendFile(fileName, preBuffer.AsSpan(), postBuffer.AsSpan(), flags);
	}

	public void SendFile(string? fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
	{
		ThrowIfDisposed();
		if (!IsConnectionOriented || !Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"::SendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}", "SendFile");
		}
		SendFileInternal(fileName, preBuffer, postBuffer, flags);
	}

	public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} size:{size} remoteEP:{remoteEP}", "SendTo");
		}
		SocketAddress socketAddress = Serialize(ref remoteEP);
		int bytesTransferred;
		SocketError socketError = SocketPal.SendTo(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer.Slice(0, socketAddress.Size), out bytesTransferred);
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "SendTo");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, size, "SendTo");
		}
		return bytesTransferred;
	}

	public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, size, socketFlags, remoteEP);
	}

	public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags, remoteEP);
	}

	public int SendTo(byte[] buffer, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None, remoteEP);
	}

	public int SendTo(ReadOnlySpan<byte> buffer, EndPoint remoteEP)
	{
		return SendTo(buffer, SocketFlags.None, remoteEP);
	}

	public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		ValidateBlockingMode();
		SocketAddress socketAddress = Serialize(ref remoteEP);
		int bytesTransferred;
		SocketError socketError = SocketPal.SendTo(_handle, buffer, socketFlags, socketAddress.Buffer.Slice(0, socketAddress.Size), out bytesTransferred);
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "SendTo");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		return bytesTransferred;
	}

	public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, SocketAddress socketAddress)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(socketAddress, "socketAddress");
		ValidateBlockingMode();
		int bytesTransferred;
		SocketError socketError = SocketPal.SendTo(_handle, buffer, socketFlags, socketAddress.Buffer.Slice(0, socketAddress.Size), out bytesTransferred);
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "SendTo");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		return bytesTransferred;
	}

	public int Receive(byte[] buffer, int size, SocketFlags socketFlags)
	{
		return Receive(buffer, 0, size, socketFlags);
	}

	public int Receive(byte[] buffer, SocketFlags socketFlags)
	{
		return Receive(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags);
	}

	public int Receive(byte[] buffer)
	{
		return Receive(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None);
	}

	public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffer, offset, size, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}", "Receive");
		}
		errorCode = SocketPal.Receive(_handle, buffer, offset, size, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred, "Receive");
		}
		return bytesTransferred;
	}

	public int Receive(Span<byte> buffer)
	{
		return Receive(buffer, SocketFlags.None);
	}

	public int Receive(Span<byte> buffer, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffer, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(Span<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBlockingMode();
		errorCode = SocketPal.Receive(_handle, buffer, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		return bytesTransferred;
	}

	public int Receive(IList<ArraySegment<byte>> buffers)
	{
		return Receive(buffers, SocketFlags.None);
	}

	public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffers, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(buffers, "buffers");
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}", "Receive");
		}
		errorCode = SocketPal.Receive(_handle, buffers, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		return bytesTransferred;
	}

	public int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		ValidateBlockingMode();
		EndPoint remoteEP2 = remoteEP;
		SocketAddress socketAddress = Serialize(ref remoteEP2);
		SetReceivingPacketInformation();
		SocketAddress receiveAddress;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveMessageFrom(this, _handle, buffer, offset, size, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		if (socketError != 0 && socketError != SocketError.MessageSize)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "ReceiveMessageFrom");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketError == SocketError.Success && SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!System.Net.Sockets.SocketAddressExtensions.Equals(socketAddress, remoteEP))
		{
			try
			{
				remoteEP = remoteEP2.Create(receiveAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, socketError, "ReceiveMessageFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveMessageFrom(Span<byte> buffer, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		if (!CanTryAddressFamily(remoteEP.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, remoteEP.AddressFamily, _addressFamily), "remoteEP");
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		ValidateBlockingMode();
		EndPoint remoteEP2 = remoteEP;
		SocketAddress socketAddress = Serialize(ref remoteEP2);
		SetReceivingPacketInformation();
		SocketAddress receiveAddress;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveMessageFrom(this, _handle, buffer, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		if (socketError != 0 && socketError != SocketError.MessageSize)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "ReceiveMessageFrom");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketError == SocketError.Success && SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!System.Net.Sockets.SocketAddressExtensions.Equals(socketAddress, remoteEP))
		{
			try
			{
				remoteEP = remoteEP2.Create(receiveAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, socketError, "ReceiveMessageFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC{LocalEndPoint} size:{size} remoteEP:{remoteEP}", "ReceiveFrom");
		}
		EndPoint endPoint = remoteEP;
		SocketAddress socketAddress = new SocketAddress(AddressFamily);
		if (endPoint.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
		{
			endPoint = s_IPEndPointIPv6;
		}
		int addressLength;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveFrom(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer, out addressLength, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		SocketException ex = null;
		if (socketError != 0)
		{
			ex = new SocketException((int)socketError);
			UpdateStatusAfterSocketError(ex);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "ReceiveFrom");
			}
			if (ex.SocketErrorCode != SocketError.MessageSize)
			{
				throw ex;
			}
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		socketAddress.Size = addressLength;
		if ((addressLength > 0 && !socketAddress.Equals(remoteEP)) || remoteEP.AddressFamily != socketAddress.Family)
		{
			try
			{
				if (endPoint.AddressFamily == socketAddress.Family)
				{
					remoteEP = endPoint.Create(socketAddress);
				}
				else if (AddressFamily == AddressFamily.InterNetworkV6 && socketAddress.Family == AddressFamily.InterNetwork)
				{
					remoteEP = new IPEndPoint(socketAddress.GetIPAddress().MapToIPv6(), socketAddress.GetPort());
				}
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = endPoint;
			}
		}
		if (ex != null)
		{
			throw ex;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, size, "ReceiveFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, size, socketFlags, ref remoteEP);
	}

	public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags, ref remoteEP);
	}

	public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None, ref remoteEP);
	}

	public int ReceiveFrom(Span<byte> buffer, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);
	}

	public int ReceiveFrom(Span<byte> buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		ValidateBlockingMode();
		EndPoint endPoint = remoteEP;
		SocketAddress socketAddress = new SocketAddress(AddressFamily);
		if (endPoint.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
		{
			endPoint = s_IPEndPointIPv6;
		}
		int addressLength;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveFrom(_handle, buffer, socketFlags, socketAddress.Buffer, out addressLength, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		SocketException ex = null;
		if (socketError != 0)
		{
			ex = new SocketException((int)socketError);
			UpdateStatusAfterSocketError(ex);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "ReceiveFrom");
			}
			if (ex.SocketErrorCode != SocketError.MessageSize)
			{
				throw ex;
			}
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		socketAddress.Size = addressLength;
		if ((addressLength > 0 && !socketAddress.Equals(remoteEP)) || remoteEP.AddressFamily != socketAddress.Family)
		{
			try
			{
				if (endPoint.AddressFamily == socketAddress.Family)
				{
					remoteEP = endPoint.Create(socketAddress);
				}
				else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6 && socketAddress.Family == AddressFamily.InterNetwork)
				{
					remoteEP = new IPEndPoint(socketAddress.GetIPAddress().MapToIPv6(), socketAddress.GetPort());
				}
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = endPoint;
			}
		}
		if (ex != null)
		{
			throw ex;
		}
		return bytesTransferred;
	}

	public int ReceiveFrom(Span<byte> buffer, SocketFlags socketFlags, SocketAddress receivedAddress)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(receivedAddress, "receivedAddress");
		if (receivedAddress.Size < SocketAddress.GetMaximumAddressSize(AddressFamily))
		{
			throw new ArgumentOutOfRangeException("receivedAddress", System.SR.net_sockets_address_small);
		}
		ValidateBlockingMode();
		int addressLength;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveFrom(_handle, buffer, socketFlags, receivedAddress.Buffer, out addressLength, out bytesTransferred);
		receivedAddress.Size = addressLength;
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		if (socketError != 0)
		{
			SocketException ex = new SocketException((int)socketError);
			UpdateStatusAfterSocketError(ex);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "ReceiveFrom");
			}
			throw ex;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		return bytesTransferred;
	}

	public int IOControl(int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
	{
		ThrowIfDisposed();
		int optionLength;
		SocketError socketError = SocketPal.WindowsIoctl(_handle, ioControlCode, optionInValue, optionOutValue, out optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"WindowsIoctl returns errorCode:{socketError}", "IOControl");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "IOControl");
		}
		return optionLength;
	}

	public int IOControl(IOControlCode ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
	{
		return IOControl((int)ioControlCode, optionInValue, optionOutValue);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		SetSocketOption(optionLevel, optionName, optionValue, silent: false);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		SocketError socketError = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetSockOpt returns errorCode:{socketError}", "SetSocketOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(socketError, "SetSocketOption");
		}
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
	{
		SetSocketOption(optionLevel, optionName, optionValue ? 1 : 0);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(optionValue, "optionValue");
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
		{
			if (!(optionValue is LingerOption lingerOption))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "LingerOption"), "optionValue");
			}
			if (lingerOption.LingerTime < 0 || lingerOption.LingerTime > 65535)
			{
				throw new ArgumentException(System.SR.Format(System.SR.ArgumentOutOfRange_Bounds_Lower_Upper_Named, 0, 65535, "optionValue.LingerTime"), "optionValue");
			}
			SetLingerOption(lingerOption);
		}
		else if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			if (!(optionValue is MulticastOption mR))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "MulticastOption"), "optionValue");
			}
			SetMulticastOption(optionName, mR);
		}
		else
		{
			if (optionLevel != SocketOptionLevel.IPv6 || (optionName != SocketOptionName.AddMembership && optionName != SocketOptionName.DropMembership))
			{
				throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "optionValue");
			}
			if (!(optionValue is IPv6MulticastOption mR2))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "IPv6MulticastOption"), "optionValue");
			}
			SetIPv6MulticastOption(optionName, mR2);
		}
	}

	public void SetRawSocketOption(int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
	{
		ThrowIfDisposed();
		SocketError socketError = SocketPal.SetRawSockOpt(_handle, optionLevel, optionName, optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetSockOpt optionLevel:{optionLevel} optionName:{optionName} returns errorCode:{socketError}", "SetRawSocketOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(socketError, "SetRawSocketOption");
		}
	}

	public object? GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
	{
		ThrowIfDisposed();
		if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
		{
			return GetLingerOpt();
		}
		if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			return GetMulticastOpt(optionName);
		}
		if (optionLevel == SocketOptionLevel.IPv6 && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			return GetIPv6MulticastOpt(optionName);
		}
		int optionValue;
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(sockOpt, "GetSocketOption");
		}
		return optionValue;
	}

	public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
	{
		ThrowIfDisposed();
		int optionLength = ((optionValue != null) ? optionValue.Length : 0);
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, optionValue, ref optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(sockOpt, "GetSocketOption");
		}
	}

	public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
	{
		ThrowIfDisposed();
		byte[] array = new byte[optionLength];
		int optionLength2 = optionLength;
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, array, ref optionLength2);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(sockOpt, "GetSocketOption");
		}
		if (optionLength != optionLength2)
		{
			byte[] array2 = new byte[optionLength2];
			Buffer.BlockCopy(array, 0, array2, 0, optionLength2);
			array = array2;
		}
		return array;
	}

	public int GetRawSocketOption(int optionLevel, int optionName, Span<byte> optionValue)
	{
		ThrowIfDisposed();
		int optionLength = optionValue.Length;
		SocketError rawSockOpt = SocketPal.GetRawSockOpt(_handle, optionLevel, optionName, optionValue, ref optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetRawSockOpt optionLevel:{optionLevel} optionName:{optionName} returned errorCode:{rawSockOpt}", "GetRawSocketOption");
		}
		if (rawSockOpt != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(rawSockOpt, "GetRawSocketOption");
		}
		return optionLength;
	}

	[SupportedOSPlatform("windows")]
	public void SetIPProtectionLevel(IPProtectionLevel level)
	{
		if (level == IPProtectionLevel.Unspecified)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "level");
		}
		if (_addressFamily == AddressFamily.InterNetworkV6)
		{
			SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IPv6, (int)level);
			return;
		}
		if (_addressFamily == AddressFamily.InterNetwork)
		{
			SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IP, (int)level);
			return;
		}
		throw new NotSupportedException(System.SR.net_invalidversion);
	}

	public bool Poll(int microSeconds, SelectMode mode)
	{
		ThrowIfDisposed();
		bool status;
		SocketError socketError = SocketPal.Poll(_handle, microSeconds, mode, out status);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Poll returns socketCount:{(int)socketError}", "Poll");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Poll");
		}
		return status;
	}

	public bool Poll(TimeSpan timeout, SelectMode mode)
	{
		return Poll(ToTimeoutMicroseconds(timeout), mode);
	}

	public static void Select(IList? checkRead, IList? checkWrite, IList? checkError, int microSeconds)
	{
		if ((checkRead == null || checkRead.Count == 0) && (checkWrite == null || checkWrite.Count == 0) && (checkError == null || checkError.Count == 0))
		{
			throw new ArgumentNullException(null, System.SR.net_sockets_empty_select);
		}
		if (checkRead != null && checkRead.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkRead", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkRead", 65536.ToString()));
		}
		if (checkWrite != null && checkWrite.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkWrite", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkWrite", 65536.ToString()));
		}
		if (checkError != null && checkError.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkError", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkError", 65536.ToString()));
		}
		SocketError socketError = SocketPal.Select(checkRead, checkWrite, checkError, microSeconds);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
	}

	public static void Select(IList? checkRead, IList? checkWrite, IList? checkError, TimeSpan timeout)
	{
		Select(checkRead, checkWrite, checkError, ToTimeoutMicroseconds(timeout));
	}

	private static int ToTimeoutMicroseconds(TimeSpan timeout)
	{
		if (timeout == Timeout.InfiniteTimeSpan)
		{
			return -1;
		}
		ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero, "timeout");
		long num = (long)timeout.TotalMicroseconds;
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return (int)num;
	}

	public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(ConnectAsync(remoteEP), callback, state);
	}

	public IAsyncResult BeginConnect(string host, int port, AsyncCallback? requestCallback, object? state)
	{
		return TaskToAsyncResult.Begin(ConnectAsync(host, port), requestCallback, state);
	}

	public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback? requestCallback, object? state)
	{
		return TaskToAsyncResult.Begin(ConnectAsync(address, port), requestCallback, state);
	}

	public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback? requestCallback, object? state)
	{
		return TaskToAsyncResult.Begin(ConnectAsync(addresses, port), requestCallback, state);
	}

	public void EndConnect(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(DisconnectAsync(reuseSocket).AsTask(), callback, state);
	}

	public void Disconnect(bool reuseSocket)
	{
		ThrowIfDisposed();
		SocketError socketError = SocketPal.Disconnect(this, _handle, reuseSocket);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"UnsafeNclNativeMethods.OSSOCK.DisConnectEx returns:{socketError}", "Disconnect");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Disconnect");
		}
		SetToDisconnected();
		_remoteEndPoint = null;
		_localEndPoint = null;
	}

	public void EndDisconnect(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		return TaskToAsyncResult.Begin(SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags).AsTask(), callback, state);
	}

	public IAsyncResult? BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		Task<int> task = SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags).AsTask();
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		return TaskToAsyncResult.Begin(SendAsync(buffers, socketFlags), callback, state);
	}

	public IAsyncResult? BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		Task<int> task = SendAsync(buffers, socketFlags);
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public int EndSend(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public int EndSend(IAsyncResult asyncResult, out SocketError errorCode)
	{
		return EndSendReceive(asyncResult, out errorCode);
	}

	public IAsyncResult BeginSendFile(string? fileName, AsyncCallback? callback, object? state)
	{
		return BeginSendFile(fileName, null, null, TransmitFileOptions.UseDefaultWorkerThread, callback, state);
	}

	public IAsyncResult BeginSendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"::DoBeginSendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}", "BeginSendFile");
		}
		return TaskToAsyncResult.Begin(SendFileAsync(fileName, preBuffer, postBuffer, flags).AsTask(), callback, state);
	}

	public void EndSendFile(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		Task<int> task = SendToAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public int EndSendTo(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		return TaskToAsyncResult.Begin(ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default(CancellationToken)).AsTask(), callback, state);
	}

	public IAsyncResult? BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		Task<int> task = ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default(CancellationToken)).AsTask();
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		return TaskToAsyncResult.Begin(ReceiveAsync(buffers, socketFlags), callback, state);
	}

	public IAsyncResult? BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		Task<int> task = ReceiveAsync(buffers, socketFlags);
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public int EndReceive(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public int EndReceive(IAsyncResult asyncResult, out SocketError errorCode)
	{
		return EndSendReceive(asyncResult, out errorCode);
	}

	private static int EndSendReceive(IAsyncResult asyncResult, out SocketError errorCode)
	{
		Task<int> task = TaskToAsyncResult.Unwrap<int>(asyncResult);
		((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
		if (task.IsCompletedSuccessfully)
		{
			errorCode = SocketError.Success;
			return task.Result;
		}
		errorCode = GetSocketErrorFromFaultedTask(task);
		return 0;
	}

	public IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"size:{size}", "BeginReceiveMessageFrom");
		}
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		Task<SocketReceiveMessageFromResult> task = ReceiveMessageFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		if (task.IsCompletedSuccessfully)
		{
			EndPoint remoteEndPoint = task.Result.RemoteEndPoint;
			if (!remoteEP.Equals(remoteEndPoint))
			{
				remoteEP = remoteEndPoint;
			}
		}
		IAsyncResult asyncResult = TaskToAsyncResult.Begin(task, callback, state);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"size:{size} returning AsyncResult:{asyncResult}", "BeginReceiveMessageFrom");
		}
		return asyncResult;
	}

	public int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation)
	{
		ArgumentNullException.ThrowIfNull(endPoint, "endPoint");
		if (!CanTryAddressFamily(endPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), "endPoint");
		}
		SocketReceiveMessageFromResult socketReceiveMessageFromResult = TaskToAsyncResult.End<SocketReceiveMessageFromResult>(asyncResult);
		if (!endPoint.Equals(socketReceiveMessageFromResult.RemoteEndPoint))
		{
			endPoint = socketReceiveMessageFromResult.RemoteEndPoint;
		}
		socketFlags = socketReceiveMessageFromResult.SocketFlags;
		ipPacketInformation = socketReceiveMessageFromResult.PacketInformation;
		return socketReceiveMessageFromResult.ReceivedBytes;
	}

	public IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		Task<SocketReceiveFromResult> task = ReceiveFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		if (task.IsCompletedSuccessfully)
		{
			EndPoint remoteEndPoint = task.Result.RemoteEndPoint;
			if (!remoteEP.Equals(remoteEndPoint))
			{
				remoteEP = remoteEndPoint;
			}
		}
		return TaskToAsyncResult.Begin(task, callback, state);
	}

	public int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
	{
		ArgumentNullException.ThrowIfNull(endPoint, "endPoint");
		if (!CanTryAddressFamily(endPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), "endPoint");
		}
		SocketReceiveFromResult socketReceiveFromResult = TaskToAsyncResult.End<SocketReceiveFromResult>(asyncResult);
		if (!endPoint.Equals(socketReceiveFromResult.RemoteEndPoint))
		{
			endPoint = socketReceiveFromResult.RemoteEndPoint;
		}
		return socketReceiveFromResult.ReceivedBytes;
	}

	public IAsyncResult BeginAccept(AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(AcceptAsync(), callback, state);
	}

	public Socket EndAccept(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<Socket>(asyncResult);
	}

	private async Task<(Socket s, byte[] buffer, int bytesReceived)> AcceptAndReceiveHelperAsync(Socket acceptSocket, int receiveSize)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(receiveSize, "receiveSize");
		Socket s = await AcceptAsync(acceptSocket).ConfigureAwait(continueOnCapturedContext: false);
		byte[] buffer;
		int item;
		if (receiveSize == 0)
		{
			buffer = Array.Empty<byte>();
			item = 0;
		}
		else
		{
			buffer = new byte[receiveSize];
			try
			{
				item = await s.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				s.Dispose();
				throw;
			}
		}
		return (s: s, buffer: buffer, bytesReceived: item);
	}

	public IAsyncResult BeginAccept(int receiveSize, AsyncCallback? callback, object? state)
	{
		return BeginAccept(null, receiveSize, callback, state);
	}

	public IAsyncResult BeginAccept(Socket? acceptSocket, int receiveSize, AsyncCallback? callback, object? state)
	{
		return TaskToAsyncResult.Begin(AcceptAndReceiveHelperAsync(acceptSocket, receiveSize), callback, state);
	}

	public Socket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
	{
		byte[] buffer2;
		int bytesTransferred;
		Socket result = EndAccept(out buffer2, out bytesTransferred, asyncResult);
		buffer = new byte[bytesTransferred];
		Buffer.BlockCopy(buffer2, 0, buffer, 0, bytesTransferred);
		return result;
	}

	public Socket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
	{
		Socket result;
		(result, buffer, bytesTransferred) = TaskToAsyncResult.End<(Socket, byte[], int)>(asyncResult);
		return result;
	}

	public void Shutdown(SocketShutdown how)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"how:{how}", "Shutdown");
		}
		SocketError socketError = SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Shutdown returns errorCode:{socketError}", "Shutdown");
		}
		if (socketError != 0 && socketError != SocketError.NotSocket)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "Shutdown");
		}
		SetToDisconnected();
		InternalSetBlocking(_willBlockInternal);
	}

	public bool AcceptAsync(SocketAsyncEventArgs e)
	{
		return AcceptAsync(e, CancellationToken.None);
	}

	private bool AcceptAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "e");
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		if (!_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustlisten);
		}
		e.AcceptSocket = GetOrCreateAcceptSocket(e.AcceptSocket, checkDisconnected: true, "AcceptSocket", out var handle);
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AcceptStart(_rightEndPoint);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Accept);
		e.StartOperationAccept();
		SocketError socketError;
		try
		{
			socketError = e.DoOperationAccept(this, _handle, handle, cancellationToken);
		}
		catch (Exception ex)
		{
			SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ConnectAsync(SocketAsyncEventArgs e)
	{
		return ConnectAsync(e, userSocket: true, saeaCancelable: true);
	}

	internal bool ConnectAsync(SocketAsyncEventArgs e, bool userSocket, bool saeaCancelable)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "BufferList");
		}
		ArgumentNullException.ThrowIfNull(e.RemoteEndPoint, "remoteEP");
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		ThrowIfConnectedStreamSocket();
		EndPoint remoteEP = e.RemoteEndPoint;
		if (remoteEP is DnsEndPoint dnsEndPoint)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.ConnectedAsyncDns(this);
			}
			if (dnsEndPoint.AddressFamily != 0 && !CanTryAddressFamily(dnsEndPoint.AddressFamily))
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			e.StartOperationCommon(this, SocketAsyncOperation.Connect);
			e.StartOperationConnect(saeaCancelable, userSocket);
			try
			{
				return e.DnsConnectAsync(dnsEndPoint, (SocketType)0, ProtocolType.IP);
			}
			catch
			{
				e.Complete();
				throw;
			}
		}
		if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		e._socketAddress = Serialize(ref remoteEP);
		_pendingConnectRightEndPoint = remoteEP;
		_nonBlockingConnectInProgress = false;
		WildcardBindForConnectIfNecessary(remoteEP.AddressFamily);
		SocketsTelemetry.Log.ConnectStart(e._socketAddress);
		try
		{
			e.StartOperationCommon(this, SocketAsyncOperation.Connect);
			e.StartOperationConnect(saeaMultiConnectCancelable: false, userSocket);
		}
		catch (Exception ex)
		{
			SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
			throw;
		}
		try
		{
			SocketError socketError = ((_socketType == SocketType.Stream && remoteEP.AddressFamily != AddressFamily.Unix) ? e.DoOperationConnectEx(this, _handle) : e.DoOperationConnect(_handle));
			return socketError == SocketError.IOPending;
		}
		catch (Exception ex2)
		{
			SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex2.Message);
			_localEndPoint = null;
			e.Complete();
			throw;
		}
	}

	public static bool ConnectAsync(SocketType socketType, ProtocolType protocolType, SocketAsyncEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(e, "e");
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "e");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		EndPoint remoteEndPoint = e.RemoteEndPoint;
		if (remoteEndPoint is DnsEndPoint dnsEndPoint)
		{
			Socket socket = ((dnsEndPoint.AddressFamily != 0) ? new Socket(dnsEndPoint.AddressFamily, socketType, protocolType) : null);
			e.StartOperationCommon(socket, SocketAsyncOperation.Connect);
			e.StartOperationConnect(saeaMultiConnectCancelable: true, userSocket: false);
			try
			{
				return e.DnsConnectAsync(dnsEndPoint, socketType, protocolType);
			}
			catch
			{
				e.Complete();
				throw;
			}
		}
		Socket socket2 = new Socket(remoteEndPoint.AddressFamily, socketType, protocolType);
		return socket2.ConnectAsync(e, userSocket: false, saeaCancelable: true);
	}

	private void WildcardBindForConnectIfNecessary(AddressFamily addressFamily)
	{
		if (_rightEndPoint == null)
		{
			CachedSerializedEndPoint cachedSerializedEndPoint;
			switch (addressFamily)
			{
			default:
				return;
			case AddressFamily.InterNetwork:
				cachedSerializedEndPoint = (IsDualMode ? (s_cachedMappedAnyV6EndPoint ?? (s_cachedMappedAnyV6EndPoint = new CachedSerializedEndPoint(s_IPAddressAnyMapToIPv6))) : (s_cachedAnyEndPoint ?? (s_cachedAnyEndPoint = new CachedSerializedEndPoint(IPAddress.Any))));
				break;
			case AddressFamily.InterNetworkV6:
				cachedSerializedEndPoint = s_cachedAnyV6EndPoint ?? (s_cachedAnyV6EndPoint = new CachedSerializedEndPoint(IPAddress.IPv6Any));
				break;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, cachedSerializedEndPoint.IPEndPoint, "WildcardBindForConnectIfNecessary");
			}
			if (_socketType == SocketType.Stream && _protocolType == ProtocolType.Tcp)
			{
				EnableReuseUnicastPort();
			}
			DoBind(cachedSerializedEndPoint.IPEndPoint, cachedSerializedEndPoint.SocketAddress);
		}
	}

	public static void CancelConnectAsync(SocketAsyncEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(e, "e");
		e.CancelConnectAsync();
	}

	public bool DisconnectAsync(SocketAsyncEventArgs e)
	{
		return DisconnectAsync(e, default(CancellationToken));
	}

	private bool DisconnectAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		e.StartOperationCommon(this, SocketAsyncOperation.Disconnect);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationDisconnect(this, _handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveAsync(SocketAsyncEventArgs e)
	{
		return ReceiveAsync(e, default(CancellationToken));
	}

	private bool ReceiveAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		e.StartOperationCommon(this, SocketAsyncOperation.Receive);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceive(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveFromAsync(SocketAsyncEventArgs e)
	{
		return ReceiveFromAsync(e, default(CancellationToken));
	}

	private bool ReceiveFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		EndPoint remoteEndPoint = e.RemoteEndPoint;
		if (e._socketAddress == null)
		{
			if (remoteEndPoint is DnsEndPoint)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_dnsendpoint, "e.RemoteEndPoint"), "e");
			}
			if (remoteEndPoint == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
			}
			if (!CanTryAddressFamily(remoteEndPoint.AddressFamily))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, remoteEndPoint.AddressFamily, _addressFamily), "e");
			}
			if (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
			{
				remoteEndPoint = s_IPEndPointIPv6;
			}
			if (e._socketAddress == null)
			{
				e._socketAddress = new SocketAddress(AddressFamily);
			}
		}
		e.RemoteEndPoint = remoteEndPoint;
		e.StartOperationCommon(this, SocketAsyncOperation.ReceiveFrom);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceiveFrom(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e)
	{
		return ReceiveMessageFromAsync(e, default(CancellationToken));
	}

	private bool ReceiveMessageFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, e.RemoteEndPoint.AddressFamily, _addressFamily), "e");
		}
		EndPoint remoteEP = e.RemoteEndPoint;
		e._socketAddress = Serialize(ref remoteEP);
		e.RemoteEndPoint = remoteEP;
		SetReceivingPacketInformation();
		e.StartOperationCommon(this, SocketAsyncOperation.ReceiveMessageFrom);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceiveMessageFrom(this, _handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendAsync(SocketAsyncEventArgs e)
	{
		return SendAsync(e, default(CancellationToken));
	}

	private bool SendAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		e.StartOperationCommon(this, SocketAsyncOperation.Send);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSend(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendPacketsAsync(SocketAsyncEventArgs e)
	{
		return SendPacketsAsync(e, default(CancellationToken));
	}

	private bool SendPacketsAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		if (e.SendPacketsElements == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.SendPacketsElements"), "e");
		}
		if (!Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.SendPackets);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSendPackets(this, _handle, cancellationToken);
		}
		catch (Exception)
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendToAsync(SocketAsyncEventArgs e)
	{
		return SendToAsync(e, default(CancellationToken));
	}

	private bool SendToAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(e, "e");
		EndPoint remoteEP = e.RemoteEndPoint;
		if (e._socketAddress == null)
		{
			if (remoteEP == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
			}
			e._socketAddress = Serialize(ref remoteEP);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.SendTo);
		EndPoint rightEndPoint = _rightEndPoint;
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSendTo(_handle, cancellationToken);
		}
		catch
		{
			_rightEndPoint = rightEndPoint;
			_localEndPoint = null;
			e.Complete();
			throw;
		}
		if (!CheckErrorAndUpdateStatus(socketError))
		{
			_rightEndPoint = rightEndPoint;
			_localEndPoint = null;
		}
		return socketError == SocketError.IOPending;
	}

	internal static void GetIPProtocolInformation(AddressFamily addressFamily, SocketAddress socketAddress, out bool isIPv4, out bool isIPv6)
	{
		bool flag = socketAddress.Family == AddressFamily.InterNetworkV6 && socketAddress.GetIPAddress().IsIPv4MappedToIPv6;
		isIPv4 = addressFamily == AddressFamily.InterNetwork || flag;
		isIPv6 = addressFamily == AddressFamily.InterNetworkV6;
	}

	internal static int GetAddressSize(EndPoint endPoint)
	{
		return endPoint.AddressFamily switch
		{
			AddressFamily.InterNetworkV6 => 28, 
			AddressFamily.InterNetwork => 16, 
			_ => endPoint.Serialize().Size, 
		};
	}

	private SocketAddress Serialize(ref EndPoint remoteEP)
	{
		if (remoteEP is IPEndPoint iPEndPoint)
		{
			IPAddress address = iPEndPoint.Address;
			if (address.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
			{
				address = address.MapToIPv6();
				remoteEP = new IPEndPoint(address, iPEndPoint.Port);
			}
		}
		else if (remoteEP is DnsEndPoint)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_dnsendpoint, "remoteEP"), "remoteEP");
		}
		return remoteEP.Serialize();
	}

	private void DoConnect(EndPoint endPointSnapshot, SocketAddress socketAddress)
	{
		SocketsTelemetry.Log.ConnectStart(socketAddress);
		SocketError socketError;
		try
		{
			socketError = SocketPal.Connect(_handle, socketAddress.Buffer.Slice(0, socketAddress.Size));
		}
		catch (Exception ex)
		{
			SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
			throw;
		}
		if (socketError != 0)
		{
			UpdateConnectSocketErrorForDisposed(ref socketError);
			SocketException ex2 = SocketExceptionFactory.CreateSocketException((int)socketError, endPointSnapshot);
			UpdateStatusAfterSocketError(ex2);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex2, "DoConnect");
			}
			SocketsTelemetry.Log.AfterConnect(socketError);
			throw ex2;
		}
		SocketsTelemetry.Log.AfterConnect(SocketError.Success);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"connection to:{endPointSnapshot}", "DoConnect");
		}
		_pendingConnectRightEndPoint = endPointSnapshot;
		_nonBlockingConnectInProgress = false;
		SetToConnected();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Connected(this, LocalEndPoint, RemoteEndPoint);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			try
			{
				System.Net.NetEventSource.Info(this, $"disposing:{disposing} Disposed:{Disposed}", "Dispose");
			}
			catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
			{
			}
		}
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
		{
			return;
		}
		SetToDisconnected();
		SafeSocketHandle handle = _handle;
		if (handle != null)
		{
			if (!disposing)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling _handle.Dispose()", "Dispose");
				}
				handle.Dispose();
			}
			else if (handle.OwnsHandle)
			{
				try
				{
					int closeTimeout = _closeTimeout;
					if (closeTimeout == 0)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "Calling _handle.CloseAsIs()", "Dispose");
						}
						handle.CloseAsIs(abortive: true);
					}
					else
					{
						if (!_willBlock || !_willBlockInternal)
						{
							bool willBlock;
							SocketError socketError = SocketPal.SetBlocking(handle, shouldBlock: false, out willBlock);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONBIO):{socketError}", "Dispose");
							}
						}
						if (closeTimeout < 0)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, "Calling _handle.CloseAsIs()", "Dispose");
							}
							handle.CloseAsIs(abortive: false);
						}
						else
						{
							SocketError socketError = SocketPal.Shutdown(handle, _isConnected, _isDisconnected, SocketShutdown.Send);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} shutdown():{socketError}", "Dispose");
							}
							socketError = SocketPal.SetSockOpt(handle, SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, closeTimeout);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} setsockopt():{socketError}", "Dispose");
							}
							if (socketError != 0)
							{
								handle.CloseAsIs(abortive: true);
							}
							else
							{
								socketError = SocketPal.Receive(handle, Array.Empty<byte>(), 0, 0, SocketFlags.None, out var _);
								if (System.Net.NetEventSource.Log.IsEnabled())
								{
									System.Net.NetEventSource.Info(this, $"handle:{handle} recv():{socketError}", "Dispose");
								}
								if (socketError != 0)
								{
									handle.CloseAsIs(abortive: true);
								}
								else
								{
									int available = 0;
									socketError = SocketPal.GetAvailable(handle, out available);
									if (System.Net.NetEventSource.Log.IsEnabled())
									{
										System.Net.NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONREAD):{socketError}", "Dispose");
									}
									if (socketError != 0 || available != 0)
									{
										handle.CloseAsIs(abortive: true);
									}
									else
									{
										handle.CloseAsIs(abortive: false);
									}
								}
							}
						}
					}
				}
				catch (ObjectDisposedException)
				{
				}
			}
			else
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling _handle.CloseAsIs() for non-owned handle", "Dispose");
				}
				handle.CloseAsIs(abortive: false);
			}
			if (_rightEndPoint is UnixDomainSocketEndPoint { BoundFileName: not null } unixDomainSocketEndPoint)
			{
				try
				{
					File.Delete(unixDomainSocketEndPoint.BoundFileName);
				}
				catch
				{
				}
			}
		}
		DisposeCachedTaskSocketAsyncEventArgs();
	}

	public void Dispose()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Dispose");
		}
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~Socket()
	{
		Dispose(disposing: false);
	}

	internal void InternalShutdown(SocketShutdown how)
	{
		if (Disposed || _handle.IsInvalid)
		{
			return;
		}
		try
		{
			SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	internal void SetReceivingPacketInformation()
	{
		if (!_receivingPacketInformation)
		{
			IPAddress iPAddress = ((_rightEndPoint is IPEndPoint iPEndPoint) ? iPEndPoint.Address : null);
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, optionValue: true);
			}
			if (iPAddress != null && IsDualMode && (iPAddress.IsIPv4MappedToIPv6 || iPAddress.Equals(IPAddress.IPv6Any)))
			{
				SocketPal.SetReceivingDualModeIPv4PacketInformation(this);
			}
			if (_addressFamily == AddressFamily.InterNetworkV6 && (iPAddress == null || !iPAddress.IsIPv4MappedToIPv6))
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, optionValue: true);
			}
			_receivingPacketInformation = true;
		}
	}

	internal void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue, bool silent)
	{
		if (silent && (Disposed || _handle.IsInvalid))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "skipping the call", "SetSocketOption");
			}
			return;
		}
		SocketError socketError;
		try
		{
			socketError = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"SetSockOpt returns errorCode:{socketError}", "SetSocketOption");
			}
		}
		catch
		{
			if (silent && _handle.IsInvalid)
			{
				return;
			}
			throw;
		}
		if (optionName == SocketOptionName.PacketInformation && optionValue == 0 && socketError == SocketError.Success)
		{
			_receivingPacketInformation = false;
		}
		if (!silent && socketError != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(socketError, "SetSocketOption");
		}
	}

	private void SetMulticastOption(SocketOptionName optionName, MulticastOption MR)
	{
		SocketError socketError = SocketPal.SetMulticastOption(_handle, optionName, MR);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetMulticastOption returns errorCode:{socketError}", "SetMulticastOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(socketError, "SetMulticastOption");
		}
	}

	private void SetIPv6MulticastOption(SocketOptionName optionName, IPv6MulticastOption MR)
	{
		SocketError socketError = SocketPal.SetIPv6MulticastOption(_handle, optionName, MR);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetIPv6MulticastOption returns errorCode:{socketError}", "SetIPv6MulticastOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "SetIPv6MulticastOption");
		}
	}

	private void SetLingerOption(LingerOption lref)
	{
		SocketError socketError = SocketPal.SetLingerOption(_handle, lref);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetLingerOption returns errorCode:{socketError}", "SetLingerOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(socketError, "SetLingerOption");
		}
	}

	private LingerOption GetLingerOpt()
	{
		LingerOption optionValue;
		SocketError lingerOption = SocketPal.GetLingerOption(_handle, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetLingerOption returns errorCode:{lingerOption}", "GetLingerOpt");
		}
		if (lingerOption != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(lingerOption, "GetLingerOpt");
		}
		return optionValue;
	}

	private MulticastOption GetMulticastOpt(SocketOptionName optionName)
	{
		MulticastOption optionValue;
		SocketError multicastOption = SocketPal.GetMulticastOption(_handle, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetMulticastOption returns errorCode:{multicastOption}", "GetMulticastOpt");
		}
		if (multicastOption != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(multicastOption, "GetMulticastOpt");
		}
		return optionValue;
	}

	private IPv6MulticastOption GetIPv6MulticastOpt(SocketOptionName optionName)
	{
		IPv6MulticastOption optionValue;
		SocketError iPv6MulticastOption = SocketPal.GetIPv6MulticastOption(_handle, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetIPv6MulticastOption returns errorCode:{iPv6MulticastOption}", "GetIPv6MulticastOpt");
		}
		if (iPv6MulticastOption != 0)
		{
			UpdateStatusAfterSocketOptionErrorAndThrowException(iPv6MulticastOption, "GetIPv6MulticastOpt");
		}
		return optionValue;
	}

	private SocketError InternalSetBlocking(bool desired, out bool current)
	{
		if (Disposed)
		{
			current = _willBlock;
			return SocketError.Success;
		}
		bool willBlock = false;
		SocketError socketError;
		try
		{
			socketError = SocketPal.SetBlocking(_handle, desired, out willBlock);
		}
		catch (ObjectDisposedException)
		{
			socketError = SocketError.NotSocket;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetBlocking returns errorCode:{socketError}", "InternalSetBlocking");
		}
		if (socketError == SocketError.Success)
		{
			_willBlockInternal = willBlock;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"errorCode:{socketError} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}", "InternalSetBlocking");
		}
		current = _willBlockInternal;
		return socketError;
	}

	internal void InternalSetBlocking(bool desired)
	{
		InternalSetBlocking(desired, out var _);
	}

	internal Socket CreateAcceptSocket(SafeSocketHandle fd, EndPoint remoteEP)
	{
		Socket socket = new Socket(fd, loadPropertiesFromHandle: false);
		return UpdateAcceptSocket(socket, remoteEP);
	}

	internal Socket UpdateAcceptSocket(Socket socket, EndPoint remoteEP)
	{
		socket._addressFamily = _addressFamily;
		socket._socketType = _socketType;
		socket._protocolType = _protocolType;
		socket._remoteEndPoint = remoteEP;
		if (_rightEndPoint is UnixDomainSocketEndPoint { BoundFileName: not null } unixDomainSocketEndPoint)
		{
			socket._rightEndPoint = unixDomainSocketEndPoint.CreateUnboundEndPoint();
		}
		else
		{
			socket._rightEndPoint = _rightEndPoint;
		}
		socket._localEndPoint = ((!IsWildcardEndPoint(_localEndPoint)) ? _localEndPoint : null);
		socket.SetToConnected();
		socket._willBlock = _willBlock;
		socket.InternalSetBlocking(_willBlock);
		return socket;
	}

	internal void SetToConnected()
	{
		if (!_isConnected)
		{
			_isConnected = true;
			_isDisconnected = false;
			if (_rightEndPoint == null)
			{
				_rightEndPoint = _pendingConnectRightEndPoint;
			}
			_pendingConnectRightEndPoint = null;
			UpdateLocalEndPointOnConnect();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "now connected", "SetToConnected");
			}
		}
	}

	private void UpdateLocalEndPointOnConnect()
	{
		if (IsWildcardEndPoint(_localEndPoint))
		{
			_localEndPoint = null;
		}
	}

	private static bool IsWildcardEndPoint(EndPoint endPoint)
	{
		if (endPoint == null)
		{
			return false;
		}
		if (endPoint is IPEndPoint iPEndPoint)
		{
			IPAddress address = iPEndPoint.Address;
			if (!IPAddress.Any.Equals(address) && !IPAddress.IPv6Any.Equals(address))
			{
				return s_IPAddressAnyMapToIPv6.Equals(address);
			}
			return true;
		}
		return false;
	}

	internal void SetToDisconnected()
	{
		if (_isConnected)
		{
			_isConnected = false;
			_isDisconnected = true;
			if (!Disposed && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "!Disposed", "SetToDisconnected");
			}
		}
	}

	private void UpdateStatusAfterSocketOptionErrorAndThrowException(SocketError error, [CallerMemberName] string callerName = null)
	{
		bool disconnectOnFailure = error != SocketError.ProtocolOption && error != SocketError.OperationNotSupported;
		UpdateStatusAfterSocketErrorAndThrowException(error, disconnectOnFailure, callerName);
	}

	private void UpdateStatusAfterSocketErrorAndThrowException(SocketError error, bool disconnectOnFailure = true, [CallerMemberName] string callerName = null)
	{
		SocketException ex = new SocketException((int)error);
		UpdateStatusAfterSocketError(ex, disconnectOnFailure);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, ex, callerName);
		}
		throw ex;
	}

	internal void UpdateStatusAfterSocketError(SocketException socketException, bool disconnectOnFailure = true)
	{
		UpdateStatusAfterSocketError(socketException.SocketErrorCode, disconnectOnFailure);
	}

	internal void UpdateStatusAfterSocketError(SocketError errorCode, bool disconnectOnFailure = true)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, $"errorCode:{errorCode}, disconnectOnFailure:{disconnectOnFailure}", "UpdateStatusAfterSocketError");
		}
		if (disconnectOnFailure && _isConnected && (_handle.IsInvalid || (errorCode != SocketError.WouldBlock && errorCode != SocketError.IOPending && errorCode != SocketError.NoBufferSpaceAvailable && errorCode != SocketError.TimedOut)))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Invalidating socket.", "UpdateStatusAfterSocketError");
			}
			SetToDisconnected();
		}
	}

	private bool CheckErrorAndUpdateStatus(SocketError errorCode)
	{
		if (errorCode == SocketError.Success || errorCode == SocketError.IOPending)
		{
			return true;
		}
		UpdateStatusAfterSocketError(errorCode);
		return false;
	}

	private void ValidateReceiveFromEndpointAndState(EndPoint remoteEndPoint, string remoteEndPointArgumentName)
	{
		ArgumentNullException.ThrowIfNull(remoteEndPoint, remoteEndPointArgumentName);
		if (remoteEndPoint is DnsEndPoint)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_dnsendpoint, remoteEndPointArgumentName), remoteEndPointArgumentName);
		}
		if (!CanTryAddressFamily(remoteEndPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, remoteEndPoint.AddressFamily, _addressFamily), remoteEndPointArgumentName);
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
	}

	private void ValidateBlockingMode()
	{
		if (_willBlock && !_willBlockInternal)
		{
			throw new InvalidOperationException(System.SR.net_invasync);
		}
	}

	private static SafeFileHandle OpenFileHandle(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			return File.OpenHandle(name, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, 0L);
		}
		return null;
	}

	private void UpdateReceiveSocketErrorForDisposed(ref SocketError socketError, int bytesTransferred)
	{
		if (bytesTransferred == 0 && Disposed)
		{
			socketError = (IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted);
		}
	}

	private void UpdateSendSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = (IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted);
		}
	}

	private void UpdateConnectSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = SocketError.NotSocket;
		}
	}

	private void UpdateAcceptSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = SocketError.Interrupted;
		}
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(Disposed, this);
	}

	private void ThrowIfConnectedStreamSocket()
	{
		if (_isConnected && _socketType == SocketType.Stream)
		{
			throw new SocketException(10056);
		}
	}

	internal static void SocketListDangerousReleaseRefs(IList socketList, ref int refsAdded)
	{
		if (socketList == null)
		{
			return;
		}
		for (int i = 0; i < socketList.Count; i++)
		{
			if (refsAdded <= 0)
			{
				break;
			}
			Socket socket = (Socket)socketList[i];
			socket.InternalSafeHandle.DangerousRelease();
			refsAdded--;
		}
	}

	private static SocketError GetSocketErrorFromFaultedTask(Task t)
	{
		if (t.IsCanceled)
		{
			return SocketError.OperationAborted;
		}
		Exception innerException = t.Exception.InnerException;
		if (!(innerException is SocketException { SocketErrorCode: var socketErrorCode }))
		{
			if (!(innerException is ObjectDisposedException))
			{
				if (innerException is OperationCanceledException)
				{
					return SocketError.OperationAborted;
				}
				return SocketError.SocketError;
			}
			return SocketError.OperationAborted;
		}
		return socketErrorCode;
	}

	private void CheckNonBlockingConnectCompleted()
	{
		if (_nonBlockingConnectInProgress && SocketPal.HasNonBlockingConnectCompleted(_handle, out var success))
		{
			_nonBlockingConnectInProgress = false;
			if (success)
			{
				SetToConnected();
			}
		}
	}

	public Task<Socket> AcceptAsync()
	{
		return AcceptAsync((Socket?)null, CancellationToken.None).AsTask();
	}

	public ValueTask<Socket> AcceptAsync(CancellationToken cancellationToken)
	{
		return AcceptAsync((Socket?)null, cancellationToken);
	}

	public Task<Socket> AcceptAsync(Socket? acceptSocket)
	{
		return AcceptAsync(acceptSocket, CancellationToken.None).AsTask();
	}

	public ValueTask<Socket> AcceptAsync(Socket? acceptSocket, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<Socket>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(null, 0, 0);
		awaitableSocketAsyncEventArgs.AcceptSocket = acceptSocket;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.AcceptAsync(this, cancellationToken);
	}

	public Task ConnectAsync(EndPoint remoteEP)
	{
		return ConnectAsync(remoteEP, default(CancellationToken)).AsTask();
	}

	public ValueTask ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEP;
		ValueTask valueTask = awaitableSocketAsyncEventArgs.ConnectAsync(this);
		if (valueTask.IsCompleted || !cancellationToken.CanBeCanceled)
		{
			return valueTask;
		}
		return WaitForConnectWithCancellation(awaitableSocketAsyncEventArgs, valueTask, cancellationToken);
		static async ValueTask WaitForConnectWithCancellation(AwaitableSocketAsyncEventArgs saea, ValueTask connectTask, CancellationToken cancellationToken)
		{
			try
			{
				using (cancellationToken.UnsafeRegister(delegate(object o)
				{
					CancelConnectAsync((SocketAsyncEventArgs)o);
				}, saea))
				{
					await connectTask.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
			{
				cancellationToken.ThrowIfCancellationRequested();
				throw;
			}
		}
	}

	public Task ConnectAsync(IPAddress address, int port)
	{
		return ConnectAsync(new IPEndPoint(address, port));
	}

	public ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
	{
		return ConnectAsync(new IPEndPoint(address, port), cancellationToken);
	}

	public Task ConnectAsync(IPAddress[] addresses, int port)
	{
		return ConnectAsync(addresses, port, CancellationToken.None).AsTask();
	}

	public ValueTask ConnectAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(addresses, "addresses");
		if (addresses.Length == 0)
		{
			throw new ArgumentException(System.SR.net_invalidAddressList, "addresses");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		return Core(addresses, port, cancellationToken);
		async ValueTask Core(IPAddress[] addresses, int port, CancellationToken cancellationToken)
		{
			Exception source = null;
			IPEndPoint endPoint = null;
			foreach (IPAddress address in addresses)
			{
				try
				{
					if (endPoint == null)
					{
						endPoint = new IPEndPoint(address, port);
					}
					else
					{
						endPoint.Address = address;
					}
					await ConnectAsync(endPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					source = ex;
				}
			}
			ExceptionDispatchInfo.Throw(source);
		}
	}

	public Task ConnectAsync(string host, int port)
	{
		return ConnectAsync(host, port, default(CancellationToken)).AsTask();
	}

	public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(host, "host");
		IPAddress address;
		EndPoint remoteEP = (IPAddress.TryParse(host, out address) ? ((EndPoint)new IPEndPoint(address, port)) : ((EndPoint)new DnsEndPoint(host, port)));
		return ConnectAsync(remoteEP, cancellationToken);
	}

	public ValueTask DisconnectAsync(bool reuseSocket, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.DisconnectReuseSocket = reuseSocket;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.DisconnectAsync(this, cancellationToken);
	}

	public Task<int> ReceiveAsync(ArraySegment<byte> buffer)
	{
		return ReceiveAsync(buffer, SocketFlags.None);
	}

	public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream: false);
	}

	internal Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream)
	{
		ValidateBuffer(buffer);
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
	}

	public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream: false, cancellationToken);
	}

	internal ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = fromNetworkStream;
		return awaitableSocketAsyncEventArgs.ReceiveAsync(this, cancellationToken);
	}

	public Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers)
	{
		return ReceiveAsync(buffers, SocketFlags.None);
	}

	public Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		ValidateBuffersList(buffers);
		TaskSocketAsyncEventArgs<int> taskSocketAsyncEventArgs = Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null);
		if (taskSocketAsyncEventArgs == null)
		{
			taskSocketAsyncEventArgs = new TaskSocketAsyncEventArgs<int>();
			taskSocketAsyncEventArgs.Completed += delegate(object s, SocketAsyncEventArgs e)
			{
				CompleteSendReceive((Socket)s, (TaskSocketAsyncEventArgs<int>)e, isReceive: true);
			};
		}
		taskSocketAsyncEventArgs.BufferList = buffers;
		taskSocketAsyncEventArgs.SocketFlags = socketFlags;
		return GetTaskForSendReceive(ReceiveAsync(taskSocketAsyncEventArgs), taskSocketAsyncEventArgs, fromNetworkStream: false, isReceive: true);
	}

	public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint)
	{
		return ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
	}

	public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		ValidateBuffer(buffer);
		return ReceiveFromAsync(buffer, socketFlags, remoteEndPoint, default(CancellationToken)).AsTask();
	}

	public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);
	}

	public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateReceiveFromEndpointAndState(remoteEndPoint, "remoteEndPoint");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<SocketReceiveFromResult>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
		awaitableSocketAsyncEventArgs._socketAddress = new SocketAddress(AddressFamily);
		if (remoteEndPoint.AddressFamily != AddressFamily && AddressFamily == AddressFamily.InterNetworkV6 && IsDualMode)
		{
			awaitableSocketAsyncEventArgs.RemoteEndPoint = s_IPEndPointIPv6;
		}
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.ReceiveFromAsync(this, cancellationToken);
	}

	public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, SocketAddress receivedAddress, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(receivedAddress, "receivedAddress");
		if (receivedAddress.Size < SocketAddress.GetMaximumAddressSize(AddressFamily))
		{
			throw new ArgumentOutOfRangeException("receivedAddress", System.SR.net_sockets_address_small);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = null;
		awaitableSocketAsyncEventArgs._socketAddress = receivedAddress;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.ReceiveFromSocketAddressAsync(this, cancellationToken);
	}

	public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, EndPoint remoteEndPoint)
	{
		return ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint);
	}

	public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		ValidateBuffer(buffer);
		return ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, default(CancellationToken)).AsTask();
	}

	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReceiveMessageFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);
	}

	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateReceiveFromEndpointAndState(remoteEndPoint, "remoteEndPoint");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<SocketReceiveMessageFromResult>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.ReceiveMessageFromAsync(this, cancellationToken);
	}

	public Task<int> SendAsync(ArraySegment<byte> buffer)
	{
		return SendAsync(buffer, SocketFlags.None);
	}

	public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		ValidateBuffer(buffer);
		return SendAsync(buffer, socketFlags, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(buffer, SocketFlags.None, cancellationToken);
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendAsync(this, cancellationToken);
	}

	internal ValueTask SendAsyncForNetworkStream(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = true;
		return awaitableSocketAsyncEventArgs.SendAsyncForNetworkStream(this, cancellationToken);
	}

	public Task<int> SendAsync(IList<ArraySegment<byte>> buffers)
	{
		return SendAsync(buffers, SocketFlags.None);
	}

	public Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		ValidateBuffersList(buffers);
		TaskSocketAsyncEventArgs<int> taskSocketAsyncEventArgs = Interlocked.Exchange(ref _multiBufferSendEventArgs, null);
		if (taskSocketAsyncEventArgs == null)
		{
			taskSocketAsyncEventArgs = new TaskSocketAsyncEventArgs<int>();
			taskSocketAsyncEventArgs.Completed += delegate(object s, SocketAsyncEventArgs e)
			{
				CompleteSendReceive((Socket)s, (TaskSocketAsyncEventArgs<int>)e, isReceive: false);
			};
		}
		taskSocketAsyncEventArgs.BufferList = buffers;
		taskSocketAsyncEventArgs.SocketFlags = socketFlags;
		return GetTaskForSendReceive(SendAsync(taskSocketAsyncEventArgs), taskSocketAsyncEventArgs, fromNetworkStream: false, isReceive: false);
	}

	public Task<int> SendToAsync(ArraySegment<byte> buffer, EndPoint remoteEP)
	{
		return SendToAsync(buffer, SocketFlags.None, remoteEP);
	}

	public Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ValidateBuffer(buffer);
		return SendToAsync(buffer, socketFlags, remoteEP, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEP, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendToAsync(buffer, SocketFlags.None, remoteEP, cancellationToken);
	}

	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(remoteEP, "remoteEP");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs._socketAddress = null;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEP;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendToAsync(this, cancellationToken);
	}

	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, SocketAddress socketAddress, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(socketAddress, "socketAddress");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs._socketAddress = socketAddress;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendToAsync(this, cancellationToken);
	}

	public ValueTask SendFileAsync(string? fileName, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendFileAsync(fileName, default(ReadOnlyMemory<byte>), default(ReadOnlyMemory<byte>), TransmitFileOptions.UseDefaultWorkerThread, cancellationToken);
	}

	public ValueTask SendFileAsync(string? fileName, ReadOnlyMemory<byte> preBuffer, ReadOnlyMemory<byte> postBuffer, TransmitFileOptions flags, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		if (!IsConnectionOriented)
		{
			NotSupportedException exception = new NotSupportedException(System.SR.net_notconnected);
			return ValueTask.FromException(exception);
		}
		int num = 0;
		if (fileName != null)
		{
			num++;
		}
		if (!preBuffer.IsEmpty)
		{
			num++;
		}
		if (!postBuffer.IsEmpty)
		{
			num++;
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		SendPacketsElement[]? sendPacketsElements = awaitableSocketAsyncEventArgs.SendPacketsElements;
		SendPacketsElement[] array = ((sendPacketsElements != null && sendPacketsElements.Length == num) ? awaitableSocketAsyncEventArgs.SendPacketsElements : new SendPacketsElement[num]);
		int num2 = 0;
		if (!preBuffer.IsEmpty)
		{
			array[num2++] = new SendPacketsElement(preBuffer, num2 == num);
		}
		if (fileName != null)
		{
			array[num2++] = new SendPacketsElement(fileName, 0, 0, num2 == num);
		}
		if (!postBuffer.IsEmpty)
		{
			array[num2++] = new SendPacketsElement(postBuffer, num2 == num);
		}
		awaitableSocketAsyncEventArgs.SendPacketsFlags = flags;
		awaitableSocketAsyncEventArgs.SendPacketsElements = array;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendPacketsAsync(this, cancellationToken);
	}

	private static void ValidateBufferArguments(byte[] buffer, int offset, int size)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)buffer.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)size, (uint)(buffer.Length - offset), "size");
	}

	private static void ValidateBuffer(ArraySegment<byte> buffer)
	{
		ArgumentNullException.ThrowIfNull(buffer.Array, "Array");
		if ((uint)buffer.Offset > (uint)buffer.Array.Length)
		{
			throw new ArgumentOutOfRangeException("Offset");
		}
		if ((uint)buffer.Count > (uint)(buffer.Array.Length - buffer.Offset))
		{
			throw new ArgumentOutOfRangeException("Count");
		}
	}

	private static void ValidateBuffersList(IList<ArraySegment<byte>> buffers)
	{
		ArgumentNullException.ThrowIfNull(buffers, "buffers");
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
	}

	private Task<int> GetTaskForSendReceive(bool pending, TaskSocketAsyncEventArgs<int> saea, bool fromNetworkStream, bool isReceive)
	{
		Task<int> result;
		if (pending)
		{
			result = saea.GetCompletionResponsibility(out var responsibleForReturningToPool).Task;
			if (responsibleForReturningToPool)
			{
				ReturnSocketAsyncEventArgs(saea, isReceive);
			}
		}
		else
		{
			result = ((saea.SocketError != 0) ? Task.FromException<int>(GetException(saea.SocketError, fromNetworkStream)) : Task.FromResult((!(fromNetworkStream && !isReceive)) ? saea.BytesTransferred : 0));
			ReturnSocketAsyncEventArgs(saea, isReceive);
		}
		return result;
	}

	private static void CompleteSendReceive(Socket s, TaskSocketAsyncEventArgs<int> saea, bool isReceive)
	{
		SocketError socketError = saea.SocketError;
		int bytesTransferred = saea.BytesTransferred;
		bool wrapExceptionsInIOExceptions = saea._wrapExceptionsInIOExceptions;
		bool responsibleForReturningToPool;
		AsyncTaskMethodBuilder<int> completionResponsibility = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
		if (responsibleForReturningToPool)
		{
			s.ReturnSocketAsyncEventArgs(saea, isReceive);
		}
		if (socketError == SocketError.Success)
		{
			completionResponsibility.SetResult(bytesTransferred);
		}
		else
		{
			completionResponsibility.SetException(GetException(socketError, wrapExceptionsInIOExceptions));
		}
	}

	private static Exception GetException(SocketError error, bool wrapExceptionsInIOExceptions = false)
	{
		Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)error));
		if (!wrapExceptionsInIOExceptions)
		{
			return ex;
		}
		return new IOException(System.SR.Format(System.SR.net_io_readwritefailure, ex.Message), ex);
	}

	private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<int> saea, bool isReceive)
	{
		saea._accessed = false;
		saea._builder = default(AsyncTaskMethodBuilder<int>);
		saea._wrapExceptionsInIOExceptions = false;
		if (Interlocked.CompareExchange(ref isReceive ? ref _multiBufferReceiveEventArgs : ref _multiBufferSendEventArgs, saea, null) != null)
		{
			saea.Dispose();
		}
	}

	private void DisposeCachedTaskSocketAsyncEventArgs()
	{
		Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _multiBufferSendEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _singleBufferSendEventArgs, null)?.Dispose();
	}

	internal void ReplaceHandleIfNecessaryAfterFailedConnect()
	{
	}

	[SupportedOSPlatform("windows")]
	public unsafe Socket(SocketInformation socketInformation)
	{
		SocketError socketError = SocketPal.CreateSocket(socketInformation, out _handle, ref _addressFamily, ref _socketType, ref _protocolType);
		if (socketError != 0)
		{
			_handle = null;
			if (socketError == SocketError.InvalidArgument)
			{
				throw new ArgumentException(System.SR.net_sockets_invalid_socketinformation, "socketInformation");
			}
			throw new SocketException((int)socketError);
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			_handle.Dispose();
			_handle = null;
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		_isConnected = socketInformation.GetOption(SocketInformationOptions.Connected);
		_willBlock = !socketInformation.GetOption(SocketInformationOptions.NonBlocking);
		InternalSetBlocking(_willBlock);
		_isListening = socketInformation.GetOption(SocketInformationOptions.Listening);
		IPEndPoint iPEndPoint = new IPEndPoint((_addressFamily == AddressFamily.InterNetwork) ? IPAddress.Any : IPAddress.IPv6Any, 0);
		SocketAddress socketAddress = iPEndPoint.Serialize();
		Memory<byte> buffer = socketAddress.Buffer;
		int length = buffer.Length;
		buffer = socketAddress.Buffer;
		fixed (byte* buffer2 = buffer.Span)
		{
			socketError = SocketPal.GetSockName(_handle, buffer2, &length);
		}
		switch (socketError)
		{
		case SocketError.Success:
			socketAddress.Size = length;
			_rightEndPoint = iPEndPoint.Create(socketAddress);
			break;
		default:
			_handle.Dispose();
			_handle = null;
			throw new SocketException((int)socketError);
		case SocketError.InvalidArgument:
			break;
		}
	}

	private unsafe void LoadSocketTypeFromHandle(SafeSocketHandle handle, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType, out bool blocking, out bool isListening, out bool isSocket)
	{
		global::Interop.Winsock.EnsureInitialized();
		global::Interop.Winsock.WSAPROTOCOL_INFOW wSAPROTOCOL_INFOW = default(global::Interop.Winsock.WSAPROTOCOL_INFOW);
		int optionLength = sizeof(global::Interop.Winsock.WSAPROTOCOL_INFOW);
		if (global::Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, (SocketOptionName)8197, (byte*)(&wSAPROTOCOL_INFOW), ref optionLength) == SocketError.SocketError)
		{
			throw new SocketException((int)SocketPal.GetLastSocketError());
		}
		addressFamily = wSAPROTOCOL_INFOW.iAddressFamily;
		socketType = wSAPROTOCOL_INFOW.iSocketType;
		protocolType = wSAPROTOCOL_INFOW.iProtocol;
		isListening = SocketPal.GetSockOpt(_handle, SocketOptionLevel.Socket, SocketOptionName.AcceptConnection, out var optionValue) == SocketError.Success && optionValue != 0;
		blocking = true;
		isSocket = true;
	}

	[SupportedOSPlatform("windows")]
	public SocketInformation DuplicateAndClose(int targetProcessId)
	{
		ThrowIfDisposed();
		SocketInformation socketInformation;
		SocketError socketError = SocketPal.DuplicateSocket(_handle, targetProcessId, out socketInformation);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
		socketInformation.SetOption(SocketInformationOptions.Connected, Connected);
		socketInformation.SetOption(SocketInformationOptions.NonBlocking, !Blocking);
		socketInformation.SetOption(SocketInformationOptions.Listening, _isListening);
		Close(-1);
		return socketInformation;
	}

	private DynamicWinsockMethods GetDynamicWinsockMethods()
	{
		return _dynamicWinsockMethods ?? (_dynamicWinsockMethods = DynamicWinsockMethods.GetMethods(_addressFamily, _socketType, _protocolType));
	}

	internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, nint buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped)
	{
		AcceptExDelegate acceptExDelegate = GetDynamicWinsockMethods().GetAcceptExDelegate(listenSocketHandle);
		return acceptExDelegate(listenSocketHandle, acceptSocketHandle, buffer, len, localAddressLength, remoteAddressLength, out bytesReceived, overlapped);
	}

	internal void GetAcceptExSockaddrs(nint buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out nint localSocketAddress, out int localSocketAddressLength, out nint remoteSocketAddress, out int remoteSocketAddressLength)
	{
		GetAcceptExSockaddrsDelegate getAcceptExSockaddrsDelegate = GetDynamicWinsockMethods().GetGetAcceptExSockaddrsDelegate(_handle);
		getAcceptExSockaddrsDelegate(buffer, receiveDataLength, localAddressLength, remoteAddressLength, out localSocketAddress, out localSocketAddressLength, out remoteSocketAddress, out remoteSocketAddressLength);
	}

	internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
	{
		DisconnectExDelegate disconnectExDelegate = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);
		return disconnectExDelegate(socketHandle, overlapped, flags, reserved);
	}

	internal unsafe bool DisconnectExBlocking(SafeSocketHandle socketHandle, int flags, int reserved)
	{
		DisconnectExDelegate disconnectExDelegate = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);
		return disconnectExDelegate(socketHandle, null, flags, reserved);
	}

	private void EnableReuseUnicastPort()
	{
		int optionValue = 1;
		SocketError socketError = global::Interop.Winsock.setsockopt(_handle, SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, ref optionValue, 4);
		if (System.Net.NetEventSource.Log.IsEnabled() && socketError != 0)
		{
			socketError = SocketPal.GetLastSocketError();
			System.Net.NetEventSource.Info($"Enabling SO_REUSE_UNICASTPORT failed with error code: {socketError}", null, "EnableReuseUnicastPort");
		}
	}

	internal unsafe bool ConnectEx(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress, nint buffer, int dataLength, out int bytesSent, NativeOverlapped* overlapped)
	{
		ConnectExDelegate connectExDelegate = GetDynamicWinsockMethods().GetConnectExDelegate(socketHandle);
		return connectExDelegate(socketHandle, socketAddress, buffer, dataLength, out bytesSent, overlapped);
	}

	internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, nint msg, out int bytesTransferred, NativeOverlapped* overlapped, nint completionRoutine)
	{
		WSARecvMsgDelegate wSARecvMsgDelegate = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(socketHandle);
		return wSARecvMsgDelegate(socketHandle, msg, out bytesTransferred, overlapped, completionRoutine);
	}

	internal unsafe SocketError WSARecvMsgBlocking(SafeSocketHandle socketHandle, nint msg, out int bytesTransferred)
	{
		WSARecvMsgDelegate wSARecvMsgDelegate = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(_handle);
		return wSARecvMsgDelegate(socketHandle, msg, out bytesTransferred, null, IntPtr.Zero);
	}

	internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, nint packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
	{
		TransmitPacketsDelegate transmitPacketsDelegate = GetDynamicWinsockMethods().GetTransmitPacketsDelegate(socketHandle);
		return transmitPacketsDelegate(socketHandle, packetArray, elementCount, sendSize, overlapped, flags);
	}

	internal static void SocketListToFileDescriptorSet(IList socketList, Span<nint> fileDescriptorSet, ref int refsAdded)
	{
		int count;
		if (socketList == null || (count = socketList.Count) == 0)
		{
			return;
		}
		fileDescriptorSet[0] = count;
		for (int i = 0; i < count; i++)
		{
			if (!(socketList[i] is Socket socket))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_select, socketList[i]?.GetType().FullName, typeof(Socket).FullName), "socketList");
			}
			bool success = false;
			socket.InternalSafeHandle.DangerousAddRef(ref success);
			fileDescriptorSet[i + 1] = socket.InternalSafeHandle.DangerousGetHandle();
			refsAdded++;
		}
	}

	internal static void SelectFileDescriptor(IList socketList, Span<nint> fileDescriptorSet, ref int refsAdded)
	{
		int num;
		if (socketList == null || (num = socketList.Count) == 0)
		{
			return;
		}
		int num2 = (int)fileDescriptorSet[0];
		if (num2 == 0)
		{
			SocketListDangerousReleaseRefs(socketList, ref refsAdded);
			socketList.Clear();
			return;
		}
		lock (socketList)
		{
			for (int i = 0; i < num; i++)
			{
				Socket socket = socketList[i] as Socket;
				int j;
				for (j = 0; j < num2 && fileDescriptorSet[j + 1] != socket._handle.DangerousGetHandle(); j++)
				{
				}
				if (j == num2)
				{
					socket.InternalSafeHandle.DangerousRelease();
					refsAdded--;
					socketList.RemoveAt(i--);
					num--;
				}
			}
		}
	}

	private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool checkDisconnected, string propertyName, out SafeSocketHandle handle)
	{
		if (acceptSocket == null)
		{
			acceptSocket = new Socket(_addressFamily, _socketType, _protocolType);
		}
		else if (acceptSocket._rightEndPoint != null && (!checkDisconnected || !acceptSocket._isDisconnected))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_sockets_namedmustnotbebound, propertyName));
		}
		handle = acceptSocket._handle;
		return acceptSocket;
	}

	private void SendFileInternal(string fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
	{
		SocketError socketError;
		using (SafeFileHandle fileHandle = OpenFileHandle(fileName))
		{
			socketError = SocketPal.SendFile(_handle, fileHandle, preBuffer, postBuffer, flags);
		}
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, disconnectOnFailure: true, "SendFileInternal");
		}
		if ((flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0)
		{
			SetToDisconnected();
			_remoteEndPoint = null;
		}
	}

	internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle()
	{
		return _handle.GetThreadPoolBoundHandle() ?? GetOrAllocateThreadPoolBoundHandleSlow();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandleSlow()
	{
		bool trySkipCompletionPortOnSuccess = !CompletionPortHelper.PlatformHasUdpIssue || _protocolType != ProtocolType.Udp;
		return _handle.GetOrAllocateThreadPoolBoundHandle(trySkipCompletionPortOnSuccess);
	}
}
