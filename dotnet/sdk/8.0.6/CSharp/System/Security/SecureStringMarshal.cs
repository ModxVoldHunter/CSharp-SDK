using System.Runtime.InteropServices;

namespace System.Security;

public static class SecureStringMarshal
{
	public static nint SecureStringToCoTaskMemAnsi(SecureString s)
	{
		return Marshal.SecureStringToCoTaskMemAnsi(s);
	}

	public static nint SecureStringToGlobalAllocAnsi(SecureString s)
	{
		return Marshal.SecureStringToGlobalAllocAnsi(s);
	}

	public static nint SecureStringToCoTaskMemUnicode(SecureString s)
	{
		return Marshal.SecureStringToCoTaskMemUnicode(s);
	}

	public static nint SecureStringToGlobalAllocUnicode(SecureString s)
	{
		return Marshal.SecureStringToGlobalAllocUnicode(s);
	}
}
