using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class StubHelpers
{
	[ThreadStatic]
	private static Exception s_pendingExceptionObject;

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint GetDelegateTarget(Delegate pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearLastError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void SetLastError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ThrowInteropParamException(int resID, int paramIdx);

	internal static nint AddToCleanupList(ref CleanupWorkListElement pCleanupWorkList, SafeHandle handle)
	{
		SafeHandleCleanupWorkListElement safeHandleCleanupWorkListElement = new SafeHandleCleanupWorkListElement(handle);
		CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, safeHandleCleanupWorkListElement);
		return safeHandleCleanupWorkListElement.AddRef();
	}

	internal static void KeepAliveViaCleanupList(ref CleanupWorkListElement pCleanupWorkList, object obj)
	{
		KeepAliveCleanupWorkListElement newElement = new KeepAliveCleanupWorkListElement(obj);
		CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, newElement);
	}

	internal static void DestroyCleanupList(ref CleanupWorkListElement pCleanupWorkList)
	{
		if (pCleanupWorkList != null)
		{
			pCleanupWorkList.Destroy();
			pCleanupWorkList = null;
		}
	}

	internal static Exception GetHRExceptionObject(int hr)
	{
		Exception ex = InternalGetHRExceptionObject(hr);
		ex.InternalPreserveStackTrace();
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception InternalGetHRExceptionObject(int hr);

	internal static Exception GetCOMHRExceptionObject(int hr, nint pCPCMD, object pThis)
	{
		Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis);
		ex.InternalPreserveStackTrace();
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception InternalGetCOMHRExceptionObject(int hr, nint pCPCMD, object pThis);

	internal static Exception GetPendingExceptionObject()
	{
		Exception ex = s_pendingExceptionObject;
		if (ex != null)
		{
			ex.InternalPreserveStackTrace();
			s_pendingExceptionObject = null;
		}
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint CreateCustomMarshalerHelper(nint pMD, int paramToken, nint hndManagedType);

	internal static nint SafeHandleAddRef(SafeHandle pHandle, ref bool success)
	{
		if (pHandle == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
		}
		pHandle.DangerousAddRef(ref success);
		return pHandle.DangerousGetHandle();
	}

	internal static void SafeHandleRelease(SafeHandle pHandle)
	{
		if (pHandle == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
		}
		pHandle.DangerousRelease();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint GetCOMIPFromRCW(object objSrc, nint pCPCMD, out nint ppTarget, out bool pfNeedsRelease);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint ProfilerBeginTransitionCallback(nint pSecretParam, nint pThread, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ProfilerEndTransitionCallback(nint pMD, nint pThread);

	internal static void CheckStringLength(int length)
	{
		CheckStringLength((uint)length);
	}

	internal static void CheckStringLength(uint length)
	{
		if (length > 2147483632)
		{
			throw new MarshalDirectiveException(SR.Marshaler_StringTooLong);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkListElement pCleanupWorkList);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void FmtClassUpdateCLRInternal(object obj, byte* pNative);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void LayoutDestroyNativeInternal(object obj, byte* pNative);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object AllocateInternal(nint typeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MarshalToUnmanagedVaListInternal(nint va_list, uint vaListSize, nint pArgIterator);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MarshalToManagedVaListInternal(nint va_list, nint pArgIterator);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern uint CalcVaListSize(nint va_list);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ValidateObject(object obj, nint pMD, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void LogPinnedArgument(nint localDesc, nint nativeArg);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ValidateByref(nint byref, nint pMD, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	internal static extern nint GetStubContext();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ArrayTypeCheck(object o, object[] arr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MulticastDebuggerTraceHelper(object o, int count);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	internal static extern nint NextCallReturnAddress();
}
