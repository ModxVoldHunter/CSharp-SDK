using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal sealed class HandleMarshaler
{
	internal static nint ConvertSafeHandleToNative(SafeHandle handle, ref CleanupWorkListElement cleanupWorkList)
	{
		if (Unsafe.IsNullRef(ref cleanupWorkList))
		{
			throw new InvalidOperationException(SR.Interop_Marshal_SafeHandle_InvalidOperation);
		}
		ArgumentNullException.ThrowIfNull(handle, "handle");
		return StubHelpers.AddToCleanupList(ref cleanupWorkList, handle);
	}

	internal static void ThrowSafeHandleFieldChanged()
	{
		throw new NotSupportedException(SR.Interop_Marshal_CannotCreateSafeHandleField);
	}

	internal static void ThrowCriticalHandleFieldChanged()
	{
		throw new NotSupportedException(SR.Interop_Marshal_CannotCreateCriticalHandleField);
	}
}
