namespace System.Net;

internal static class HttpValidationHelpers
{
	private static readonly char[] s_httpTrimCharacters = new char[6] { '\t', '\n', '\v', '\f', '\r', ' ' };

	internal static string CheckBadHeaderNameChars(string name)
	{
		if (IsInvalidMethodOrHeaderString(name))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebHeaderInvalidHeaderChars, name), "name");
		}
		if (ContainsNonAsciiChars(name))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebHeaderInvalidHeaderChars, name), "name");
		}
		return name;
	}

	internal static bool ContainsNonAsciiChars(string token)
	{
		return token.AsSpan().ContainsAnyExceptInRange(' ', '~');
	}

	public static string CheckBadHeaderValueChars(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		value = value.Trim(s_httpTrimCharacters);
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char c = (char)(0xFFu & value[i]);
			switch (num)
			{
			case 0:
				if (c == '\r')
				{
					num = 1;
				}
				else if (c == '\n')
				{
					num = 2;
				}
				else if (c == '\u007f' || (c < ' ' && c != '\t'))
				{
					throw new ArgumentException(System.SR.net_WebHeaderInvalidControlChars, "value");
				}
				break;
			case 1:
				if (c == '\n')
				{
					num = 2;
					break;
				}
				throw new ArgumentException(System.SR.net_WebHeaderInvalidCRLFChars, "value");
			case 2:
				if (c == ' ' || c == '\t')
				{
					num = 0;
					break;
				}
				throw new ArgumentException(System.SR.net_WebHeaderInvalidControlChars, "value");
			}
		}
		if (num != 0)
		{
			throw new ArgumentException(System.SR.net_WebHeaderInvalidCRLFChars, "value");
		}
		return value;
	}

	public static bool IsInvalidMethodOrHeaderString(string stringValue)
	{
		for (int i = 0; i < stringValue.Length; i++)
		{
			switch (stringValue[i])
			{
			case '\t':
			case '\n':
			case '\r':
			case ' ':
			case '"':
			case '\'':
			case '(':
			case ')':
			case ',':
			case '/':
			case ':':
			case ';':
			case '<':
			case '=':
			case '>':
			case '?':
			case '@':
			case '[':
			case '\\':
			case ']':
			case '{':
			case '}':
				return true;
			}
		}
		return false;
	}
}
