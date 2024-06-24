namespace System.Security.Cryptography.X509Certificates;

internal struct PolicyData
{
	internal byte[] ApplicationCertPolicies;

	internal byte[] CertPolicies;

	internal byte[] CertPolicyMappings;

	internal byte[] CertPolicyConstraints;

	internal byte[] EnhancedKeyUsage;

	internal byte[] InhibitAnyPolicyExtension;
}
