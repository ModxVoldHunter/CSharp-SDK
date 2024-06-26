namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_POLICIES_INFO
{
	public int cPolicyInfo;

	public unsafe CERT_POLICY_INFO* rgPolicyInfo;
}
