using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class CSTRMarshaler
{
	internal unsafe static nint ConvertToNative(int flags, string strManaged, nint pNativeBuffer)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte* ptr = (byte*)pNativeBuffer;
		int num;
		if (ptr != null || Marshal.SystemMaxDBCSCharSize == 1)
		{
			num = checked((strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize + 1);
			bool flag = false;
			if (ptr == null)
			{
				ptr = (byte*)Marshal.AllocCoTaskMem(num);
				flag = true;
			}
			try
			{
				num = Marshal.StringToAnsiString(strManaged, ptr, num, (flags & 0xFF) != 0, flags >> 8 != 0);
			}
			catch (Exception) when (flag)
			{
				Marshal.FreeCoTaskMem((nint)ptr);
				throw;
			}
		}
		else if (strManaged.Length == 0)
		{
			num = 0;
			ptr = (byte*)Marshal.AllocCoTaskMem(2);
		}
		else
		{
			byte[] array = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 0xFF) != 0, flags >> 8 != 0, out num);
			ptr = (byte*)Marshal.AllocCoTaskMem(num + 2);
			Buffer.Memmove(ref *ptr, ref MemoryMarshal.GetArrayDataReference(array), (nuint)num);
		}
		ptr[num] = 0;
		ptr[num + 1] = 0;
		return (nint)ptr;
	}

	internal unsafe static string ConvertToManaged(nint cstr)
	{
		if (IntPtr.Zero == cstr)
		{
			return null;
		}
		return new string((sbyte*)cstr);
	}

	internal unsafe static void ConvertFixedToNative(int flags, string strManaged, nint pNativeBuffer, int length)
	{
		if (strManaged == null)
		{
			if (length > 0)
			{
				*(sbyte*)pNativeBuffer = 0;
			}
			return;
		}
		int num = strManaged.Length;
		if (num >= length)
		{
			num = length - 1;
		}
		bool flag = flags >> 8 != 0;
		bool flag2 = (flags & 0xFF) != 0;
		Interop.BOOL bOOL = Interop.BOOL.FALSE;
		int num2;
		fixed (char* lpWideCharStr = strManaged)
		{
			num2 = Interop.Kernel32.WideCharToMultiByte(0u, (!flag2) ? 1024u : 0u, lpWideCharStr, num, (byte*)pNativeBuffer, length, null, flag ? (&bOOL) : null);
		}
		if (bOOL != 0)
		{
			throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
		}
		if (num2 == length)
		{
			num2--;
		}
		*(sbyte*)(pNativeBuffer + num2) = 0;
	}

	internal unsafe static string ConvertFixedToManaged(nint cstr, int length)
	{
		int num = new ReadOnlySpan<byte>((void*)cstr, length).IndexOf<byte>(0);
		if (num >= 0)
		{
			length = num;
		}
		return new string((sbyte*)cstr, 0, length);
	}
}
