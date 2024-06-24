using System.Buffers;
using System.Text;

namespace System.Net.Http;

internal static class HttpRuleParser
{
	private static readonly SearchValues<char> s_tokenChars = SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

	private static readonly SearchValues<byte> s_tokenBytes = SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~"u8);

	private static readonly SearchValues<char> s_hostDelimiterChars = SearchValues.Create("/ \t\r,");

	internal static Encoding DefaultHttpEncoding => Encoding.Latin1;

	internal static int GetTokenLength(string input, int startIndex)
	{
		ReadOnlySpan<char> span = input.AsSpan(startIndex);
		int num = span.IndexOfAnyExcept(s_tokenChars);
		if (num >= 0)
		{
			return num;
		}
		return span.Length;
	}

	internal static bool IsToken(ReadOnlySpan<char> input)
	{
		return !input.ContainsAnyExcept(s_tokenChars);
	}

	internal static bool IsToken(ReadOnlySpan<byte> input)
	{
		return !input.ContainsAnyExcept(s_tokenBytes);
	}

	internal static string GetTokenString(ReadOnlySpan<byte> input)
	{
		return Encoding.ASCII.GetString(input);
	}

	internal static int GetWhitespaceLength(string input, int startIndex)
	{
		if (startIndex >= input.Length)
		{
			return 0;
		}
		for (int i = startIndex; i < input.Length; i++)
		{
			char c = input[i];
			if (c != ' ' && c != '\t')
			{
				return i - startIndex;
			}
		}
		return input.Length - startIndex;
	}

	internal static bool ContainsNewLine(string value, int startIndex = 0)
	{
		return value.AsSpan(startIndex).ContainsAny('\r', '\n');
	}

	internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
	{
		int num = startIndex;
		bool flag = !allowDecimal;
		if (input[num] == '.')
		{
			return 0;
		}
		while (num < input.Length)
		{
			char c = input[num];
			if (char.IsAsciiDigit(c))
			{
				num++;
				continue;
			}
			if (flag || c != '.')
			{
				break;
			}
			flag = true;
			num++;
		}
		return num - startIndex;
	}

	internal static int GetHostLength(string input, int startIndex, bool allowToken)
	{
		if (startIndex >= input.Length)
		{
			return 0;
		}
		ReadOnlySpan<char> readOnlySpan = input.AsSpan(startIndex);
		int num = readOnlySpan.IndexOfAny(s_hostDelimiterChars);
		if (num >= 0)
		{
			if (num == 0)
			{
				return 0;
			}
			if (readOnlySpan[num] == '/')
			{
				return 0;
			}
			readOnlySpan = readOnlySpan.Slice(0, num);
		}
		if ((allowToken && IsToken(readOnlySpan)) || IsValidHostName(readOnlySpan))
		{
			return readOnlySpan.Length;
		}
		return 0;
	}

	internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
	{
		return GetExpressionLength(input, startIndex, '(', ')', supportsNesting: true, 1, out length);
	}

	internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
	{
		return GetExpressionLength(input, startIndex, '"', '"', supportsNesting: false, 1, out length);
	}

	internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
	{
		length = 0;
		if (input[startIndex] != '\\')
		{
			return HttpParseResult.NotParsed;
		}
		if (startIndex + 2 > input.Length || input[startIndex + 1] > '\u007f')
		{
			return HttpParseResult.InvalidFormat;
		}
		length = 2;
		return HttpParseResult.Parsed;
	}

	private static HttpParseResult GetExpressionLength(string input, int startIndex, char openChar, char closeChar, bool supportsNesting, int nestedCount, out int length)
	{
		length = 0;
		if (input[startIndex] != openChar)
		{
			return HttpParseResult.NotParsed;
		}
		int num = startIndex + 1;
		while (num < input.Length)
		{
			if (num + 2 < input.Length && GetQuotedPairLength(input, num, out var length2) == HttpParseResult.Parsed)
			{
				num += length2;
				continue;
			}
			char c = input[num];
			if (c == '\r' || c == '\n')
			{
				return HttpParseResult.InvalidFormat;
			}
			if (supportsNesting && c == openChar)
			{
				if (nestedCount > 5)
				{
					return HttpParseResult.InvalidFormat;
				}
				int length3;
				switch (GetExpressionLength(input, num, openChar, closeChar, supportsNesting, nestedCount + 1, out length3))
				{
				case HttpParseResult.Parsed:
					num += length3;
					break;
				case HttpParseResult.InvalidFormat:
					return HttpParseResult.InvalidFormat;
				}
			}
			else
			{
				if (input[num] == closeChar)
				{
					length = num - startIndex + 1;
					return HttpParseResult.Parsed;
				}
				num++;
			}
		}
		return HttpParseResult.InvalidFormat;
	}

	private static bool IsValidHostName(ReadOnlySpan<char> host)
	{
		Uri result;
		return Uri.TryCreate($"http://u@{host}/", UriKind.Absolute, out result);
	}
}
