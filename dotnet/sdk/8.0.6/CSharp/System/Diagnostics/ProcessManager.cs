using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics;

internal static class ProcessManager
{
	public static bool IsRemoteMachine(string machineName)
	{
		ArgumentException.ThrowIfNullOrEmpty(machineName, "machineName");
		return IsRemoteMachineCore(machineName);
	}

	public static bool IsProcessRunning(int processId)
	{
		return IsProcessRunning(processId, ".");
	}

	public static bool IsProcessRunning(int processId, string machineName)
	{
		if (processId != 0 && !IsRemoteMachine(machineName))
		{
			using SafeProcessHandle safeProcessHandle = global::Interop.Kernel32.OpenProcess(1052672, inherit: false, processId);
			if (!safeProcessHandle.IsInvalid)
			{
				bool signaled = false;
				int exitCode;
				return !HasExited(safeProcessHandle, ref signaled, out exitCode);
			}
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == 87)
			{
				return false;
			}
		}
		return Array.IndexOf(GetProcessIds(machineName), processId) >= 0;
	}

	public static ProcessInfo[] GetProcessInfos(string processNameFilter, string machineName)
	{
		if (!IsRemoteMachine(machineName))
		{
			return NtProcessInfoHelper.GetProcessInfos(null, processNameFilter);
		}
		ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
		if (string.IsNullOrEmpty(processNameFilter))
		{
			return processInfos;
		}
		System.Collections.Generic.ArrayBuilder<ProcessInfo> arrayBuilder = default(System.Collections.Generic.ArrayBuilder<ProcessInfo>);
		ProcessInfo[] array = processInfos;
		foreach (ProcessInfo processInfo in array)
		{
			if (string.Equals(processNameFilter, processInfo.ProcessName, StringComparison.OrdinalIgnoreCase))
			{
				arrayBuilder.Add(processInfo);
			}
		}
		return arrayBuilder.ToArray();
	}

	public static ProcessInfo GetProcessInfo(int processId, string machineName)
	{
		if (IsRemoteMachine(machineName))
		{
			ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
			ProcessInfo[] array = processInfos;
			foreach (ProcessInfo processInfo in array)
			{
				if (processInfo.ProcessId == processId)
				{
					return processInfo;
				}
			}
		}
		else
		{
			ProcessInfo[] processInfos2 = NtProcessInfoHelper.GetProcessInfos(processId);
			if (processInfos2.Length == 1)
			{
				return processInfos2[0];
			}
		}
		return null;
	}

	public static string GetProcessName(int processId, string machineName)
	{
		if (IsRemoteMachine(machineName))
		{
			ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
			ProcessInfo[] array = processInfos;
			foreach (ProcessInfo processInfo in array)
			{
				if (processInfo.ProcessId == processId)
				{
					return processInfo.ProcessName;
				}
			}
		}
		else
		{
			string processName = global::Interop.Kernel32.GetProcessName((uint)processId);
			if (processName != null)
			{
				ReadOnlySpan<char> processShortName = NtProcessInfoHelper.GetProcessShortName(processName);
				if (!processShortName.SequenceEqual(processName))
				{
					return processShortName.ToString();
				}
				return processName;
			}
		}
		return null;
	}

	public static int[] GetProcessIds(string machineName)
	{
		if (!IsRemoteMachine(machineName))
		{
			return GetProcessIds();
		}
		return NtProcessManager.GetProcessIds(machineName, isRemoteMachine: true);
	}

	public static int[] GetProcessIds()
	{
		return NtProcessManager.GetProcessIds();
	}

	public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
	{
		return NtProcessManager.GetProcessIdFromHandle(processHandle);
	}

	public static ProcessModuleCollection GetModules(int processId)
	{
		return NtProcessManager.GetModules(processId);
	}

	private static bool IsRemoteMachineCore(string machineName)
	{
		ReadOnlySpan<char> span = machineName.AsSpan(machineName.StartsWith('\\') ? 2 : 0);
		if (!span.SequenceEqual(".".AsSpan()))
		{
			return !MemoryExtensions.Equals(span, global::Interop.Kernel32.GetComputerName(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	unsafe static ProcessManager()
	{
		if (!global::Interop.Advapi32.LookupPrivilegeValue(null, "SeDebugPrivilege", out var lpLuid))
		{
			return;
		}
		Microsoft.Win32.SafeHandles.SafeTokenHandle TokenHandle = null;
		try
		{
			if (global::Interop.Advapi32.OpenProcessToken(global::Interop.Kernel32.GetCurrentProcess(), 32, out TokenHandle))
			{
				Unsafe.SkipInit(out global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE);
				tOKEN_PRIVILEGE.PrivilegeCount = 1u;
				tOKEN_PRIVILEGE.Privileges.Luid = lpLuid;
				tOKEN_PRIVILEGE.Privileges.Attributes = 2u;
				global::Interop.Advapi32.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges: false, &tOKEN_PRIVILEGE, 0u, null, null);
			}
		}
		finally
		{
			TokenHandle?.Dispose();
		}
	}

	public static SafeProcessHandle OpenProcess(int processId, int access, bool throwIfExited)
	{
		SafeProcessHandle safeProcessHandle = global::Interop.Kernel32.OpenProcess(access, inherit: false, processId);
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (!safeProcessHandle.IsInvalid)
		{
			return safeProcessHandle;
		}
		safeProcessHandle.Dispose();
		if (processId == 0)
		{
			throw new Win32Exception(5);
		}
		if (lastWin32Error != 5 && !IsProcessRunning(processId))
		{
			if (throwIfExited)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ProcessHasExited, processId.ToString()));
			}
			return SafeProcessHandle.InvalidHandle;
		}
		throw new Win32Exception(lastWin32Error);
	}

	public static Microsoft.Win32.SafeHandles.SafeThreadHandle OpenThread(int threadId, int access)
	{
		Microsoft.Win32.SafeHandles.SafeThreadHandle safeThreadHandle = global::Interop.Kernel32.OpenThread(access, bInheritHandle: false, threadId);
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (safeThreadHandle.IsInvalid)
		{
			safeThreadHandle.Dispose();
			if (lastWin32Error == 87)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ThreadExited, threadId.ToString()));
			}
			throw new Win32Exception(lastWin32Error);
		}
		return safeThreadHandle;
	}

	public static bool HasExited(SafeProcessHandle handle, ref bool signaled, out int exitCode)
	{
		if (global::Interop.Kernel32.GetExitCodeProcess(handle, out exitCode) && exitCode != 259)
		{
			return true;
		}
		if (!signaled)
		{
			using global::Interop.Kernel32.ProcessWaitHandle processWaitHandle = new global::Interop.Kernel32.ProcessWaitHandle(handle);
			signaled = processWaitHandle.WaitOne(0);
		}
		if (signaled)
		{
			if (!global::Interop.Kernel32.GetExitCodeProcess(handle, out exitCode))
			{
				throw new Win32Exception();
			}
			return true;
		}
		return false;
	}

	public static nint GetMainWindowHandle(int processId)
	{
		return MainWindowFinder.FindMainWindow(processId);
	}
}
