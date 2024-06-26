using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.CustomMarshalers;

[SupportedOSPlatform("windows")]
internal sealed class EnumerableToDispatchMarshaler : ICustomMarshaler
{
	private static readonly EnumerableToDispatchMarshaler s_enumerableToDispatchMarshaler = new EnumerableToDispatchMarshaler();

	public static ICustomMarshaler GetInstance(string cookie)
	{
		return s_enumerableToDispatchMarshaler;
	}

	private EnumerableToDispatchMarshaler()
	{
	}

	public void CleanUpManagedData(object ManagedObj)
	{
	}

	public void CleanUpNativeData(nint pNativeData)
	{
		Marshal.Release(pNativeData);
	}

	public int GetNativeDataSize()
	{
		return -1;
	}

	public nint MarshalManagedToNative(object ManagedObj)
	{
		ArgumentNullException.ThrowIfNull(ManagedObj, "ManagedObj");
		return Marshal.GetComInterfaceForObject<object, IEnumerable>(ManagedObj);
	}

	public object MarshalNativeToManaged(nint pNativeData)
	{
		ArgumentNullException.ThrowIfNull(pNativeData, "pNativeData");
		object objectForIUnknown = Marshal.GetObjectForIUnknown(pNativeData);
		return ComDataHelpers.GetOrCreateManagedViewFromComData(objectForIUnknown, (object obj) => new EnumerableViewOfDispatch(obj));
	}
}
