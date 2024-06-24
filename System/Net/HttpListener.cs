using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

public sealed class HttpListener : IDisposable
{
	public delegate ExtendedProtectionPolicy ExtendedProtectionSelector(HttpListenerRequest request);

	private enum State
	{
		Stopped,
		Started,
		Closed
	}

	private sealed class DisconnectAsyncResult : IAsyncResult
	{
		private unsafe static readonly IOCompletionCallback s_IOCallback = WaitCallback;

		private readonly ulong _connectionId;

		private readonly HttpListenerSession _listenerSession;

		private unsafe readonly NativeOverlapped* _nativeOverlapped;

		private int _ownershipState;

		internal unsafe NativeOverlapped* NativeOverlapped => _nativeOverlapped;

		public object AsyncState
		{
			get
			{
				throw new NotImplementedException(System.SR.net_PropertyNotImplementedException);
			}
		}

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				throw new NotImplementedException(System.SR.net_PropertyNotImplementedException);
			}
		}

		public bool CompletedSynchronously
		{
			get
			{
				throw new NotImplementedException(System.SR.net_PropertyNotImplementedException);
			}
		}

		public bool IsCompleted
		{
			get
			{
				throw new NotImplementedException(System.SR.net_PropertyNotImplementedException);
			}
		}

		internal WindowsPrincipal AuthenticatedConnection { get; set; }

		internal NegotiateAuthentication Session { get; set; }

		internal string SessionPackage { get; set; }

		internal unsafe DisconnectAsyncResult(HttpListenerSession session, ulong connectionId)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"HttpListener: {session.Listener}, ConnectionId: {connectionId}", ".ctor");
			}
			_ownershipState = 1;
			_listenerSession = session;
			_connectionId = connectionId;
			_nativeOverlapped = session.RequestQueueBoundHandle.AllocateNativeOverlapped(s_IOCallback, this, null);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info($"DisconnectAsyncResult: ThreadPoolBoundHandle.AllocateNativeOverlapped({session.RequestQueueBoundHandle}) -> {_nativeOverlapped->GetHashCode()}", null, ".ctor");
			}
		}

		internal bool StartOwningDisconnectHandling()
		{
			SpinWait spinWait = default(SpinWait);
			int num;
			while ((num = Interlocked.CompareExchange(ref _ownershipState, 1, 0)) == 2)
			{
				spinWait.SpinOnce();
			}
			return num < 2;
		}

		internal void FinishOwningDisconnectHandling()
		{
			if (Interlocked.CompareExchange(ref _ownershipState, 0, 1) == 2)
			{
				HandleDisconnect();
			}
		}

		internal unsafe void IOCompleted(NativeOverlapped* nativeOverlapped)
		{
			IOCompleted(this, nativeOverlapped);
		}

		private unsafe static void IOCompleted(DisconnectAsyncResult asyncResult, NativeOverlapped* nativeOverlapped)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, "_connectionId:" + asyncResult._connectionId, "IOCompleted");
			}
			asyncResult._listenerSession.RequestQueueBoundHandle.FreeNativeOverlapped(nativeOverlapped);
			if (Interlocked.Exchange(ref asyncResult._ownershipState, 2) == 0)
			{
				asyncResult.HandleDisconnect();
			}
		}

		private unsafe static void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"errorCode: {errorCode}, numBytes: {numBytes}, nativeOverlapped: {(nint)nativeOverlapped:x}", "WaitCallback");
			}
			DisconnectAsyncResult asyncResult = (DisconnectAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
			IOCompleted(asyncResult, nativeOverlapped);
		}

		private void HandleDisconnect()
		{
			HttpListener listener = _listenerSession.Listener;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"DisconnectResults {listener.DisconnectResults} removing for _connectionId: {_connectionId}", "HandleDisconnect");
			}
			listener.DisconnectResults.Remove(_connectionId);
			Session?.Dispose();
			if (AuthenticatedConnection?.Identity is IDisposable disposable && AuthenticatedConnection.Identity.AuthenticationType == "NTLM" && listener.UnsafeConnectionNtlmAuthentication)
			{
				disposable.Dispose();
			}
			int num = Interlocked.Exchange(ref _ownershipState, 3);
		}
	}

	private readonly object _internalLock;

	private volatile State _state;

	private readonly HttpListenerPrefixCollection _prefixes;

	internal Hashtable _uriPrefixes = new Hashtable();

	private bool _ignoreWriteExceptions;

	private readonly ServiceNameStore _defaultServiceNames;

	private readonly HttpListenerTimeoutManager _timeoutManager;

	private ExtendedProtectionPolicy _extendedProtectionPolicy;

	private AuthenticationSchemeSelector _authenticationDelegate;

	private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.Anonymous;

	private ExtendedProtectionSelector _extendedProtectionSelectorDelegate;

	private string _realm;

	internal static readonly bool SkipIOCPCallbackOnSuccess = Environment.OSVersion.Version >= new Version(6, 2);

	private static readonly byte[] s_wwwAuthenticateBytes = "WWW-Authenticate"u8.ToArray();

	private HttpListenerSession _currentSession;

	private bool _unsafeConnectionNtlmAuthentication;

	private HttpServerSessionHandle _serverSessionHandle;

	private ulong _urlGroupId;

	private bool _V2Initialized;

	private Dictionary<ulong, DisconnectAsyncResult> _disconnectResults;

	internal ICollection PrefixCollection => _uriPrefixes.Keys;

	public AuthenticationSchemeSelector? AuthenticationSchemeSelectorDelegate
	{
		get
		{
			return _authenticationDelegate;
		}
		set
		{
			CheckDisposed();
			_authenticationDelegate = value;
		}
	}

	public ExtendedProtectionSelector? ExtendedProtectionSelectorDelegate
	{
		get
		{
			return _extendedProtectionSelectorDelegate;
		}
		[param: DisallowNull]
		set
		{
			CheckDisposed();
			ArgumentNullException.ThrowIfNull(value, "value");
			_extendedProtectionSelectorDelegate = value;
		}
	}

	public AuthenticationSchemes AuthenticationSchemes
	{
		get
		{
			return _authenticationScheme;
		}
		set
		{
			CheckDisposed();
			_authenticationScheme = value;
		}
	}

	public ExtendedProtectionPolicy ExtendedProtectionPolicy
	{
		get
		{
			return _extendedProtectionPolicy;
		}
		set
		{
			CheckDisposed();
			ArgumentNullException.ThrowIfNull(value, "value");
			if (value.CustomChannelBinding != null)
			{
				throw new ArgumentException(System.SR.net_listener_cannot_set_custom_cbt, "value");
			}
			_extendedProtectionPolicy = value;
		}
	}

	public ServiceNameCollection DefaultServiceNames => _defaultServiceNames.ServiceNames;

	public HttpListenerPrefixCollection Prefixes
	{
		get
		{
			CheckDisposed();
			return _prefixes;
		}
	}

	public string? Realm
	{
		get
		{
			return _realm;
		}
		set
		{
			CheckDisposed();
			_realm = value;
		}
	}

	public bool IsListening => _state == State.Started;

	public bool IgnoreWriteExceptions
	{
		get
		{
			return _ignoreWriteExceptions;
		}
		set
		{
			CheckDisposed();
			_ignoreWriteExceptions = value;
		}
	}

	public static bool IsSupported => global::Interop.HttpApi.s_supported;

	public bool UnsafeConnectionNtlmAuthentication
	{
		get
		{
			return _unsafeConnectionNtlmAuthentication;
		}
		set
		{
			CheckDisposed();
			if (_unsafeConnectionNtlmAuthentication == value)
			{
				return;
			}
			lock (((ICollection)DisconnectResults).SyncRoot)
			{
				if (_unsafeConnectionNtlmAuthentication == value)
				{
					return;
				}
				_unsafeConnectionNtlmAuthentication = value;
				if (value)
				{
					return;
				}
				foreach (DisconnectAsyncResult value2 in DisconnectResults.Values)
				{
					value2.AuthenticatedConnection = null;
				}
			}
		}
	}

	private Dictionary<ulong, DisconnectAsyncResult> DisconnectResults => LazyInitializer.EnsureInitialized(ref _disconnectResults, () => new Dictionary<ulong, DisconnectAsyncResult>());

	public HttpListenerTimeoutManager TimeoutManager
	{
		get
		{
			ValidateV2Property();
			return _timeoutManager;
		}
	}

	public HttpListener()
	{
		_state = State.Stopped;
		_internalLock = new object();
		_defaultServiceNames = new ServiceNameStore();
		_timeoutManager = new HttpListenerTimeoutManager(this);
		_prefixes = new HttpListenerPrefixCollection(this);
		_extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
	}

	internal void AddPrefix(string uriPrefix)
	{
		ArgumentNullException.ThrowIfNull(uriPrefix, "uriPrefix");
		try
		{
			CheckDisposed();
			int num;
			if (string.Compare(uriPrefix, 0, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
			{
				num = 7;
			}
			else
			{
				if (string.Compare(uriPrefix, 0, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) != 0)
				{
					throw new ArgumentException(System.SR.net_listener_scheme, "uriPrefix");
				}
				num = 8;
			}
			bool flag = false;
			int k;
			for (k = num; k < uriPrefix.Length && uriPrefix[k] != '/' && (uriPrefix[k] != ':' || flag); k++)
			{
				if (uriPrefix[k] == '[')
				{
					if (flag)
					{
						k = num;
						break;
					}
					flag = true;
				}
				if (flag && uriPrefix[k] == ']')
				{
					flag = false;
				}
			}
			if (num == k)
			{
				throw new ArgumentException(System.SR.net_listener_host, "uriPrefix");
			}
			if (!uriPrefix.EndsWith('/'))
			{
				throw new ArgumentException(System.SR.net_listener_slash, "uriPrefix");
			}
			string text = CreateRegisteredPrefix(uriPrefix, k, num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"mapped uriPrefix: {uriPrefix} to registeredPrefix: {text}", "AddPrefix");
			}
			if (_state == State.Started)
			{
				AddPrefixCore(text);
			}
			_uriPrefixes[uriPrefix] = text;
			_defaultServiceNames.Add(uriPrefix);
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "AddPrefix");
			}
			throw;
		}
		static string CreateRegisteredPrefix(string uriPrefix, int j, int i)
		{
			int num2 = uriPrefix.Length;
			if (uriPrefix[j] != ':')
			{
				num2 += ((i == 7) ? ":80".Length : ":443".Length);
			}
			return string.Create(num2, (uriPrefix, j, i), delegate(Span<char> destination, (string uriPrefix, int j, int i) state)
			{
				if (state.uriPrefix[state.j] == ':')
				{
					state.uriPrefix.CopyTo(destination);
				}
				else
				{
					int item = state.j;
					state.uriPrefix.AsSpan(0, item).CopyTo(destination);
					if (state.i == 7)
					{
						":80".CopyTo(destination.Slice(item));
						item += 3;
					}
					else
					{
						":443".CopyTo(destination.Slice(item));
						item += 4;
					}
					state.uriPrefix.AsSpan(state.j).CopyTo(destination.Slice(item));
				}
				int num3 = destination.IndexOf(':');
				if (num3 < 0)
				{
					num3 = destination.Length;
				}
				int charsWritten;
				OperationStatus operationStatus = Ascii.ToLowerInPlace(destination.Slice(0, num3), out charsWritten);
			});
		}
	}

	internal bool ContainsPrefix(string uriPrefix)
	{
		return _uriPrefixes.Contains(uriPrefix);
	}

	internal bool RemovePrefix(string uriPrefix)
	{
		try
		{
			CheckDisposed();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"uriPrefix: {uriPrefix}", "RemovePrefix");
			}
			ArgumentNullException.ThrowIfNull(uriPrefix, "uriPrefix");
			if (!_uriPrefixes.Contains(uriPrefix))
			{
				return false;
			}
			if (_state == State.Started)
			{
				RemovePrefixCore((string)_uriPrefixes[uriPrefix]);
			}
			_uriPrefixes.Remove(uriPrefix);
			_defaultServiceNames.Remove(uriPrefix);
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "RemovePrefix");
			}
			throw;
		}
		return true;
	}

	internal void RemoveAll(bool clear)
	{
		CheckDisposed();
		if (_uriPrefixes.Count <= 0)
		{
			return;
		}
		if (_state == State.Started)
		{
			foreach (string value in _uriPrefixes.Values)
			{
				RemovePrefixCore(value);
			}
		}
		if (clear)
		{
			_uriPrefixes.Clear();
			_defaultServiceNames.Clear();
		}
	}

	public Task<HttpListenerContext> GetContextAsync()
	{
		return Task.Factory.FromAsync((AsyncCallback callback, object state) => ((HttpListener)state).BeginGetContext(callback, state), (IAsyncResult iar) => ((HttpListener)iar.AsyncState).EndGetContext(iar), this);
	}

	public void Close()
	{
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info("HttpListenerRequest::Close()", null, "Close");
			}
			((IDisposable)this).Dispose();
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"Close {ex}", "Close");
			}
			throw;
		}
	}

	internal void CheckDisposed()
	{
		ObjectDisposedException.ThrowIf(_state == State.Closed, this);
	}

	void IDisposable.Dispose()
	{
		Dispose();
	}

	private void ValidateV2Property()
	{
		lock (_internalLock)
		{
			CheckDisposed();
			SetupV2Config();
		}
	}

	private unsafe void SetUrlGroupProperty(global::Interop.HttpApi.HTTP_SERVER_PROPERTY property, void* info, uint infosize)
	{
		uint num = global::Interop.HttpApi.HttpSetUrlGroupProperty(_urlGroupId, property, info, infosize);
		if (num != 0)
		{
			HttpListenerException ex = new HttpListenerException((int)num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"HttpSetUrlGroupProperty:: Property: {property} {ex}", "SetUrlGroupProperty");
			}
			throw ex;
		}
	}

	internal unsafe void SetServerTimeout(int[] timeouts, uint minSendBytesPerSecond)
	{
		ValidateV2Property();
		global::Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO hTTP_TIMEOUT_LIMIT_INFO = default(global::Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO);
		hTTP_TIMEOUT_LIMIT_INFO.Flags = global::Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
		hTTP_TIMEOUT_LIMIT_INFO.DrainEntityBody = (ushort)timeouts[1];
		hTTP_TIMEOUT_LIMIT_INFO.EntityBody = (ushort)timeouts[0];
		hTTP_TIMEOUT_LIMIT_INFO.RequestQueue = (ushort)timeouts[2];
		hTTP_TIMEOUT_LIMIT_INFO.IdleConnection = (ushort)timeouts[3];
		hTTP_TIMEOUT_LIMIT_INFO.HeaderWait = (ushort)timeouts[4];
		hTTP_TIMEOUT_LIMIT_INFO.MinSendRate = minSendBytesPerSecond;
		SetUrlGroupProperty(global::Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty, &hTTP_TIMEOUT_LIMIT_INFO, (uint)sizeof(global::Interop.HttpApi.HTTP_TIMEOUT_LIMIT_INFO));
	}

	private unsafe void SetupV2Config()
	{
		ulong num = 0uL;
		if (_V2Initialized)
		{
			return;
		}
		try
		{
			uint num2 = global::Interop.HttpApi.HttpCreateServerSession(global::Interop.HttpApi.s_version, &num, 0u);
			if (num2 != 0)
			{
				throw new HttpListenerException((int)num2);
			}
			_serverSessionHandle = new HttpServerSessionHandle(num);
			num = 0uL;
			num2 = global::Interop.HttpApi.HttpCreateUrlGroup(_serverSessionHandle.DangerousGetServerSessionId(), &num, 0u);
			if (num2 != 0)
			{
				throw new HttpListenerException((int)num2);
			}
			_urlGroupId = num;
			_V2Initialized = true;
		}
		catch (Exception ex)
		{
			_state = State.Closed;
			_serverSessionHandle?.Dispose();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"SetupV2Config {ex}", "SetupV2Config");
			}
			throw;
		}
	}

	public void Start()
	{
		lock (_internalLock)
		{
			try
			{
				CheckDisposed();
				if (_state != State.Started)
				{
					SetupV2Config();
					CreateRequestQueueHandle();
					AttachRequestQueueToUrlGroup();
					try
					{
						AddAllPrefixes();
					}
					catch (HttpListenerException)
					{
						DetachRequestQueueFromUrlGroup();
						throw;
					}
					_state = State.Started;
				}
			}
			catch (Exception ex2)
			{
				_state = State.Closed;
				CloseRequestQueueHandle();
				CleanupV2Config();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"Start {ex2}", "Start");
				}
				throw;
			}
		}
	}

	private void CleanupV2Config()
	{
		if (_V2Initialized)
		{
			uint num = global::Interop.HttpApi.HttpCloseUrlGroup(_urlGroupId);
			if (num != 0 && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"CloseV2Config {System.SR.Format(System.SR.net_listener_close_urlgroup_error, num)}", "CleanupV2Config");
			}
			_urlGroupId = 0uL;
			_serverSessionHandle.Dispose();
		}
	}

	private unsafe void AttachRequestQueueToUrlGroup()
	{
		global::Interop.HttpApi.HTTP_BINDING_INFO hTTP_BINDING_INFO = default(global::Interop.HttpApi.HTTP_BINDING_INFO);
		hTTP_BINDING_INFO.Flags = global::Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
		hTTP_BINDING_INFO.RequestQueueHandle = _currentSession.RequestQueueHandle.DangerousGetHandle();
		SetUrlGroupProperty(global::Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty, &hTTP_BINDING_INFO, (uint)sizeof(global::Interop.HttpApi.HTTP_BINDING_INFO));
	}

	private unsafe void DetachRequestQueueFromUrlGroup()
	{
		global::Interop.HttpApi.HTTP_BINDING_INFO hTTP_BINDING_INFO = default(global::Interop.HttpApi.HTTP_BINDING_INFO);
		hTTP_BINDING_INFO.Flags = global::Interop.HttpApi.HTTP_FLAGS.NONE;
		hTTP_BINDING_INFO.RequestQueueHandle = IntPtr.Zero;
		uint num = global::Interop.HttpApi.HttpSetUrlGroupProperty(_urlGroupId, global::Interop.HttpApi.HTTP_SERVER_PROPERTY.HttpServerBindingProperty, &hTTP_BINDING_INFO, (uint)sizeof(global::Interop.HttpApi.HTTP_BINDING_INFO));
		if (num != 0 && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, $"DetachRequestQueueFromUrlGroup {System.SR.Format(System.SR.net_listener_detach_error, num)}", "DetachRequestQueueFromUrlGroup");
		}
	}

	public void Stop()
	{
		try
		{
			lock (_internalLock)
			{
				CheckDisposed();
				if (_state != 0)
				{
					RemoveAll(clear: false);
					DetachRequestQueueFromUrlGroup();
					CloseRequestQueueHandle();
					_state = State.Stopped;
				}
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"Stop {ex}", "Stop");
			}
			throw;
		}
	}

	private void CreateRequestQueueHandle()
	{
		_currentSession = new HttpListenerSession(this);
	}

	private void CloseRequestQueueHandle()
	{
		_currentSession?.CloseRequestQueueHandle();
		_currentSession = null;
	}

	public void Abort()
	{
		lock (_internalLock)
		{
			try
			{
				if (_state != State.Closed)
				{
					if (_state == State.Started)
					{
						DetachRequestQueueFromUrlGroup();
						CloseRequestQueueHandle();
					}
					CleanupV2Config();
				}
			}
			catch (Exception ex)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"Abort {ex}", "Abort");
				}
				throw;
			}
			finally
			{
				_state = State.Closed;
			}
		}
	}

	private void Dispose()
	{
		lock (_internalLock)
		{
			try
			{
				if (_state != State.Closed)
				{
					Stop();
					CleanupV2Config();
				}
			}
			catch (Exception ex)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"Dispose {ex}", "Dispose");
				}
				throw;
			}
			finally
			{
				_state = State.Closed;
			}
		}
	}

	private void RemovePrefixCore(string uriPrefix)
	{
		global::Interop.HttpApi.HttpRemoveUrlFromUrlGroup(_urlGroupId, uriPrefix, 0u);
	}

	private void AddAllPrefixes()
	{
		if (_uriPrefixes.Count <= 0)
		{
			return;
		}
		foreach (string value in _uriPrefixes.Values)
		{
			AddPrefixCore(value);
		}
	}

	private void AddPrefixCore(string registeredPrefix)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpAddUrl[ToUrlGroup]", "AddPrefixCore");
		}
		uint num = global::Interop.HttpApi.HttpAddUrlToUrlGroup(_urlGroupId, registeredPrefix, 0uL, 0u);
		switch (num)
		{
		case 183u:
			throw new HttpListenerException((int)num, System.SR.Format(System.SR.net_listener_already, registeredPrefix));
		default:
			throw new HttpListenerException((int)num);
		case 0u:
			break;
		}
	}

	public unsafe HttpListenerContext GetContext()
	{
		SyncRequestContext syncRequestContext = null;
		HttpListenerContext httpListenerContext = null;
		bool stoleBlob = false;
		try
		{
			CheckDisposed();
			if (_state == State.Stopped)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "Start()"));
			}
			if (_uriPrefixes.Count == 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "AddPrefix()"));
			}
			uint num = 0u;
			uint num2 = 4096u;
			ulong num3 = 0uL;
			syncRequestContext = new SyncRequestContext((int)num2);
			HttpListenerSession currentSession = _currentSession;
			if (currentSession == null)
			{
				throw new HttpListenerException(87);
			}
			while (true)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"Calling Interop.HttpApi.HttpReceiveHttpRequest RequestId: {num3}", "GetContext");
				}
				uint num4 = 0u;
				num = global::Interop.HttpApi.HttpReceiveHttpRequest(currentSession.RequestQueueHandle, num3, 1u, syncRequestContext.RequestBlob, num2, &num4, null);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + num, "GetContext");
				}
				if (num == 87 && num3 != 0L)
				{
					num3 = 0uL;
					continue;
				}
				switch (num)
				{
				case 234u:
					num2 = num4;
					num3 = syncRequestContext.RequestBlob->RequestId;
					syncRequestContext.Reset(checked((int)num2));
					break;
				default:
					throw new HttpListenerException((int)num);
				case 0u:
					if (ValidateRequest(currentSession, syncRequestContext))
					{
						httpListenerContext = HandleAuthentication(currentSession, syncRequestContext, out stoleBlob);
					}
					if (stoleBlob)
					{
						syncRequestContext = null;
						stoleBlob = false;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, ":HandleAuthentication() returned httpContext" + httpListenerContext, "GetContext");
					}
					if (httpListenerContext != null)
					{
						return httpListenerContext;
					}
					if (syncRequestContext == null)
					{
						syncRequestContext = new SyncRequestContext(checked((int)num2));
					}
					num3 = 0uL;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"{ex}", "GetContext");
			}
			throw;
		}
		finally
		{
			if (syncRequestContext != null && !stoleBlob)
			{
				syncRequestContext.ReleasePins();
				syncRequestContext.Close();
			}
		}
	}

	internal unsafe static bool ValidateRequest(HttpListenerSession session, RequestContextBase requestMemory)
	{
		if (requestMemory.RequestBlob->Headers.UnknownHeaderCount > 1000)
		{
			SendError(session, requestMemory.RequestBlob->RequestId, HttpStatusCode.BadRequest, null);
			return false;
		}
		return true;
	}

	public IAsyncResult BeginGetContext(AsyncCallback? callback, object? state)
	{
		ListenerAsyncResult listenerAsyncResult = null;
		try
		{
			CheckDisposed();
			if (_state == State.Stopped)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "Start()"));
			}
			HttpListenerSession currentSession = _currentSession;
			if (currentSession == null)
			{
				throw new HttpListenerException(87);
			}
			listenerAsyncResult = new ListenerAsyncResult(currentSession, state, callback);
			uint num = listenerAsyncResult.QueueBeginGetContext();
			if (num != 0 && num != 997)
			{
				throw new HttpListenerException((int)num);
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"BeginGetContext {ex}", "BeginGetContext");
			}
			throw;
		}
		return listenerAsyncResult;
	}

	public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
	{
		HttpListenerContext httpListenerContext = null;
		try
		{
			CheckDisposed();
			ArgumentNullException.ThrowIfNull(asyncResult, "asyncResult");
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"asyncResult: {asyncResult}", "EndGetContext");
			}
			if (!(asyncResult is ListenerAsyncResult { AsyncObject: HttpListenerSession asyncObject } listenerAsyncResult) || asyncObject.Listener != this)
			{
				throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
			}
			if (listenerAsyncResult.EndCalled)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetContext"));
			}
			listenerAsyncResult.EndCalled = true;
			httpListenerContext = listenerAsyncResult.InternalWaitForCompletion() as HttpListenerContext;
			if (httpListenerContext == null)
			{
				ExceptionDispatchInfo.Throw(listenerAsyncResult.Result as Exception);
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"EndGetContext {ex}", "EndGetContext");
			}
			throw;
		}
		return httpListenerContext;
	}

	internal unsafe HttpListenerContext HandleAuthentication(HttpListenerSession session, RequestContextBase memoryBlob, out bool stoleBlob)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"HandleAuthentication() memoryBlob:0x{(nint)memoryBlob.RequestBlob:x}", "HandleAuthentication");
		}
		string text = null;
		stoleBlob = false;
		string knownHeader = global::Interop.HttpApi.GetKnownHeader(memoryBlob.RequestBlob, 24);
		ulong connectionId = memoryBlob.RequestBlob->ConnectionId;
		ulong requestId = memoryBlob.RequestBlob->RequestId;
		bool isSecureConnection = memoryBlob.RequestBlob->pSslInfo != null;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"HandleAuthentication() authorizationHeader: ({knownHeader})", "HandleAuthentication");
		}
		DisconnectResults.TryGetValue(connectionId, out DisconnectAsyncResult value);
		if (UnsafeConnectionNtlmAuthentication)
		{
			if (knownHeader == null)
			{
				WindowsPrincipal windowsPrincipal = value?.AuthenticatedConnection;
				if (windowsPrincipal != null)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"Principal: {windowsPrincipal} principal.Identity.Name: {windowsPrincipal.Identity.Name} creating request", "HandleAuthentication");
					}
					stoleBlob = true;
					HttpListenerContext httpListenerContext = new HttpListenerContext(session, memoryBlob);
					httpListenerContext.SetIdentity(windowsPrincipal, null);
					httpListenerContext.Request.ReleasePins();
					return httpListenerContext;
				}
			}
			else
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Clearing principal cache", "HandleAuthentication");
				}
				if (value != null)
				{
					value.AuthenticatedConnection = null;
				}
			}
		}
		stoleBlob = true;
		HttpListenerContext httpContext = null;
		NegotiateAuthentication negotiateAuthentication = null;
		bool flag = false;
		string text2 = null;
		AuthenticationSchemes authenticationSchemes = AuthenticationSchemes.None;
		AuthenticationSchemes authenticationSchemes2 = AuthenticationSchemes;
		ExtendedProtectionPolicy policy = _extendedProtectionPolicy;
		try
		{
			if (value != null && !value.StartOwningDisconnectHandling())
			{
				value = null;
			}
			if (value != null)
			{
				negotiateAuthentication = value.Session;
				text2 = value.SessionPackage;
			}
			httpContext = new HttpListenerContext(session, memoryBlob);
			AuthenticationSchemeSelector authenticationDelegate = _authenticationDelegate;
			if (authenticationDelegate != null)
			{
				try
				{
					httpContext.Request.ReleasePins();
					authenticationSchemes2 = (httpContext.AuthenticationSchemes = authenticationDelegate(httpContext.Request));
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"AuthenticationScheme: {authenticationSchemes2}", "HandleAuthentication");
					}
				}
				catch (Exception ex) when (!System.Net.ExceptionCheck.IsFatal(ex))
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_delegate_exception, ex), "HandleAuthentication");
						System.Net.NetEventSource.Info(this, $"authenticationScheme: {authenticationSchemes2}", "HandleAuthentication");
					}
					SendError(session, requestId, HttpStatusCode.InternalServerError, null);
					FreeContext(ref httpContext, memoryBlob);
					return null;
				}
			}
			else
			{
				stoleBlob = false;
			}
			ExtendedProtectionSelector extendedProtectionSelectorDelegate = _extendedProtectionSelectorDelegate;
			if (extendedProtectionSelectorDelegate != null)
			{
				ExtendedProtectionPolicy obj = extendedProtectionSelectorDelegate(httpContext.Request) ?? new ExtendedProtectionPolicy(PolicyEnforcement.Never);
				policy = (httpContext.ExtendedProtectionPolicy = obj);
			}
			int num = -1;
			if (knownHeader != null && ((uint)authenticationSchemes2 & 0xFFFF7FFFu) != 0)
			{
				num = knownHeader.AsSpan().IndexOfAny(" \t\r\n");
				if (num < 0)
				{
					num = knownHeader.Length;
				}
				if (num < knownHeader.Length)
				{
					if ((authenticationSchemes2 & AuthenticationSchemes.Negotiate) != 0 && string.Compare(knownHeader, 0, "Negotiate", 0, num, StringComparison.OrdinalIgnoreCase) == 0)
					{
						authenticationSchemes = AuthenticationSchemes.Negotiate;
					}
					else if ((authenticationSchemes2 & AuthenticationSchemes.Ntlm) != 0 && string.Compare(knownHeader, 0, "NTLM", 0, num, StringComparison.OrdinalIgnoreCase) == 0)
					{
						authenticationSchemes = AuthenticationSchemes.Ntlm;
					}
					else if ((authenticationSchemes2 & AuthenticationSchemes.Basic) != 0 && string.Compare(knownHeader, 0, "Basic", 0, num, StringComparison.OrdinalIgnoreCase) == 0)
					{
						authenticationSchemes = AuthenticationSchemes.Basic;
					}
					else if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_unsupported_authentication_scheme, knownHeader, authenticationSchemes2), "HandleAuthentication");
					}
				}
			}
			HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
			bool flag2 = false;
			if (authenticationSchemes == AuthenticationSchemes.None)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_unmatched_authentication_scheme, authenticationSchemes2.ToString(), knownHeader ?? "<null>"), "HandleAuthentication");
				}
				if ((authenticationSchemes2 & AuthenticationSchemes.Anonymous) != 0)
				{
					if (!stoleBlob)
					{
						stoleBlob = true;
						httpContext.Request.ReleasePins();
					}
					return httpContext;
				}
				httpStatusCode = HttpStatusCode.Unauthorized;
				FreeContext(ref httpContext, memoryBlob);
			}
			else
			{
				byte[] array = null;
				byte[] array2 = null;
				string text3 = null;
				int num2 = knownHeader.AsSpan(num + 1).IndexOfAnyExcept(" \t\r\n");
				num = ((num2 >= 0) ? (num + 1 + num2) : knownHeader.Length);
				string s = ((num < knownHeader.Length) ? knownHeader.Substring(num) : "");
				IPrincipal principal = null;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"Performing Authentication headerScheme: {authenticationSchemes}", "HandleAuthentication");
				}
				switch (authenticationSchemes)
				{
				case AuthenticationSchemes.Negotiate:
				case AuthenticationSchemes.Ntlm:
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"context: {negotiateAuthentication} for connectionId: {connectionId}", "HandleAuthentication");
					}
					string text5 = ((authenticationSchemes == AuthenticationSchemes.Ntlm) ? "NTLM" : "Negotiate");
					if (negotiateAuthentication == null || negotiateAuthentication.IsAuthenticated || text2 != text5)
					{
						negotiateAuthentication?.Dispose();
						ChannelBinding channelBinding = GetChannelBinding(session, connectionId, isSecureConnection, policy);
						NegotiateAuthenticationServerOptions serverOptions = new NegotiateAuthenticationServerOptions
						{
							Package = text5,
							Binding = channelBinding,
							Policy = GetAuthenticationExtendedProtectionPolicy(policy)
						};
						negotiateAuthentication = new NegotiateAuthentication(serverOptions);
						text2 = text5;
					}
					try
					{
						array = Convert.FromBase64String(s);
					}
					catch (FormatException)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"FormatException from FormBase64String", "HandleAuthentication");
						}
						httpStatusCode = HttpStatusCode.BadRequest;
						flag2 = true;
					}
					if (!flag2)
					{
						array2 = negotiateAuthentication.GetOutgoingBlob(array, out var statusCode);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"GetOutgoingBlob returned IsAuthenticated: {negotiateAuthentication.IsAuthenticated} and statusCodeNew: {statusCode}", "HandleAuthentication");
						}
						flag2 = statusCode >= NegotiateAuthenticationStatusCode.GenericFailure;
						if (flag2)
						{
							httpStatusCode = HttpStatusFromSecurityStatus(statusCode);
						}
					}
					if (array2 != null)
					{
						text3 = ((authenticationSchemes == AuthenticationSchemes.Ntlm) ? "NTLM" : "Negotiate") + " " + Convert.ToBase64String(array2);
					}
					if (flag2)
					{
						break;
					}
					if (negotiateAuthentication.IsAuthenticated)
					{
						httpContext.Request.ServiceName = negotiateAuthentication.TargetName;
						try
						{
							IIdentity remoteIdentity = negotiateAuthentication.RemoteIdentity;
							if (!(remoteIdentity is WindowsIdentity windowsIdentity))
							{
								if (System.Net.NetEventSource.Log.IsEnabled())
								{
									System.Net.NetEventSource.Info(this, $"HandleAuthentication RemoteIdentity return non-Windows identity: {remoteIdentity.GetType().Name}", "HandleAuthentication");
								}
								httpStatusCode = HttpStatusCode.InternalServerError;
								break;
							}
							WindowsPrincipal windowsPrincipal2 = new WindowsPrincipal((WindowsIdentity)windowsIdentity.Clone());
							principal = windowsPrincipal2;
							if (!UnsafeConnectionNtlmAuthentication || !(negotiateAuthentication.Package == "NTLM"))
							{
								break;
							}
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"HandleAuthentication inserting principal: {principal} for connectionId: {connectionId}", "HandleAuthentication");
							}
							if (value == null)
							{
								RegisterForDisconnectNotification(session, connectionId, ref value);
							}
							if (value != null)
							{
								lock (((ICollection)DisconnectResults).SyncRoot)
								{
									if (UnsafeConnectionNtlmAuthentication)
									{
										value.AuthenticatedConnection = windowsPrincipal2;
										flag = true;
									}
								}
							}
							else if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"HandleAuthentication RegisterForDisconnectNotification failed.", "HandleAuthentication");
							}
						}
						catch (Exception ex4)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"HandleAuthentication RemoteIdentity failed with exception: {ex4.Message}", "HandleAuthentication");
							}
							httpStatusCode = HttpStatusCode.InternalServerError;
						}
					}
					else
					{
						text = ((!string.IsNullOrEmpty(text3)) ? text3 : ((authenticationSchemes == AuthenticationSchemes.Ntlm) ? "NTLM" : "Negotiate"));
						flag = true;
					}
					break;
				}
				case AuthenticationSchemes.Basic:
					try
					{
						array = Convert.FromBase64String(s);
						s = WebHeaderEncoding.GetString(array, 0, array.Length);
						num = s.IndexOf(':');
						if (num != -1)
						{
							string text4 = s.Substring(0, num);
							string password = s.Substring(num + 1);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"Basic Identity found, userName: {text4}", "HandleAuthentication");
							}
							principal = new GenericPrincipal(new HttpListenerBasicIdentity(text4, password), null);
						}
						else
						{
							httpStatusCode = HttpStatusCode.BadRequest;
						}
					}
					catch (FormatException)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"FromBase64String threw a FormatException.", "HandleAuthentication");
						}
					}
					break;
				}
				if (principal != null)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"Got principal: {principal}, IdentityName: {principal.Identity.Name} for creating request.", "HandleAuthentication");
					}
					httpContext.SetIdentity(principal, text3);
				}
				else
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Handshake has failed.", "HandleAuthentication");
					}
					FreeContext(ref httpContext, memoryBlob);
				}
			}
			ArrayList challenges = null;
			if (httpContext == null)
			{
				if (text != null)
				{
					AddChallenge(ref challenges, text);
				}
				else
				{
					negotiateAuthentication?.Dispose();
					negotiateAuthentication = null;
					if (httpStatusCode != HttpStatusCode.Unauthorized)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "ConnectionId:" + connectionId + " because of error:" + httpStatusCode, "HandleAuthentication");
						}
						SendError(session, requestId, httpStatusCode, null);
						return null;
					}
					challenges = BuildChallenge(authenticationSchemes2);
				}
			}
			if (flag)
			{
				if (value == null)
				{
					RegisterForDisconnectNotification(session, connectionId, ref value);
					if (value == null)
					{
						negotiateAuthentication?.Dispose();
						negotiateAuthentication = null;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "connectionId:" + connectionId + " because of failed HttpWaitForDisconnect", "HandleAuthentication");
						}
						SendError(session, requestId, HttpStatusCode.InternalServerError, null);
						FreeContext(ref httpContext, memoryBlob);
						return null;
					}
				}
				value.Session = negotiateAuthentication;
				value.SessionPackage = text2;
				negotiateAuthentication = null;
			}
			if (httpContext == null)
			{
				SendError(session, requestId, (challenges != null && challenges.Count > 0) ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, challenges);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Scheme:" + authenticationSchemes2, "HandleAuthentication");
				}
				return null;
			}
			if (!stoleBlob)
			{
				stoleBlob = true;
				httpContext.Request.ReleasePins();
			}
			return httpContext;
		}
		catch
		{
			FreeContext(ref httpContext, memoryBlob);
			negotiateAuthentication?.Dispose();
			negotiateAuthentication = null;
			throw;
		}
		finally
		{
			try
			{
				if (negotiateAuthentication != null)
				{
					if (value != null)
					{
						value.Session = null;
						value.SessionPackage = null;
					}
					negotiateAuthentication?.Dispose();
				}
			}
			finally
			{
				value?.FinishOwningDisconnectHandling();
			}
		}
	}

	private static void FreeContext(ref HttpListenerContext httpContext, RequestContextBase memoryBlob)
	{
		if (httpContext != null)
		{
			httpContext.Request.DetachBlob(memoryBlob);
			httpContext.Close();
			httpContext = null;
		}
	}

	internal void SetAuthenticationHeaders(HttpListenerContext context)
	{
		HttpListenerResponse response = context.Response;
		ArrayList arrayList = BuildChallenge(context.AuthenticationSchemes);
		if (arrayList == null)
		{
			return;
		}
		foreach (string item in arrayList)
		{
			response.Headers.Add("WWW-Authenticate", item);
		}
	}

	private ChannelBinding GetChannelBinding(HttpListenerSession session, ulong connectionId, bool isSecureConnection, ExtendedProtectionPolicy policy)
	{
		if (policy.PolicyEnforcement == PolicyEnforcement.Never)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_cbt_disabled, "GetChannelBinding");
			}
			return null;
		}
		if (!isSecureConnection)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_cbt_http, "GetChannelBinding");
			}
			return null;
		}
		if (policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_cbt_trustedproxy, "GetChannelBinding");
			}
			return null;
		}
		ChannelBinding channelBindingFromTls = GetChannelBindingFromTls(session, connectionId);
		if (System.Net.NetEventSource.Log.IsEnabled() && channelBindingFromTls != null)
		{
			System.Net.NetEventSource.Info(this, "GetChannelBindingFromTls returned null even though OS supposedly supports Extended Protection", "GetChannelBinding");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, System.SR.net_log_listener_cbt, "GetChannelBinding");
		}
		return channelBindingFromTls;
	}

	private ExtendedProtectionPolicy GetAuthenticationExtendedProtectionPolicy(ExtendedProtectionPolicy policy)
	{
		if (policy.PolicyEnforcement == PolicyEnforcement.Never || policy.CustomServiceNames != null)
		{
			return policy;
		}
		if (_defaultServiceNames.ServiceNames.Count == 0)
		{
			throw new InvalidOperationException(System.SR.net_listener_no_spns);
		}
		return new ExtendedProtectionPolicy(policy.PolicyEnforcement, policy.ProtectionScenario, _defaultServiceNames.ServiceNames.Merge("HTTP/localhost"));
	}

	private static HttpStatusCode HttpStatusFromSecurityStatus(NegotiateAuthenticationStatusCode statusErrorCode)
	{
		if (IsCredentialFailure(statusErrorCode))
		{
			return HttpStatusCode.Unauthorized;
		}
		if (IsClientFault(statusErrorCode))
		{
			return HttpStatusCode.BadRequest;
		}
		return HttpStatusCode.InternalServerError;
	}

	internal static bool IsCredentialFailure(NegotiateAuthenticationStatusCode error)
	{
		if (error != NegotiateAuthenticationStatusCode.UnknownCredentials && error != NegotiateAuthenticationStatusCode.CredentialsExpired && error != NegotiateAuthenticationStatusCode.BadBinding && error != NegotiateAuthenticationStatusCode.TargetUnknown)
		{
			return error == NegotiateAuthenticationStatusCode.ImpersonationValidationFailed;
		}
		return true;
	}

	internal static bool IsClientFault(NegotiateAuthenticationStatusCode error)
	{
		if (error != NegotiateAuthenticationStatusCode.InvalidToken && error != NegotiateAuthenticationStatusCode.QopNotSupported && error != NegotiateAuthenticationStatusCode.UnknownCredentials && error != NegotiateAuthenticationStatusCode.MessageAltered && error != NegotiateAuthenticationStatusCode.OutOfSequence)
		{
			return error == NegotiateAuthenticationStatusCode.InvalidCredentials;
		}
		return true;
	}

	private static void AddChallenge(ref ArrayList challenges, string challenge)
	{
		if (challenge == null)
		{
			return;
		}
		challenge = challenge.Trim();
		if (challenge.Length > 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, "challenge:" + challenge, "AddChallenge");
			}
			if (challenges == null)
			{
				challenges = new ArrayList(4);
			}
			challenges.Add(challenge);
		}
	}

	private ArrayList BuildChallenge(AuthenticationSchemes authenticationScheme)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "AuthenticationScheme:" + authenticationScheme, "BuildChallenge");
		}
		ArrayList challenges = null;
		if ((authenticationScheme & AuthenticationSchemes.Negotiate) != 0)
		{
			AddChallenge(ref challenges, "Negotiate");
		}
		if ((authenticationScheme & AuthenticationSchemes.Ntlm) != 0)
		{
			AddChallenge(ref challenges, "NTLM");
		}
		if ((authenticationScheme & AuthenticationSchemes.Basic) != 0)
		{
			AddChallenge(ref challenges, "Basic realm=\"" + Realm + "\"");
		}
		return challenges;
	}

	private unsafe static void RegisterForDisconnectNotification(HttpListenerSession session, ulong connectionId, ref DisconnectAsyncResult disconnectResult)
	{
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(session.Listener, "Calling Interop.HttpApi.HttpWaitForDisconnect", "RegisterForDisconnectNotification");
			}
			DisconnectAsyncResult disconnectAsyncResult = new DisconnectAsyncResult(session, connectionId);
			uint num = global::Interop.HttpApi.HttpWaitForDisconnect(session.RequestQueueHandle, connectionId, disconnectAsyncResult.NativeOverlapped);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpWaitForDisconnect returned:" + num, "RegisterForDisconnectNotification");
			}
			if (num == 0 || num == 997)
			{
				disconnectResult = disconnectAsyncResult;
				session.Listener.DisconnectResults[connectionId] = disconnectResult;
			}
			if (num == 0 && SkipIOCPCallbackOnSuccess)
			{
				disconnectAsyncResult.IOCompleted(disconnectAsyncResult.NativeOverlapped);
			}
		}
		catch (Win32Exception ex)
		{
			uint nativeErrorCode = (uint)ex.NativeErrorCode;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpWaitForDisconnect threw, statusCode:" + nativeErrorCode, "RegisterForDisconnectNotification");
			}
		}
	}

	private unsafe static void SendError(HttpListenerSession session, ulong requestId, HttpStatusCode httpStatusCode, ArrayList challenges)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(session.Listener, $"RequestId: {requestId}", "SendError");
		}
		global::Interop.HttpApi.HTTP_RESPONSE hTTP_RESPONSE = default(global::Interop.HttpApi.HTTP_RESPONSE);
		hTTP_RESPONSE.Version = default(global::Interop.HttpApi.HTTP_VERSION);
		hTTP_RESPONSE.Version.MajorVersion = 1;
		hTTP_RESPONSE.Version.MinorVersion = 1;
		hTTP_RESPONSE.StatusCode = (ushort)httpStatusCode;
		string s = HttpStatusDescription.Get(httpStatusCode);
		uint num = 0u;
		byte[] bytes = Encoding.Default.GetBytes(s);
		uint num2;
		fixed (byte* pReason = bytes)
		{
			hTTP_RESPONSE.pReason = (sbyte*)pReason;
			hTTP_RESPONSE.ReasonLength = (ushort)bytes.Length;
			ReadOnlySpan<byte> readOnlySpan = "0"u8;
			fixed (byte* pRawValue = readOnlySpan)
			{
				(&hTTP_RESPONSE.Headers.KnownHeaders)[11].pRawValue = (sbyte*)pRawValue;
				(&hTTP_RESPONSE.Headers.KnownHeaders)[11].RawValueLength = (ushort)readOnlySpan.Length;
				hTTP_RESPONSE.Headers.UnknownHeaderCount = checked((ushort)(challenges?.Count ?? 0));
				GCHandle[] array = null;
				global::Interop.HttpApi.HTTP_UNKNOWN_HEADER[] array2 = null;
				GCHandle gCHandle = default(GCHandle);
				GCHandle gCHandle2 = default(GCHandle);
				if (hTTP_RESPONSE.Headers.UnknownHeaderCount > 0)
				{
					array = new GCHandle[hTTP_RESPONSE.Headers.UnknownHeaderCount];
					array2 = new global::Interop.HttpApi.HTTP_UNKNOWN_HEADER[hTTP_RESPONSE.Headers.UnknownHeaderCount];
				}
				try
				{
					if (hTTP_RESPONSE.Headers.UnknownHeaderCount > 0)
					{
						gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
						hTTP_RESPONSE.Headers.pUnknownHeaders = (global::Interop.HttpApi.HTTP_UNKNOWN_HEADER*)Marshal.UnsafeAddrOfPinnedArrayElement(array2, 0);
						gCHandle2 = GCHandle.Alloc(s_wwwAuthenticateBytes, GCHandleType.Pinned);
						sbyte* pName = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(s_wwwAuthenticateBytes, 0);
						for (int i = 0; i < array.Length; i++)
						{
							byte[] bytes2 = Encoding.Default.GetBytes((string)challenges[i]);
							array[i] = GCHandle.Alloc(bytes2, GCHandleType.Pinned);
							array2[i].pName = pName;
							array2[i].NameLength = (ushort)s_wwwAuthenticateBytes.Length;
							array2[i].pRawValue = (sbyte*)Marshal.UnsafeAddrOfPinnedArrayElement(bytes2, 0);
							array2[i].RawValueLength = checked((ushort)bytes2.Length);
						}
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(session.Listener, "Calling Interop.HttpApi.HttpSendHtthttpResponse", "SendError");
					}
					num2 = global::Interop.HttpApi.HttpSendHttpResponse(session.RequestQueueHandle, requestId, 0u, &hTTP_RESPONSE, null, &num, null, 0u, null, null);
				}
				finally
				{
					if (gCHandle.IsAllocated)
					{
						gCHandle.Free();
					}
					if (gCHandle2.IsAllocated)
					{
						gCHandle2.Free();
					}
					if (array != null)
					{
						for (int j = 0; j < array.Length; j++)
						{
							if (array[j].IsAllocated)
							{
								array[j].Free();
							}
						}
					}
				}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(session.Listener, "Call to Interop.HttpApi.HttpSendHttpResponse returned:" + num2, "SendError");
		}
		if (num2 != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(session.Listener, "SendUnauthorized returned:" + num2, "SendError");
			}
			HttpListenerContext.CancelRequest(session.RequestQueueHandle, requestId);
		}
	}

	private unsafe static int GetTokenOffsetFromBlob(nint blob)
	{
		nint channelToken = ((global::Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS*)blob)->ChannelToken;
		return (int)((channelToken - blob) / 1);
	}

	private unsafe static int GetTokenSizeFromBlob(nint blob)
	{
		return (int)((global::Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS*)blob)->ChannelTokenSize;
	}

	internal unsafe static ChannelBinding GetChannelBindingFromTls(HttpListenerSession session, ulong connectionId)
	{
		int num = sizeof(global::Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS) + 128;
		byte[] array = null;
		global::Interop.HttpApi.SafeLocalFreeChannelBinding safeLocalFreeChannelBinding = null;
		uint num2 = 0u;
		uint num3;
		do
		{
			array = new byte[num];
			fixed (byte* ptr = &array[0])
			{
				num3 = global::Interop.HttpApi.HttpReceiveClientCertificate(session.RequestQueueHandle, connectionId, 1u, ptr, (uint)num, &num2, null);
				switch (num3)
				{
				case 0u:
				{
					int tokenOffsetFromBlob = GetTokenOffsetFromBlob((nint)ptr);
					int tokenSizeFromBlob = GetTokenSizeFromBlob((nint)ptr);
					safeLocalFreeChannelBinding = global::Interop.HttpApi.SafeLocalFreeChannelBinding.LocalAlloc(tokenSizeFromBlob);
					if (safeLocalFreeChannelBinding.IsInvalid)
					{
						safeLocalFreeChannelBinding.Dispose();
						throw new OutOfMemoryException();
					}
					Marshal.Copy(array, tokenOffsetFromBlob, safeLocalFreeChannelBinding.DangerousGetHandle(), tokenSizeFromBlob);
					break;
				}
				case 234u:
				{
					int tokenSizeFromBlob2 = GetTokenSizeFromBlob((nint)ptr);
					num = sizeof(global::Interop.HttpApi.HTTP_REQUEST_CHANNEL_BIND_STATUS) + tokenSizeFromBlob2;
					break;
				}
				case 87u:
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(session.Listener, System.SR.net_ssp_dont_support_cbt, "GetChannelBindingFromTls");
					}
					return null;
				default:
					throw new HttpListenerException((int)num3);
				}
			}
		}
		while (num3 != 0);
		return safeLocalFreeChannelBinding;
	}
}
