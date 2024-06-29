namespace System.Security.Cryptography.X509Certificates;

internal struct CERT_NAME_VALUE
{
	public int dwValueType;

	public global::Interop.Crypt32.DATA_BLOB Value;
}
