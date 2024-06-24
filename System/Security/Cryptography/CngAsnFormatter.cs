using System.Buffers;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography;

internal sealed class CngAsnFormatter : AsnFormatter
{
	protected unsafe override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
	{
		string s = string.Empty;
		if (oid != null && oid.Value != null)
		{
			s = oid.Value;
		}
		int dwFormatStrType = (multiLine ? 1 : 0);
		int pcbFormat = 0;
		nint num = Marshal.StringToHGlobalAnsi(s);
		char[] array = null;
		try
		{
			if (global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)num, rawData, rawData.Length, null, ref pcbFormat))
			{
				int num2 = (pcbFormat + 1) / 2;
				Span<char> span = ((num2 > 256) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(num2))) : stackalloc char[256]);
				Span<char> span2 = span;
				fixed (char* ptr = span2)
				{
					if (global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)num, rawData, rawData.Length, ptr, ref pcbFormat))
					{
						return new string(ptr);
					}
				}
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num);
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
		return null;
	}
}
