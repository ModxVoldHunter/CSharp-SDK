namespace System.Runtime.InteropServices.Marshalling;

public sealed class ComObject : IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy.IComImportAdapter
{
	private unsafe readonly void* _instancePointer;

	private readonly object _runtimeCallableWrapper;

	internal static bool BuiltInComSupported { get; } = !AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out var isEnabled) || isEnabled;


	internal static bool ComImportInteropEnabled { get; } = AppContext.TryGetSwitch("System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop", out var isEnabled2) && isEnabled2;


	private IIUnknownInterfaceDetailsStrategy InterfaceDetailsStrategy { get; }

	private IIUnknownStrategy IUnknownStrategy { get; }

	private IIUnknownCacheStrategy CacheStrategy { get; }

	internal bool UniqueInstance { get; init; }

	internal unsafe ComObject(IIUnknownInterfaceDetailsStrategy interfaceDetailsStrategy, IIUnknownStrategy iunknownStrategy, IIUnknownCacheStrategy cacheStrategy, void* thisPointer)
	{
		InterfaceDetailsStrategy = interfaceDetailsStrategy;
		IUnknownStrategy = iunknownStrategy;
		CacheStrategy = cacheStrategy;
		_instancePointer = IUnknownStrategy.CreateInstancePointer(thisPointer);
		if (OperatingSystem.IsWindows() && BuiltInComSupported && ComImportInteropEnabled)
		{
			_runtimeCallableWrapper = Marshal.GetObjectForIUnknown((nint)thisPointer);
		}
	}

	unsafe ~ComObject()
	{
		CacheStrategy.Clear(IUnknownStrategy);
		IUnknownStrategy.Release(_instancePointer);
	}

	public unsafe void FinalRelease()
	{
		if (UniqueInstance)
		{
			CacheStrategy.Clear(IUnknownStrategy);
			IUnknownStrategy.Release(_instancePointer);
		}
	}

	RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
	{
		if (!LookUpVTableInfo(interfaceType, out var result, out var qiHResult))
		{
			Marshal.ThrowExceptionForHR(qiHResult);
		}
		return result.ManagedType;
	}

	bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
	{
		if (!LookUpVTableInfo(interfaceType, out var _, out var qiHResult))
		{
			if (throwIfNotImplemented)
			{
				Marshal.ThrowExceptionForHR(qiHResult);
			}
			return false;
		}
		return true;
	}

	private unsafe bool LookUpVTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo result, out int qiHResult)
	{
		qiHResult = 0;
		if (!CacheStrategy.TryGetTableInfo(handle, out result))
		{
			IIUnknownDerivedDetails iUnknownDerivedDetails = InterfaceDetailsStrategy.GetIUnknownDerivedDetails(handle);
			if (iUnknownDerivedDetails == null)
			{
				return false;
			}
			IIUnknownStrategy iUnknownStrategy = IUnknownStrategy;
			void* instancePointer = _instancePointer;
			Guid iid = iUnknownDerivedDetails.Iid;
			void* ppObj;
			int num = iUnknownStrategy.QueryInterface(instancePointer, in iid, out ppObj);
			if (num < 0)
			{
				qiHResult = num;
				return false;
			}
			result = CacheStrategy.ConstructTableInfo(handle, iUnknownDerivedDetails, ppObj);
			if (!CacheStrategy.TrySetTableInfo(handle, result))
			{
				bool flag = CacheStrategy.TryGetTableInfo(handle, out result);
				IUnknownStrategy.Release(ppObj);
			}
		}
		return true;
	}

	unsafe VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
	{
		if (!LookUpVTableInfo(type.TypeHandle, out var result, out var qiHResult))
		{
			Marshal.ThrowExceptionForHR(qiHResult);
		}
		return new VirtualMethodTableInfo(result.ThisPtr, result.Table);
	}

	object System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy.IComImportAdapter.GetRuntimeCallableWrapper()
	{
		return _runtimeCallableWrapper;
	}
}
