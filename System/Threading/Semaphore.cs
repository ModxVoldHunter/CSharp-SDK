using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public sealed class Semaphore : WaitHandle
{
	public Semaphore(int initialCount, int maximumCount)
		: this(initialCount, maximumCount, null)
	{
	}

	public Semaphore(int initialCount, int maximumCount, string? name)
		: this(initialCount, maximumCount, name, out var _)
	{
	}

	public Semaphore(int initialCount, int maximumCount, string? name, out bool createdNew)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(initialCount, "initialCount");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount, "maximumCount");
		if (initialCount > maximumCount)
		{
			throw new ArgumentException(SR.Argument_SemaphoreInitialMaximum);
		}
		CreateSemaphoreCore(initialCount, maximumCount, name, out createdNew);
	}

	[SupportedOSPlatform("windows")]
	public static Semaphore OpenExisting(string name)
	{
		Semaphore result;
		return OpenExistingWorker(name, out result) switch
		{
			OpenExistingResult.NameNotFound => throw new WaitHandleCannotBeOpenedException(), 
			OpenExistingResult.NameInvalid => throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name)), 
			OpenExistingResult.PathNotFound => throw new IOException(SR.Format(SR.IO_PathNotFound_Path, name)), 
			_ => result, 
		};
	}

	[SupportedOSPlatform("windows")]
	public static bool TryOpenExisting(string name, [NotNullWhen(true)] out Semaphore? result)
	{
		return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
	}

	public int Release()
	{
		return ReleaseCore(1);
	}

	public int Release(int releaseCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(releaseCount, "releaseCount");
		return ReleaseCore(releaseCount);
	}

	private Semaphore(SafeWaitHandle handle)
	{
		base.SafeWaitHandle = handle;
	}

	private void CreateSemaphoreCore(int initialCount, int maximumCount, string name, out bool createdNew)
	{
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.CreateSemaphoreEx(IntPtr.Zero, initialCount, maximumCount, name, 0u, 34603010u);
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (safeWaitHandle.IsInvalid)
		{
			safeWaitHandle.Dispose();
			if (!string.IsNullOrEmpty(name) && lastPInvokeError == 6)
			{
				throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
			}
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		createdNew = lastPInvokeError != 183;
		base.SafeWaitHandle = safeWaitHandle;
	}

	private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		SafeWaitHandle safeWaitHandle = Interop.Kernel32.OpenSemaphore(34603010u, inheritHandle: false, name);
		if (safeWaitHandle.IsInvalid)
		{
			result = null;
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			safeWaitHandle.Dispose();
			switch (lastPInvokeError)
			{
			case 2:
			case 123:
				return OpenExistingResult.NameNotFound;
			case 3:
				return OpenExistingResult.PathNotFound;
			case 6:
				return OpenExistingResult.NameInvalid;
			default:
				throw Win32Marshal.GetExceptionForLastWin32Error();
			}
		}
		result = new Semaphore(safeWaitHandle);
		return OpenExistingResult.Success;
	}

	private int ReleaseCore(int releaseCount)
	{
		if (!Interop.Kernel32.ReleaseSemaphore(base.SafeWaitHandle, releaseCount, out var previousCount))
		{
			throw new SemaphoreFullException();
		}
		return previousCount;
	}
}
