using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System;

public static class GC
{
	internal enum GC_ALLOC_FLAGS
	{
		GC_ALLOC_NO_FLAGS = 0,
		GC_ALLOC_ZEROING_OPTIONAL = 0x10,
		GC_ALLOC_PINNED_OBJECT_HEAP = 0x40
	}

	private enum StartNoGCRegionStatus
	{
		Succeeded,
		NotEnoughMemory,
		AmountTooLarge,
		AlreadyInProgress
	}

	private enum EndNoGCRegionStatus
	{
		Succeeded,
		NotInProgress,
		GCInduced,
		AllocationExceeded
	}

	private struct NoGCRegionCallbackFinalizerWorkItem
	{
		public unsafe NoGCRegionCallbackFinalizerWorkItem* next;

		public unsafe delegate* unmanaged<NoGCRegionCallbackFinalizerWorkItem*, void> callback;

		public bool scheduled;

		public bool abandoned;

		public GCHandle action;
	}

	internal enum EnableNoGCRegionCallbackStatus
	{
		Success,
		NotStarted,
		InsufficientBudget,
		AlreadyRegistered
	}

	internal struct GCConfigurationContext
	{
		internal Dictionary<string, object> Configurations;
	}

	internal enum GCConfigurationType
	{
		Int64,
		StringUtf8,
		Boolean
	}

	internal enum RefreshMemoryStatus
	{
		Succeeded,
		HardLimitTooLow,
		HardLimitInvalid
	}

	internal struct GCHeapHardLimitInfo
	{
		internal ulong HeapHardLimit;

		internal ulong HeapHardLimitPercent;

		internal ulong HeapHardLimitSOH;

		internal ulong HeapHardLimitLOH;

		internal ulong HeapHardLimitPOH;

		internal ulong HeapHardLimitSOHPercent;

		internal ulong HeapHardLimitLOHPercent;

		internal ulong HeapHardLimitPOHPercent;
	}

	public static int MaxGeneration => GetMaxGeneration();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetMemoryInfo(GCMemoryInfoData data, int kind);

	public static GCMemoryInfo GetGCMemoryInfo()
	{
		return GetGCMemoryInfo(GCKind.Any);
	}

	public static GCMemoryInfo GetGCMemoryInfo(GCKind kind)
	{
		if (kind < GCKind.Any || kind > GCKind.Background)
		{
			throw new ArgumentOutOfRangeException("kind", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, GCKind.Any, GCKind.Background));
		}
		GCMemoryInfoData data = new GCMemoryInfoData();
		GetMemoryInfo(data, (int)kind);
		return new GCMemoryInfo(data);
	}

	[LibraryImport("QCall", EntryPoint = "GCInterface_StartNoGCRegion")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal static int _StartNoGCRegion(long totalSize, [MarshalAs(UnmanagedType.Bool)] bool lohSizeKnown, long lohSize, [MarshalAs(UnmanagedType.Bool)] bool disallowFullBlockingGC)
	{
		int _disallowFullBlockingGC_native = (disallowFullBlockingGC ? 1 : 0);
		int _lohSizeKnown_native = (lohSizeKnown ? 1 : 0);
		return __PInvoke(totalSize, _lohSizeKnown_native, lohSize, _disallowFullBlockingGC_native);
		[DllImport("QCall", EntryPoint = "GCInterface_StartNoGCRegion", ExactSpelling = true)]
		static extern int __PInvoke(long __totalSize_native, int __lohSizeKnown_native, long __lohSize_native, int __disallowFullBlockingGC_native);
	}

	[DllImport("QCall", EntryPoint = "GCInterface_EndNoGCRegion", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_EndNoGCRegion")]
	internal static extern int _EndNoGCRegion();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Array AllocateNewArray(nint typeHandle, int length, GC_ALLOC_FLAGS flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetGenerationWR(nint handle);

	[DllImport("QCall", EntryPoint = "GCInterface_GetTotalMemory", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_GetTotalMemory")]
	private static extern long GetTotalMemory();

	[DllImport("QCall", EntryPoint = "GCInterface_Collect", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_Collect")]
	private static extern void _Collect(int generation, int mode);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetMaxGeneration();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _CollectionCount(int generation, int getSpecialGCCount);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern ulong GetSegmentSize();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetLastGCPercentTimeInGC();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern ulong GetGenerationSize(int gen);

	[DllImport("QCall", EntryPoint = "GCInterface_AddMemoryPressure", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_AddMemoryPressure")]
	private static extern void _AddMemoryPressure(ulong bytesAllocated);

	[DllImport("QCall", EntryPoint = "GCInterface_RemoveMemoryPressure", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_RemoveMemoryPressure")]
	private static extern void _RemoveMemoryPressure(ulong bytesAllocated);

	public static void AddMemoryPressure(long bytesAllocated)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated, "bytesAllocated");
		if (8 == 4)
		{
		}
		_AddMemoryPressure((ulong)bytesAllocated);
	}

	public static void RemoveMemoryPressure(long bytesAllocated)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated, "bytesAllocated");
		if (8 == 4)
		{
		}
		_RemoveMemoryPressure((ulong)bytesAllocated);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetGeneration(object obj);

	public static void Collect(int generation)
	{
		Collect(generation, GCCollectionMode.Default);
	}

	public static void Collect()
	{
		_Collect(-1, 2);
	}

	public static void Collect(int generation, GCCollectionMode mode)
	{
		Collect(generation, mode, blocking: true);
	}

	public static void Collect(int generation, GCCollectionMode mode, bool blocking)
	{
		bool compacting = generation == MaxGeneration && mode == GCCollectionMode.Aggressive;
		Collect(generation, mode, blocking, compacting);
	}

	public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(generation, "generation");
		if (mode < GCCollectionMode.Default || mode > GCCollectionMode.Aggressive)
		{
			throw new ArgumentOutOfRangeException("mode", SR.ArgumentOutOfRange_Enum);
		}
		int num = 0;
		switch (mode)
		{
		case GCCollectionMode.Optimized:
			num |= 4;
			break;
		case GCCollectionMode.Aggressive:
			num |= 0x10;
			if (generation != MaxGeneration)
			{
				throw new ArgumentException(SR.Argument_AggressiveGCRequiresMaxGeneration, "generation");
			}
			if (!blocking)
			{
				throw new ArgumentException(SR.Argument_AggressiveGCRequiresBlocking, "blocking");
			}
			if (!compacting)
			{
				throw new ArgumentException(SR.Argument_AggressiveGCRequiresCompacting, "compacting");
			}
			break;
		}
		if (compacting)
		{
			num |= 8;
		}
		if (blocking)
		{
			num |= 2;
		}
		else if (!compacting)
		{
			num |= 1;
		}
		_Collect(generation, num);
	}

	public static int CollectionCount(int generation)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(generation, "generation");
		return _CollectionCount(generation, 0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Intrinsic]
	public static void KeepAlive(object? obj)
	{
	}

	public static int GetGeneration(WeakReference wo)
	{
		int generationWR = GetGenerationWR(wo.WeakHandle);
		KeepAlive(wo);
		return generationWR;
	}

	[DllImport("QCall", EntryPoint = "GCInterface_WaitForPendingFinalizers", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_WaitForPendingFinalizers")]
	private static extern void _WaitForPendingFinalizers();

	public static void WaitForPendingFinalizers()
	{
		_WaitForPendingFinalizers();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _SuppressFinalize(object o);

	public static void SuppressFinalize(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		_SuppressFinalize(obj);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _ReRegisterForFinalize(object o);

	public static void ReRegisterForFinalize(object obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		_ReRegisterForFinalize(obj);
	}

	public static long GetTotalMemory(bool forceFullCollection)
	{
		long totalMemory = GetTotalMemory();
		if (!forceFullCollection)
		{
			return totalMemory;
		}
		int num = 20;
		long num2 = totalMemory;
		float num3;
		do
		{
			WaitForPendingFinalizers();
			Collect();
			totalMemory = num2;
			num2 = GetTotalMemory();
			num3 = (float)(num2 - totalMemory) / (float)totalMemory;
		}
		while (num-- > 0 && (!(-0.05 < (double)num3) || !((double)num3 < 0.05)));
		return num2;
	}

	[DllImport("QCall", EntryPoint = "GCInterface_RegisterFrozenSegment", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_RegisterFrozenSegment")]
	private static extern nint _RegisterFrozenSegment(nint sectionAddress, nint sectionSize);

	[DllImport("QCall", EntryPoint = "GCInterface_UnregisterFrozenSegment", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_UnregisterFrozenSegment")]
	private static extern void _UnregisterFrozenSegment(nint segmentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetAllocatedBytesForCurrentThread();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetTotalAllocatedBytes(bool precise = false);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _CancelFullGCNotification();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _WaitForFullGCApproach(int millisecondsTimeout);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _WaitForFullGCComplete(int millisecondsTimeout);

	public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
	{
		if (maxGenerationThreshold <= 0 || maxGenerationThreshold >= 100)
		{
			throw new ArgumentOutOfRangeException("maxGenerationThreshold", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
		}
		if (largeObjectHeapThreshold <= 0 || largeObjectHeapThreshold >= 100)
		{
			throw new ArgumentOutOfRangeException("largeObjectHeapThreshold", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
		}
		if (!_RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
		}
	}

	public static void CancelFullGCNotification()
	{
		if (!_CancelFullGCNotification())
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
		}
	}

	public static GCNotificationStatus WaitForFullGCApproach()
	{
		return (GCNotificationStatus)_WaitForFullGCApproach(-1);
	}

	public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1, "millisecondsTimeout");
		return (GCNotificationStatus)_WaitForFullGCApproach(millisecondsTimeout);
	}

	public static GCNotificationStatus WaitForFullGCComplete()
	{
		return (GCNotificationStatus)_WaitForFullGCComplete(-1);
	}

	public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1, "millisecondsTimeout");
		return (GCNotificationStatus)_WaitForFullGCComplete(millisecondsTimeout);
	}

	private static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSize, "totalSize");
		if (hasLohSize)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lohSize, "lohSize");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(lohSize, totalSize, "lohSize");
		}
		return (StartNoGCRegionStatus)_StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC) switch
		{
			StartNoGCRegionStatus.NotEnoughMemory => false, 
			StartNoGCRegionStatus.AlreadyInProgress => throw new InvalidOperationException(SR.InvalidOperationException_AlreadyInNoGCRegion), 
			StartNoGCRegionStatus.AmountTooLarge => throw new ArgumentOutOfRangeException("totalSize", SR.ArgumentOutOfRangeException_NoGCRegionSizeTooLarge), 
			_ => true, 
		};
	}

	public static bool TryStartNoGCRegion(long totalSize)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: false, 0L, disallowFullBlockingGC: false);
	}

	public static bool TryStartNoGCRegion(long totalSize, long lohSize)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: true, lohSize, disallowFullBlockingGC: false);
	}

	public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: false, 0L, disallowFullBlockingGC);
	}

	public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: true, lohSize, disallowFullBlockingGC);
	}

	public static void EndNoGCRegion()
	{
		switch ((EndNoGCRegionStatus)_EndNoGCRegion())
		{
		case EndNoGCRegionStatus.NotInProgress:
			throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionNotInProgress);
		case EndNoGCRegionStatus.GCInduced:
			throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionInduced);
		case EndNoGCRegionStatus.AllocationExceeded:
			throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionAllocationExceeded);
		}
	}

	public unsafe static void RegisterNoGCRegionCallback(long totalSize, Action callback)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSize, "totalSize");
		ArgumentNullException.ThrowIfNull(callback, "callback");
		NoGCRegionCallbackFinalizerWorkItem* ptr = null;
		try
		{
			ptr = (NoGCRegionCallbackFinalizerWorkItem*)NativeMemory.AllocZeroed((nuint)sizeof(NoGCRegionCallbackFinalizerWorkItem));
			ptr->action = GCHandle.Alloc(callback);
			ptr->callback = (delegate* unmanaged<NoGCRegionCallbackFinalizerWorkItem*, void>)(delegate*<NoGCRegionCallbackFinalizerWorkItem*, void>)(&Callback);
			ptr = _EnableNoGCRegionCallback(ptr, totalSize) switch
			{
				EnableNoGCRegionCallbackStatus.NotStarted => throw new InvalidOperationException(SR.Format(SR.InvalidOperationException_NoGCRegionNotInProgress)), 
				EnableNoGCRegionCallbackStatus.InsufficientBudget => throw new InvalidOperationException(SR.Format(SR.InvalidOperationException_NoGCRegionAllocationExceeded)), 
				EnableNoGCRegionCallbackStatus.AlreadyRegistered => throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionCallbackAlreadyRegistered), 
				_ => null, 
			};
		}
		finally
		{
			if (ptr != null)
			{
				Free(ptr);
			}
		}
		[UnmanagedCallersOnly]
		static unsafe void Callback(NoGCRegionCallbackFinalizerWorkItem* pWorkItem)
		{
			if (!pWorkItem->abandoned)
			{
				((Action)pWorkItem->action.Target)();
			}
			Free(pWorkItem);
		}
		unsafe static void Free(NoGCRegionCallbackFinalizerWorkItem* pWorkItem)
		{
			if (pWorkItem->action.IsAllocated)
			{
				pWorkItem->action.Free();
			}
			NativeMemory.Free(pWorkItem);
		}
	}

	[DllImport("QCall", EntryPoint = "GCInterface_EnableNoGCRegionCallback", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_EnableNoGCRegionCallback")]
	private unsafe static extern EnableNoGCRegionCallbackStatus _EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, long totalSize);

	internal static long GetGenerationBudget(int generation)
	{
		return _GetGenerationBudget(generation);
	}

	[DllImport("QCall", EntryPoint = "GCInterface_GetGenerationBudget", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_GetGenerationBudget")]
	internal static extern long _GetGenerationBudget(int generation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] AllocateUninitializedArray<T>(int length, bool pinned = false)
	{
		if (!pinned)
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				return new T[length];
			}
			if (length < 2048 / Unsafe.SizeOf<T>())
			{
				return new T[length];
			}
		}
		GC_ALLOC_FLAGS gC_ALLOC_FLAGS = GC_ALLOC_FLAGS.GC_ALLOC_ZEROING_OPTIONAL;
		if (pinned)
		{
			gC_ALLOC_FLAGS |= GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
		}
		return Unsafe.As<T[]>(AllocateNewArray(RuntimeTypeHandle.ToIntPtr(typeof(T[]).TypeHandle), length, gC_ALLOC_FLAGS));
	}

	public static T[] AllocateArray<T>(int length, bool pinned = false)
	{
		GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_NO_FLAGS;
		if (pinned)
		{
			flags = GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
		}
		return Unsafe.As<T[]>(AllocateNewArray(RuntimeTypeHandle.ToIntPtr(typeof(T[]).TypeHandle), length, flags));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern long _GetTotalPauseDuration();

	public static TimeSpan GetTotalPauseDuration()
	{
		return new TimeSpan(_GetTotalPauseDuration());
	}

	[UnmanagedCallersOnly]
	private unsafe static void ConfigCallback(void* configurationContext, void* name, void* publicKey, GCConfigurationType type, long data)
	{
		if (publicKey != null)
		{
			Dictionary<string, object> configurations = Unsafe.As<byte, GCConfigurationContext>(ref *(byte*)configurationContext).Configurations;
			string key = Marshal.PtrToStringUTF8((nint)name);
			switch (type)
			{
			case GCConfigurationType.Int64:
				configurations[key] = data;
				break;
			case GCConfigurationType.StringUtf8:
			{
				string text = Marshal.PtrToStringUTF8((nint)data);
				configurations[key] = text ?? string.Empty;
				break;
			}
			case GCConfigurationType.Boolean:
				configurations[key] = data != 0;
				break;
			}
		}
	}

	public unsafe static IReadOnlyDictionary<string, object> GetConfigurationVariables()
	{
		GCConfigurationContext gCConfigurationContext = default(GCConfigurationContext);
		gCConfigurationContext.Configurations = new Dictionary<string, object>();
		GCConfigurationContext value = gCConfigurationContext;
		_EnumerateConfigurationValues(Unsafe.AsPointer(ref value), (delegate* unmanaged<void*, void*, void*, GCConfigurationType, long, void>)(delegate*<void*, void*, void*, GCConfigurationType, long, void>)(&ConfigCallback));
		return value.Configurations;
	}

	[DllImport("QCall", EntryPoint = "GCInterface_EnumerateConfigurationValues", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_EnumerateConfigurationValues")]
	internal unsafe static extern void _EnumerateConfigurationValues(void* configurationDictionary, delegate* unmanaged<void*, void*, void*, GCConfigurationType, long, void> callback);

	public static void RefreshMemoryLimit()
	{
		ulong heapHardLimit = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimit") as ulong?)) ?? (-1L));
		ulong heapHardLimitPercent = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitPercent") as ulong?)) ?? (-1L));
		ulong heapHardLimitSOH = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitSOH") as ulong?)) ?? (-1L));
		ulong heapHardLimitLOH = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitLOH") as ulong?)) ?? (-1L));
		ulong heapHardLimitPOH = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitPOH") as ulong?)) ?? (-1L));
		ulong heapHardLimitSOHPercent = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitSOHPercent") as ulong?)) ?? (-1L));
		ulong heapHardLimitLOHPercent = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitLOHPercent") as ulong?)) ?? (-1L));
		ulong heapHardLimitPOHPercent = (ulong)(((long?)(AppContext.GetData("GCHeapHardLimitPOHPercent") as ulong?)) ?? (-1L));
		GCHeapHardLimitInfo gCHeapHardLimitInfo = default(GCHeapHardLimitInfo);
		gCHeapHardLimitInfo.HeapHardLimit = heapHardLimit;
		gCHeapHardLimitInfo.HeapHardLimitPercent = heapHardLimitPercent;
		gCHeapHardLimitInfo.HeapHardLimitSOH = heapHardLimitSOH;
		gCHeapHardLimitInfo.HeapHardLimitLOH = heapHardLimitLOH;
		gCHeapHardLimitInfo.HeapHardLimitPOH = heapHardLimitPOH;
		gCHeapHardLimitInfo.HeapHardLimitSOHPercent = heapHardLimitSOHPercent;
		gCHeapHardLimitInfo.HeapHardLimitLOHPercent = heapHardLimitLOHPercent;
		gCHeapHardLimitInfo.HeapHardLimitPOHPercent = heapHardLimitPOHPercent;
		GCHeapHardLimitInfo heapHardLimitInfo = gCHeapHardLimitInfo;
		switch ((RefreshMemoryStatus)_RefreshMemoryLimit(heapHardLimitInfo))
		{
		case RefreshMemoryStatus.HardLimitTooLow:
			throw new InvalidOperationException(SR.InvalidOperationException_HardLimitTooLow);
		case RefreshMemoryStatus.HardLimitInvalid:
			throw new InvalidOperationException(SR.InvalidOperationException_HardLimitInvalid);
		}
	}

	[DllImport("QCall", EntryPoint = "GCInterface_RefreshMemoryLimit", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "GCInterface_RefreshMemoryLimit")]
	internal static extern int _RefreshMemoryLimit(GCHeapHardLimitInfo heapHardLimitInfo);

	public static GCNotificationStatus WaitForFullGCApproach(TimeSpan timeout)
	{
		return WaitForFullGCApproach(WaitHandle.ToTimeoutMilliseconds(timeout));
	}

	public static GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout)
	{
		return WaitForFullGCComplete(WaitHandle.ToTimeoutMilliseconds(timeout));
	}
}
