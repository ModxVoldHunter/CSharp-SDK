using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling;

[CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsNaNMarshaller<>))]
public static class ExceptionAsNaNMarshaller<T> where T : unmanaged, IFloatingPointIeee754<T>
{
	public static T ConvertToUnmanaged(Exception e)
	{
		Marshal.GetHRForException(e);
		return T.NaN;
	}
}
