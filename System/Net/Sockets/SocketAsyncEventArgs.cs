using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

public class SocketAsyncEventArgs : EventArgs, IDisposable
{
	private sealed class MultiConnectSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource
	{
		private ManualResetValueTaskSourceCore<bool> _mrvtsc;

		private int _isCompleted;

		public short Version => _mrvtsc.Version;

		public MultiConnectSocketAsyncEventArgs()
			: base(unsafeSuppressExecutionContextFlow: false)
		{
		}

		public void GetResult(short token)
		{
			_mrvtsc.GetResult(token);
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _mrvtsc.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_mrvtsc.OnCompleted(continuation, state, token, flags);
		}

		public void Reset()
		{
			_mrvtsc.Reset();
		}

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			_mrvtsc.SetResult(result: true);
		}

		public bool ReachedCoordinationPointFirst()
		{
			return Interlocked.Exchange(ref _isCompleted, 1) == 0;
		}
	}

	private enum PinState : byte
	{
		None,
		MultipleBuffer,
		SendPackets
	}

	private Socket _acceptSocket;

	private Socket _connectSocket;

	private Memory<byte> _buffer;

	private int _offset;

	private int _count;

	private bool _bufferIsExplicitArray;

	private IList<ArraySegment<byte>> _bufferList;

	private List<ArraySegment<byte>> _bufferListInternal;

	private int _bytesTransferred;

	private bool _disconnectReuseSocket;

	private SocketAsyncOperation _completedOperation;

	private IPPacketInformation _receiveMessageFromPacketInfo;

	private EndPoint _remoteEndPoint;

	private int _sendPacketsSendSize;

	private SendPacketsElement[] _sendPacketsElements;

	private TransmitFileOptions _sendPacketsFlags;

	private SocketError _socketError;

	private Exception _connectByNameError;

	private SocketFlags _socketFlags;

	private object _userToken;

	private byte[] _acceptBuffer;

	private int _acceptAddressBufferCount;

	internal SocketAddress _socketAddress;

	private readonly bool _flowExecutionContext;

	private ExecutionContext _context;

	private static readonly ContextCallback s_executionCallback = ExecutionCallback;

	private Socket _currentSocket;

	private bool _userSocket;

	private bool _disposeCalled;

	private int _operating;

	private CancellationTokenSource _multipleConnectCancellation;

	private ulong _asyncCompletionOwnership;

	private MemoryHandle _singleBufferHandle;

	private WSABuffer[] _wsaBufferArrayPinned;

	private MemoryHandle[] _multipleBufferMemoryHandles;

	private byte[] _wsaMessageBufferPinned;

	private byte[] _controlBufferPinned;

	private WSABuffer[] _wsaRecvMsgWSABufferArrayPinned;

	private nint _socketAddressPtr;

	private SafeFileHandle[] _sendPacketsFileHandles;

	private PreAllocatedOverlapped _preAllocatedOverlapped;

	private readonly StrongBox<SocketAsyncEventArgs> _strongThisRef = new StrongBox<SocketAsyncEventArgs>();

	private CancellationTokenRegistration _registrationToCancelPendingIO;

	private unsafe NativeOverlapped* _pendingOverlappedForCancellation;

	private PinState _pinState;

	private unsafe static readonly IOCompletionCallback s_completionPortCallback = delegate(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		StrongBox<SocketAsyncEventArgs> strongBox = (StrongBox<SocketAsyncEventArgs>)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
		SocketAsyncEventArgs value = strongBox.Value;
		if (value._asyncCompletionOwnership == 0L)
		{
			ulong value2 = 0x8000000000000000uL | ((ulong)numBytes << 32) | errorCode;
			if (Interlocked.Exchange(ref value._asyncCompletionOwnership, value2) == 0L)
			{
				return;
			}
		}
		if (errorCode == 0)
		{
			value.FreeNativeOverlapped(ref nativeOverlapped);
			value.FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
		}
		else
		{
			SocketError socketError = (SocketError)errorCode;
			SocketFlags socketFlags = SocketFlags.None;
			value.GetOverlappedResultOnError(ref socketError, ref numBytes, ref socketFlags, nativeOverlapped);
			value.FreeNativeOverlapped(ref nativeOverlapped);
			value.FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
		}
	};

	public Socket? AcceptSocket
	{
		get
		{
			return _acceptSocket;
		}
		set
		{
			_acceptSocket = value;
		}
	}

	public Socket? ConnectSocket => _connectSocket;

	public byte[]? Buffer
	{
		get
		{
			if (_bufferIsExplicitArray)
			{
				ArraySegment<byte> segment;
				bool flag = MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)_buffer, out segment);
				return segment.Array;
			}
			return null;
		}
	}

	public Memory<byte> MemoryBuffer => _buffer;

	public int Offset => _offset;

	public int Count => _count;

	public TransmitFileOptions SendPacketsFlags
	{
		get
		{
			return _sendPacketsFlags;
		}
		set
		{
			_sendPacketsFlags = value;
		}
	}

	public IList<ArraySegment<byte>>? BufferList
	{
		get
		{
			return _bufferList;
		}
		set
		{
			StartConfiguring();
			try
			{
				if (value != null)
				{
					if (!_buffer.Equals(default(Memory<byte>)))
					{
						throw new ArgumentException(System.SR.net_ambiguousbuffers);
					}
					int count = value.Count;
					if (_bufferListInternal == null)
					{
						_bufferListInternal = new List<ArraySegment<byte>>(count);
					}
					else
					{
						_bufferListInternal.Clear();
					}
					for (int i = 0; i < count; i++)
					{
						ArraySegment<byte> arraySegment = value[i];
						RangeValidationHelpers.ValidateSegment(arraySegment);
						_bufferListInternal.Add(arraySegment);
					}
				}
				else
				{
					_bufferListInternal?.Clear();
				}
				_bufferList = value;
				SetupMultipleBuffers();
			}
			finally
			{
				Complete();
			}
		}
	}

	public int BytesTransferred => _bytesTransferred;

	public bool DisconnectReuseSocket
	{
		get
		{
			return _disconnectReuseSocket;
		}
		set
		{
			_disconnectReuseSocket = value;
		}
	}

	public SocketAsyncOperation LastOperation => _completedOperation;

	public IPPacketInformation ReceiveMessageFromPacketInfo => _receiveMessageFromPacketInfo;

	public EndPoint? RemoteEndPoint
	{
		get
		{
			return _remoteEndPoint;
		}
		set
		{
			_remoteEndPoint = value;
		}
	}

	public SendPacketsElement[]? SendPacketsElements
	{
		get
		{
			return _sendPacketsElements;
		}
		set
		{
			StartConfiguring();
			try
			{
				_sendPacketsElements = value;
			}
			finally
			{
				Complete();
			}
		}
	}

	public int SendPacketsSendSize
	{
		get
		{
			return _sendPacketsSendSize;
		}
		set
		{
			_sendPacketsSendSize = value;
		}
	}

	public SocketError SocketError
	{
		get
		{
			return _socketError;
		}
		set
		{
			_socketError = value;
		}
	}

	public Exception? ConnectByNameError => _connectByNameError;

	public SocketFlags SocketFlags
	{
		get
		{
			return _socketFlags;
		}
		set
		{
			_socketFlags = value;
		}
	}

	public object? UserToken
	{
		get
		{
			return _userToken;
		}
		set
		{
			_userToken = value;
		}
	}

	internal bool HasMultipleBuffers => _bufferList != null;

	public event EventHandler<SocketAsyncEventArgs>? Completed;

	public SocketAsyncEventArgs()
		: this(unsafeSuppressExecutionContextFlow: false)
	{
	}

	public SocketAsyncEventArgs(bool unsafeSuppressExecutionContextFlow)
	{
		_flowExecutionContext = !unsafeSuppressExecutionContextFlow;
		InitializeInternals();
	}

	private void OnCompletedInternal()
	{
		if (LastOperation <= SocketAsyncOperation.Connect)
		{
			AfterConnectAcceptTelemetry();
		}
		OnCompleted(this);
	}

	protected virtual void OnCompleted(SocketAsyncEventArgs e)
	{
		this.Completed?.Invoke(e._currentSocket, e);
	}

	private void AfterConnectAcceptTelemetry()
	{
		switch (LastOperation)
		{
		case SocketAsyncOperation.Accept:
			SocketsTelemetry.Log.AfterAccept(SocketError);
			break;
		case SocketAsyncOperation.Connect:
			SocketsTelemetry.Log.AfterConnect(SocketError);
			break;
		}
	}

	public void SetBuffer(int offset, int count)
	{
		StartConfiguring();
		try
		{
			if (!_buffer.Equals(default(Memory<byte>)))
			{
				ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)_buffer.Length, "offset");
				ArgumentOutOfRangeException.ThrowIfGreaterThan<long>((uint)count, _buffer.Length - offset, "count");
				if (!_bufferIsExplicitArray)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_BufferNotExplicitArray);
				}
				_offset = offset;
				_count = count;
			}
		}
		finally
		{
			Complete();
		}
	}

	internal void CopyBufferFrom(SocketAsyncEventArgs source)
	{
		StartConfiguring();
		try
		{
			_buffer = source._buffer;
			_offset = source._offset;
			_count = source._count;
			_bufferIsExplicitArray = source._bufferIsExplicitArray;
		}
		finally
		{
			Complete();
		}
	}

	public void SetBuffer(byte[]? buffer, int offset, int count)
	{
		StartConfiguring();
		try
		{
			if (buffer == null)
			{
				_buffer = default(Memory<byte>);
				_offset = 0;
				_count = 0;
				_bufferIsExplicitArray = false;
				return;
			}
			if (_bufferList != null)
			{
				throw new ArgumentException(System.SR.net_ambiguousbuffers);
			}
			ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)buffer.Length, "offset");
			ArgumentOutOfRangeException.ThrowIfGreaterThan<long>((uint)count, buffer.Length - offset, "count");
			_buffer = buffer;
			_offset = offset;
			_count = count;
			_bufferIsExplicitArray = true;
		}
		finally
		{
			Complete();
		}
	}

	public void SetBuffer(Memory<byte> buffer)
	{
		StartConfiguring();
		try
		{
			if (buffer.Length != 0 && _bufferList != null)
			{
				throw new ArgumentException(System.SR.net_ambiguousbuffers);
			}
			_buffer = buffer;
			_offset = 0;
			_count = buffer.Length;
			_bufferIsExplicitArray = false;
		}
		finally
		{
			Complete();
		}
	}

	internal void SetResults(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		_socketError = socketError;
		_connectByNameError = null;
		_bytesTransferred = bytesTransferred;
		_socketFlags = flags;
	}

	internal void SetResults(Exception exception, int bytesTransferred, SocketFlags flags)
	{
		_connectByNameError = exception;
		_bytesTransferred = bytesTransferred;
		_socketFlags = flags;
		if (exception == null)
		{
			_socketError = SocketError.Success;
		}
		else if (exception is SocketException ex)
		{
			_socketError = ex.SocketErrorCode;
		}
		else
		{
			_socketError = SocketError.SocketError;
		}
	}

	private static void ExecutionCallback(object state)
	{
		SocketAsyncEventArgs socketAsyncEventArgs = (SocketAsyncEventArgs)state;
		socketAsyncEventArgs.OnCompletedInternal();
	}

	internal void Complete()
	{
		CompleteCore();
		_context = null;
		_operating = 0;
		if (_disposeCalled)
		{
			Dispose();
		}
	}

	public void Dispose()
	{
		_disposeCalled = true;
		if (Interlocked.CompareExchange(ref _operating, 2, 0) == 0)
		{
			FreeInternals();
			FinishOperationSendPackets();
			GC.SuppressFinalize(this);
		}
	}

	~SocketAsyncEventArgs()
	{
		if (!Environment.HasShutdownStarted)
		{
			FreeInternals();
		}
	}

	private void StartConfiguring()
	{
		int num = Interlocked.CompareExchange(ref _operating, -1, 0);
		if (num != 0)
		{
			ThrowForNonFreeStatus(num);
		}
	}

	private void ThrowForNonFreeStatus(int status)
	{
		ObjectDisposedException.ThrowIf(status == 2, this);
		throw new InvalidOperationException(System.SR.net_socketopinprogress);
	}

	internal void StartOperationCommon(Socket socket, SocketAsyncOperation operation)
	{
		int num = Interlocked.CompareExchange(ref _operating, 1, 0);
		if (num != 0)
		{
			ThrowForNonFreeStatus(num);
		}
		_completedOperation = operation;
		_currentSocket = socket;
		if (_flowExecutionContext || (SocketsTelemetry.Log.IsEnabled() && (operation == SocketAsyncOperation.Connect || operation == SocketAsyncOperation.Accept)))
		{
			_context = ExecutionContext.Capture();
		}
		StartOperationCommonCore();
	}

	private void StartOperationCommonCore()
	{
		_strongThisRef.Value = this;
	}

	internal void StartOperationAccept()
	{
		_acceptAddressBufferCount = 2 * (Socket.GetAddressSize(_currentSocket._rightEndPoint) + 16);
		if (!_buffer.Equals(default(Memory<byte>)))
		{
			if (_count < _acceptAddressBufferCount)
			{
				throw new ArgumentException(System.SR.net_buffercounttoosmall, "Count");
			}
		}
		else if (_acceptBuffer == null || _acceptBuffer.Length < _acceptAddressBufferCount)
		{
			_acceptBuffer = new byte[_acceptAddressBufferCount];
		}
	}

	internal void StartOperationConnect(bool saeaMultiConnectCancelable, bool userSocket)
	{
		_multipleConnectCancellation = (saeaMultiConnectCancelable ? new CancellationTokenSource() : null);
		_connectSocket = null;
		_userSocket = userSocket;
	}

	internal void CancelConnectAsync()
	{
		if (_operating == 1 && _completedOperation == SocketAsyncOperation.Connect)
		{
			CancellationTokenSource multipleConnectCancellation = _multipleConnectCancellation;
			if (multipleConnectCancellation != null)
			{
				multipleConnectCancellation.Cancel();
			}
			else
			{
				_currentSocket?.Dispose();
			}
		}
	}

	internal void FinishOperationSyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		SetResults(socketError, bytesTransferred, flags);
		Socket currentSocket = _currentSocket;
		if (currentSocket != null)
		{
			currentSocket.UpdateStatusAfterSocketError(socketError);
			if (_completedOperation == SocketAsyncOperation.Connect && !_userSocket)
			{
				currentSocket.Dispose();
				_currentSocket = null;
			}
		}
		SocketAsyncOperation completedOperation = _completedOperation;
		if (completedOperation == SocketAsyncOperation.SendPackets)
		{
			FinishOperationSendPackets();
		}
		Complete();
	}

	internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		ExecutionContext context = _context;
		FinishOperationSyncFailure(socketError, bytesTransferred, flags);
		if (context == null)
		{
			OnCompletedInternal();
		}
		else
		{
			ExecutionContext.Run(context, s_executionCallback, this);
		}
	}

	internal bool DnsConnectAsync(DnsEndPoint endPoint, SocketType socketType, ProtocolType protocolType)
	{
		CancellationToken cancellationToken2 = _multipleConnectCancellation?.Token ?? default(CancellationToken);
		Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(endPoint.Host, endPoint.AddressFamily, cancellationToken2);
		MultiConnectSocketAsyncEventArgs multiConnectSocketAsyncEventArgs = new MultiConnectSocketAsyncEventArgs();
		multiConnectSocketAsyncEventArgs.CopyBufferFrom(this);
		Core(multiConnectSocketAsyncEventArgs, hostAddressesAsync, endPoint.Port, socketType, protocolType, cancellationToken2);
		return multiConnectSocketAsyncEventArgs.ReachedCoordinationPointFirst();
		async Task Core(MultiConnectSocketAsyncEventArgs internalArgs, Task<IPAddress[]> addressesTask, int port, SocketType socketType, ProtocolType protocolType, CancellationToken cancellationToken)
		{
			Socket tempSocketIPv4 = null;
			Socket tempSocketIPv6 = null;
			Exception caughtException = null;
			try
			{
				SocketError lastError = SocketError.NoData;
				IPAddress[] array = await addressesTask.ConfigureAwait(continueOnCapturedContext: false);
				foreach (IPAddress iPAddress in array)
				{
					Socket socket = null;
					if (_currentSocket != null)
					{
						if (!_currentSocket.CanTryAddressFamily(iPAddress.AddressFamily))
						{
							continue;
						}
						socket = _currentSocket;
					}
					else
					{
						if (iPAddress.AddressFamily == AddressFamily.InterNetworkV6)
						{
							Socket socket2 = tempSocketIPv6;
							if (socket2 == null)
							{
								Socket socket3;
								tempSocketIPv6 = (socket3 = (Socket.OSSupportsIPv6 ? new Socket(AddressFamily.InterNetworkV6, socketType, protocolType) : null));
								socket2 = socket3;
							}
							socket = socket2;
							if (socket != null && iPAddress.IsIPv4MappedToIPv6)
							{
								socket.DualMode = true;
							}
						}
						else if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
						{
							Socket socket4 = tempSocketIPv4;
							if (socket4 == null)
							{
								Socket socket3;
								tempSocketIPv4 = (socket3 = (Socket.OSSupportsIPv4 ? new Socket(AddressFamily.InterNetwork, socketType, protocolType) : null));
								socket4 = socket3;
							}
							socket = socket4;
						}
						if (socket == null)
						{
							continue;
						}
					}
					socket.ReplaceHandleIfNecessaryAfterFailedConnect();
					if (internalArgs.RemoteEndPoint is IPEndPoint iPEndPoint)
					{
						iPEndPoint.Address = iPAddress;
					}
					else
					{
						internalArgs.RemoteEndPoint = new IPEndPoint(iPAddress, port);
					}
					if (socket.ConnectAsync(internalArgs))
					{
						using (cancellationToken.UnsafeRegister(delegate(object s)
						{
							Socket.CancelConnectAsync((SocketAsyncEventArgs)s);
						}, internalArgs))
						{
							await new ValueTask(internalArgs, internalArgs.Version).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					if (internalArgs.SocketError == SocketError.Success)
					{
						return;
					}
					if (cancellationToken.IsCancellationRequested)
					{
						throw new SocketException(995);
					}
					lastError = internalArgs.SocketError;
					internalArgs.Reset();
				}
				caughtException = new SocketException((int)lastError);
			}
			catch (ObjectDisposedException)
			{
				caughtException = new SocketException(995);
			}
			catch (Exception ex2)
			{
				caughtException = ex2;
			}
			finally
			{
				if (tempSocketIPv4 != null && !tempSocketIPv4.Connected)
				{
					tempSocketIPv4.Dispose();
				}
				if (tempSocketIPv6 != null && !tempSocketIPv6.Connected)
				{
					tempSocketIPv6.Dispose();
				}
				if (_currentSocket != null && ((!_userSocket && !_currentSocket.Connected) || caughtException is OperationCanceledException || caughtException is SocketException { SocketErrorCode: SocketError.OperationAborted }))
				{
					_currentSocket.Dispose();
				}
				if (caughtException != null)
				{
					SetResults(caughtException, 0, SocketFlags.None);
					_currentSocket?.UpdateStatusAfterSocketError(_socketError);
				}
				else
				{
					SetResults(SocketError.Success, internalArgs.BytesTransferred, internalArgs.SocketFlags);
					_connectSocket = (_currentSocket = internalArgs.ConnectSocket);
				}
				if (SocketsTelemetry.Log.IsEnabled())
				{
					LogBytesTransferEvents(_connectSocket?.SocketType, SocketAsyncOperation.Connect, internalArgs.BytesTransferred);
				}
				Complete();
				internalArgs.Dispose();
				if (!internalArgs.ReachedCoordinationPointFirst())
				{
					OnCompleted(this);
				}
			}
		}
	}

	internal void FinishOperationSyncSuccess(int bytesTransferred, SocketFlags flags)
	{
		SetResults(SocketError.Success, bytesTransferred, flags);
		if (System.Net.NetEventSource.Log.IsEnabled() && bytesTransferred > 0)
		{
			LogBuffer(bytesTransferred);
		}
		switch (_completedOperation)
		{
		case SocketAsyncOperation.Accept:
		{
			SocketAddress socketAddress = _currentSocket._rightEndPoint.Serialize();
			SocketError socketError = FinishOperationAccept(socketAddress);
			if (socketError == SocketError.Success)
			{
				_acceptSocket = _currentSocket.UpdateAcceptSocket(_acceptSocket, _currentSocket._rightEndPoint.Create(socketAddress));
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					try
					{
						System.Net.NetEventSource.Accepted(_acceptSocket, _acceptSocket.RemoteEndPoint, _acceptSocket.LocalEndPoint);
					}
					catch (ObjectDisposedException)
					{
					}
				}
			}
			else
			{
				SetResults(socketError, bytesTransferred, flags);
				_acceptSocket = null;
				_currentSocket.UpdateStatusAfterSocketError(socketError);
			}
			break;
		}
		case SocketAsyncOperation.Connect:
		{
			SocketError socketError = FinishOperationConnect();
			if (socketError == SocketError.Success)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					try
					{
						System.Net.NetEventSource.Connected(_currentSocket, _currentSocket.LocalEndPoint, _currentSocket.RemoteEndPoint);
					}
					catch (ObjectDisposedException)
					{
					}
				}
				_currentSocket.SetToConnected();
				_connectSocket = _currentSocket;
			}
			else
			{
				SetResults(socketError, bytesTransferred, flags);
				_currentSocket.UpdateStatusAfterSocketError(socketError);
			}
			break;
		}
		case SocketAsyncOperation.Disconnect:
			_currentSocket.SetToDisconnected();
			_currentSocket._remoteEndPoint = null;
			break;
		case SocketAsyncOperation.ReceiveFrom:
			UpdateReceivedSocketAddress(_socketAddress);
			if (_remoteEndPoint == null || System.Net.Sockets.SocketAddressExtensions.Equals(_socketAddress, _remoteEndPoint))
			{
				break;
			}
			try
			{
				if (_remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 && _socketAddress.Family == AddressFamily.InterNetwork)
				{
					_remoteEndPoint = new IPEndPoint(_socketAddress.GetIPAddress().MapToIPv6(), _socketAddress.GetPort());
				}
				else
				{
					_remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
				}
			}
			catch
			{
			}
			break;
		case SocketAsyncOperation.ReceiveMessageFrom:
			UpdateReceivedSocketAddress(_socketAddress);
			if (!System.Net.Sockets.SocketAddressExtensions.Equals(_socketAddress, _remoteEndPoint))
			{
				try
				{
					if (_remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 && _socketAddress.Family == AddressFamily.InterNetwork)
					{
						_remoteEndPoint = new IPEndPoint(_socketAddress.GetIPAddress().MapToIPv6(), _socketAddress.GetPort());
					}
					else
					{
						_remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
					}
				}
				catch
				{
				}
			}
			FinishOperationReceiveMessageFrom();
			break;
		case SocketAsyncOperation.SendPackets:
			FinishOperationSendPackets();
			break;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			LogBytesTransferEvents(_currentSocket?.SocketType, _completedOperation, bytesTransferred);
		}
		Complete();
	}

	internal void FinishOperationAsyncSuccess(int bytesTransferred, SocketFlags flags)
	{
		ExecutionContext context = _context;
		FinishOperationSyncSuccess(bytesTransferred, flags);
		if (context == null)
		{
			OnCompletedInternal();
		}
		else
		{
			ExecutionContext.Run(context, s_executionCallback, this);
		}
	}

	private void FinishOperationSync(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		if (socketError == SocketError.Success)
		{
			FinishOperationSyncSuccess(bytesTransferred, flags);
		}
		else
		{
			FinishOperationSyncFailure(socketError, bytesTransferred, flags);
		}
		if (LastOperation <= SocketAsyncOperation.Connect)
		{
			AfterConnectAcceptTelemetry();
		}
	}

	private static void LogBytesTransferEvents(SocketType? socketType, SocketAsyncOperation operation, int bytesTransferred)
	{
		switch (operation)
		{
		case SocketAsyncOperation.Accept:
		case SocketAsyncOperation.Receive:
		case SocketAsyncOperation.ReceiveFrom:
		case SocketAsyncOperation.ReceiveMessageFrom:
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
			break;
		case SocketAsyncOperation.Connect:
		case SocketAsyncOperation.Send:
		case SocketAsyncOperation.SendPackets:
		case SocketAsyncOperation.SendTo:
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (socketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
			break;
		case SocketAsyncOperation.Disconnect:
			break;
		}
	}

	[MemberNotNull("_preAllocatedOverlapped")]
	private void InitializeInternals()
	{
		_preAllocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(s_completionPortCallback, _strongThisRef, null);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"new PreAllocatedOverlapped {_preAllocatedOverlapped}", "InitializeInternals");
		}
	}

	private void FreeInternals()
	{
		FreePinHandles();
		FreeOverlapped();
	}

	private unsafe NativeOverlapped* AllocateNativeOverlapped()
	{
		ThreadPoolBoundHandle orAllocateThreadPoolBoundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
		return orAllocateThreadPoolBoundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
	}

	private unsafe void FreeNativeOverlapped(ref NativeOverlapped* overlapped)
	{
		_currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
		overlapped = null;
	}

	private unsafe SocketError GetIOCPResult(bool success, ref NativeOverlapped* overlapped)
	{
		if (success)
		{
			if (_currentSocket.SafeHandle.SkipCompletionPortOnSuccess)
			{
				FreeNativeOverlapped(ref overlapped);
				return SocketError.Success;
			}
			return SocketError.IOPending;
		}
		SocketError lastSocketError = SocketPal.GetLastSocketError();
		if (lastSocketError != SocketError.IOPending)
		{
			FreeNativeOverlapped(ref overlapped);
			return lastSocketError;
		}
		return SocketError.IOPending;
	}

	private unsafe SocketError ProcessIOCPResult(bool success, int bytesTransferred, ref NativeOverlapped* overlapped, Memory<byte> bufferToPin, CancellationToken cancellationToken)
	{
		SocketError socketError = GetIOCPResult(success, ref overlapped);
		SocketFlags socketFlags = SocketFlags.None;
		if (socketError == SocketError.IOPending)
		{
			if (cancellationToken.CanBeCanceled)
			{
				_pendingOverlappedForCancellation = overlapped;
				_registrationToCancelPendingIO = cancellationToken.UnsafeRegister(delegate(object s)
				{
					SocketAsyncEventArgs socketAsyncEventArgs = (SocketAsyncEventArgs)s;
					SafeSocketHandle safeHandle = socketAsyncEventArgs._currentSocket.SafeHandle;
					if (!safeHandle.IsClosed)
					{
						try
						{
							bool flag = global::Interop.Kernel32.CancelIoEx(safeHandle, socketAsyncEventArgs._pendingOverlappedForCancellation);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(socketAsyncEventArgs, flag ? "Socket operation canceled." : $"CancelIoEx failed with error '{Marshal.GetLastPInvokeError()}'.", "ProcessIOCPResult");
							}
						}
						catch (ObjectDisposedException)
						{
						}
					}
				}, this);
			}
			if (!bufferToPin.Equals(default(Memory<byte>)))
			{
				_singleBufferHandle = bufferToPin.Pin();
			}
			ulong num = Interlocked.Exchange(ref _asyncCompletionOwnership, 1uL);
			if (num == 0L)
			{
				return SocketError.IOPending;
			}
			bytesTransferred = (int)((num >> 32) & 0x7FFFFFFF);
			socketError = (SocketError)(num & 0xFFFFFFFFu);
			if (socketError != 0)
			{
				GetOverlappedResultOnError(ref socketError, ref *(uint*)(&bytesTransferred), ref socketFlags, overlapped);
			}
			FreeNativeOverlapped(ref overlapped);
		}
		FinishOperationSync(socketError, bytesTransferred, socketFlags);
		return socketError;
	}

	internal unsafe SocketError DoOperationAccept(Socket socket, SafeSocketHandle handle, SafeSocketHandle acceptHandle, CancellationToken cancellationToken)
	{
		bool flag = _count != 0;
		Memory<byte> bufferToPin = (flag ? _buffer : ((Memory<byte>)_acceptBuffer));
		fixed (byte* ptr = &MemoryMarshal.GetReference(bufferToPin.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				int bytesReceived;
				bool success = socket.AcceptEx(handle, acceptHandle, (nint)(flag ? (ptr + _offset) : ptr), flag ? (_count - _acceptAddressBufferCount) : 0, _acceptAddressBufferCount / 2, _acceptAddressBufferCount / 2, out bytesReceived, overlapped);
				return ProcessIOCPResult(success, bytesReceived, ref overlapped, bufferToPin, cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal SocketError DoOperationConnect(SafeSocketHandle handle)
	{
		SocketError socketError = SocketPal.Connect(handle, _socketAddress.Buffer);
		FinishOperationSync(socketError, 0, SocketFlags.None);
		return socketError;
	}

	internal unsafe SocketError DoOperationConnectEx(Socket socket, SafeSocketHandle handle)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				int bytesSent;
				bool success = socket.ConnectEx(handle, _socketAddress.Buffer.Span, (nint)(ptr + _offset), _count, out bytesSent, overlapped);
				return ProcessIOCPResult(success, bytesSent, ref overlapped, _buffer, default(CancellationToken));
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationDisconnect(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			bool success = socket.DisconnectEx(handle, overlapped, DisconnectReuseSocket ? 2 : 0, 0);
			return ProcessIOCPResult(success, 0, ref overlapped, default(Memory<byte>), cancellationToken);
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	internal SocketError DoOperationReceive(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		if (_bufferList != null)
		{
			return DoOperationReceiveMultiBuffer(handle);
		}
		return DoOperationReceiveSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationReceiveSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (nint)(ptr + _offset);
				WSABuffer wSABuffer2 = wSABuffer;
				SocketFlags socketFlags = _socketFlags;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSARecv(handle, &wSABuffer2, 1, out bytesTransferred, ref socketFlags, overlapped, IntPtr.Zero);
				return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationReceiveMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			SocketFlags socketFlags = _socketFlags;
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSARecv(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, ref socketFlags, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, default(Memory<byte>), default(CancellationToken));
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	internal SocketError DoOperationReceiveFrom(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		AllocateSocketAddressBuffer();
		if (_bufferList != null)
		{
			return DoOperationReceiveFromMultiBuffer(handle);
		}
		return DoOperationReceiveFromSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationReceiveFromSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (nint)(ptr + _offset);
				WSABuffer buffer = wSABuffer;
				SocketFlags socketFlags = _socketFlags;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSARecvFrom(handle, ref buffer, 1, out bytesTransferred, ref socketFlags, PtrSocketAddressBuffer(), PtrSocketAddressSize(), overlapped, IntPtr.Zero);
				return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationReceiveFromMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			SocketFlags socketFlags = _socketFlags;
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSARecvFrom(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, ref socketFlags, PtrSocketAddressBuffer(), PtrSocketAddressSize(), overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, default(Memory<byte>), default(CancellationToken));
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		AllocateSocketAddressBuffer();
		if (_wsaMessageBufferPinned == null)
		{
			_wsaMessageBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.WSAMsg), pinned: true);
		}
		IPAddress iPAddress = ((_socketAddress.Family == AddressFamily.InterNetworkV6) ? _socketAddress.GetIPAddress() : null);
		bool flag = _currentSocket.AddressFamily == AddressFamily.InterNetwork || (iPAddress?.IsIPv4MappedToIPv6 ?? false);
		if (_currentSocket.AddressFamily == AddressFamily.InterNetworkV6 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(global::Interop.Winsock.ControlDataIPv6)))
		{
			_controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.ControlDataIPv6), pinned: true);
		}
		else if (flag && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(global::Interop.Winsock.ControlData)))
		{
			_controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.ControlData), pinned: true);
		}
		WSABuffer[] wsaRecvMsgWSABufferArray;
		uint wsaRecvMsgWSABufferCount;
		if (_bufferList == null)
		{
			if (_wsaRecvMsgWSABufferArrayPinned == null)
			{
				_wsaRecvMsgWSABufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(1, pinned: true);
			}
			fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
			{
				_wsaRecvMsgWSABufferArrayPinned[0].Pointer = (nint)(ptr + _offset);
				_wsaRecvMsgWSABufferArrayPinned[0].Length = _count;
				wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArrayPinned;
				wsaRecvMsgWSABufferCount = 1u;
				return Core();
			}
		}
		wsaRecvMsgWSABufferArray = _wsaBufferArrayPinned;
		wsaRecvMsgWSABufferCount = (uint)_bufferListInternal.Count;
		return Core();
		unsafe SocketError Core()
		{
			global::Interop.Winsock.WSAMsg* ptr2 = (global::Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
			ptr2->socketAddress = PtrSocketAddressBuffer();
			ptr2->addressLength = (uint)SocketAddress.GetMaximumAddressSize(_socketAddress.Family);
			fixed (WSABuffer* ptr3 = &wsaRecvMsgWSABufferArray[0])
			{
				void* buffers = ptr3;
				ptr2->buffers = (nint)buffers;
			}
			ptr2->count = wsaRecvMsgWSABufferCount;
			if (_controlBufferPinned != null)
			{
				fixed (byte* ptr4 = &_controlBufferPinned[0])
				{
					void* pointer = ptr4;
					ptr2->controlBuffer.Pointer = (nint)pointer;
				}
				ptr2->controlBuffer.Length = _controlBufferPinned.Length;
			}
			ptr2->flags = _socketFlags;
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				int bytesTransferred;
				SocketError socketError = socket.WSARecvMsg(handle, Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0), out bytesTransferred, overlapped, IntPtr.Zero);
				return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, (_bufferList == null) ? _buffer : default(Memory<byte>), cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal SocketError DoOperationSend(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		if (_bufferList != null)
		{
			return DoOperationSendMultiBuffer(handle);
		}
		return DoOperationSendSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationSendSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (nint)(ptr + _offset);
				WSABuffer wSABuffer2 = wSABuffer;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSASend(handle, &wSABuffer2, 1, out bytesTransferred, _socketFlags, overlapped, IntPtr.Zero);
				return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationSendMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSASend(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, _socketFlags, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, default(Memory<byte>), default(CancellationToken));
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	internal unsafe SocketError DoOperationSendPackets(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		SendPacketsElement[] array = (SendPacketsElement[])_sendPacketsElements.Clone();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		SendPacketsElement[] array2 = array;
		foreach (SendPacketsElement sendPacketsElement in array2)
		{
			if (sendPacketsElement != null)
			{
				if (sendPacketsElement.FilePath != null)
				{
					num++;
				}
				else if (sendPacketsElement.FileStream != null)
				{
					num2++;
				}
				else if (sendPacketsElement.MemoryBuffer.HasValue && sendPacketsElement.Count > 0)
				{
					num3++;
				}
			}
		}
		if (num + num2 + num3 == 0)
		{
			FinishOperationSyncSuccess(0, SocketFlags.None);
			return SocketError.Success;
		}
		if (num > 0)
		{
			int num4 = 0;
			_sendPacketsFileHandles = new SafeFileHandle[num];
			try
			{
				SendPacketsElement[] array3 = array;
				foreach (SendPacketsElement sendPacketsElement2 in array3)
				{
					if (sendPacketsElement2 != null && sendPacketsElement2.FilePath != null)
					{
						_sendPacketsFileHandles[num4] = File.OpenHandle(sendPacketsElement2.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, 0L);
						num4++;
					}
				}
			}
			catch
			{
				for (int num5 = num4 - 1; num5 >= 0; num5--)
				{
					_sendPacketsFileHandles[num5].Dispose();
				}
				_sendPacketsFileHandles = null;
				throw;
			}
		}
		global::Interop.Winsock.TransmitPacketsElement[] array4 = SetupPinHandlesSendPackets(array, num, num2, num3);
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			bool success = socket.TransmitPackets(handle, Marshal.UnsafeAddrOfPinnedArrayElement(array4, 0), array4.Length, _sendPacketsSendSize, overlapped, _sendPacketsFlags);
			return ProcessIOCPResult(success, 0, ref overlapped, default(Memory<byte>), cancellationToken);
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	internal SocketError DoOperationSendTo(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		if (_bufferList != null)
		{
			return DoOperationSendToMultiBuffer(handle);
		}
		return DoOperationSendToSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationSendToSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (nint)(ptr + _offset);
				WSABuffer buffer = wSABuffer;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSASendTo(handle, ref buffer, 1, out bytesTransferred, _socketFlags, _socketAddress.Buffer.Span, overlapped, IntPtr.Zero);
				return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
			}
			catch when (overlapped != null)
			{
				FreeNativeOverlapped(ref overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationSendToMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSASendTo(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, _socketFlags, _socketAddress.Buffer.Span, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, default(Memory<byte>), default(CancellationToken));
		}
		catch when (overlapped != null)
		{
			FreeNativeOverlapped(ref overlapped);
			throw;
		}
	}

	private void SetupMultipleBuffers()
	{
		if (_bufferListInternal == null || _bufferListInternal.Count == 0)
		{
			if (_pinState == PinState.MultipleBuffer)
			{
				FreePinHandles();
			}
			return;
		}
		FreePinHandles();
		try
		{
			int count = _bufferListInternal.Count;
			if (_multipleBufferMemoryHandles == null || _multipleBufferMemoryHandles.Length < count)
			{
				_multipleBufferMemoryHandles = new MemoryHandle[count];
			}
			for (int i = 0; i < count; i++)
			{
				_multipleBufferMemoryHandles[i] = _bufferListInternal[i].Array.AsMemory().Pin();
			}
			if (_wsaBufferArrayPinned == null || _wsaBufferArrayPinned.Length < count)
			{
				_wsaBufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(count, pinned: true);
			}
			for (int j = 0; j < count; j++)
			{
				ArraySegment<byte> arraySegment = _bufferListInternal[j];
				_wsaBufferArrayPinned[j].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(arraySegment.Array, arraySegment.Offset);
				_wsaBufferArrayPinned[j].Length = arraySegment.Count;
			}
			_pinState = PinState.MultipleBuffer;
		}
		catch (Exception)
		{
			FreePinHandles();
			throw;
		}
	}

	private unsafe void AllocateSocketAddressBuffer()
	{
		int maximumAddressSize = SocketAddress.GetMaximumAddressSize(_socketAddress.Family);
		if (_socketAddressPtr == IntPtr.Zero)
		{
			_socketAddressPtr = (nint)NativeMemory.Alloc((uint)(_socketAddress.Size + sizeof(nint)));
		}
		*(int*)_socketAddressPtr = maximumAddressSize;
	}

	private unsafe nint PtrSocketAddressBuffer()
	{
		return _socketAddressPtr + sizeof(nint);
	}

	private nint PtrSocketAddressSize()
	{
		return _socketAddressPtr;
	}

	private void FreeOverlapped()
	{
		if (_preAllocatedOverlapped != null)
		{
			_preAllocatedOverlapped.Dispose();
			_preAllocatedOverlapped = null;
		}
	}

	private unsafe void FreePinHandles()
	{
		_pinState = PinState.None;
		if (_multipleBufferMemoryHandles != null)
		{
			for (int i = 0; i < _multipleBufferMemoryHandles.Length; i++)
			{
				_multipleBufferMemoryHandles[i].Dispose();
				_multipleBufferMemoryHandles[i] = default(MemoryHandle);
			}
		}
		if (_socketAddressPtr != IntPtr.Zero)
		{
			NativeMemory.Free((void*)_socketAddressPtr);
			_socketAddressPtr = IntPtr.Zero;
		}
	}

	private unsafe global::Interop.Winsock.TransmitPacketsElement[] SetupPinHandlesSendPackets(SendPacketsElement[] sendPacketsElementsCopy, int sendPacketsElementsFileCount, int sendPacketsElementsFileStreamCount, int sendPacketsElementsBufferCount)
	{
		if (_pinState != 0)
		{
			FreePinHandles();
		}
		global::Interop.Winsock.TransmitPacketsElement[] array = GC.AllocateUninitializedArray<global::Interop.Winsock.TransmitPacketsElement>(sendPacketsElementsFileCount + sendPacketsElementsFileStreamCount + sendPacketsElementsBufferCount, pinned: true);
		if (_multipleBufferMemoryHandles == null || _multipleBufferMemoryHandles.Length < sendPacketsElementsBufferCount)
		{
			_multipleBufferMemoryHandles = new MemoryHandle[sendPacketsElementsBufferCount];
		}
		int num = 0;
		foreach (SendPacketsElement sendPacketsElement in sendPacketsElementsCopy)
		{
			if (sendPacketsElement != null && sendPacketsElement.MemoryBuffer.HasValue && sendPacketsElement.Count > 0)
			{
				_multipleBufferMemoryHandles[num] = sendPacketsElement.MemoryBuffer.Value.Pin();
				num++;
			}
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (SendPacketsElement sendPacketsElement2 in sendPacketsElementsCopy)
		{
			if (sendPacketsElement2 != null)
			{
				if (sendPacketsElement2.MemoryBuffer.HasValue && sendPacketsElement2.Count > 0)
				{
					array[num3].buffer = (nint)_multipleBufferMemoryHandles[num2].Pointer;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.Memory | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num2++;
					num3++;
				}
				else if (sendPacketsElement2.FilePath != null)
				{
					array[num3].fileHandle = _sendPacketsFileHandles[num4].DangerousGetHandle();
					array[num3].fileOffset = sendPacketsElement2.OffsetLong;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.File | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num4++;
					num3++;
				}
				else if (sendPacketsElement2.FileStream != null)
				{
					array[num3].fileHandle = sendPacketsElement2.FileStream.SafeFileHandle.DangerousGetHandle();
					array[num3].fileOffset = sendPacketsElement2.OffsetLong;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.File | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num3++;
				}
			}
		}
		_pinState = PinState.SendPackets;
		return array;
	}

	internal unsafe void LogBuffer(int size)
	{
		if (_bufferList != null)
		{
			for (int i = 0; i < _bufferListInternal.Count; i++)
			{
				WSABuffer wSABuffer = _wsaBufferArrayPinned[i];
				System.Net.NetEventSource.DumpBuffer(this, new ReadOnlySpan<byte>((void*)wSABuffer.Pointer, Math.Min(wSABuffer.Length, size)), "LogBuffer");
				if ((size -= wSABuffer.Length) <= 0)
				{
					break;
				}
			}
		}
		else if (_buffer.Length != 0)
		{
			System.Net.NetEventSource.DumpBuffer(this, _buffer, _offset, size, "LogBuffer");
		}
	}

	private unsafe SocketError FinishOperationAccept(SocketAddress remoteSocketAddress)
	{
		bool success = false;
		SafeHandle safeHandle = _currentSocket.SafeHandle;
		SocketError socketError;
		try
		{
			safeHandle.DangerousAddRef(ref success);
			nint pointer = safeHandle.DangerousGetHandle();
			bool flag = _count != 0;
			Memory<byte> memory = (flag ? _buffer : ((Memory<byte>)_acceptBuffer));
			fixed (byte* ptr = &MemoryMarshal.GetReference(memory.Span))
			{
				_currentSocket.GetAcceptExSockaddrs((nint)(flag ? (ptr + _offset) : ptr), flag ? (_count - _acceptAddressBufferCount) : 0, _acceptAddressBufferCount / 2, _acceptAddressBufferCount / 2, out var _, out var _, out var remoteSocketAddress2, out var remoteSocketAddressLength);
				new ReadOnlySpan<byte>((void*)remoteSocketAddress2, remoteSocketAddressLength).CopyTo(remoteSocketAddress.Buffer.Span);
				remoteSocketAddress.Size = remoteSocketAddressLength;
			}
			socketError = global::Interop.Winsock.setsockopt(_acceptSocket.SafeHandle, SocketOptionLevel.Socket, SocketOptionName.UpdateAcceptContext, ref pointer, IntPtr.Size);
			if (socketError == SocketError.SocketError)
			{
				socketError = SocketPal.GetLastSocketError();
			}
		}
		catch (ObjectDisposedException)
		{
			socketError = SocketError.OperationAborted;
		}
		finally
		{
			if (success)
			{
				safeHandle.DangerousRelease();
			}
		}
		return socketError;
	}

	private unsafe SocketError FinishOperationConnect()
	{
		try
		{
			if (_currentSocket.SocketType != SocketType.Stream)
			{
				return SocketError.Success;
			}
			SocketError socketError = global::Interop.Winsock.setsockopt(_currentSocket.SafeHandle, SocketOptionLevel.Socket, SocketOptionName.UpdateConnectContext, null, 0);
			return (socketError == SocketError.SocketError) ? SocketPal.GetLastSocketError() : socketError;
		}
		catch (ObjectDisposedException)
		{
			return SocketError.OperationAborted;
		}
	}

	private unsafe void UpdateReceivedSocketAddress(SocketAddress socketAddress)
	{
		new Span<byte>(length: socketAddress.Size = *(int*)_socketAddressPtr, pointer: (void*)PtrSocketAddressBuffer()).CopyTo(socketAddress.Buffer.Span);
	}

	private void CompleteCore()
	{
		_strongThisRef.Value = null;
		if (_asyncCompletionOwnership != 0L)
		{
			CleanupIOCPResult();
		}
		unsafe void CleanupIOCPResult()
		{
			_registrationToCancelPendingIO.Dispose();
			_registrationToCancelPendingIO = default(CancellationTokenRegistration);
			_pendingOverlappedForCancellation = null;
			_singleBufferHandle.Dispose();
			_singleBufferHandle = default(MemoryHandle);
			_asyncCompletionOwnership = 0uL;
		}
	}

	private unsafe void FinishOperationReceiveMessageFrom()
	{
		global::Interop.Winsock.WSAMsg* ptr = (global::Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
		_socketFlags = ptr->flags;
		if (_controlBufferPinned.Length == sizeof(global::Interop.Winsock.ControlData))
		{
			_receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((global::Interop.Winsock.ControlData*)ptr->controlBuffer.Pointer);
		}
		else if (_controlBufferPinned.Length == sizeof(global::Interop.Winsock.ControlDataIPv6))
		{
			_receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((global::Interop.Winsock.ControlDataIPv6*)ptr->controlBuffer.Pointer);
		}
		else
		{
			_receiveMessageFromPacketInfo = default(IPPacketInformation);
		}
	}

	private void FinishOperationSendPackets()
	{
		if (_sendPacketsFileHandles != null)
		{
			for (int i = 0; i < _sendPacketsFileHandles.Length; i++)
			{
				_sendPacketsFileHandles[i]?.Dispose();
			}
			_sendPacketsFileHandles = null;
		}
	}

	private unsafe void GetOverlappedResultOnError(ref SocketError socketError, ref uint numBytes, ref SocketFlags socketFlags, NativeOverlapped* nativeOverlapped)
	{
		if (socketError == SocketError.OperationAborted)
		{
			return;
		}
		if (_currentSocket.Disposed)
		{
			socketError = SocketError.OperationAborted;
			return;
		}
		try
		{
			global::Interop.Winsock.WSAGetOverlappedResult(_currentSocket.SafeHandle, nativeOverlapped, out numBytes, wait: false, out socketFlags);
			socketError = SocketPal.GetLastSocketError();
		}
		catch
		{
			socketError = SocketError.OperationAborted;
		}
	}
}
