using System.Numerics;

namespace System.IO;

internal static class DriveInfoInternal
{
	public static string[] GetLogicalDrives()
	{
		int logicalDrives = Interop.Kernel32.GetLogicalDrives();
		if (logicalDrives == 0)
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		int num = BitOperations.PopCount((uint)logicalDrives);
		string[] array = new string[num];
		Span<char> span = stackalloc char[3] { 'A', ':', '\\' };
		uint num2 = (uint)logicalDrives;
		num = 0;
		while (num2 != 0)
		{
			if ((num2 & (true ? 1u : 0u)) != 0)
			{
				array[num++] = span.ToString();
			}
			num2 >>= 1;
			span[0] += '\u0001';
		}
		return array;
	}

	public static string NormalizeDriveName(string driveName)
	{
		string text;
		if (driveName.Length == 1)
		{
			text = driveName + ":\\";
		}
		else
		{
			text = Path.GetPathRoot(driveName);
			if (string.IsNullOrEmpty(text) || text.StartsWith("\\\\", StringComparison.Ordinal))
			{
				throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir, "driveName");
			}
		}
		if (text.Length == 2 && text[1] == ':')
		{
			text += "\\";
		}
		if (!char.IsAsciiLetter(driveName[0]))
		{
			throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir, "driveName");
		}
		return text;
	}
}
