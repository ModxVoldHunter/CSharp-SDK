namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_POLICY_INFO
{
	public nint pszPolicyIdentifier;

	public int cPolicyQualifier;

	public nint rgPolicyQualifier;
}
