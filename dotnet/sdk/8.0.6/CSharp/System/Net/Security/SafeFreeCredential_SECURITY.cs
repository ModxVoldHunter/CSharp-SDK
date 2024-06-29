using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

internal sealed class SafeFreeCredential_SECURITY : SafeFreeCredentials
{
	public X509Certificate LocalCertificate;

	protected override bool ReleaseHandle()
	{
		LocalCertificate?.Dispose();
		return global::Interop.SspiCli.FreeCredentialsHandle(ref _handle) == 0;
	}
}
