using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

public class WindowsIdentity : ClaimsIdentity, IDisposable, ISerializable, IDeserializationCallback
{
	private static SecurityIdentifier s_authenticatedUserRid;

	private static SecurityIdentifier s_domainRid;

	private static SecurityIdentifier s_localSystemRid;

	private static SecurityIdentifier s_anonymousRid;

	private string _name;

	private SecurityIdentifier _owner;

	private SecurityIdentifier _user;

	private IdentityReferenceCollection _groups;

	private SafeAccessTokenHandle _safeTokenHandle;

	private readonly string _authType;

	private int _isAuthenticated = -1;

	private volatile TokenImpersonationLevel _impersonationLevel;

	private volatile bool _impersonationLevelInitialized;

	public new const string DefaultIssuer = "AD AUTHORITY";

	private readonly string _issuerName = "AD AUTHORITY";

	private object _claimsIntiailizedLock;

	private bool _claimsInitialized;

	private List<Claim> _deviceClaims;

	private List<Claim> _userClaims;

	private static bool s_ignoreWindows8Properties;

	private static readonly AsyncLocal<SafeAccessTokenHandle> s_currentImpersonatedToken = new AsyncLocal<SafeAccessTokenHandle>(CurrentImpersonatedTokenChanged);

	public unsafe sealed override string? AuthenticationType
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return string.Empty;
			}
			if (_authType == null)
			{
				global::Interop.LUID LogonId = GetLogonAuthId(_safeTokenHandle);
				if (LogonId.LowPart == 998)
				{
					return string.Empty;
				}
				SafeLsaReturnBufferHandle ppLogonSessionData = null;
				try
				{
					int num = global::Interop.SspiCli.LsaGetLogonSessionData(ref LogonId, out ppLogonSessionData);
					if (num < 0)
					{
						throw GetExceptionFromNtStatus(num);
					}
					ppLogonSessionData.Initialize((uint)sizeof(global::Interop.SECURITY_LOGON_SESSION_DATA));
					return Marshal.PtrToStringUni(ppLogonSessionData.Read<global::Interop.SECURITY_LOGON_SESSION_DATA>(0uL).AuthenticationPackage.Buffer);
				}
				finally
				{
					ppLogonSessionData?.Dispose();
				}
			}
			return _authType;
		}
	}

	public TokenImpersonationLevel ImpersonationLevel
	{
		get
		{
			if (!_impersonationLevelInitialized)
			{
				TokenImpersonationLevel impersonationLevel;
				if (_safeTokenHandle.IsInvalid)
				{
					impersonationLevel = TokenImpersonationLevel.Anonymous;
				}
				else
				{
					TokenType tokenInformation = (TokenType)GetTokenInformation<int>(TokenInformationClass.TokenType);
					if (tokenInformation == TokenType.TokenPrimary)
					{
						impersonationLevel = TokenImpersonationLevel.None;
					}
					else
					{
						int tokenInformation2 = GetTokenInformation<int>(TokenInformationClass.TokenImpersonationLevel);
						impersonationLevel = (TokenImpersonationLevel)(tokenInformation2 + 1);
					}
				}
				_impersonationLevel = impersonationLevel;
				_impersonationLevelInitialized = true;
			}
			return _impersonationLevel;
		}
	}

	public override bool IsAuthenticated
	{
		get
		{
			if (_isAuthenticated == -1)
			{
				if ((object)s_authenticatedUserRid == null)
				{
					s_authenticatedUserRid = new SecurityIdentifier(IdentifierAuthority.NTAuthority, RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/));
				}
				_isAuthenticated = (CheckNtTokenForSid(s_authenticatedUserRid) ? 1 : 0);
			}
			return _isAuthenticated == 1;
		}
	}

	public virtual bool IsGuest
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return false;
			}
			if ((object)s_domainRid == null)
			{
				s_domainRid = new SecurityIdentifier(IdentifierAuthority.NTAuthority, RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/));
			}
			return CheckNtTokenForSid(s_domainRid);
		}
	}

	public virtual bool IsSystem
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return false;
			}
			if ((object)s_localSystemRid == null)
			{
				s_localSystemRid = new SecurityIdentifier(IdentifierAuthority.NTAuthority, RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/));
			}
			return User == s_localSystemRid;
		}
	}

	public virtual bool IsAnonymous
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return true;
			}
			if ((object)s_anonymousRid == null)
			{
				s_anonymousRid = new SecurityIdentifier(IdentifierAuthority.NTAuthority, RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/));
			}
			return User == s_anonymousRid;
		}
	}

	public override string Name => GetName();

	public SecurityIdentifier? Owner
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return null;
			}
			if (_owner == null)
			{
				using SafeLocalAllocHandle safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenOwner);
				_owner = new SecurityIdentifier(((SafeBuffer)safeLocalAllocHandle).Read<nint>(0uL));
			}
			return _owner;
		}
	}

	public SecurityIdentifier? User
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return null;
			}
			if (_user == null)
			{
				using SafeLocalAllocHandle safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenUser);
				_user = new SecurityIdentifier(((SafeBuffer)safeLocalAllocHandle).Read<nint>(0uL));
			}
			return _user;
		}
	}

	public unsafe IdentityReferenceCollection? Groups
	{
		get
		{
			if (_safeTokenHandle.IsInvalid)
			{
				return null;
			}
			if (_groups == null)
			{
				IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection();
				using (SafeLocalAllocHandle safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenGroups))
				{
					global::Interop.SID_AND_ATTRIBUTES[] array = new global::Interop.SID_AND_ATTRIBUTES[safeLocalAllocHandle.Read<global::Interop.TOKEN_GROUPS>(0uL).GroupCount];
					safeLocalAllocHandle.ReadArray((uint)sizeof(nint), array, 0, array.Length);
					global::Interop.SID_AND_ATTRIBUTES[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						global::Interop.SID_AND_ATTRIBUTES sID_AND_ATTRIBUTES = array2[i];
						uint num = 3221225492u;
						if ((sID_AND_ATTRIBUTES.Attributes & num) == 4)
						{
							identityReferenceCollection.Add(new SecurityIdentifier(sID_AND_ATTRIBUTES.Sid));
						}
					}
				}
				Interlocked.CompareExchange(ref _groups, identityReferenceCollection, null);
			}
			return _groups;
		}
	}

	public SafeAccessTokenHandle AccessToken => _safeTokenHandle;

	public virtual nint Token => _safeTokenHandle.DangerousGetHandle();

	public virtual IEnumerable<Claim> UserClaims
	{
		get
		{
			InitializeClaims();
			return _userClaims.ToArray();
		}
	}

	public virtual IEnumerable<Claim> DeviceClaims
	{
		get
		{
			InitializeClaims();
			return _deviceClaims.ToArray();
		}
	}

	public override IEnumerable<Claim> Claims
	{
		get
		{
			if (!_claimsInitialized)
			{
				InitializeClaims();
			}
			foreach (Claim claim in base.Claims)
			{
				yield return claim;
			}
			foreach (Claim userClaim in _userClaims)
			{
				yield return userClaim;
			}
			foreach (Claim deviceClaim in _deviceClaims)
			{
				yield return deviceClaim;
			}
		}
	}

	public WindowsIdentity(nint userToken)
		: this(userToken, null, -1)
	{
	}

	public WindowsIdentity(nint userToken, string type)
		: this(userToken, type, -1)
	{
	}

	public WindowsIdentity(nint userToken, string type, WindowsAccountType acctType)
		: this(userToken, type, -1)
	{
	}

	public WindowsIdentity(nint userToken, string type, WindowsAccountType acctType, bool isAuthenticated)
		: this(userToken, type, isAuthenticated ? 1 : 0)
	{
	}

	protected WindowsIdentity(WindowsIdentity identity)
		: base(identity, null, GetAuthType(identity), null, null)
	{
		bool success = false;
		try
		{
			if (!identity._safeTokenHandle.IsInvalid && identity._safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
			{
				identity._safeTokenHandle.DangerousAddRef(ref success);
				if (!identity._safeTokenHandle.IsInvalid && identity._safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
				{
					CreateFromToken(identity._safeTokenHandle.DangerousGetHandle());
				}
				_authType = identity._authType;
				_isAuthenticated = identity._isAuthenticated;
			}
			if (_safeTokenHandle == null)
			{
				_safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;
			}
		}
		finally
		{
			if (success)
			{
				identity._safeTokenHandle.DangerousRelease();
			}
		}
	}

	private WindowsIdentity(nint userToken, string authType, int isAuthenticated)
		: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
	{
		CreateFromToken(userToken);
		_authType = authType;
		_isAuthenticated = isAuthenticated;
	}

	private WindowsIdentity()
		: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
	{
		_safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;
	}

	private WindowsIdentity(SafeAccessTokenHandle safeTokenHandle)
		: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
	{
		_safeTokenHandle = safeTokenHandle;
	}

	public unsafe WindowsIdentity(string sUserPrincipalName)
		: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
	{
		using SafeLsaHandle lsaHandle = ConnectToLsa();
		int authenticationPackage = LookupAuthenticationPackage(lsaHandle, "Kerberos");
		ReadOnlySpan<byte> readOnlySpan = new byte[4] { 67, 76, 82, 0 };
		Unsafe.SkipInit(out global::Interop.SspiCli.TOKEN_SOURCE SourceContext);
		if (!global::Interop.Advapi32.AllocateLocallyUniqueId(&SourceContext.SourceIdentifier))
		{
			throw new SecurityException(Marshal.GetLastPInvokeErrorMessage());
		}
		readOnlySpan.CopyTo(new Span<byte>(SourceContext.SourceName, 8));
		ArgumentNullException.ThrowIfNull(sUserPrincipalName, "sUserPrincipalName");
		byte[] array = Encoding.Unicode.GetBytes(sUserPrincipalName);
		if (array.Length > 65535)
		{
			Array.Resize(ref array, array.Length & 0xFFFF);
		}
		int num = checked(sizeof(global::Interop.SspiCli.KERB_S4U_LOGON) + array.Length);
		using SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.LocalAlloc(num);
		global::Interop.SspiCli.KERB_S4U_LOGON* ptr = (global::Interop.SspiCli.KERB_S4U_LOGON*)safeLocalAllocHandle.DangerousGetHandle();
		ptr->MessageType = global::Interop.SspiCli.KERB_LOGON_SUBMIT_TYPE.KerbS4ULogon;
		ptr->Flags = global::Interop.SspiCli.KerbS4uLogonFlags.None;
		ptr->ClientUpn.Length = (ptr->ClientUpn.MaximumLength = checked((ushort)array.Length));
		nint buffer = (nint)(ptr + 1);
		ptr->ClientUpn.Buffer = buffer;
		Marshal.Copy(array, 0, ptr->ClientUpn.Buffer, array.Length);
		ptr->ClientRealm.Length = (ptr->ClientRealm.MaximumLength = 0);
		ptr->ClientRealm.Buffer = IntPtr.Zero;
		ushort num2 = checked((ushort)readOnlySpan.Length);
		using SafeLocalAllocHandle safeLocalAllocHandle2 = SafeLocalAllocHandle.LocalAlloc(num2);
		readOnlySpan.CopyTo(new Span<byte>((void*)safeLocalAllocHandle2.DangerousGetHandle(), readOnlySpan.Length));
		global::Interop.Advapi32.LSA_STRING OriginName = new global::Interop.Advapi32.LSA_STRING(safeLocalAllocHandle2.DangerousGetHandle(), num2);
		SafeLsaReturnBufferHandle ProfileBuffer;
		int ProfileBufferLength;
		global::Interop.LUID LogonId;
		SafeAccessTokenHandle Token;
		global::Interop.SspiCli.QUOTA_LIMITS Quotas;
		int SubStatus;
		int num3 = global::Interop.SspiCli.LsaLogonUser(lsaHandle, in OriginName, global::Interop.SspiCli.SECURITY_LOGON_TYPE.Network, authenticationPackage, safeLocalAllocHandle.DangerousGetHandle(), num, IntPtr.Zero, in SourceContext, out ProfileBuffer, out ProfileBufferLength, out LogonId, out Token, out Quotas, out SubStatus);
		ProfileBuffer.Dispose();
		if (num3 == -1073741714 && SubStatus < 0)
		{
			num3 = SubStatus;
		}
		if (num3 < 0)
		{
			Token.Dispose();
			throw GetExceptionFromNtStatus(num3);
		}
		if (SubStatus < 0)
		{
			Token.Dispose();
			throw GetExceptionFromNtStatus(SubStatus);
		}
		_safeTokenHandle = Token;
	}

	private static SafeLsaHandle ConnectToLsa()
	{
		SafeLsaHandle LsaHandle;
		int num = global::Interop.SspiCli.LsaConnectUntrusted(out LsaHandle);
		if (num < 0)
		{
			throw GetExceptionFromNtStatus(num);
		}
		return LsaHandle;
	}

	private unsafe static int LookupAuthenticationPackage(SafeLsaHandle lsaHandle, string packageName)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(packageName);
		int AuthenticationPackage;
		fixed (byte* pBuffer = &bytes[0])
		{
			global::Interop.Advapi32.LSA_STRING PackageName = new global::Interop.Advapi32.LSA_STRING((nint)pBuffer, checked((ushort)bytes.Length));
			int num = global::Interop.SspiCli.LsaLookupAuthenticationPackage(lsaHandle, ref PackageName, out AuthenticationPackage);
			if (num < 0)
			{
				throw GetExceptionFromNtStatus(num);
			}
		}
		return AuthenticationPackage;
	}

	private static SafeAccessTokenHandle DuplicateAccessToken(nint accessToken)
	{
		if (accessToken == IntPtr.Zero)
		{
			throw new ArgumentException(System.SR.Argument_TokenZero);
		}
		if (!global::Interop.Advapi32.GetTokenInformation(accessToken, 8u, IntPtr.Zero, 0u, out var _) && Marshal.GetLastPInvokeError() == 6)
		{
			throw new ArgumentException(System.SR.Argument_InvalidImpersonationToken);
		}
		nint currentProcess = global::Interop.Kernel32.GetCurrentProcess();
		if (!global::Interop.Kernel32.DuplicateHandle(currentProcess, accessToken, currentProcess, out var lpTargetHandle, 0u, bInheritHandle: true, 2u))
		{
			throw new SecurityException(Marshal.GetLastPInvokeErrorMessage());
		}
		return lpTargetHandle;
	}

	private static SafeAccessTokenHandle DuplicateAccessToken(SafeAccessTokenHandle accessToken)
	{
		if (accessToken.IsInvalid)
		{
			return accessToken;
		}
		bool success = false;
		try
		{
			accessToken.DangerousAddRef(ref success);
			return DuplicateAccessToken(accessToken.DangerousGetHandle());
		}
		finally
		{
			if (success)
			{
				accessToken.DangerousRelease();
			}
		}
	}

	[MemberNotNull("_safeTokenHandle")]
	private void CreateFromToken(nint userToken)
	{
		_safeTokenHandle = DuplicateAccessToken(userToken);
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public WindowsIdentity(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}

	public static WindowsIdentity GetCurrent()
	{
		return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, threadOnly: false);
	}

	public static WindowsIdentity? GetCurrent(bool ifImpersonating)
	{
		return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, ifImpersonating);
	}

	public static WindowsIdentity GetCurrent(TokenAccessLevels desiredAccess)
	{
		return GetCurrentInternal(desiredAccess, threadOnly: false);
	}

	public static WindowsIdentity GetAnonymous()
	{
		return new WindowsIdentity();
	}

	private bool CheckNtTokenForSid(SecurityIdentifier sid)
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return false;
		}
		using SafeAccessTokenHandle safeAccessTokenHandle = SafeAccessTokenHandle.InvalidHandle;
		SafeAccessTokenHandle phNewToken = safeAccessTokenHandle;
		TokenImpersonationLevel impersonationLevel = ImpersonationLevel;
		bool IsMember = false;
		try
		{
			if (impersonationLevel == TokenImpersonationLevel.None && !global::Interop.Advapi32.DuplicateTokenEx(_safeTokenHandle, 8u, IntPtr.Zero, 2u, 2u, ref phNewToken))
			{
				throw new SecurityException(Marshal.GetLastPInvokeErrorMessage());
			}
			if (!global::Interop.Advapi32.CheckTokenMembership((impersonationLevel != 0) ? _safeTokenHandle : phNewToken, sid.BinaryForm, ref IsMember))
			{
				throw new SecurityException(Marshal.GetLastPInvokeErrorMessage());
			}
		}
		finally
		{
			phNewToken.Dispose();
		}
		return IsMember;
	}

	internal string GetName()
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return string.Empty;
		}
		if (_name == null)
		{
			using SafeAccessTokenHandle safeAccessTokenHandle = SafeAccessTokenHandle.InvalidHandle;
			RunImpersonated(safeAccessTokenHandle, delegate
			{
				NTAccount nTAccount = User.Translate(typeof(NTAccount)) as NTAccount;
				_name = nTAccount.ToString();
			});
		}
		return _name;
	}

	public static void RunImpersonated(SafeAccessTokenHandle safeAccessTokenHandle, Action action)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		RunImpersonatedInternal(safeAccessTokenHandle, action);
	}

	public static T RunImpersonated<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<T> func)
	{
		Func<T> func = func;
		ArgumentNullException.ThrowIfNull(func, "func");
		T result = default(T);
		RunImpersonatedInternal(safeAccessTokenHandle, delegate
		{
			result = func();
		});
		return result;
	}

	public static Task RunImpersonatedAsync(SafeAccessTokenHandle safeAccessTokenHandle, Func<Task> func)
	{
		return RunImpersonated(safeAccessTokenHandle, func);
	}

	public static Task<T> RunImpersonatedAsync<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<Task<T>> func)
	{
		return RunImpersonated(safeAccessTokenHandle, func);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && _safeTokenHandle != null && !_safeTokenHandle.IsClosed)
		{
			_safeTokenHandle.Dispose();
		}
		_name = null;
		_owner = null;
		_user = null;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	private static void RunImpersonatedInternal(SafeAccessTokenHandle token, Action action)
	{
		token = DuplicateAccessToken(token);
		bool isImpersonating;
		int hr;
		SafeAccessTokenHandle currentToken = GetCurrentToken(TokenAccessLevels.MaximumAllowed, threadOnly: false, out isImpersonating, out hr);
		if (currentToken == null || currentToken.IsInvalid)
		{
			currentToken?.Dispose();
			throw new SecurityException(Marshal.GetPInvokeErrorMessage(hr));
		}
		if (isImpersonating)
		{
			s_currentImpersonatedToken.Value = currentToken;
		}
		else
		{
			currentToken.Dispose();
		}
		ExecutionContext executionContext = ExecutionContext.Capture();
		ExecutionContext.Run(executionContext, delegate
		{
			if (!global::Interop.Advapi32.RevertToSelf())
			{
				Environment.FailFast(Marshal.GetLastPInvokeErrorMessage());
			}
			s_currentImpersonatedToken.Value = null;
			if (!token.IsInvalid && !global::Interop.Advapi32.ImpersonateLoggedOnUser(token))
			{
				throw new SecurityException(System.SR.Argument_ImpersonateUser);
			}
			s_currentImpersonatedToken.Value = token;
			action();
		}, null);
	}

	private static void CurrentImpersonatedTokenChanged(AsyncLocalValueChangedArgs<SafeAccessTokenHandle> args)
	{
		if (args.ThreadContextChanged)
		{
			if (!global::Interop.Advapi32.RevertToSelf())
			{
				Environment.FailFast(Marshal.GetLastPInvokeErrorMessage());
			}
			if (args.CurrentValue != null && !args.CurrentValue.IsInvalid && !global::Interop.Advapi32.ImpersonateLoggedOnUser(args.CurrentValue))
			{
				Environment.FailFast(Marshal.GetLastPInvokeErrorMessage());
			}
		}
	}

	internal static WindowsIdentity GetCurrentInternal(TokenAccessLevels desiredAccess, bool threadOnly)
	{
		bool isImpersonating;
		int hr;
		SafeAccessTokenHandle currentToken = GetCurrentToken(desiredAccess, threadOnly, out isImpersonating, out hr);
		if (currentToken == null || currentToken.IsInvalid)
		{
			currentToken?.Dispose();
			if (threadOnly && !isImpersonating)
			{
				return null;
			}
			throw new SecurityException(Marshal.GetPInvokeErrorMessage(hr));
		}
		return new WindowsIdentity(currentToken);
	}

	private static int GetHRForWin32Error(int dwLastError)
	{
		if ((dwLastError & 0x80000000u) == 2147483648u)
		{
			return dwLastError;
		}
		return (dwLastError & 0xFFFF) | -2147024896;
	}

	private static Exception GetExceptionFromNtStatus(int status)
	{
		switch (status)
		{
		case -1073741790:
			return new UnauthorizedAccessException();
		case -1073741801:
		case -1073741670:
			return new OutOfMemoryException();
		default:
		{
			uint error = global::Interop.Advapi32.LsaNtStatusToWinError((uint)status);
			return new SecurityException(Marshal.GetPInvokeErrorMessage((int)error));
		}
		}
	}

	private static SafeAccessTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess, bool threadOnly, out bool isImpersonating, out int hr)
	{
		isImpersonating = true;
		hr = 0;
		if (!global::Interop.Advapi32.OpenThreadToken(desiredAccess, WinSecurityContext.Both, out var tokenHandle))
		{
			hr = Marshal.GetHRForLastWin32Error();
			if (hr == GetHRForWin32Error(1008))
			{
				isImpersonating = false;
				if (!threadOnly)
				{
					tokenHandle.Dispose();
					return GetCurrentProcessToken(desiredAccess, out hr);
				}
			}
		}
		return tokenHandle;
	}

	private static SafeAccessTokenHandle GetCurrentProcessToken(TokenAccessLevels desiredAccess, out int hr)
	{
		hr = 0;
		if (!global::Interop.Advapi32.OpenProcessToken(global::Interop.Kernel32.GetCurrentProcess(), desiredAccess, out var TokenHandle))
		{
			hr = GetHRForWin32Error(Marshal.GetLastPInvokeError());
		}
		return TokenHandle;
	}

	private T GetTokenInformation<T>(TokenInformationClass tokenInformationClass) where T : unmanaged
	{
		using SafeLocalAllocHandle safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, tokenInformationClass);
		return safeLocalAllocHandle.Read<T>(0uL);
	}

	private static global::Interop.LUID GetLogonAuthId(SafeAccessTokenHandle safeTokenHandle)
	{
		using SafeLocalAllocHandle safeLocalAllocHandle = GetTokenInformation(safeTokenHandle, TokenInformationClass.TokenStatistics);
		return safeLocalAllocHandle.Read<global::Interop.TOKEN_STATISTICS>(0uL).AuthenticationId;
	}

	private static SafeLocalAllocHandle GetTokenInformation(SafeAccessTokenHandle tokenHandle, TokenInformationClass tokenInformationClass, bool nullOnInvalidParam = false)
	{
		SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
		try
		{
			global::Interop.Advapi32.GetTokenInformation(tokenHandle, (uint)tokenInformationClass, safeLocalAllocHandle, 0u, out var ReturnLength);
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			switch (lastPInvokeError)
			{
			case 24:
			case 122:
			{
				safeLocalAllocHandle.Dispose();
				safeLocalAllocHandle = SafeLocalAllocHandle.LocalAlloc(checked((int)ReturnLength));
				if (!global::Interop.Advapi32.GetTokenInformation(tokenHandle, (uint)tokenInformationClass, safeLocalAllocHandle, ReturnLength, out var _))
				{
					throw new SecurityException(Marshal.GetLastPInvokeErrorMessage());
				}
				return safeLocalAllocHandle;
			}
			case 6:
				throw new ArgumentException(System.SR.Argument_InvalidImpersonationToken);
			case 87:
				if (nullOnInvalidParam)
				{
					safeLocalAllocHandle.Dispose();
					return null;
				}
				break;
			}
			throw new SecurityException(Marshal.GetPInvokeErrorMessage(lastPInvokeError));
		}
		catch
		{
			safeLocalAllocHandle.Dispose();
			throw;
		}
	}

	private static string GetAuthType(WindowsIdentity identity)
	{
		ArgumentNullException.ThrowIfNull(identity, "identity");
		return identity._authType;
	}

	public override ClaimsIdentity Clone()
	{
		return new WindowsIdentity(this);
	}

	private void InitializeClaims()
	{
		bool target = false;
		LazyInitializer.EnsureInitialized(ref target, ref _claimsInitialized, ref _claimsIntiailizedLock, delegate
		{
			_userClaims = new List<Claim>();
			_deviceClaims = new List<Claim>();
			if (!string.IsNullOrEmpty(Name))
			{
				_userClaims.Add(new Claim(base.NameClaimType, Name, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this));
			}
			AddPrimarySidClaim(_userClaims);
			AddGroupSidClaims(_userClaims);
			if (!s_ignoreWindows8Properties)
			{
				AddDeviceGroupSidClaims(_deviceClaims, TokenInformationClass.TokenDeviceGroups);
				if (!s_ignoreWindows8Properties)
				{
					AddTokenClaims(_userClaims, TokenInformationClass.TokenUserClaimAttributes, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim");
					AddTokenClaims(_deviceClaims, TokenInformationClass.TokenDeviceClaimAttributes, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim");
				}
			}
			return true;
		});
	}

	private unsafe void AddGroupSidClaims(List<Claim> instanceClaims)
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return;
		}
		SafeLocalAllocHandle safeLocalAllocHandle = null;
		SafeLocalAllocHandle safeLocalAllocHandle2 = null;
		try
		{
			safeLocalAllocHandle2 = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenPrimaryGroup);
			global::Interop.TOKEN_PRIMARY_GROUP tOKEN_PRIMARY_GROUP = *(global::Interop.TOKEN_PRIMARY_GROUP*)safeLocalAllocHandle2.DangerousGetHandle();
			SecurityIdentifier securityIdentifier = new SecurityIdentifier(tOKEN_PRIMARY_GROUP.PrimaryGroup);
			bool flag = false;
			safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenGroups);
			int num = *(int*)safeLocalAllocHandle.DangerousGetHandle();
			global::Interop.SID_AND_ATTRIBUTES* ptr = (global::Interop.SID_AND_ATTRIBUTES*)(safeLocalAllocHandle.DangerousGetHandle() + sizeof(nint));
			for (int i = 0; i < num; i++)
			{
				global::Interop.SID_AND_ATTRIBUTES sID_AND_ATTRIBUTES = ptr[i];
				uint num2 = 3221225492u;
				SecurityIdentifier securityIdentifier2 = new SecurityIdentifier(sID_AND_ATTRIBUTES.Sid);
				if ((sID_AND_ATTRIBUTES.Attributes & num2) == 4)
				{
					Claim claim;
					if (!flag && StringComparer.Ordinal.Equals(securityIdentifier2.Value, securityIdentifier.Value))
					{
						claim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
						claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier2.IdentifierAuthority.ToString());
						instanceClaims.Add(claim);
						flag = true;
					}
					claim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
					claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier2.IdentifierAuthority.ToString());
					instanceClaims.Add(claim);
				}
				else if ((sID_AND_ATTRIBUTES.Attributes & num2) == 16)
				{
					Claim claim;
					if (!flag && StringComparer.Ordinal.Equals(securityIdentifier2.Value, securityIdentifier.Value))
					{
						claim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
						claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier2.IdentifierAuthority.ToString());
						instanceClaims.Add(claim);
						flag = true;
					}
					claim = new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
					claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier2.IdentifierAuthority.ToString());
					instanceClaims.Add(claim);
				}
			}
		}
		finally
		{
			safeLocalAllocHandle?.Dispose();
			safeLocalAllocHandle2?.Dispose();
		}
	}

	private unsafe void AddPrimarySidClaim(List<Claim> instanceClaims)
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return;
		}
		SafeLocalAllocHandle safeLocalAllocHandle = null;
		try
		{
			safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenUser);
			global::Interop.SID_AND_ATTRIBUTES sID_AND_ATTRIBUTES = *(global::Interop.SID_AND_ATTRIBUTES*)safeLocalAllocHandle.DangerousGetHandle();
			uint num = 16u;
			SecurityIdentifier securityIdentifier = new SecurityIdentifier(sID_AND_ATTRIBUTES.Sid);
			if (sID_AND_ATTRIBUTES.Attributes == 0)
			{
				Claim claim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid", securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
				claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier.IdentifierAuthority.ToString());
				instanceClaims.Add(claim);
			}
			else if ((sID_AND_ATTRIBUTES.Attributes & num) == 16)
			{
				Claim claim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid", securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
				claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier.IdentifierAuthority.ToString());
				instanceClaims.Add(claim);
			}
		}
		finally
		{
			safeLocalAllocHandle?.Dispose();
		}
	}

	private unsafe void AddDeviceGroupSidClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass)
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return;
		}
		SafeLocalAllocHandle safeLocalAllocHandle = null;
		try
		{
			safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, tokenInformationClass, nullOnInvalidParam: true);
			if (safeLocalAllocHandle == null)
			{
				s_ignoreWindows8Properties = true;
				return;
			}
			int num = *(int*)safeLocalAllocHandle.DangerousGetHandle();
			global::Interop.SID_AND_ATTRIBUTES* ptr = (global::Interop.SID_AND_ATTRIBUTES*)(safeLocalAllocHandle.DangerousGetHandle() + sizeof(nint));
			for (int i = 0; i < num; i++)
			{
				global::Interop.SID_AND_ATTRIBUTES sID_AND_ATTRIBUTES = ptr[i];
				uint num2 = 3221225492u;
				SecurityIdentifier securityIdentifier = new SecurityIdentifier(sID_AND_ATTRIBUTES.Sid);
				if ((sID_AND_ATTRIBUTES.Attributes & num2) == 4)
				{
					string text = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup";
					Claim claim = new Claim(text, securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
					claim.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier.IdentifierAuthority.ToString());
					claim.Properties.Add(text, "");
					instanceClaims.Add(claim);
				}
				else if ((sID_AND_ATTRIBUTES.Attributes & num2) == 16)
				{
					string text = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup";
					Claim claim2 = new Claim(text, securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
					claim2.Properties.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", securityIdentifier.IdentifierAuthority.ToString());
					claim2.Properties.Add(text, "");
					instanceClaims.Add(claim2);
				}
			}
		}
		finally
		{
			safeLocalAllocHandle?.Dispose();
		}
	}

	private unsafe void AddTokenClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass, string propertyValue)
	{
		if (_safeTokenHandle.IsInvalid)
		{
			return;
		}
		SafeLocalAllocHandle safeLocalAllocHandle = null;
		try
		{
			safeLocalAllocHandle = GetTokenInformation(_safeTokenHandle, tokenInformationClass);
			global::Interop.CLAIM_SECURITY_ATTRIBUTES_INFORMATION cLAIM_SECURITY_ATTRIBUTES_INFORMATION = *(global::Interop.CLAIM_SECURITY_ATTRIBUTES_INFORMATION*)safeLocalAllocHandle.DangerousGetHandle();
			for (int i = 0; i < cLAIM_SECURITY_ATTRIBUTES_INFORMATION.AttributeCount; i++)
			{
				global::Interop.CLAIM_SECURITY_ATTRIBUTE_V1 cLAIM_SECURITY_ATTRIBUTE_V = *(global::Interop.CLAIM_SECURITY_ATTRIBUTE_V1*)(cLAIM_SECURITY_ATTRIBUTES_INFORMATION.Attribute.pAttributeV1 + (nint)i * (nint)sizeof(global::Interop.CLAIM_SECURITY_ATTRIBUTE_V1));
				string type = Marshal.PtrToStringUni(cLAIM_SECURITY_ATTRIBUTE_V.Name);
				switch (cLAIM_SECURITY_ATTRIBUTE_V.ValueType)
				{
				case global::Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING:
				{
					nint[] array4 = new nint[cLAIM_SECURITY_ATTRIBUTE_V.ValueCount];
					Marshal.Copy(cLAIM_SECURITY_ATTRIBUTE_V.Values.ppString, array4, 0, (int)cLAIM_SECURITY_ATTRIBUTE_V.ValueCount);
					for (int m = 0; m < cLAIM_SECURITY_ATTRIBUTE_V.ValueCount; m++)
					{
						Claim claim4 = new Claim(type, Marshal.PtrToStringUni(array4[m]), "http://www.w3.org/2001/XMLSchema#string", _issuerName, _issuerName, this);
						claim4.Properties.Add(propertyValue, string.Empty);
						instanceClaims.Add(claim4);
					}
					break;
				}
				case global::Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64:
				{
					long[] array2 = new long[cLAIM_SECURITY_ATTRIBUTE_V.ValueCount];
					Marshal.Copy(cLAIM_SECURITY_ATTRIBUTE_V.Values.pInt64, array2, 0, (int)cLAIM_SECURITY_ATTRIBUTE_V.ValueCount);
					for (int k = 0; k < cLAIM_SECURITY_ATTRIBUTE_V.ValueCount; k++)
					{
						Claim claim2 = new Claim(type, array2[k].ToString(CultureInfo.InvariantCulture), "http://www.w3.org/2001/XMLSchema#integer64", _issuerName, _issuerName, this);
						claim2.Properties.Add(propertyValue, string.Empty);
						instanceClaims.Add(claim2);
					}
					break;
				}
				case global::Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64:
				{
					long[] array3 = new long[cLAIM_SECURITY_ATTRIBUTE_V.ValueCount];
					Marshal.Copy(cLAIM_SECURITY_ATTRIBUTE_V.Values.pUint64, array3, 0, (int)cLAIM_SECURITY_ATTRIBUTE_V.ValueCount);
					for (int l = 0; l < cLAIM_SECURITY_ATTRIBUTE_V.ValueCount; l++)
					{
						ulong num = (ulong)array3[l];
						Claim claim3 = new Claim(type, num.ToString(CultureInfo.InvariantCulture), "http://www.w3.org/2001/XMLSchema#uinteger64", _issuerName, _issuerName, this);
						claim3.Properties.Add(propertyValue, string.Empty);
						instanceClaims.Add(claim3);
					}
					break;
				}
				case global::Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN:
				{
					long[] array = new long[cLAIM_SECURITY_ATTRIBUTE_V.ValueCount];
					Marshal.Copy(cLAIM_SECURITY_ATTRIBUTE_V.Values.pUint64, array, 0, (int)cLAIM_SECURITY_ATTRIBUTE_V.ValueCount);
					for (int j = 0; j < cLAIM_SECURITY_ATTRIBUTE_V.ValueCount; j++)
					{
						Claim claim = new Claim(type, (array[j] != 0).ToString(), "http://www.w3.org/2001/XMLSchema#boolean", _issuerName, _issuerName, this);
						claim.Properties.Add(propertyValue, string.Empty);
						instanceClaims.Add(claim);
					}
					break;
				}
				}
			}
		}
		finally
		{
			safeLocalAllocHandle?.Dispose();
		}
	}
}
