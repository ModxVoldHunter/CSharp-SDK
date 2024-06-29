using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class AnsiBSTRMarshaler
{
	internal unsafe static nint ConvertToNative(int flags, string strManaged)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte[] array = null;
		int cbLength = 0;
		if (strManaged.Length > 0)
		{
			array = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 0xFF) != 0, flags >> 8 != 0, out cbLength);
		}
		uint num = (uint)cbLength;
		nint num2 = Marshal.AllocBSTRByteLen(num);
		if (array != null)
		{
			Buffer.Memmove(ref *(byte*)num2, ref MemoryMarshal.GetArrayDataReference(array), num);
		}
		return num2;
	}

	internal unsafe static string ConvertToManaged(nint bstr)
	{
		if (IntPtr.Zero == bstr)
		{
			return null;
		}
		return new string((sbyte*)bstr);
	}

	internal static void ClearNative(nint pNative)
	{
		Marshal.FreeBSTR(pNative);
	}
}
