using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal sealed class SecurityContextTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private int _disposed;

	public SecurityContextTokenHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		if (!IsInvalid && Interlocked.Increment(ref _disposed) == 1)
		{
			return global::Interop.Kernel32.CloseHandle(handle);
		}
		return true;
	}
}
