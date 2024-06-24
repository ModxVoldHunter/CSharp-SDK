using System.Text;

namespace System.Net.Http;

internal static class StringBuilderExtensions
{
	public static void AppendKeyValue(this StringBuilder sb, string key, string value, bool includeQuotes = true, bool includeComma = true)
	{
		sb.Append(key).Append('=');
		if (includeQuotes)
		{
			ReadOnlySpan<char> readOnlySpan = value;
			sb.Append('"');
			while (true)
			{
				int num = readOnlySpan.IndexOfAny('"', '\\');
				if (num < 0)
				{
					break;
				}
				sb.Append(readOnlySpan.Slice(0, num)).Append('\\').Append(readOnlySpan[num]);
				readOnlySpan = readOnlySpan.Slice(num + 1);
			}
			sb.Append(readOnlySpan);
			sb.Append('"');
		}
		else
		{
			sb.Append(value);
		}
		if (includeComma)
		{
			sb.Append(',').Append(' ');
		}
	}
}
