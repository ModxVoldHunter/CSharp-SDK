using System.Buffers;
using System.Globalization;

namespace System.Net.Security;

internal static class TargetHostNameHelper
{
	private static readonly IdnMapping s_idnMapping = new IdnMapping();

	private static readonly SearchValues<char> s_safeDnsChars = SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

	private static bool IsSafeDnsString(ReadOnlySpan<char> name)
	{
		return !name.ContainsAnyExcept(s_safeDnsChars);
	}

	internal static string NormalizeHostName(string targetHost)
	{
		if (string.IsNullOrEmpty(targetHost))
		{
			return string.Empty;
		}
		targetHost = targetHost.TrimEnd('.');
		try
		{
			return s_idnMapping.GetAscii(targetHost);
		}
		catch (ArgumentException) when (IsSafeDnsString(targetHost))
		{
			return targetHost;
		}
	}
}
