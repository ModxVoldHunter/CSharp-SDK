using System.Buffers;
using System.Text;

namespace System.Net.Mime;

internal static class MailBnfHelper
{
	internal static readonly bool[] Atext = CreateCharactersAllowedInAtoms();

	internal static readonly bool[] Qtext = CreateCharactersAllowedInQuotedStrings();

	internal static readonly bool[] Dtext = CreateCharactersAllowedInDomainLiterals();

	internal static readonly bool[] Ctext = CreateCharactersAllowedInComments();

	private static readonly SearchValues<char> s_charactersAllowedInHeaderNames = SearchValues.Create("!\"#$%&'()*+,-./0123456789;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");

	private static readonly SearchValues<char> s_charactersAllowedInTokens = SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz{|}~");

	private static readonly string[] s_months = new string[13]
	{
		null, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep",
		"Oct", "Nov", "Dec"
	};

	private static bool[] CreateCharactersAllowedInAtoms()
	{
		bool[] array = new bool[128];
		for (int i = 48; i <= 57; i++)
		{
			array[i] = true;
		}
		for (int j = 65; j <= 90; j++)
		{
			array[j] = true;
		}
		for (int k = 97; k <= 122; k++)
		{
			array[k] = true;
		}
		array[33] = true;
		array[35] = true;
		array[36] = true;
		array[37] = true;
		array[38] = true;
		array[39] = true;
		array[42] = true;
		array[43] = true;
		array[45] = true;
		array[47] = true;
		array[61] = true;
		array[63] = true;
		array[94] = true;
		array[95] = true;
		array[96] = true;
		array[123] = true;
		array[124] = true;
		array[125] = true;
		array[126] = true;
		return array;
	}

	private static bool[] CreateCharactersAllowedInQuotedStrings()
	{
		bool[] array = new bool[128];
		for (int i = 1; i <= 9; i++)
		{
			array[i] = true;
		}
		array[11] = true;
		array[12] = true;
		for (int j = 14; j <= 33; j++)
		{
			array[j] = true;
		}
		for (int k = 35; k <= 91; k++)
		{
			array[k] = true;
		}
		for (int l = 93; l <= 127; l++)
		{
			array[l] = true;
		}
		return array;
	}

	private static bool[] CreateCharactersAllowedInDomainLiterals()
	{
		bool[] array = new bool[128];
		for (int i = 1; i <= 8; i++)
		{
			array[i] = true;
		}
		array[11] = true;
		array[12] = true;
		for (int j = 14; j <= 31; j++)
		{
			array[j] = true;
		}
		for (int k = 33; k <= 90; k++)
		{
			array[k] = true;
		}
		for (int l = 94; l <= 127; l++)
		{
			array[l] = true;
		}
		return array;
	}

	private static bool[] CreateCharactersAllowedInComments()
	{
		bool[] array = new bool[128];
		for (int i = 1; i <= 8; i++)
		{
			array[i] = true;
		}
		array[11] = true;
		array[12] = true;
		for (int j = 14; j <= 31; j++)
		{
			array[j] = true;
		}
		for (int k = 33; k <= 39; k++)
		{
			array[k] = true;
		}
		for (int l = 42; l <= 91; l++)
		{
			array[l] = true;
		}
		for (int m = 93; m <= 127; m++)
		{
			array[m] = true;
		}
		return array;
	}

	internal static bool SkipCFWS(string data, ref int offset)
	{
		int num = 0;
		while (offset < data.Length)
		{
			if (data[offset] > '\u007f')
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[offset]));
			}
			if (data[offset] == '\\' && num > 0)
			{
				offset += 2;
			}
			else if (data[offset] == '(')
			{
				num++;
			}
			else if (data[offset] == ')')
			{
				num--;
			}
			else if (data[offset] != ' ' && data[offset] != '\t' && num == 0)
			{
				return true;
			}
			if (num < 0)
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[offset]));
			}
			offset++;
		}
		return false;
	}

	internal static void ValidateHeaderName(string data)
	{
		if (data.Length == 0 || data.AsSpan().ContainsAnyExcept(s_charactersAllowedInHeaderNames))
		{
			throw new FormatException(System.SR.InvalidHeaderName);
		}
	}

	internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder)
	{
		return ReadQuotedString(data, ref offset, builder, doesntRequireQuotes: false, permitUnicodeInDisplayName: false);
	}

	internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder, bool doesntRequireQuotes, bool permitUnicodeInDisplayName)
	{
		if (!doesntRequireQuotes)
		{
			offset++;
		}
		int num = offset;
		StringBuilder stringBuilder = builder ?? new StringBuilder();
		while (offset < data.Length)
		{
			if (data[offset] == '\\')
			{
				stringBuilder.Append(data, num, offset - num);
				num = ++offset;
			}
			else
			{
				if (data[offset] == '"')
				{
					stringBuilder.Append(data, num, offset - num);
					offset++;
					if (builder == null)
					{
						return stringBuilder.ToString();
					}
					return null;
				}
				if (data[offset] == '=' && data.Length > offset + 3 && data[offset + 1] == '\r' && data[offset + 2] == '\n' && (data[offset + 3] == ' ' || data[offset + 3] == '\t'))
				{
					offset += 3;
				}
				else if (permitUnicodeInDisplayName)
				{
					if (Ascii.IsValid(data[offset]) && !Qtext[(uint)data[offset]])
					{
						throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[offset]));
					}
				}
				else if (!Ascii.IsValid(data[offset]) || !Qtext[(uint)data[offset]])
				{
					throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[offset]));
				}
			}
			offset++;
		}
		if (doesntRequireQuotes)
		{
			stringBuilder.Append(data, num, offset - num);
			if (builder == null)
			{
				return stringBuilder.ToString();
			}
			return null;
		}
		throw new FormatException(System.SR.MailHeaderFieldMalformedHeader);
	}

	internal static string ReadParameterAttribute(string data, ref int offset)
	{
		if (!SkipCFWS(data, ref offset))
		{
			return null;
		}
		return ReadToken(data, ref offset);
	}

	internal static string ReadToken(string data, ref int offset)
	{
		int num = offset;
		if (num >= data.Length)
		{
			return string.Empty;
		}
		ReadOnlySpan<char> span = data.AsSpan(num);
		int num2 = span.IndexOfAnyExcept(s_charactersAllowedInTokens);
		if (num2 >= 0)
		{
			if (num2 == 0 || !Ascii.IsValid(span[num2]))
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, span[num2]));
			}
		}
		else
		{
			num2 = span.Length;
		}
		offset += num2;
		return data.Substring(num, num2);
	}

	internal static string GetDateTimeString(DateTime value, StringBuilder builder)
	{
		StringBuilder stringBuilder = builder ?? new StringBuilder();
		stringBuilder.Append(value.Day);
		stringBuilder.Append(' ');
		stringBuilder.Append(s_months[value.Month]);
		stringBuilder.Append(' ');
		stringBuilder.Append(value.Year);
		stringBuilder.Append(' ');
		if (value.Hour <= 9)
		{
			stringBuilder.Append('0');
		}
		stringBuilder.Append(value.Hour);
		stringBuilder.Append(':');
		if (value.Minute <= 9)
		{
			stringBuilder.Append('0');
		}
		stringBuilder.Append(value.Minute);
		stringBuilder.Append(':');
		if (value.Second <= 9)
		{
			stringBuilder.Append('0');
		}
		stringBuilder.Append(value.Second);
		string text = TimeZoneInfo.Local.GetUtcOffset(value).ToString();
		if (text[0] != '-')
		{
			stringBuilder.Append(" +");
		}
		else
		{
			stringBuilder.Append(' ');
		}
		string[] array = text.Split(':');
		stringBuilder.Append(array[0]);
		stringBuilder.Append(array[1]);
		if (builder == null)
		{
			return stringBuilder.ToString();
		}
		return null;
	}

	internal static void GetTokenOrQuotedString(string data, StringBuilder builder, bool allowUnicode)
	{
		int i = 0;
		int num = 0;
		for (; i < data.Length; i++)
		{
			if (CheckForUnicode(data[i], allowUnicode) || (s_charactersAllowedInTokens.Contains(data[i]) && data[i] != ' '))
			{
				continue;
			}
			builder.Append('"');
			for (; i < data.Length; i++)
			{
				if (!CheckForUnicode(data[i], allowUnicode))
				{
					if (IsFWSAt(data, i))
					{
						i += 2;
					}
					else if (!Qtext[(uint)data[i]])
					{
						builder.Append(data, num, i - num);
						builder.Append('\\');
						num = i;
					}
				}
			}
			builder.Append(data, num, i - num);
			builder.Append('"');
			return;
		}
		if (data.Length == 0)
		{
			builder.Append("\"\"");
		}
		builder.Append(data);
	}

	private static bool CheckForUnicode(char ch, bool allowUnicode)
	{
		if (Ascii.IsValid(ch))
		{
			return false;
		}
		if (!allowUnicode)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, ch));
		}
		return true;
	}

	internal static bool IsAllowedWhiteSpace(char c)
	{
		if (c != '\t' && c != ' ' && c != '\r')
		{
			return c == '\n';
		}
		return true;
	}

	internal static bool HasCROrLF(string data)
	{
		return data.AsSpan().ContainsAny('\r', '\n');
	}

	internal static bool IsFWSAt(string data, int index)
	{
		if (data[index] == '\r' && index + 2 < data.Length && data[index + 1] == '\n')
		{
			if (data[index + 2] != ' ')
			{
				return data[index + 2] == '\t';
			}
			return true;
		}
		return false;
	}
}
