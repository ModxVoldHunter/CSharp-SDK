namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_BASIC_CONSTRAINTS_INFO
{
	public global::Interop.Crypt32.CRYPT_BIT_BLOB SubjectType;

	public int fPathLenConstraint;

	public int dwPathLenConstraint;

	public int cSubtreesConstraint;

	public unsafe global::Interop.Crypt32.DATA_BLOB* rgSubtreesConstraint;
}
