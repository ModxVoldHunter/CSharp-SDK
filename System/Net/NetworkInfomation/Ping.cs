using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation;

public class Ping : Component
{
	private readonly ManualResetEventSlim _lockObject = new ManualResetEventSlim(initialState: true);

	private SendOrPostCallback _onPingCompletedDelegate;

	private bool _disposeRequested;

	private byte[] _defaultSendBuffer;

	private CancellationTokenSource _timeoutOrCancellationSource;

	private bool _canceled;

	private int _status;

	private static readonly SafeWaitHandle s_nullSafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, ownsHandle: true);

	private int _sendSize;

	private bool _ipv6;

	private ManualResetEvent _pingEvent;

	private RegisteredWaitHandle _registeredWait;

	private SafeLocalAllocHandle _requestBuffer;

	private SafeLocalAllocHandle _replyBuffer;

	private global::Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV4;

	private global::Interop.IpHlpApi.SafeCloseIcmpHandle _handlePingV6;

	private TaskCompletionSource<PingReply> _taskCompletionSource;

	private byte[] DefaultSendBuffer
	{
		get
		{
			if (_defaultSendBuffer == null)
			{
				_defaultSendBuffer = new byte[32];
				for (int i = 0; i < 32; i++)
				{
					_defaultSendBuffer[i] = (byte)(97 + i % 23);
				}
			}
			return _defaultSendBuffer;
		}
	}

	public event PingCompletedEventHandler? PingCompleted;

	public Ping()
	{
		if (GetType() == typeof(Ping))
		{
			GC.SuppressFinalize(this);
		}
	}

	private void CheckArgs(int timeout, byte[] buffer)
	{
		ObjectDisposedException.ThrowIf(_disposeRequested, this);
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		if (buffer.Length > 65500)
		{
			throw new ArgumentException(System.SR.net_invalidPingBufferSize, "buffer");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(timeout, "timeout");
	}

	private void CheckArgs(IPAddress address, int timeout, byte[] buffer)
	{
		CheckArgs(timeout, buffer);
		ArgumentNullException.ThrowIfNull(address, "address");
		TestIsIpSupported(address);
		if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
		{
			throw new ArgumentException(System.SR.net_invalid_ip_addr, "address");
		}
	}

	[MemberNotNull("_timeoutOrCancellationSource")]
	private void CheckStart()
	{
		int status;
		lock (_lockObject)
		{
			status = _status;
			if (status == 0)
			{
				if (_timeoutOrCancellationSource == null)
				{
					_timeoutOrCancellationSource = new CancellationTokenSource();
				}
				_canceled = false;
				_status = 1;
				_lockObject.Reset();
				return;
			}
		}
		if (status == 1)
		{
			throw new InvalidOperationException(System.SR.net_inasync);
		}
		throw new ObjectDisposedException(GetType().FullName);
	}

	private static IPAddress GetAddressSnapshot(IPAddress address)
	{
		return (address.AddressFamily == AddressFamily.InterNetwork) ? new IPAddress(address.Address) : new IPAddress(address.GetAddressBytes(), address.ScopeId);
	}

	private void Finish()
	{
		lock (_lockObject)
		{
			_status = 0;
			if (!_timeoutOrCancellationSource.TryReset())
			{
				_timeoutOrCancellationSource = null;
			}
			_lockObject.Set();
		}
		if (_disposeRequested)
		{
			InternalDispose();
		}
	}

	private void InternalDispose()
	{
		_disposeRequested = true;
		lock (_lockObject)
		{
			if (_status != 0)
			{
				return;
			}
			_status = 2;
		}
		_timeoutOrCancellationSource?.Dispose();
		InternalDisposeCore();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			InternalDispose();
		}
	}

	protected void OnPingCompleted(PingCompletedEventArgs e)
	{
		this.PingCompleted?.Invoke(this, e);
	}

	public PingReply Send(string hostNameOrAddress)
	{
		return Send(hostNameOrAddress, 5000, DefaultSendBuffer);
	}

	public PingReply Send(string hostNameOrAddress, int timeout)
	{
		return Send(hostNameOrAddress, timeout, DefaultSendBuffer);
	}

	public PingReply Send(IPAddress address)
	{
		return Send(address, 5000, DefaultSendBuffer);
	}

	public PingReply Send(IPAddress address, int timeout)
	{
		return Send(address, timeout, DefaultSendBuffer);
	}

	public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer)
	{
		return Send(hostNameOrAddress, timeout, buffer, null);
	}

	public PingReply Send(IPAddress address, int timeout, byte[] buffer)
	{
		return Send(address, timeout, buffer, null);
	}

	public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
	{
		if (string.IsNullOrEmpty(hostNameOrAddress))
		{
			throw new ArgumentNullException("hostNameOrAddress");
		}
		if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
		{
			return Send(address, timeout, buffer, options);
		}
		CheckArgs(timeout, buffer);
		return GetAddressAndSend(hostNameOrAddress, timeout, buffer, options);
	}

	public PingReply Send(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
	{
		CheckArgs(address, timeout, buffer);
		IPAddress addressSnapshot = GetAddressSnapshot(address);
		CheckStart();
		try
		{
			return SendPingCore(addressSnapshot, buffer, timeout, options);
		}
		catch (Exception ex) when (!(ex is PlatformNotSupportedException))
		{
			throw new PingException(System.SR.net_ping, ex);
		}
		finally
		{
			Finish();
		}
	}

	public PingReply Send(IPAddress address, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null)
	{
		return Send(address, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options);
	}

	public PingReply Send(string hostNameOrAddress, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null)
	{
		return Send(hostNameOrAddress, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options);
	}

	public void SendAsync(string hostNameOrAddress, object? userToken)
	{
		SendAsync(hostNameOrAddress, 5000, DefaultSendBuffer, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, object? userToken)
	{
		SendAsync(hostNameOrAddress, timeout, DefaultSendBuffer, userToken);
	}

	public void SendAsync(IPAddress address, object? userToken)
	{
		SendAsync(address, 5000, DefaultSendBuffer, userToken);
	}

	public void SendAsync(IPAddress address, int timeout, object? userToken)
	{
		SendAsync(address, timeout, DefaultSendBuffer, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, object? userToken)
	{
		SendAsync(hostNameOrAddress, timeout, buffer, null, userToken);
	}

	public void SendAsync(IPAddress address, int timeout, byte[] buffer, object? userToken)
	{
		SendAsync(address, timeout, buffer, null, userToken);
	}

	public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options, object? userToken)
	{
		TranslateTaskToEap(userToken, SendPingAsync(hostNameOrAddress, timeout, buffer, options));
	}

	public void SendAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options, object? userToken)
	{
		TranslateTaskToEap(userToken, SendPingAsync(address, timeout, buffer, options));
	}

	private void TranslateTaskToEap(object userToken, Task<PingReply> pingTask)
	{
		pingTask.ContinueWith(delegate(Task<PingReply> t, object state)
		{
			AsyncOperation asyncOperation = (AsyncOperation)state;
			PingCompletedEventArgs arg = new PingCompletedEventArgs(t.IsCompletedSuccessfully ? t.Result : null, t.Exception, t.IsCanceled, asyncOperation.UserSuppliedState);
			SendOrPostCallback d = delegate(object o)
			{
				OnPingCompleted((PingCompletedEventArgs)o);
			};
			asyncOperation.PostOperationCompleted(d, arg);
		}, AsyncOperationManager.CreateOperation(userToken), CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public Task<PingReply> SendPingAsync(IPAddress address)
	{
		return SendPingAsync(address, 5000, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress)
	{
		return SendPingAsync(hostNameOrAddress, 5000, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout)
	{
		return SendPingAsync(address, timeout, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout)
	{
		return SendPingAsync(hostNameOrAddress, timeout, DefaultSendBuffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer)
	{
		return SendPingAsync(address, timeout, buffer, null);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer)
	{
		return SendPingAsync(hostNameOrAddress, timeout, buffer, null);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions? options)
	{
		return SendPingAsync(address, timeout, buffer, options, CancellationToken.None);
	}

	public Task<PingReply> SendPingAsync(IPAddress address, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendPingAsync(address, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options, cancellationToken);
	}

	private Task<PingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options, CancellationToken cancellationToken)
	{
		CheckArgs(address, timeout, buffer);
		return SendPingAsyncInternal(GetAddressSnapshot(address), (IPAddress address, CancellationToken cancellationToken) => new ValueTask<IPAddress>(address), timeout, buffer, options, cancellationToken);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions? options)
	{
		return SendPingAsync(hostNameOrAddress, timeout, buffer, options, CancellationToken.None);
	}

	public Task<PingReply> SendPingAsync(string hostNameOrAddress, TimeSpan timeout, byte[]? buffer = null, PingOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendPingAsync(hostNameOrAddress, ToTimeoutMilliseconds(timeout), buffer ?? DefaultSendBuffer, options, cancellationToken);
	}

	private Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostNameOrAddress))
		{
			throw new ArgumentNullException("hostNameOrAddress");
		}
		if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
		{
			return SendPingAsync(address, timeout, buffer, options, cancellationToken);
		}
		CheckArgs(timeout, buffer);
		return SendPingAsyncInternal(hostNameOrAddress, async (string hostName, CancellationToken cancellationToken) => (await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))[0], timeout, buffer, options, cancellationToken);
	}

	private static int ToTimeoutMilliseconds(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "timeout");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return (int)num;
	}

	public void SendAsyncCancel()
	{
		lock (_lockObject)
		{
			if (!_lockObject.IsSet)
			{
				SetCanceled();
			}
		}
		_lockObject.Wait();
	}

	private void SetCanceled()
	{
		_canceled = true;
		_timeoutOrCancellationSource?.Cancel();
	}

	private PingReply GetAddressAndSend(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options)
	{
		CheckStart();
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostNameOrAddress);
			return SendPingCore(hostAddresses[0], buffer, timeout, options);
		}
		catch (Exception ex) when (!(ex is PlatformNotSupportedException))
		{
			throw new PingException(System.SR.net_ping, ex);
		}
		finally
		{
			Finish();
		}
	}

	private async Task<PingReply> SendPingAsyncInternal<TArg>(TArg getAddressArg, Func<TArg, CancellationToken, ValueTask<IPAddress>> getAddress, int timeout, byte[] buffer, PingOptions options, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		CheckStart();
		try
		{
			using (cancellationToken.UnsafeRegister(delegate(object state)
			{
				((Ping)state).SetCanceled();
			}, this))
			{
				Task<PingReply> task = SendPingAsyncCore(await getAddress(getAddressArg, _timeoutOrCancellationSource.Token).ConfigureAwait(continueOnCapturedContext: false), buffer, timeout, options);
				_timeoutOrCancellationSource.CancelAfter(timeout);
				return await task.ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception ex) when (!(ex is PlatformNotSupportedException) && (!(ex is OperationCanceledException) || !_canceled))
		{
			throw new PingException(System.SR.net_ping, ex);
		}
		finally
		{
			Finish();
		}
	}

	private static void TestIsIpSupported(IPAddress ip)
	{
		if (ip.AddressFamily == AddressFamily.InterNetwork && !System.Net.SocketProtocolSupportPal.OSSupportsIPv4)
		{
			throw new NotSupportedException(System.SR.net_ipv4_not_installed);
		}
		if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !System.Net.SocketProtocolSupportPal.OSSupportsIPv6)
		{
			throw new NotSupportedException(System.SR.net_ipv6_not_installed);
		}
	}

	private void InternalDisposeCore()
	{
		if (_handlePingV4 != null)
		{
			_handlePingV4.Dispose();
			_handlePingV4 = null;
		}
		if (_handlePingV6 != null)
		{
			_handlePingV6.Dispose();
			_handlePingV6 = null;
		}
		UnregisterWaitHandle();
		if (_pingEvent != null)
		{
			_pingEvent.Dispose();
			_pingEvent = null;
		}
		if (_replyBuffer != null)
		{
			_replyBuffer.Dispose();
			_replyBuffer = null;
		}
	}

	private PingReply SendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
	{
		return DoSendPingCore(address, buffer, timeout, options, isAsync: false).GetAwaiter().GetResult();
	}

	private Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
	{
		return DoSendPingCore(address, buffer, timeout, options, isAsync: true);
	}

	private Task<PingReply> DoSendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions options, bool isAsync)
	{
		TaskCompletionSource<PingReply> taskCompletionSource = null;
		if (isAsync)
		{
			taskCompletionSource = (_taskCompletionSource = new TaskCompletionSource<PingReply>());
		}
		_ipv6 = address.AddressFamily == AddressFamily.InterNetworkV6;
		_sendSize = buffer.Length;
		InitialiseIcmpHandle();
		if (_replyBuffer == null)
		{
			_replyBuffer = SafeLocalAllocHandle.LocalAlloc(65791);
		}
		int num;
		try
		{
			if (isAsync)
			{
				RegisterWaitHandle();
			}
			SetUnmanagedStructures(buffer);
			num = SendEcho(address, buffer, timeout, options, isAsync);
		}
		catch
		{
			Cleanup(isAsync);
			throw;
		}
		if (num == 0)
		{
			num = Marshal.GetLastPInvokeError();
			if (!isAsync || (long)num != 997)
			{
				Cleanup(isAsync);
				IPStatus statusFromCode = GetStatusFromCode(num);
				return Task.FromResult(new PingReply(address, null, statusFromCode, 0L, Array.Empty<byte>()));
			}
		}
		if (taskCompletionSource != null)
		{
			return taskCompletionSource.Task;
		}
		Cleanup(isAsync);
		return Task.FromResult(CreatePingReply());
	}

	private void RegisterWaitHandle()
	{
		if (_pingEvent == null)
		{
			_pingEvent = new ManualResetEvent(initialState: false);
		}
		else
		{
			_pingEvent.Reset();
		}
		_registeredWait = ThreadPool.RegisterWaitForSingleObject(_pingEvent, delegate(object state, bool _)
		{
			((Ping)state).PingCallback();
		}, this, -1, executeOnlyOnce: true);
	}

	private void UnregisterWaitHandle()
	{
		lock (_lockObject)
		{
			if (_registeredWait != null)
			{
				_registeredWait.Unregister(null);
				_registeredWait = null;
			}
		}
	}

	private SafeWaitHandle GetWaitHandle(bool async)
	{
		if (async)
		{
			return _pingEvent.GetSafeWaitHandle();
		}
		return s_nullSafeWaitHandle;
	}

	private void InitialiseIcmpHandle()
	{
		if (!_ipv6 && _handlePingV4 == null)
		{
			_handlePingV4 = global::Interop.IpHlpApi.IcmpCreateFile();
			if (_handlePingV4.IsInvalid)
			{
				_handlePingV4.Dispose();
				_handlePingV4 = null;
				throw new Win32Exception();
			}
		}
		else if (_ipv6 && _handlePingV6 == null)
		{
			_handlePingV6 = global::Interop.IpHlpApi.Icmp6CreateFile();
			if (_handlePingV6.IsInvalid)
			{
				_handlePingV6.Dispose();
				_handlePingV6 = null;
				throw new Win32Exception();
			}
		}
	}

	private int SendEcho(IPAddress address, byte[] buffer, int timeout, PingOptions options, bool isAsync)
	{
		global::Interop.IpHlpApi.IP_OPTION_INFORMATION options2 = new global::Interop.IpHlpApi.IP_OPTION_INFORMATION(options);
		if (!_ipv6)
		{
			return (int)global::Interop.IpHlpApi.IcmpSendEcho2(_handlePingV4, GetWaitHandle(isAsync), IntPtr.Zero, IntPtr.Zero, (uint)address.Address, _requestBuffer, (ushort)buffer.Length, ref options2, _replyBuffer, 65791u, (uint)timeout);
		}
		Span<byte> span = stackalloc byte[28];
		System.Net.Sockets.IPEndPointExtensions.SetIPAddress(span, address);
		Span<byte> sourceSocketAddress = stackalloc byte[28];
		sourceSocketAddress.Clear();
		return (int)global::Interop.IpHlpApi.Icmp6SendEcho2(_handlePingV6, GetWaitHandle(isAsync), IntPtr.Zero, IntPtr.Zero, sourceSocketAddress, span, _requestBuffer, (ushort)buffer.Length, ref options2, _replyBuffer, 65791u, (uint)timeout);
	}

	private unsafe PingReply CreatePingReply()
	{
		SafeLocalAllocHandle replyBuffer = _replyBuffer;
		if (_ipv6)
		{
			return CreatePingReplyFromIcmp6EchoReply(in *(global::Interop.IpHlpApi.ICMPV6_ECHO_REPLY*)replyBuffer.DangerousGetHandle(), replyBuffer.DangerousGetHandle(), _sendSize);
		}
		return CreatePingReplyFromIcmpEchoReply(in *(global::Interop.IpHlpApi.ICMP_ECHO_REPLY*)replyBuffer.DangerousGetHandle());
	}

	private void Cleanup(bool isAsync)
	{
		FreeUnmanagedStructures();
		if (isAsync)
		{
			UnregisterWaitHandle();
		}
	}

	private void PingCallback()
	{
		TaskCompletionSource<PingReply> taskCompletionSource = _taskCompletionSource;
		_taskCompletionSource = null;
		PingReply pingReply = null;
		Exception exception = null;
		bool flag = false;
		try
		{
			lock (_lockObject)
			{
				flag = _canceled;
				pingReply = CreatePingReply();
			}
		}
		catch (Exception innerException)
		{
			exception = new PingException(System.SR.net_ping, innerException);
		}
		finally
		{
			Cleanup(isAsync: true);
		}
		if (flag)
		{
			taskCompletionSource.SetCanceled();
		}
		else if (pingReply != null)
		{
			taskCompletionSource.SetResult(pingReply);
		}
		else
		{
			taskCompletionSource.SetException(exception);
		}
	}

	private unsafe void SetUnmanagedStructures(byte[] buffer)
	{
		_requestBuffer = SafeLocalAllocHandle.LocalAlloc(buffer.Length);
		byte* ptr = (byte*)_requestBuffer.DangerousGetHandle();
		for (int i = 0; i < buffer.Length; i++)
		{
			ptr[i] = buffer[i];
		}
	}

	private void FreeUnmanagedStructures()
	{
		if (_requestBuffer != null)
		{
			_requestBuffer.Dispose();
			_requestBuffer = null;
		}
	}

	private static IPStatus GetStatusFromCode(int statusCode)
	{
		if (statusCode != 0 && statusCode < 11000)
		{
			throw new Win32Exception(statusCode);
		}
		return (IPStatus)statusCode;
	}

	private static PingReply CreatePingReplyFromIcmpEchoReply(in global::Interop.IpHlpApi.ICMP_ECHO_REPLY reply)
	{
		IPAddress address = new IPAddress(reply.address);
		IPStatus statusFromCode = GetStatusFromCode((int)reply.status);
		long rtt;
		PingOptions options;
		byte[] array;
		if (statusFromCode == IPStatus.Success)
		{
			rtt = reply.roundTripTime;
			options = new PingOptions(reply.options.ttl, (reply.options.flags & 2) > 0);
			array = new byte[reply.dataSize];
			Marshal.Copy(reply.data, array, 0, reply.dataSize);
		}
		else
		{
			rtt = 0L;
			options = null;
			array = Array.Empty<byte>();
		}
		return new PingReply(address, options, statusFromCode, rtt, array);
	}

	private static PingReply CreatePingReplyFromIcmp6EchoReply(in global::Interop.IpHlpApi.ICMPV6_ECHO_REPLY reply, nint dataPtr, int sendSize)
	{
		IPAddress address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
		IPStatus statusFromCode = GetStatusFromCode((int)reply.Status);
		long rtt;
		byte[] array;
		if (statusFromCode == IPStatus.Success)
		{
			rtt = reply.RoundTripTime;
			array = new byte[sendSize];
			Marshal.Copy(dataPtr + 36, array, 0, sendSize);
		}
		else
		{
			rtt = 0L;
			array = Array.Empty<byte>();
		}
		return new PingReply(address, null, statusFromCode, rtt, array);
	}
}
