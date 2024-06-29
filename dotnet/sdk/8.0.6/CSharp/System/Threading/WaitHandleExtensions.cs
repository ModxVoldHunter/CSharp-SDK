using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public static class WaitHandleExtensions
{
	public static SafeWaitHandle GetSafeWaitHandle(this WaitHandle waitHandle)
	{
		ArgumentNullException.ThrowIfNull(waitHandle, "waitHandle");
		return waitHandle.SafeWaitHandle;
	}

	public static void SetSafeWaitHandle(this WaitHandle waitHandle, SafeWaitHandle? value)
	{
		ArgumentNullException.ThrowIfNull(waitHandle, "waitHandle");
		waitHandle.SafeWaitHandle = value;
	}
}
