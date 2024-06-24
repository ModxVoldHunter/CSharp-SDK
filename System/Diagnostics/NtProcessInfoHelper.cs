using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

internal static class NtProcessInfoHelper
{
	private static uint MostRecentSize = 1048576u;

	internal unsafe static ProcessInfo[] GetProcessInfos(int? processIdFilter = null, string processNameFilter = null)
	{
		uint num = MostRecentSize;
		while (true)
		{
			void* ptr = NativeMemory.Alloc(num);
			try
			{
				uint num2 = 0u;
				uint num3 = global::Interop.NtDll.NtQuerySystemInformation(5, ptr, num, &num2);
				if (num3 != 3221225476u)
				{
					if ((int)num3 < 0)
					{
						throw new InvalidOperationException(System.SR.CouldntGetProcessInfos, new Win32Exception((int)num3));
					}
					MostRecentSize = GetEstimatedBufferSize(num2);
					return GetProcessInfos(new ReadOnlySpan<byte>(ptr, (int)num2), processIdFilter, processNameFilter);
				}
				num = GetEstimatedBufferSize(num2);
			}
			finally
			{
				NativeMemory.Free(ptr);
			}
		}
	}

	private static uint GetEstimatedBufferSize(uint actualSize)
	{
		return actualSize + 10240;
	}

	private unsafe static ProcessInfo[] GetProcessInfos(ReadOnlySpan<byte> data, int? processIdFilter, string processNameFilter)
	{
		Dictionary<int, ProcessInfo> dictionary = new Dictionary<int, ProcessInfo>(60);
		int num = 0;
		while (true)
		{
			ref readonly global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION reference = ref MemoryMarshal.AsRef<global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION>(data.Slice(num));
			int num2 = ((IntPtr)reference.UniqueProcessId).ToInt32();
			if (!processIdFilter.HasValue || processIdFilter.GetValueOrDefault() == num2)
			{
				string text = null;
				ReadOnlySpan<char> span = ((reference.ImageName.Buffer != IntPtr.Zero) ? GetProcessShortName(new ReadOnlySpan<char>(((IntPtr)reference.ImageName.Buffer).ToPointer(), reference.ImageName.Length / 2)) : ((ReadOnlySpan<char>)(text = num2 switch
				{
					0 => "Idle", 
					4 => "System", 
					_ => num2.ToString(CultureInfo.InvariantCulture), 
				})));
				if (string.IsNullOrEmpty(processNameFilter) || MemoryExtensions.Equals(span, processNameFilter, StringComparison.OrdinalIgnoreCase))
				{
					if (text == null)
					{
						text = span.ToString();
					}
					ProcessInfo processInfo = new ProcessInfo((int)reference.NumberOfThreads)
					{
						ProcessName = text,
						ProcessId = num2,
						SessionId = (int)reference.SessionId,
						PoolPagedBytes = (long)reference.QuotaPagedPoolUsage,
						PoolNonPagedBytes = (long)reference.QuotaNonPagedPoolUsage,
						VirtualBytes = (long)reference.VirtualSize,
						VirtualBytesPeak = (long)reference.PeakVirtualSize,
						WorkingSetPeak = (long)reference.PeakWorkingSetSize,
						WorkingSet = (long)reference.WorkingSetSize,
						PageFileBytesPeak = (long)reference.PeakPagefileUsage,
						PageFileBytes = (long)reference.PagefileUsage,
						PrivateBytes = (long)reference.PrivatePageCount,
						BasePriority = reference.BasePriority,
						HandleCount = (int)reference.HandleCount
					};
					dictionary[processInfo.ProcessId] = processInfo;
					int num3 = num + sizeof(global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION);
					for (int i = 0; i < reference.NumberOfThreads; i++)
					{
						ref readonly global::Interop.NtDll.SYSTEM_THREAD_INFORMATION reference2 = ref MemoryMarshal.AsRef<global::Interop.NtDll.SYSTEM_THREAD_INFORMATION>(data.Slice(num3));
						ThreadInfo item = new ThreadInfo
						{
							_processId = (int)reference2.ClientId.UniqueProcess,
							_threadId = (ulong)reference2.ClientId.UniqueThread,
							_basePriority = reference2.BasePriority,
							_currentPriority = reference2.Priority,
							_startAddress = reference2.StartAddress,
							_threadState = (ThreadState)reference2.ThreadState,
							_threadWaitReason = NtProcessManager.GetThreadWaitReason((int)reference2.WaitReason)
						};
						processInfo._threadInfoList.Add(item);
						num3 += sizeof(global::Interop.NtDll.SYSTEM_THREAD_INFORMATION);
					}
				}
			}
			if (reference.NextEntryOffset == 0)
			{
				break;
			}
			num += (int)reference.NextEntryOffset;
		}
		ProcessInfo[] array = new ProcessInfo[dictionary.Values.Count];
		dictionary.Values.CopyTo(array, 0);
		return array;
	}

	internal static ReadOnlySpan<char> GetProcessShortName(ReadOnlySpan<char> name)
	{
		name = name.Slice(name.LastIndexOf('\\') + 1);
		if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			name = name.Slice(0, name.Length - 4);
		}
		return name;
	}
}
