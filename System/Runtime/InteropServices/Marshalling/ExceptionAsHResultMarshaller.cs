using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling;

[CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsHResultMarshaller<>))]
public static class ExceptionAsHResultMarshaller<T> where T : unmanaged, INumber<T>
{
	public static T ConvertToUnmanaged(Exception e)
	{
		return T.CreateTruncating(Marshal.GetHRForException(e));
	}
}
