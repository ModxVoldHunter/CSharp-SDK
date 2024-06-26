using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Quic;

namespace System.Net.Quic;

internal static class MsQuicConfiguration
{
	private static bool HasPrivateKey(this X509Certificate certificate)
	{
		if (certificate is X509Certificate2 x509Certificate && x509Certificate.Handle != IntPtr.Zero)
		{
			return x509Certificate.HasPrivateKey;
		}
		return false;
	}

	public static MsQuicSafeHandle Create(QuicClientConnectionOptions options)
	{
		SslClientAuthenticationOptions clientAuthenticationOptions = options.ClientAuthenticationOptions;
		QUIC_CREDENTIAL_FLAGS qUIC_CREDENTIAL_FLAGS = QUIC_CREDENTIAL_FLAGS.NONE;
		qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.CLIENT;
		qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED;
		qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
		if (MsQuicApi.UsesSChannelBackend)
		{
			qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.USE_SUPPLIED_CREDENTIALS;
		}
		X509Certificate x509Certificate = null;
		if (clientAuthenticationOptions.LocalCertificateSelectionCallback != null)
		{
			X509Certificate x509Certificate2 = clientAuthenticationOptions.LocalCertificateSelectionCallback(options, clientAuthenticationOptions.TargetHost ?? string.Empty, clientAuthenticationOptions.ClientCertificates ?? new X509CertificateCollection(), null, Array.Empty<string>());
			if (x509Certificate2.HasPrivateKey())
			{
				x509Certificate = x509Certificate2;
			}
			else if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(options, $"'{x509Certificate}' not selected because it doesn't have a private key.", "Create");
			}
		}
		else if (clientAuthenticationOptions.ClientCertificates != null)
		{
			foreach (X509Certificate clientCertificate in clientAuthenticationOptions.ClientCertificates)
			{
				if (clientCertificate.HasPrivateKey())
				{
					x509Certificate = clientCertificate;
					break;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(options, $"'{x509Certificate}' not selected because it doesn't have a private key.", "Create");
				}
			}
		}
		return Create(options, qUIC_CREDENTIAL_FLAGS, x509Certificate, null, clientAuthenticationOptions.ApplicationProtocols, clientAuthenticationOptions.CipherSuitesPolicy, clientAuthenticationOptions.EncryptionPolicy);
	}

	public static MsQuicSafeHandle Create(QuicServerConnectionOptions options, string targetHost)
	{
		SslServerAuthenticationOptions serverAuthenticationOptions = options.ServerAuthenticationOptions;
		QUIC_CREDENTIAL_FLAGS qUIC_CREDENTIAL_FLAGS = QUIC_CREDENTIAL_FLAGS.NONE;
		if (serverAuthenticationOptions.ClientCertificateRequired)
		{
			qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.REQUIRE_CLIENT_AUTHENTICATION;
			qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED;
			qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
		}
		X509Certificate x509Certificate = null;
		ReadOnlyCollection<X509Certificate2> intermediates = null;
		if (serverAuthenticationOptions.ServerCertificateContext != null)
		{
			x509Certificate = serverAuthenticationOptions.ServerCertificateContext.TargetCertificate;
			intermediates = serverAuthenticationOptions.ServerCertificateContext.IntermediateCertificates;
		}
		if (x509Certificate == null)
		{
			x509Certificate = serverAuthenticationOptions.ServerCertificate ?? serverAuthenticationOptions.ServerCertificateSelectionCallback?.Invoke(serverAuthenticationOptions, targetHost);
		}
		if (x509Certificate == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_quic_not_null_ceritifcate, "ServerCertificate", "ServerCertificateContext", "ServerCertificateSelectionCallback"), "options");
		}
		return Create(options, qUIC_CREDENTIAL_FLAGS, x509Certificate, intermediates, serverAuthenticationOptions.ApplicationProtocols, serverAuthenticationOptions.CipherSuitesPolicy, serverAuthenticationOptions.EncryptionPolicy);
	}

	private unsafe static MsQuicSafeHandle Create(QuicConnectionOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate certificate, ReadOnlyCollection<X509Certificate2> intermediates, List<SslApplicationProtocol> alpnProtocols, CipherSuitesPolicy cipherSuitesPolicy, EncryptionPolicy encryptionPolicy)
	{
		if (alpnProtocols == null || alpnProtocols.Count <= 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_quic_not_null_not_empty_connection, "SslApplicationProtocol"), "options");
		}
		if (encryptionPolicy == EncryptionPolicy.NoEncryption)
		{
			throw new PlatformNotSupportedException(System.SR.Format(System.SR.net_quic_ssl_option, encryptionPolicy));
		}
		QUIC_SETTINGS qUIC_SETTINGS = default(QUIC_SETTINGS);
		qUIC_SETTINGS.IsSet.PeerUnidiStreamCount = 1uL;
		qUIC_SETTINGS.PeerUnidiStreamCount = (ushort)options.MaxInboundUnidirectionalStreams;
		qUIC_SETTINGS.IsSet.PeerBidiStreamCount = 1uL;
		qUIC_SETTINGS.PeerBidiStreamCount = (ushort)options.MaxInboundBidirectionalStreams;
		if (options.IdleTimeout != TimeSpan.Zero)
		{
			qUIC_SETTINGS.IsSet.IdleTimeoutMs = 1uL;
			qUIC_SETTINGS.IdleTimeoutMs = ((options.IdleTimeout != Timeout.InfiniteTimeSpan) ? ((ulong)options.IdleTimeout.TotalMilliseconds) : 0);
		}
		using MsQuicBuffers msQuicBuffers = new MsQuicBuffers();
		msQuicBuffers.Initialize(alpnProtocols, (SslApplicationProtocol alpnProtocol) => alpnProtocol.Protocol);
		Unsafe.SkipInit(out QUIC_HANDLE* handle);
		ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConfigurationOpen(MsQuicApi.Api.Registration, msQuicBuffers.Buffers, (uint)alpnProtocols.Count, &qUIC_SETTINGS, (uint)sizeof(QUIC_SETTINGS), (void*)IntPtr.Zero, &handle), "ConfigurationOpen failed");
		MsQuicSafeHandle msQuicSafeHandle = new MsQuicSafeHandle(handle, SafeHandleType.Configuration);
		try
		{
			QUIC_CREDENTIAL_CONFIG qUIC_CREDENTIAL_CONFIG = default(QUIC_CREDENTIAL_CONFIG);
			qUIC_CREDENTIAL_CONFIG.Flags = flags;
			QUIC_CREDENTIAL_CONFIG qUIC_CREDENTIAL_CONFIG2 = qUIC_CREDENTIAL_CONFIG;
			qUIC_CREDENTIAL_CONFIG2.Flags |= (QUIC_CREDENTIAL_FLAGS)((!MsQuicApi.UsesSChannelBackend) ? 16384 : 0);
			if (cipherSuitesPolicy != null)
			{
				qUIC_CREDENTIAL_CONFIG2.Flags |= QUIC_CREDENTIAL_FLAGS.SET_ALLOWED_CIPHER_SUITES;
				qUIC_CREDENTIAL_CONFIG2.AllowedCipherSuites = CipherSuitePolicyToFlags(cipherSuitesPolicy);
			}
			int num;
			if (certificate == null)
			{
				qUIC_CREDENTIAL_CONFIG2.Type = QUIC_CREDENTIAL_TYPE.NONE;
				num = MsQuicApi.Api.ConfigurationLoadCredential(msQuicSafeHandle, &qUIC_CREDENTIAL_CONFIG2);
			}
			else if (MsQuicApi.UsesSChannelBackend)
			{
				qUIC_CREDENTIAL_CONFIG2.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_CONTEXT;
				qUIC_CREDENTIAL_CONFIG2.CertificateContext = (void*)certificate.Handle;
				num = MsQuicApi.Api.ConfigurationLoadCredential(msQuicSafeHandle, &qUIC_CREDENTIAL_CONFIG2);
			}
			else
			{
				qUIC_CREDENTIAL_CONFIG2.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_PKCS12;
				byte[] array;
				if (intermediates != null && intermediates.Count > 0)
				{
					X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
					x509Certificate2Collection.Add(certificate);
					foreach (X509Certificate2 intermediate in intermediates)
					{
						x509Certificate2Collection.Add(intermediate);
					}
					array = x509Certificate2Collection.Export(X509ContentType.Pfx);
				}
				else
				{
					array = certificate.Export(X509ContentType.Pfx);
				}
				fixed (byte* asn1Blob = array)
				{
					QUIC_CERTIFICATE_PKCS12 qUIC_CERTIFICATE_PKCS = default(QUIC_CERTIFICATE_PKCS12);
					qUIC_CERTIFICATE_PKCS.Asn1Blob = asn1Blob;
					qUIC_CERTIFICATE_PKCS.Asn1BlobLength = (uint)array.Length;
					qUIC_CERTIFICATE_PKCS.PrivateKeyPassword = (sbyte*)IntPtr.Zero;
					QUIC_CERTIFICATE_PKCS12 qUIC_CERTIFICATE_PKCS2 = qUIC_CERTIFICATE_PKCS;
					qUIC_CREDENTIAL_CONFIG2.CertificatePkcs12 = &qUIC_CERTIFICATE_PKCS2;
					num = MsQuicApi.Api.ConfigurationLoadCredential(msQuicSafeHandle, &qUIC_CREDENTIAL_CONFIG2);
				}
			}
			if (num == -2146893007 && (((flags & QUIC_CREDENTIAL_FLAGS.CLIENT) == 0) ? MsQuicApi.Tls13ServerMayBeDisabled : MsQuicApi.Tls13ClientMayBeDisabled))
			{
				ThrowHelper.ThrowIfMsQuicError(num, System.SR.net_quic_tls_version_notsupported);
			}
			ThrowHelper.ThrowIfMsQuicError(num, "ConfigurationLoadCredential failed");
		}
		catch
		{
			msQuicSafeHandle.Dispose();
			throw;
		}
		return msQuicSafeHandle;
	}

	private static QUIC_ALLOWED_CIPHER_SUITE_FLAGS CipherSuitePolicyToFlags(CipherSuitesPolicy cipherSuitesPolicy)
	{
		QUIC_ALLOWED_CIPHER_SUITE_FLAGS qUIC_ALLOWED_CIPHER_SUITE_FLAGS = QUIC_ALLOWED_CIPHER_SUITE_FLAGS.NONE;
		using (IEnumerator<TlsCipherSuite> enumerator = cipherSuitesPolicy.AllowedCipherSuites.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
				case TlsCipherSuite.TLS_AES_128_GCM_SHA256:
					qUIC_ALLOWED_CIPHER_SUITE_FLAGS |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.AES_128_GCM_SHA256;
					break;
				case TlsCipherSuite.TLS_AES_256_GCM_SHA384:
					qUIC_ALLOWED_CIPHER_SUITE_FLAGS |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.AES_256_GCM_SHA384;
					break;
				case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256:
					qUIC_ALLOWED_CIPHER_SUITE_FLAGS |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.CHACHA20_POLY1305_SHA256;
					break;
				}
			}
		}
		if (qUIC_ALLOWED_CIPHER_SUITE_FLAGS == QUIC_ALLOWED_CIPHER_SUITE_FLAGS.NONE)
		{
			throw new ArgumentException(System.SR.net_quic_empty_cipher_suite, "CipherSuitesPolicy");
		}
		return qUIC_ALLOWED_CIPHER_SUITE_FLAGS;
	}
}
