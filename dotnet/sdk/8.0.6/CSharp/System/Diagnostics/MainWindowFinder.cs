using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

internal struct MainWindowFinder
{
	private nint _bestHandle;

	private int _processId;

	public unsafe static nint FindMainWindow(int processId)
	{
		Unsafe.SkipInit(out MainWindowFinder mainWindowFinder);
		mainWindowFinder._bestHandle = IntPtr.Zero;
		mainWindowFinder._processId = processId;
		global::Interop.User32.EnumWindows((delegate* unmanaged<nint, nint, global::Interop.BOOL>)(delegate*<nint, nint, global::Interop.BOOL>)(&EnumWindowsCallback), (nint)(&mainWindowFinder));
		return mainWindowFinder._bestHandle;
	}

	private static bool IsMainWindow(nint handle)
	{
		if (global::Interop.User32.GetWindow(handle, 4) == IntPtr.Zero)
		{
			return global::Interop.User32.IsWindowVisible(handle) != global::Interop.BOOL.FALSE;
		}
		return false;
	}

	[UnmanagedCallersOnly]
	private unsafe static global::Interop.BOOL EnumWindowsCallback(nint handle, nint extraParameter)
	{
		int num = 0;
		global::Interop.User32.GetWindowThreadProcessId(handle, &num);
		if (num == ((MainWindowFinder*)extraParameter)->_processId && IsMainWindow(handle))
		{
			((MainWindowFinder*)extraParameter)->_bestHandle = handle;
			return global::Interop.BOOL.FALSE;
		}
		return global::Interop.BOOL.TRUE;
	}
}
