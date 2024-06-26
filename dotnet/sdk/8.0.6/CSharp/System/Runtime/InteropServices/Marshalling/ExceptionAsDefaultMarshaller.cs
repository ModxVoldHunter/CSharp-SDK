namespace System.Runtime.InteropServices.Marshalling;

[CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsDefaultMarshaller<>))]
public static class ExceptionAsDefaultMarshaller<T> where T : unmanaged
{
	public static T ConvertToUnmanaged(Exception e)
	{
		Marshal.GetHRForException(e);
		return default(T);
	}
}
