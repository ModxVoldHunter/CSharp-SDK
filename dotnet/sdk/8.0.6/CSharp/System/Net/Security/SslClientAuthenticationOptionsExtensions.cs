using System.Collections.Generic;

namespace System.Net.Security;

internal static class SslClientAuthenticationOptionsExtensions
{
	public static SslClientAuthenticationOptions ShallowClone(this SslClientAuthenticationOptions options)
	{
		return new SslClientAuthenticationOptions
		{
			AllowRenegotiation = options.AllowRenegotiation,
			AllowTlsResume = options.AllowTlsResume,
			ApplicationProtocols = ((options.ApplicationProtocols != null) ? new List<SslApplicationProtocol>(options.ApplicationProtocols) : null),
			CertificateRevocationCheckMode = options.CertificateRevocationCheckMode,
			CertificateChainPolicy = options.CertificateChainPolicy,
			CipherSuitesPolicy = options.CipherSuitesPolicy,
			ClientCertificates = options.ClientCertificates,
			ClientCertificateContext = options.ClientCertificateContext,
			EnabledSslProtocols = options.EnabledSslProtocols,
			EncryptionPolicy = options.EncryptionPolicy,
			LocalCertificateSelectionCallback = options.LocalCertificateSelectionCallback,
			RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback,
			TargetHost = options.TargetHost
		};
	}
}
