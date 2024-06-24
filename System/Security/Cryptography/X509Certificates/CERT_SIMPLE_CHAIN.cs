namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_SIMPLE_CHAIN
{
	public int cbSize;

	public CERT_TRUST_STATUS TrustStatus;

	public int cElement;

	public unsafe CERT_CHAIN_ELEMENT** rgpElement;

	public nint pTrustListInfo;

	public int fHasRevocationFreshnessTime;

	public int dwRevocationFreshnessTime;
}
