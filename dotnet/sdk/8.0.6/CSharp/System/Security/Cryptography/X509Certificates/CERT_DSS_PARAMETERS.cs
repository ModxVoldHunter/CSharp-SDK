namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_DSS_PARAMETERS
{
	public global::Interop.Crypt32.DATA_BLOB p;

	public global::Interop.Crypt32.DATA_BLOB q;

	public global::Interop.Crypt32.DATA_BLOB g;
}
