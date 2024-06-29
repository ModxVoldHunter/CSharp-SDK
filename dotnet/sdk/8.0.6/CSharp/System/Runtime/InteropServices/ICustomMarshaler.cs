namespace System.Runtime.InteropServices;

public interface ICustomMarshaler
{
	object MarshalNativeToManaged(nint pNativeData);

	nint MarshalManagedToNative(object ManagedObj);

	void CleanUpNativeData(nint pNativeData);

	void CleanUpManagedData(object ManagedObj);

	int GetNativeDataSize();
}
