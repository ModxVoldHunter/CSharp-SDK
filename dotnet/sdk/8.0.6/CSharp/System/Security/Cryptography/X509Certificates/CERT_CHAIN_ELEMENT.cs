namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_CHAIN_ELEMENT
{
	public int cbSize;

	public unsafe global::Interop.Crypt32.CERT_CONTEXT* pCertContext;

	public CERT_TRUST_STATUS TrustStatus;

	public nint pRevocationInfo;

	public nint pIssuanceUsage;

	public nint pApplicationUsage;

	public nint pwszExtendedErrorInfo;
}
