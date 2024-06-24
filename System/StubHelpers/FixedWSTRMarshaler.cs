namespace System.StubHelpers;

internal static class FixedWSTRMarshaler
{
	internal unsafe static void ConvertToNative(string strManaged, nint nativeHome, int length)
	{
		ReadOnlySpan<char> readOnlySpan = strManaged;
		Span<char> destination = new Span<char>((void*)nativeHome, length);
		int num = Math.Min(readOnlySpan.Length, length - 1);
		readOnlySpan.Slice(0, num).CopyTo(destination);
		destination[num] = '\0';
	}

	internal unsafe static string ConvertToManaged(nint nativeHome, int length)
	{
		int num = new ReadOnlySpan<char>((void*)nativeHome, length).IndexOf('\0');
		if (num >= 0)
		{
			length = num;
		}
		return new string((char*)nativeHome, 0, length);
	}
}
