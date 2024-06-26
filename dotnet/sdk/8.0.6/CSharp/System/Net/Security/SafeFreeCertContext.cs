using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal sealed class SafeFreeCertContext : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeFreeCertContext()
		: base(ownsHandle: true)
	{
	}

	internal void Set(nint value)
	{
		handle = value;
	}

	protected override bool ReleaseHandle()
	{
		global::Interop.Crypt32.CertFreeCertificateContext(handle);
		return true;
	}
}
