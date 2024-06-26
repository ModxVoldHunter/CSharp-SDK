using System.Buffers;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security;

public sealed class NegotiateAuthentication : IDisposable
{
	private readonly NegotiateAuthenticationPal _pal;

	private readonly string _requestedPackage;

	private readonly bool _isServer;

	private readonly TokenImpersonationLevel _requiredImpersonationLevel;

	private readonly ProtectionLevel _requiredProtectionLevel;

	private readonly ExtendedProtectionPolicy _extendedProtectionPolicy;

	private readonly bool _isSecureConnection;

	private bool _isDisposed;

	private IIdentity _remoteIdentity;

	public bool IsAuthenticated
	{
		get
		{
			if (!_isDisposed)
			{
				return _pal.IsAuthenticated;
			}
			return false;
		}
	}

	public ProtectionLevel ProtectionLevel
	{
		get
		{
			if (IsSigned)
			{
				if (IsEncrypted)
				{
					return ProtectionLevel.EncryptAndSign;
				}
				return ProtectionLevel.Sign;
			}
			return ProtectionLevel.None;
		}
	}

	public bool IsSigned
	{
		get
		{
			if (!_isDisposed)
			{
				return _pal.IsSigned;
			}
			return false;
		}
	}

	public bool IsEncrypted
	{
		get
		{
			if (!_isDisposed)
			{
				return _pal.IsEncrypted;
			}
			return false;
		}
	}

	public bool IsMutuallyAuthenticated
	{
		get
		{
			if (!_isDisposed)
			{
				return _pal.IsMutuallyAuthenticated;
			}
			return false;
		}
	}

	public bool IsServer => _isServer;

	public string Package => _pal.Package ?? _requestedPackage;

	public string? TargetName => _pal.TargetName;

	public IIdentity RemoteIdentity
	{
		get
		{
			IIdentity identity = _remoteIdentity;
			if (identity == null)
			{
				if (!IsAuthenticated || _isDisposed)
				{
					throw new InvalidOperationException(System.SR.net_auth_noauth);
				}
				if (!IsServer)
				{
					return new GenericIdentity(TargetName ?? string.Empty, Package);
				}
				identity = (_remoteIdentity = _pal.RemoteIdentity);
			}
			return identity;
		}
	}

	public TokenImpersonationLevel ImpersonationLevel => _pal.ImpersonationLevel;

	public NegotiateAuthentication(NegotiateAuthenticationClientOptions clientOptions)
	{
		ArgumentNullException.ThrowIfNull(clientOptions, "clientOptions");
		_isServer = false;
		_requestedPackage = clientOptions.Package;
		_requiredImpersonationLevel = TokenImpersonationLevel.None;
		_requiredProtectionLevel = clientOptions.RequiredProtectionLevel;
		_pal = NegotiateAuthenticationPal.Create(clientOptions);
	}

	public NegotiateAuthentication(NegotiateAuthenticationServerOptions serverOptions)
	{
		ArgumentNullException.ThrowIfNull(serverOptions, "serverOptions");
		_isServer = true;
		_requestedPackage = serverOptions.Package;
		_requiredImpersonationLevel = serverOptions.RequiredImpersonationLevel;
		_requiredProtectionLevel = serverOptions.RequiredProtectionLevel;
		_extendedProtectionPolicy = serverOptions.Policy;
		_isSecureConnection = serverOptions.Binding != null;
		_pal = NegotiateAuthenticationPal.Create(serverOptions);
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			_pal?.Dispose();
			if (_remoteIdentity is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}

	public byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
	{
		if (_isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		byte[] outgoingBlob = _pal.GetOutgoingBlob(incomingBlob, out statusCode);
		if (statusCode == NegotiateAuthenticationStatusCode.Completed)
		{
			if (IsServer && _extendedProtectionPolicy != null && !CheckSpn())
			{
				statusCode = NegotiateAuthenticationStatusCode.TargetUnknown;
			}
			else if (_requiredImpersonationLevel != 0 && ImpersonationLevel < _requiredImpersonationLevel)
			{
				statusCode = NegotiateAuthenticationStatusCode.ImpersonationValidationFailed;
			}
			else if (_requiredProtectionLevel != 0 && ProtectionLevel < _requiredProtectionLevel)
			{
				statusCode = NegotiateAuthenticationStatusCode.SecurityQosFailed;
			}
		}
		return outgoingBlob;
	}

	public string? GetOutgoingBlob(string? incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
	{
		byte[] array = null;
		if (!string.IsNullOrEmpty(incomingBlob))
		{
			array = Convert.FromBase64String(incomingBlob);
		}
		byte[] outgoingBlob = GetOutgoingBlob(array, out statusCode);
		string result = null;
		if (outgoingBlob != null && outgoingBlob.Length != 0)
		{
			result = Convert.ToBase64String(outgoingBlob);
		}
		return result;
	}

	public NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
	{
		if (!IsAuthenticated || _isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		return _pal.Wrap(input, outputWriter, requestEncryption, out isEncrypted);
	}

	public NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
	{
		if (!IsAuthenticated || _isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		return _pal.Unwrap(input, outputWriter, out wasEncrypted);
	}

	public NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
	{
		if (!IsAuthenticated || _isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		return _pal.UnwrapInPlace(input, out unwrappedOffset, out unwrappedLength, out wasEncrypted);
	}

	internal void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
	{
		if (!IsAuthenticated || _isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		_pal.GetMIC(message, signature);
	}

	internal bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
	{
		if (!IsAuthenticated || _isDisposed)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
		return _pal.VerifyMIC(message, signature);
	}

	private bool CheckSpn()
	{
		if (_pal.Package == "Kerberos")
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_spn_kerberos, "CheckSpn");
			}
			return true;
		}
		if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Never)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_spn_disabled, "CheckSpn");
			}
			return true;
		}
		if (_isSecureConnection && _extendedProtectionPolicy.ProtectionScenario == ProtectionScenario.TransportSelected)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_spn_cbt, "CheckSpn");
			}
			return true;
		}
		if (_extendedProtectionPolicy.CustomServiceNames == null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_spns, "CheckSpn");
			}
			return true;
		}
		string targetName = _pal.TargetName;
		if (string.IsNullOrEmpty(targetName))
		{
			if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, System.SR.net_log_listener_no_spn_whensupported, "CheckSpn");
				}
				return true;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn_failed_always, "CheckSpn");
			}
			return false;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn, targetName);
		}
		bool flag = _extendedProtectionPolicy.CustomServiceNames.Contains(targetName);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			if (flag)
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn_passed, "CheckSpn");
			}
			else
			{
				System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn_failed, "CheckSpn");
				if (_extendedProtectionPolicy.CustomServiceNames.Count == 0)
				{
					System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn_failed_empty, "CheckSpn");
				}
				else
				{
					System.Net.NetEventSource.Info(this, System.SR.net_log_listener_spn_failed_dump, "CheckSpn");
					foreach (string customServiceName in _extendedProtectionPolicy.CustomServiceNames)
					{
						System.Net.NetEventSource.Info(this, "\t" + customServiceName, "CheckSpn");
					}
				}
			}
		}
		return flag;
	}
}
