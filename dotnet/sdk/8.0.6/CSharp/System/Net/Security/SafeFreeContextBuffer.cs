using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal abstract class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
{
	protected SafeFreeContextBuffer()
		: base(ownsHandle: true)
	{
	}

	internal void Set(nint value)
	{
		handle = value;
	}

	internal static int EnumeratePackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
	{
		SafeFreeContextBuffer_SECURITY safeFreeContextBuffer_SECURITY;
		int num = global::Interop.SspiCli.EnumerateSecurityPackagesW(out pkgnum, out safeFreeContextBuffer_SECURITY);
		pkgArray = safeFreeContextBuffer_SECURITY;
		if (num != 0)
		{
			pkgArray?.SetHandleAsInvalid();
		}
		return num;
	}

	internal static SafeFreeContextBuffer CreateEmptyHandle()
	{
		return new SafeFreeContextBuffer_SECURITY();
	}

	public unsafe static int QueryContextAttributes(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
	{
		int num = -2146893055;
		try
		{
			bool success = false;
			phContext.DangerousAddRef(ref success);
			num = global::Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
		}
		finally
		{
			phContext.DangerousRelease();
		}
		if (num == 0 && refHandle != null)
		{
			if (refHandle is SafeFreeContextBuffer)
			{
				((SafeFreeContextBuffer)refHandle).Set(*(nint*)buffer);
			}
			else
			{
				((SafeFreeCertContext)refHandle).Set(*(nint*)buffer);
			}
		}
		if (num != 0)
		{
			refHandle?.SetHandleAsInvalid();
		}
		return num;
	}
}
