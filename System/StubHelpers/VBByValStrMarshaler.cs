using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class VBByValStrMarshaler
{
	internal unsafe static nint ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		cch = strManaged.Length;
		int cb = checked(4 + (cch + 1) * Marshal.SystemMaxDBCSCharSize);
		byte* ptr = (byte*)Marshal.AllocCoTaskMem(cb);
		int* ptr2 = (int*)ptr;
		ptr += 4;
		if (cch == 0)
		{
			*ptr = 0;
			*ptr2 = 0;
		}
		else
		{
			int cbLength;
			byte[] array = AnsiCharMarshaler.DoAnsiConversion(strManaged, fBestFit, fThrowOnUnmappableChar, out cbLength);
			Buffer.Memmove(ref *ptr, ref MemoryMarshal.GetArrayDataReference(array), (nuint)cbLength);
			ptr[cbLength] = 0;
			*ptr2 = cbLength;
		}
		return new IntPtr(ptr);
	}

	internal unsafe static string ConvertToManaged(nint pNative, int cch)
	{
		if (IntPtr.Zero == pNative)
		{
			return null;
		}
		return new string((sbyte*)pNative, 0, cch);
	}

	internal static void ClearNative(nint pNative)
	{
		if (IntPtr.Zero != pNative)
		{
			Marshal.FreeCoTaskMem(pNative - 4);
		}
	}
}
