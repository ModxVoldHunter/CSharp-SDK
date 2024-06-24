using System.Text;

namespace System.StubHelpers;

internal static class UTF8BufferMarshaler
{
	internal unsafe static nint ConvertToNative(StringBuilder sb, nint pNativeBuffer, int flags)
	{
		if (sb == null)
		{
			return IntPtr.Zero;
		}
		string text = sb.ToString();
		int byteCount = Encoding.UTF8.GetByteCount(text);
		byteCount = text.GetBytesFromEncoding((byte*)pNativeBuffer, byteCount, Encoding.UTF8);
		*(sbyte*)(pNativeBuffer + byteCount) = 0;
		return pNativeBuffer;
	}

	internal unsafe static void ConvertToManaged(StringBuilder sb, nint pNative)
	{
		if (pNative != IntPtr.Zero)
		{
			int length = string.strlen((byte*)pNative);
			sb.ReplaceBufferUtf8Internal(new ReadOnlySpan<byte>((void*)pNative, length));
		}
	}
}
