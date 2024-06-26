using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net;

internal static class UnmanagedCertificateContext
{
	internal static void GetRemoteCertificatesFromStoreContext(SafeFreeCertContext certContext, X509Certificate2Collection collection)
	{
		if (!certContext.IsInvalid)
		{
			GetRemoteCertificatesFromStoreContext(certContext.DangerousGetHandle(), collection);
		}
	}

	internal unsafe static void GetRemoteCertificatesFromStoreContext(nint certContext, X509Certificate2Collection result)
	{
		if (certContext == IntPtr.Zero)
		{
			return;
		}
		global::Interop.Crypt32.CERT_CONTEXT cERT_CONTEXT = *(global::Interop.Crypt32.CERT_CONTEXT*)certContext;
		if (cERT_CONTEXT.hCertStore == IntPtr.Zero)
		{
			return;
		}
		global::Interop.Crypt32.CERT_CONTEXT* pPrevCertContext = null;
		while (true)
		{
			global::Interop.Crypt32.CERT_CONTEXT* ptr = global::Interop.Crypt32.CertEnumCertificatesInStore(cERT_CONTEXT.hCertStore, pPrevCertContext);
			if (ptr == null)
			{
				break;
			}
			if (ptr != (global::Interop.Crypt32.CERT_CONTEXT*)certContext)
			{
				X509Certificate2 x509Certificate = new X509Certificate2(new IntPtr(ptr));
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(certContext, $"Adding remote certificate:{x509Certificate}", "GetRemoteCertificatesFromStoreContext");
				}
				result.Add(x509Certificate);
			}
			pPrevCertContext = ptr;
		}
	}
}
