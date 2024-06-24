using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class SafeLocalAllocHandle : SafeCrypt32Handle<SafeLocalAllocHandle>
{
	public static SafeLocalAllocHandle Create(int cb)
	{
		SafeLocalAllocHandle safeLocalAllocHandle = new SafeLocalAllocHandle();
		safeLocalAllocHandle.SetHandle(Marshal.AllocHGlobal(cb));
		return safeLocalAllocHandle;
	}

	protected sealed override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
