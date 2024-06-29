using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal static class SslStreamPal
{
	private static readonly byte[] s_http1 = global::Interop.Sec_Application_Protocols.ToByteArray(new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 });

	private static readonly byte[] s_http2 = global::Interop.Sec_Application_Protocols.ToByteArray(new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 });

	private static readonly byte[] s_http12 = global::Interop.Sec_Application_Protocols.ToByteArray(new List<SslApplicationProtocol>
	{
		SslApplicationProtocol.Http11,
		SslApplicationProtocol.Http2
	});

	private static readonly byte[] s_http21 = global::Interop.Sec_Application_Protocols.ToByteArray(new List<SslApplicationProtocol>
	{
		SslApplicationProtocol.Http2,
		SslApplicationProtocol.Http11
	});

	private static readonly bool UseNewCryptoApi = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 18836;

	private static readonly byte[] s_sessionTokenBuffer = InitSessionTokenBuffer();

	private static readonly byte[] s_schannelShutdownBytes = BitConverter.GetBytes(1);

	public static Exception GetException(SecurityStatusPal status)
	{
		int interopFromSecurityStatusPal = (int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status);
		return new Win32Exception(interopFromSecurityStatusPal);
	}

	private static byte[] InitSessionTokenBuffer()
	{
		global::Interop.SChannel.SCHANNEL_SESSION_TOKEN sCHANNEL_SESSION_TOKEN = default(global::Interop.SChannel.SCHANNEL_SESSION_TOKEN);
		sCHANNEL_SESSION_TOKEN.dwTokenType = 3u;
		sCHANNEL_SESSION_TOKEN.dwFlags = 2u;
		global::Interop.SChannel.SCHANNEL_SESSION_TOKEN reference = sCHANNEL_SESSION_TOKEN;
		return MemoryMarshal.AsBytes(new ReadOnlySpan<global::Interop.SChannel.SCHANNEL_SESSION_TOKEN>(ref reference)).ToArray();
	}

	private unsafe static void SetAlpn(ref InputSecurityBuffers inputBuffers, List<SslApplicationProtocol> alpn, Span<byte> localBuffer)
	{
		if (alpn.Count == 1 && alpn[0] == SslApplicationProtocol.Http11)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(s_http1, SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
			return;
		}
		if (alpn.Count == 1 && alpn[0] == SslApplicationProtocol.Http2)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(s_http2, SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
			return;
		}
		if (alpn.Count == 2 && alpn[0] == SslApplicationProtocol.Http11 && alpn[1] == SslApplicationProtocol.Http2)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(s_http12, SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
			return;
		}
		if (alpn.Count == 2 && alpn[0] == SslApplicationProtocol.Http2 && alpn[1] == SslApplicationProtocol.Http11)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(s_http21, SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
			return;
		}
		int protocolLength = global::Interop.Sec_Application_Protocols.GetProtocolLength(alpn);
		int num = sizeof(global::Interop.Sec_Application_Protocols) + protocolLength;
		Span<byte> span = ((num <= localBuffer.Length) ? localBuffer : ((Span<byte>)new byte[num]));
		global::Interop.Sec_Application_Protocols.SetProtocols(span, alpn, protocolLength);
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(span, SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
	}

	public static SecurityStatusPal SelectApplicationProtocol(SafeFreeCredentials credentialsHandle, SafeDeleteSslContext context, SslAuthenticationOptions sslAuthenticationOptions, ReadOnlySpan<byte> clientProtocols)
	{
		throw new PlatformNotSupportedException("SelectApplicationProtocol");
	}

	public static SecurityStatusPal AcceptSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, ReadOnlySpan<byte> inputBuffer, ref byte[] outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
	{
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(default(ReadOnlySpan<byte>), SecurityBufferType.SECBUFFER_EMPTY));
		if (context == null && sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
		{
			Span<byte> localBuffer = stackalloc byte[64];
			SetAlpn(ref inputBuffers, sslAuthenticationOptions.ApplicationProtocols, localBuffer);
		}
		SecurityBuffer outputBuffer2 = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);
		int win32SecurityStatus = SSPIWrapper.AcceptSecurityContext(GlobalSSPI.SSPISecureChannel, credentialsHandle, ref context, global::Interop.SspiCli.ContextFlags.ReplayDetect | global::Interop.SspiCli.ContextFlags.SequenceDetect | global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AllocateMemory | global::Interop.SspiCli.ContextFlags.AcceptExtendedError | global::Interop.SspiCli.ContextFlags.AcceptStream | (sslAuthenticationOptions.RemoteCertRequired ? global::Interop.SspiCli.ContextFlags.MutualAuth : global::Interop.SspiCli.ContextFlags.Zero), global::Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP, inputBuffers, ref outputBuffer2, ref outFlags);
		outputBuffer = outputBuffer2.token;
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(win32SecurityStatus);
	}

	public static bool TryUpdateClintCertificate(SafeFreeCredentials _1, SafeDeleteSslContext _2, SslAuthenticationOptions _3)
	{
		return false;
	}

	public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, string targetName, ReadOnlySpan<byte> inputBuffer, ref byte[] outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
	{
		bool flag = context == null;
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(default(ReadOnlySpan<byte>), SecurityBufferType.SECBUFFER_EMPTY));
		if (context == null && sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
		{
			Span<byte> localBuffer = stackalloc byte[64];
			SetAlpn(ref inputBuffers, sslAuthenticationOptions.ApplicationProtocols, localBuffer);
		}
		SecurityBuffer outputBuffer2 = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);
		int win32SecurityStatus = SSPIWrapper.InitializeSecurityContext(GlobalSSPI.SSPISecureChannel, ref credentialsHandle, ref context, targetName, global::Interop.SspiCli.ContextFlags.ReplayDetect | global::Interop.SspiCli.ContextFlags.SequenceDetect | global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AllocateMemory | global::Interop.SspiCli.ContextFlags.InitManualCredValidation, global::Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP, inputBuffers, ref outputBuffer2, ref outFlags);
		if (!sslAuthenticationOptions.AllowTlsResume && flag && context != null)
		{
			SecurityBuffer inputBuffer2 = new SecurityBuffer(s_sessionTokenBuffer, SecurityBufferType.SECBUFFER_TOKEN);
			SecurityStatusPal securityStatusPalFromNativeInt = SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(SSPIWrapper.ApplyControlToken(GlobalSSPI.SSPISecureChannel, ref context, in inputBuffer2));
			if (securityStatusPalFromNativeInt.ErrorCode != SecurityStatusPalErrorCode.OK)
			{
				return securityStatusPalFromNativeInt;
			}
		}
		outputBuffer = outputBuffer2.token;
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(win32SecurityStatus);
	}

	public static SecurityStatusPal Renegotiate(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, SslAuthenticationOptions sslAuthenticationOptions, out byte[] outputBuffer)
	{
		byte[] outputBuffer2 = Array.Empty<byte>();
		SecurityStatusPal result = AcceptSecurityContext(ref credentialsHandle, ref context, Span<byte>.Empty, ref outputBuffer2, sslAuthenticationOptions);
		outputBuffer = outputBuffer2;
		return result;
	}

	public static SafeFreeCredentials AcquireCredentialsHandle(SslAuthenticationOptions sslAuthenticationOptions, bool newCredentialsRequested)
	{
		SslStreamCertificateContext certificateContext = sslAuthenticationOptions.CertificateContext;
		try
		{
			EncryptionPolicy encryptionPolicy = sslAuthenticationOptions.EncryptionPolicy;
			SafeFreeCredentials safeFreeCredentials = ((!UseNewCryptoApi || encryptionPolicy == EncryptionPolicy.NoEncryption) ? AcquireCredentialsHandleSchannelCred(sslAuthenticationOptions) : AcquireCredentialsHandleSchCredentials(sslAuthenticationOptions));
			if (certificateContext != null && certificateContext.Trust != null && certificateContext.Trust._sendTrustInHandshake)
			{
				AttachCertificateStore(safeFreeCredentials, certificateContext.Trust._store);
			}
			if (newCredentialsRequested && sslAuthenticationOptions.CertificateContext != null)
			{
				SafeFreeCredential_SECURITY safeFreeCredential_SECURITY = (SafeFreeCredential_SECURITY)safeFreeCredentials;
				safeFreeCredential_SECURITY.LocalCertificate = new X509Certificate2(sslAuthenticationOptions.CertificateContext.TargetCertificate);
			}
			return safeFreeCredentials;
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == -2146893042 && certificateContext != null)
		{
			using Microsoft.Win32.SafeHandles.SafeCertContextHandle safeCertContextHandle = global::Interop.Crypt32.CertDuplicateCertificateContext(certificateContext.TargetCertificate.Handle);
			throw new AuthenticationException(safeCertContextHandle.HasEphemeralPrivateKey ? System.SR.net_auth_ephemeral : System.SR.net_auth_SSPI, ex);
		}
		catch (Win32Exception innerException)
		{
			throw new AuthenticationException(System.SR.net_auth_SSPI, innerException);
		}
	}

	private unsafe static void AttachCertificateStore(SafeFreeCredentials cred, X509Store store)
	{
		global::Interop.SspiCli.SecPkgCred_ClientCertPolicy pBuffer = default(global::Interop.SspiCli.SecPkgCred_ClientCertPolicy);
		fixed (char* pwszSslCtlStoreName = store.Name)
		{
			pBuffer.pwszSslCtlStoreName = pwszSslCtlStoreName;
			global::Interop.SECURITY_STATUS sECURITY_STATUS = global::Interop.SspiCli.SetCredentialsAttributesW(in cred._handle, 96L, in pBuffer, sizeof(global::Interop.SspiCli.SecPkgCred_ClientCertPolicy));
			if (sECURITY_STATUS != 0)
			{
				throw new Win32Exception((int)sECURITY_STATUS);
			}
		}
	}

	public unsafe static SafeFreeCredentials AcquireCredentialsHandleSchannelCred(SslAuthenticationOptions authOptions)
	{
		X509Certificate2 x509Certificate = authOptions.CertificateContext?.TargetCertificate;
		bool isServer = authOptions.IsServer;
		int protocolFlagsFromSslProtocols = GetProtocolFlagsFromSslProtocols(authOptions.EnabledSslProtocols, isServer);
		global::Interop.SspiCli.CredentialUse credUsage;
		global::Interop.SspiCli.SCHANNEL_CRED.Flags flags;
		if (!isServer)
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
			flags = global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_MANUAL_CRED_VALIDATION | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_NO_DEFAULT_CREDS | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
			if (authOptions.CertificateRevocationCheckMode != 0)
			{
				flags |= global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_REVOCATION_CHECK_END_CERT | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_IGNORE_NO_REVOCATION_CHECK | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_IGNORE_REVOCATION_OFFLINE;
			}
		}
		else
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
			flags = global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_NO_SYSTEM_MAPPER | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
			if (!authOptions.AllowTlsResume)
			{
				flags |= global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_DISABLE_RECONNECTS;
			}
		}
		EncryptionPolicy encryptionPolicy = authOptions.EncryptionPolicy;
		if ((protocolFlagsFromSslProtocols == 0 || ((uint)protocolFlagsFromSslProtocols & 0xFFFFFFC3u) != 0) && encryptionPolicy != EncryptionPolicy.AllowNoEncryption && encryptionPolicy != EncryptionPolicy.NoEncryption)
		{
			flags |= global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_USE_STRONG_CRYPTO;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlagsFromSslProtocols}), EncryptionPolicy={encryptionPolicy}", null, "AcquireCredentialsHandleSchannelCred");
		}
		global::Interop.SspiCli.SCHANNEL_CRED sCHANNEL_CRED = CreateSecureCredential(flags, protocolFlagsFromSslProtocols, encryptionPolicy);
		if (!isServer && !authOptions.AllowTlsResume)
		{
			sCHANNEL_CRED.dwSessionLifespan = -1;
		}
		if (x509Certificate != null)
		{
			sCHANNEL_CRED.cCreds = 1;
			global::Interop.Crypt32.CERT_CONTEXT* handle = (global::Interop.Crypt32.CERT_CONTEXT*)x509Certificate.Handle;
			sCHANNEL_CRED.paCred = &handle;
		}
		return AcquireCredentialsHandle(credUsage, &sCHANNEL_CRED);
	}

	public unsafe static SafeFreeCredentials AcquireCredentialsHandleSchCredentials(SslAuthenticationOptions authOptions)
	{
		X509Certificate2 x509Certificate = authOptions.CertificateContext?.TargetCertificate;
		bool isServer = authOptions.IsServer;
		int protocolFlagsFromSslProtocols = GetProtocolFlagsFromSslProtocols(authOptions.EnabledSslProtocols, isServer);
		global::Interop.SspiCli.CredentialUse credUsage;
		global::Interop.SspiCli.SCH_CREDENTIALS.Flags flags;
		if (isServer)
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
			flags = global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;
			SslStreamCertificateContext certificateContext = authOptions.CertificateContext;
			if (certificateContext != null && certificateContext.Trust?._sendTrustInHandshake == true)
			{
				flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_NO_SYSTEM_MAPPER;
			}
			if (!authOptions.AllowTlsResume)
			{
				flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_DISABLE_RECONNECTS;
			}
		}
		else
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
			flags = global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_MANUAL_CRED_VALIDATION | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_NO_DEFAULT_CREDS | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;
			if (authOptions.CertificateRevocationCheckMode != 0)
			{
				flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_REVOCATION_CHECK_END_CERT | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_IGNORE_NO_REVOCATION_CHECK | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_IGNORE_REVOCATION_OFFLINE;
			}
		}
		EncryptionPolicy encryptionPolicy = authOptions.EncryptionPolicy;
		switch (encryptionPolicy)
		{
		case EncryptionPolicy.RequireEncryption:
			if ((protocolFlagsFromSslProtocols & 0x30) == 0)
			{
				flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_USE_STRONG_CRYPTO;
			}
			break;
		case EncryptionPolicy.AllowNoEncryption:
			flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_ALLOW_NULL_ENCRYPTION;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "EncryptionPolicy"), "policy");
		}
		global::Interop.SspiCli.SCH_CREDENTIALS sCH_CREDENTIALS = default(global::Interop.SspiCli.SCH_CREDENTIALS);
		sCH_CREDENTIALS.dwVersion = 5;
		sCH_CREDENTIALS.dwFlags = flags;
		if (!isServer && !authOptions.AllowTlsResume)
		{
			sCH_CREDENTIALS.dwSessionLifespan = -1;
		}
		if (x509Certificate != null)
		{
			sCH_CREDENTIALS.cCreds = 1;
			global::Interop.Crypt32.CERT_CONTEXT* handle = (global::Interop.Crypt32.CERT_CONTEXT*)x509Certificate.Handle;
			sCH_CREDENTIALS.paCred = &handle;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlagsFromSslProtocols}), EncryptionPolicy={encryptionPolicy}", null, "AcquireCredentialsHandleSchCredentials");
		}
		if (protocolFlagsFromSslProtocols != 0)
		{
			global::Interop.SspiCli.TLS_PARAMETERS tLS_PARAMETERS = default(global::Interop.SspiCli.TLS_PARAMETERS);
			tLS_PARAMETERS.grbitDisabledProtocols = (uint)protocolFlagsFromSslProtocols ^ 0xFFFFFFFFu;
			sCH_CREDENTIALS.cTlsParameters = 1;
			sCH_CREDENTIALS.pTlsParameters = &tLS_PARAMETERS;
		}
		return AcquireCredentialsHandle(credUsage, &sCH_CREDENTIALS);
	}

	public unsafe static SecurityStatusPal EncryptMessage(SafeDeleteSslContext securityContext, ReadOnlyMemory<byte> input, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
	{
		int num = checked(input.Length + headerSize + trailerSize);
		if (output == null || output.Length < num)
		{
			output = new byte[num];
		}
		input.Span.CopyTo(new Span<byte>(output, headerSize, input.Length));
		global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[4];
		global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(4);
		secBufferDesc.pBuffers = ptr;
		global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
		fixed (byte* ptr3 = output)
		{
			global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
			ptr2->BufferType = SecurityBufferType.SECBUFFER_STREAM_HEADER;
			ptr2->pvBuffer = (nint)ptr3;
			ptr2->cbBuffer = headerSize;
			global::Interop.SspiCli.SecBuffer* ptr4 = ptr + 1;
			ptr4->BufferType = SecurityBufferType.SECBUFFER_DATA;
			ptr4->pvBuffer = (nint)(ptr3 + headerSize);
			ptr4->cbBuffer = input.Length;
			global::Interop.SspiCli.SecBuffer* ptr5 = ptr + 2;
			ptr5->BufferType = SecurityBufferType.SECBUFFER_STREAM_TRAILER;
			ptr5->pvBuffer = (nint)(ptr3 + headerSize + input.Length);
			ptr5->cbBuffer = trailerSize;
			global::Interop.SspiCli.SecBuffer* ptr6 = ptr + 3;
			ptr6->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
			ptr6->cbBuffer = 0;
			ptr6->pvBuffer = IntPtr.Zero;
			int num2 = GlobalSSPI.SSPISecureChannel.EncryptMessage(securityContext, ref inputOutput, 0u);
			if (num2 != 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(securityContext, $"Encrypt ERROR {num2:X}", "EncryptMessage");
				}
				resultSize = 0;
				return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(num2);
			}
			resultSize = checked(ptr2->cbBuffer + ptr4->cbBuffer + ptr5->cbBuffer);
			return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
		}
	}

	public unsafe static SecurityStatusPal DecryptMessage(SafeDeleteSslContext securityContext, Span<byte> buffer, out int offset, out int count)
	{
		fixed (byte* ptr3 = buffer)
		{
			global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[4];
			global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
			ptr2->BufferType = SecurityBufferType.SECBUFFER_DATA;
			ptr2->pvBuffer = (nint)ptr3;
			ptr2->cbBuffer = buffer.Length;
			for (int i = 1; i < 4; i++)
			{
				global::Interop.SspiCli.SecBuffer* ptr4 = ptr + i;
				ptr4->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
				ptr4->pvBuffer = IntPtr.Zero;
				ptr4->cbBuffer = 0;
			}
			global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(4);
			secBufferDesc.pBuffers = ptr;
			global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
			uint qop;
			global::Interop.SECURITY_STATUS sECURITY_STATUS = (global::Interop.SECURITY_STATUS)GlobalSSPI.SSPISecureChannel.DecryptMessage(securityContext, ref inputOutput, out qop);
			count = 0;
			offset = 0;
			for (int j = 0; j < 4; j++)
			{
				if ((sECURITY_STATUS == global::Interop.SECURITY_STATUS.OK && ptr[j].BufferType == SecurityBufferType.SECBUFFER_DATA) || (sECURITY_STATUS != 0 && ptr[j].BufferType == SecurityBufferType.SECBUFFER_EXTRA))
				{
					offset = (int)((byte*)ptr[j].pvBuffer - ptr3);
					count = ptr[j].cbBuffer;
					break;
				}
			}
			return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(sECURITY_STATUS);
		}
	}

	public static SecurityStatusPal ApplyAlertToken(SafeDeleteSslContext securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
	{
		global::Interop.SChannel.SCHANNEL_ALERT_TOKEN sCHANNEL_ALERT_TOKEN = default(global::Interop.SChannel.SCHANNEL_ALERT_TOKEN);
		sCHANNEL_ALERT_TOKEN.dwTokenType = 2u;
		sCHANNEL_ALERT_TOKEN.dwAlertType = (uint)alertType;
		sCHANNEL_ALERT_TOKEN.dwAlertNumber = (uint)alertMessage;
		global::Interop.SChannel.SCHANNEL_ALERT_TOKEN reference = sCHANNEL_ALERT_TOKEN;
		byte[] data = MemoryMarshal.AsBytes(new ReadOnlySpan<global::Interop.SChannel.SCHANNEL_ALERT_TOKEN>(ref reference)).ToArray();
		SecurityBuffer inputBuffer = new SecurityBuffer(data, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(GlobalSSPI.SSPISecureChannel, ref securityContext, in inputBuffer);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus, attachException: true);
	}

	public static SecurityStatusPal ApplyShutdownToken(SafeDeleteSslContext securityContext)
	{
		SecurityBuffer inputBuffer = new SecurityBuffer(s_schannelShutdownBytes, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(GlobalSSPI.SSPISecureChannel, ref securityContext, in inputBuffer);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus, attachException: true);
	}

	public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
	{
		return SSPIWrapper.QueryContextChannelBinding(GlobalSSPI.SSPISecureChannel, securityContext, (global::Interop.SspiCli.ContextAttribute)attribute);
	}

	public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
	{
		SecPkgContext_StreamSizes attribute = default(SecPkgContext_StreamSizes);
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES, ref attribute);
		streamSizes = new StreamSizes(attribute);
	}

	public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, ref SslConnectionInfo connectionInfo)
	{
		connectionInfo.UpdateSslConnectionInfo(securityContext);
	}

	private static int GetProtocolFlagsFromSslProtocols(SslProtocols protocols, bool isServer)
	{
		int num = (int)protocols;
		if (isServer)
		{
			return num & 0x1554;
		}
		return num & 0x2AA8;
	}

	private unsafe static global::Interop.SspiCli.SCHANNEL_CRED CreateSecureCredential(global::Interop.SspiCli.SCHANNEL_CRED.Flags flags, int protocols, EncryptionPolicy policy)
	{
		global::Interop.SspiCli.SCHANNEL_CRED sCHANNEL_CRED = default(global::Interop.SspiCli.SCHANNEL_CRED);
		sCHANNEL_CRED.hRootStore = IntPtr.Zero;
		sCHANNEL_CRED.aphMappers = IntPtr.Zero;
		sCHANNEL_CRED.palgSupportedAlgs = IntPtr.Zero;
		sCHANNEL_CRED.paCred = null;
		sCHANNEL_CRED.cCreds = 0;
		sCHANNEL_CRED.cMappers = 0;
		sCHANNEL_CRED.cSupportedAlgs = 0;
		sCHANNEL_CRED.dwSessionLifespan = 0;
		sCHANNEL_CRED.reserved = 0;
		sCHANNEL_CRED.dwVersion = 4;
		global::Interop.SspiCli.SCHANNEL_CRED result = sCHANNEL_CRED;
		switch (policy)
		{
		case EncryptionPolicy.RequireEncryption:
			result.dwMinimumCipherStrength = 0;
			result.dwMaximumCipherStrength = 0;
			break;
		case EncryptionPolicy.AllowNoEncryption:
			result.dwMinimumCipherStrength = -1;
			result.dwMaximumCipherStrength = 0;
			break;
		case EncryptionPolicy.NoEncryption:
			result.dwMinimumCipherStrength = -1;
			result.dwMaximumCipherStrength = -1;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "EncryptionPolicy"), "policy");
		}
		result.dwFlags = flags;
		result.grbitEnabledProtocols = protocols;
		return result;
	}

	private unsafe static SafeFreeCredentials AcquireCredentialsHandle(global::Interop.SspiCli.CredentialUse credUsage, global::Interop.SspiCli.SCHANNEL_CRED* secureCredential)
	{
		try
		{
			using SafeAccessTokenHandle safeAccessTokenHandle = SafeAccessTokenHandle.InvalidHandle;
			return WindowsIdentity.RunImpersonated(safeAccessTokenHandle, () => SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential));
		}
		catch
		{
			return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential);
		}
	}

	private unsafe static SafeFreeCredentials AcquireCredentialsHandle(global::Interop.SspiCli.CredentialUse credUsage, global::Interop.SspiCli.SCH_CREDENTIALS* secureCredential)
	{
		try
		{
			using SafeAccessTokenHandle safeAccessTokenHandle = SafeAccessTokenHandle.InvalidHandle;
			return WindowsIdentity.RunImpersonated(safeAccessTokenHandle, () => SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential));
		}
		catch
		{
			return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential);
		}
	}
}
