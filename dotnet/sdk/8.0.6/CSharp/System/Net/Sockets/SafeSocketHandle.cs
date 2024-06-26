using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

public sealed class SafeSocketHandle : SafeHandleMinusOneIsInvalid
{
	private int _ownClose;

	private volatile bool _released;

	private bool _hasShutdownSend;

	private ThreadPoolBoundHandle _iocpBoundHandle;

	private bool _skipCompletionPortOnSuccess;

	internal bool OwnsHandle { get; }

	public override bool IsInvalid
	{
		get
		{
			if (!base.IsClosed)
			{
				return base.IsInvalid;
			}
			return true;
		}
	}

	internal ThreadPoolBoundHandle? IOCPBoundHandle => _iocpBoundHandle;

	internal bool SkipCompletionPortOnSuccess => _skipCompletionPortOnSuccess;

	public SafeSocketHandle()
		: base(ownsHandle: true)
	{
		OwnsHandle = true;
	}

	public SafeSocketHandle(nint preexistingHandle, bool ownsHandle)
		: base(ownsHandle: true)
	{
		OwnsHandle = ownsHandle;
		SetHandleAndValid(preexistingHandle);
	}

	private bool TryOwnClose()
	{
		return Interlocked.CompareExchange(ref _ownClose, 1, 0) == 0;
	}

	internal void TrackShutdown(SocketShutdown how)
	{
		if (how == SocketShutdown.Send || how == SocketShutdown.Both)
		{
			_hasShutdownSend = true;
		}
	}

	protected override bool ReleaseHandle()
	{
		_released = true;
		bool flag = TryOwnClose();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"shouldClose={flag}", "ReleaseHandle");
		}
		if (flag)
		{
			CloseHandle(abortive: true, canceledOperations: false);
		}
		return true;
	}

	internal void CloseAsIs(bool abortive)
	{
		bool flag = TryOwnClose();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"abortive={abortive}, shouldClose ={flag}", "CloseAsIs");
		}
		Dispose();
		if (flag)
		{
			bool flag2 = false;
			SpinWait spinWait = default(SpinWait);
			while (!_released)
			{
				flag2 |= TryUnblockSocket(abortive);
				spinWait.SpinOnce();
			}
			CloseHandle(abortive, flag2);
		}
	}

	private bool CloseHandle(bool abortive, bool canceledOperations)
	{
		bool flag = false;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"handle:{handle}", "CloseHandle");
		}
		canceledOperations |= OnHandleClose();
		if (canceledOperations && !_hasShutdownSend)
		{
			abortive = true;
		}
		return !OwnsHandle || DoCloseHandle(abortive) == SocketError.Success;
	}

	private void SetHandleAndValid(nint handle)
	{
		SetHandle(handle);
		if (IsInvalid)
		{
			TryOwnClose();
			SetHandleAsInvalid();
		}
	}

	internal void SetExposed()
	{
	}

	internal ThreadPoolBoundHandle GetThreadPoolBoundHandle()
	{
		if (_released)
		{
			return null;
		}
		return _iocpBoundHandle;
	}

	internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle(bool trySkipCompletionPortOnSuccess)
	{
		if (_released)
		{
			ThrowSocketDisposedException();
		}
		if (_iocpBoundHandle != null)
		{
			return _iocpBoundHandle;
		}
		lock (this)
		{
			ThreadPoolBoundHandle threadPoolBoundHandle = _iocpBoundHandle;
			if (threadPoolBoundHandle == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "calling ThreadPool.BindHandle()", "GetOrAllocateThreadPoolBoundHandle");
				}
				try
				{
					threadPoolBoundHandle = ThreadPoolBoundHandle.BindHandle(this);
				}
				catch (Exception ex) when (!ExceptionCheck.IsFatal(ex))
				{
					bool isClosed = base.IsClosed;
					bool flag = !IsInvalid && !base.IsClosed && ex is ArgumentException;
					CloseAsIs(abortive: false);
					if (isClosed)
					{
						ThrowSocketDisposedException(ex);
					}
					if (flag)
					{
						throw new InvalidOperationException(System.SR.net_sockets_asyncoperations_notallowed, ex);
					}
					throw;
				}
				if (trySkipCompletionPortOnSuccess && CompletionPortHelper.SkipCompletionPortOnSuccess(threadPoolBoundHandle.Handle))
				{
					_skipCompletionPortOnSuccess = true;
				}
				Volatile.Write(ref _iocpBoundHandle, threadPoolBoundHandle);
			}
			return threadPoolBoundHandle;
		}
	}

	private unsafe bool OnHandleClose()
	{
		if (_iocpBoundHandle != null)
		{
			if (!OwnsHandle)
			{
				global::Interop.Kernel32.CancelIoEx(handle, null);
			}
			_iocpBoundHandle.Dispose();
		}
		return false;
	}

	private unsafe bool TryUnblockSocket(bool _)
	{
		return global::Interop.Kernel32.CancelIoEx(handle, null);
	}

	private SocketError DoCloseHandle(bool abortive)
	{
		SocketError socketError;
		if (!abortive)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"handle:{handle}, Following 'blockable' branch", "DoCloseHandle");
			}
			socketError = global::Interop.Winsock.closesocket(handle);
			if (socketError == SocketError.SocketError)
			{
				socketError = (SocketError)Marshal.GetLastPInvokeError();
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"handle:{handle}, closesocket()#1:{socketError}", "DoCloseHandle");
			}
			if (socketError != SocketError.WouldBlock)
			{
				return socketError;
			}
			int argp = 0;
			socketError = global::Interop.Winsock.ioctlsocket(handle, -2147195266, ref argp);
			if (socketError == SocketError.SocketError)
			{
				socketError = (SocketError)Marshal.GetLastPInvokeError();
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"handle:{handle}, ioctlsocket()#1:{socketError}", "DoCloseHandle");
			}
			if (socketError == SocketError.Success)
			{
				socketError = global::Interop.Winsock.closesocket(handle);
				if (socketError == SocketError.SocketError)
				{
					socketError = (SocketError)Marshal.GetLastPInvokeError();
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"handle:{handle}, closesocket#2():{socketError}", "DoCloseHandle");
				}
				if (socketError != SocketError.WouldBlock)
				{
					return socketError;
				}
			}
		}
		Unsafe.SkipInit(out global::Interop.Winsock.Linger linger);
		linger.OnOff = 1;
		linger.Time = 0;
		socketError = global::Interop.Winsock.setsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, ref linger, 4);
		if (socketError == SocketError.SocketError)
		{
			socketError = (SocketError)Marshal.GetLastPInvokeError();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"handle:{handle}, setsockopt():{socketError}", "DoCloseHandle");
		}
		if (socketError != 0 && socketError != SocketError.InvalidArgument && socketError != SocketError.ProtocolOption)
		{
			return socketError;
		}
		socketError = global::Interop.Winsock.closesocket(handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"handle:{handle}, closesocket#3():{((socketError == SocketError.SocketError) ? ((SocketError)Marshal.GetLastPInvokeError()) : socketError)}", "DoCloseHandle");
		}
		return socketError;
	}

	private static void ThrowSocketDisposedException(Exception innerException = null)
	{
		throw new ObjectDisposedException(typeof(Socket).FullName, innerException);
	}
}
