namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_TRUST_STATUS
{
	public CertTrustErrorStatus dwErrorStatus;

	public CertTrustInfoStatus dwInfoStatus;
}
