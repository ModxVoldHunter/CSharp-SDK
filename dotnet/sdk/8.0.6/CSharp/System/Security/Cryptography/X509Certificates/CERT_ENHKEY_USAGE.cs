namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_ENHKEY_USAGE
{
	public int cUsageIdentifier;

	public unsafe nint* rgpszUsageIdentifier;
}
