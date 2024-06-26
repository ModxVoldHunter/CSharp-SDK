namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(Utf16StringMarshaller))]
public static class Utf16StringMarshaller
{
	public unsafe static ushort* ConvertToUnmanaged(string? managed)
	{
		return (ushort*)Marshal.StringToCoTaskMemUni(managed);
	}

	public unsafe static string? ConvertToManaged(ushort* unmanaged)
	{
		return Marshal.PtrToStringUni((nint)unmanaged);
	}

	public unsafe static void Free(ushort* unmanaged)
	{
		Marshal.FreeCoTaskMem((nint)unmanaged);
	}

	public unsafe static ref readonly char GetPinnableReference(string? str)
	{
		if (str != null)
		{
			return ref str.GetPinnableReference();
		}
		return ref *(char*)null;
	}
}
