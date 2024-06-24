namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class TypeToTypeInfoMarshaler : ICustomMarshaler
{
	private static readonly TypeToTypeInfoMarshaler s_typeToTypeInfoMarshaler = new TypeToTypeInfoMarshaler();

	public static ICustomMarshaler GetInstance(string cookie)
	{
		return s_typeToTypeInfoMarshaler;
	}

	private TypeToTypeInfoMarshaler()
	{
	}

	public void CleanUpManagedData(object ManagedObj)
	{
	}

	public void CleanUpNativeData(nint pNativeData)
	{
	}

	public int GetNativeDataSize()
	{
		return -1;
	}

	public nint MarshalManagedToNative(object ManagedObj)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ITypeInfo);
	}

	public object MarshalNativeToManaged(nint pNativeData)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ITypeInfo);
	}
}
