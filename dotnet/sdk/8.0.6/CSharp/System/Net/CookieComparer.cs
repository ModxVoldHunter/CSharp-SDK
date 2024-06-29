namespace System.Net;

internal static class CookieComparer
{
	internal static bool Equals(Cookie left, Cookie right)
	{
		if (!string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!EqualDomains(left.Domain, right.Domain))
		{
			return false;
		}
		return string.Equals(left.Path, right.Path, StringComparison.Ordinal);
	}

	internal static bool EqualDomains(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
	{
		if (left.Length != 0 && left[0] == '.')
		{
			left = left.Slice(1);
		}
		if (right.Length != 0 && right[0] == '.')
		{
			right = right.Slice(1);
		}
		return MemoryExtensions.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}
}
