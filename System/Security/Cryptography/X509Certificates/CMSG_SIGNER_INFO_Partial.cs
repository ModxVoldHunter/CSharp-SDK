namespace System.Security.Cryptography.X509Certificates;

internal struct CMSG_SIGNER_INFO_Partial
{
	public int dwVersion;

	public global::Interop.Crypt32.DATA_BLOB Issuer;

	public global::Interop.Crypt32.DATA_BLOB SerialNumber;
}
