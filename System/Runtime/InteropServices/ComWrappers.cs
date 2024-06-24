using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices;

[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[CLSCompliant(false)]
public abstract class ComWrappers
{
	public struct ComInterfaceDispatch
	{
		private struct ComInterfaceInstance
		{
			public nint GcHandle;
		}

		public nint Vtable;

		public unsafe static T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
		{
			long num = ((sizeof(void*) == 8) ? 64 : 16);
			long num2 = ~(num - 1);
			ComInterfaceInstance* ptr = *(ComInterfaceInstance**)((ulong)dispatchPtr & (ulong)num2);
			return Unsafe.As<T>(GCHandle.InternalGet(ptr->GcHandle));
		}
	}

	public struct ComInterfaceEntry
	{
		public Guid IID;

		public nint Vtable;
	}

	private static ComWrappers s_globalInstanceForTrackerSupport;

	private static ComWrappers s_globalInstanceForMarshalling;

	private static long s_instanceCounter;

	private readonly long id = Interlocked.Increment(ref s_instanceCounter);

	public static bool TryGetComInstance(object obj, out nint unknown)
	{
		if (obj == null)
		{
			unknown = IntPtr.Zero;
			return false;
		}
		return TryGetComInstanceInternal(ObjectHandleOnStack.Create(ref obj), out unknown);
	}

	[LibraryImport("QCall", EntryPoint = "ComWrappers_TryGetComInstance")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool TryGetComInstanceInternal(ObjectHandleOnStack wrapperMaybe, out nint externalComObject)
	{
		Unsafe.SkipInit<nint>(out externalComObject);
		int num;
		fixed (nint* _externalComObject_native = &externalComObject)
		{
			num = __PInvoke(wrapperMaybe, _externalComObject_native);
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "ComWrappers_TryGetComInstance", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ObjectHandleOnStack __wrapperMaybe_native, nint* __externalComObject_native);
	}

	public static bool TryGetObject(nint unknown, [NotNullWhen(true)] out object? obj)
	{
		obj = null;
		if (unknown == IntPtr.Zero)
		{
			return false;
		}
		return TryGetObjectInternal(unknown, ObjectHandleOnStack.Create(ref obj));
	}

	[LibraryImport("QCall", EntryPoint = "ComWrappers_TryGetObject")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool TryGetObjectInternal(nint wrapperMaybe, ObjectHandleOnStack instance)
	{
		int num = __PInvoke(wrapperMaybe, instance);
		return num != 0;
		[DllImport("QCall", EntryPoint = "ComWrappers_TryGetObject", ExactSpelling = true)]
		static extern int __PInvoke(nint __wrapperMaybe_native, ObjectHandleOnStack __instance_native);
	}

	public nint GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
	{
		if (!TryGetOrCreateComInterfaceForObjectInternal(this, instance, flags, out var retValue))
		{
			throw new ArgumentException(null, "instance");
		}
		return retValue;
	}

	private static bool TryGetOrCreateComInterfaceForObjectInternal(ComWrappers impl, object instance, CreateComInterfaceFlags flags, out nint retValue)
	{
		ArgumentNullException.ThrowIfNull(instance, "instance");
		return TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack.Create(ref impl), impl.id, ObjectHandleOnStack.Create(ref instance), flags, out retValue);
	}

	[LibraryImport("QCall", EntryPoint = "ComWrappers_TryGetOrCreateComInterfaceForObject")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, ObjectHandleOnStack instance, CreateComInterfaceFlags flags, out nint retValue)
	{
		Unsafe.SkipInit<nint>(out retValue);
		int num;
		fixed (nint* _retValue_native = &retValue)
		{
			num = __PInvoke(comWrappersImpl, wrapperId, instance, flags, _retValue_native);
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "ComWrappers_TryGetOrCreateComInterfaceForObject", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ObjectHandleOnStack __comWrappersImpl_native, long __wrapperId_native, ObjectHandleOnStack __instance_native, CreateComInterfaceFlags __flags_native, nint* __retValue_native);
	}

	internal unsafe static void* CallComputeVtables(ComWrappersScenario scenario, ComWrappers comWrappersImpl, object obj, CreateComInterfaceFlags flags, out int count)
	{
		ComWrappers comWrappers = null;
		switch (scenario)
		{
		case ComWrappersScenario.Instance:
			comWrappers = comWrappersImpl;
			break;
		case ComWrappersScenario.TrackerSupportGlobalInstance:
			comWrappers = s_globalInstanceForTrackerSupport;
			break;
		case ComWrappersScenario.MarshallingGlobalInstance:
			comWrappers = s_globalInstanceForMarshalling;
			break;
		}
		if (comWrappers == null)
		{
			count = -1;
			return null;
		}
		return comWrappers.ComputeVtables(obj, flags, out count);
	}

	public object GetOrCreateObjectForComInstance(nint externalComObject, CreateObjectFlags flags)
	{
		if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, IntPtr.Zero, flags, null, out var retValue))
		{
			throw new ArgumentNullException("externalComObject");
		}
		return retValue;
	}

	internal static object CallCreateObject(ComWrappersScenario scenario, ComWrappers comWrappersImpl, nint externalComObject, CreateObjectFlags flags)
	{
		ComWrappers comWrappers = null;
		switch (scenario)
		{
		case ComWrappersScenario.Instance:
			comWrappers = comWrappersImpl;
			break;
		case ComWrappersScenario.TrackerSupportGlobalInstance:
			comWrappers = s_globalInstanceForTrackerSupport;
			break;
		case ComWrappersScenario.MarshallingGlobalInstance:
			comWrappers = s_globalInstanceForMarshalling;
			break;
		}
		return comWrappers?.CreateObject(externalComObject, flags);
	}

	public object GetOrRegisterObjectForComInstance(nint externalComObject, CreateObjectFlags flags, object wrapper)
	{
		return GetOrRegisterObjectForComInstance(externalComObject, flags, wrapper, IntPtr.Zero);
	}

	public object GetOrRegisterObjectForComInstance(nint externalComObject, CreateObjectFlags flags, object wrapper, nint inner)
	{
		ArgumentNullException.ThrowIfNull(wrapper, "wrapper");
		if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, inner, flags, wrapper, out var retValue))
		{
			throw new ArgumentNullException("externalComObject");
		}
		return retValue;
	}

	private static bool TryGetOrCreateObjectForComInstanceInternal(ComWrappers impl, nint externalComObject, nint innerMaybe, CreateObjectFlags flags, object wrapperMaybe, out object retValue)
	{
		ArgumentNullException.ThrowIfNull(externalComObject, "externalComObject");
		if (innerMaybe != IntPtr.Zero && !flags.HasFlag(CreateObjectFlags.Aggregation))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SuppliedInnerMustBeMarkedAggregation);
		}
		object o = wrapperMaybe;
		retValue = null;
		return TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack.Create(ref impl), impl.id, externalComObject, innerMaybe, flags, ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref retValue));
	}

	[LibraryImport("QCall", EntryPoint = "ComWrappers_TryGetOrCreateObjectForComInstance")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, nint externalComObject, nint innerMaybe, CreateObjectFlags flags, ObjectHandleOnStack wrapper, ObjectHandleOnStack retValue)
	{
		int num = __PInvoke(comWrappersImpl, wrapperId, externalComObject, innerMaybe, flags, wrapper, retValue);
		return num != 0;
		[DllImport("QCall", EntryPoint = "ComWrappers_TryGetOrCreateObjectForComInstance", ExactSpelling = true)]
		static extern int __PInvoke(ObjectHandleOnStack __comWrappersImpl_native, long __wrapperId_native, nint __externalComObject_native, nint __innerMaybe_native, CreateObjectFlags __flags_native, ObjectHandleOnStack __wrapper_native, ObjectHandleOnStack __retValue_native);
	}

	internal static void CallReleaseObjects(ComWrappers comWrappersImpl, IEnumerable objects)
	{
		(comWrappersImpl ?? s_globalInstanceForTrackerSupport).ReleaseObjects(objects);
	}

	public static void RegisterForTrackerSupport(ComWrappers instance)
	{
		ArgumentNullException.ThrowIfNull(instance, "instance");
		if (Interlocked.CompareExchange(ref s_globalInstanceForTrackerSupport, instance, null) != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
		}
		SetGlobalInstanceRegisteredForTrackerSupport(instance.id);
	}

	[DllImport("QCall", EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForTrackerSupport", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForTrackerSupport")]
	[SuppressGCTransition]
	private static extern void SetGlobalInstanceRegisteredForTrackerSupport(long id);

	[SupportedOSPlatform("windows")]
	public static void RegisterForMarshalling(ComWrappers instance)
	{
		ArgumentNullException.ThrowIfNull(instance, "instance");
		if (Interlocked.CompareExchange(ref s_globalInstanceForMarshalling, instance, null) != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
		}
		SetGlobalInstanceRegisteredForMarshalling(instance.id);
	}

	[DllImport("QCall", EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForMarshalling", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ComWrappers_SetGlobalInstanceRegisteredForMarshalling")]
	[SuppressGCTransition]
	private static extern void SetGlobalInstanceRegisteredForMarshalling(long id);

	public static void GetIUnknownImpl(out nint fpQueryInterface, out nint fpAddRef, out nint fpRelease)
	{
		GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);
	}

	[LibraryImport("QCall", EntryPoint = "ComWrappers_GetIUnknownImpl")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetIUnknownImplInternal(out nint fpQueryInterface, out nint fpAddRef, out nint fpRelease)
	{
		Unsafe.SkipInit<nint>(out fpQueryInterface);
		Unsafe.SkipInit<nint>(out fpAddRef);
		Unsafe.SkipInit<nint>(out fpRelease);
		fixed (nint* _fpRelease_native = &fpRelease)
		{
			fixed (nint* _fpAddRef_native = &fpAddRef)
			{
				fixed (nint* _fpQueryInterface_native = &fpQueryInterface)
				{
					__PInvoke(_fpQueryInterface_native, _fpAddRef_native, _fpRelease_native);
				}
			}
		}
		[DllImport("QCall", EntryPoint = "ComWrappers_GetIUnknownImpl", ExactSpelling = true)]
		static extern unsafe void __PInvoke(nint* __fpQueryInterface_native, nint* __fpAddRef_native, nint* __fpRelease_native);
	}

	internal static int CallICustomQueryInterface(object customQueryInterfaceMaybe, ref Guid iid, out nint ppObject)
	{
		if (!(customQueryInterfaceMaybe is ICustomQueryInterface customQueryInterface))
		{
			ppObject = IntPtr.Zero;
			return -1;
		}
		return (int)customQueryInterface.GetInterface(ref iid, out ppObject);
	}

	protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

	protected abstract object? CreateObject(nint externalComObject, CreateObjectFlags flags);

	protected abstract void ReleaseObjects(IEnumerable objects);
}
