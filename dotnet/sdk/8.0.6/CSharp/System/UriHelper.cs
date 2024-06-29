using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class UriHelper
{
	public static readonly SearchValues<char> Unreserved = SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~");

	public static readonly SearchValues<char> UnreservedReserved = SearchValues.Create("!#$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

	public static readonly SearchValues<char> UnreservedReservedExceptHash = SearchValues.Create("!$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

	public static readonly SearchValues<char> UnreservedReservedExceptQuestionMarkHash = SearchValues.Create("!$&'()*+,-./0123456789:;=@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

	internal static readonly char[] s_WSchars = new char[4] { ' ', '\n', '\r', '\t' };

	public unsafe static string SpanToLowerInvariantString(ReadOnlySpan<char> span)
	{
		return string.Create(span.Length, (nint)(&span), delegate(Span<char> buffer, nint spanPtr)
		{
			int num = Unsafe.Read<ReadOnlySpan<char>>((void*)spanPtr).ToLowerInvariant(buffer);
		});
	}

	internal unsafe static bool TestForSubPath(char* selfPtr, int selfLength, char* otherPtr, int otherLength, bool ignoreCase)
	{
		int i = 0;
		bool flag = true;
		for (; i < selfLength && i < otherLength; i++)
		{
			char c = selfPtr[i];
			char c2 = otherPtr[i];
			switch (c)
			{
			case '#':
			case '?':
				return true;
			case '/':
				if (c2 != '/')
				{
					return false;
				}
				if (!flag)
				{
					return false;
				}
				flag = true;
				continue;
			default:
				if (c2 == '?' || c2 == '#')
				{
					break;
				}
				if (!ignoreCase)
				{
					if (c != c2)
					{
						flag = false;
					}
				}
				else if (char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
				{
					flag = false;
				}
				continue;
			}
			break;
		}
		for (; i < selfLength; i++)
		{
			char c;
			if ((c = selfPtr[i]) != '?')
			{
				switch (c)
				{
				case '#':
					break;
				case '/':
					return false;
				default:
					continue;
				}
			}
			return true;
		}
		return true;
	}

	internal static string EscapeString(string stringToEscape, bool checkExistingEscaped, SearchValues<char> noEscape)
	{
		ArgumentNullException.ThrowIfNull(stringToEscape, "stringToEscape");
		int num = stringToEscape.AsSpan().IndexOfAnyExcept(noEscape);
		if (num < 0)
		{
			return stringToEscape;
		}
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder vsb = new System.Text.ValueStringBuilder(initialBuffer);
		vsb.Append(stringToEscape.AsSpan(0, num));
		EscapeStringToBuilder(stringToEscape.AsSpan(num), ref vsb, noEscape, checkExistingEscaped);
		return vsb.ToString();
	}

	internal static void EscapeString(ReadOnlySpan<char> stringToEscape, ref System.Text.ValueStringBuilder dest, bool checkExistingEscaped, SearchValues<char> noEscape)
	{
		int num = stringToEscape.IndexOfAnyExcept(noEscape);
		if (num < 0)
		{
			dest.Append(stringToEscape);
			return;
		}
		dest.Append(stringToEscape.Slice(0, num));
		EscapeStringToBuilder(stringToEscape.Slice(num), ref dest, noEscape, checkExistingEscaped);
	}

	private static void EscapeStringToBuilder(ReadOnlySpan<char> stringToEscape, ref System.Text.ValueStringBuilder vsb, SearchValues<char> noEscape, bool checkExistingEscaped)
	{
		Span<byte> destination = stackalloc byte[4];
		while (!stringToEscape.IsEmpty)
		{
			char c = stringToEscape[0];
			if (!char.IsAscii(c))
			{
				if (Rune.DecodeFromUtf16(stringToEscape, out var result, out var charsConsumed) != 0)
				{
					result = Rune.ReplacementChar;
				}
				stringToEscape = stringToEscape.Slice(charsConsumed);
				result.TryEncodeToUtf8(destination, out var bytesWritten);
				Span<byte> span = destination.Slice(0, bytesWritten);
				for (int i = 0; i < span.Length; i++)
				{
					byte b = span[i];
					PercentEncodeByte(b, ref vsb);
				}
			}
			else if (!noEscape.Contains(c))
			{
				if (c == '%' && checkExistingEscaped && stringToEscape.Length > 2 && char.IsAsciiHexDigit(stringToEscape[1]) && char.IsAsciiHexDigit(stringToEscape[2]))
				{
					vsb.Append('%');
					vsb.Append(stringToEscape[1]);
					vsb.Append(stringToEscape[2]);
					stringToEscape = stringToEscape.Slice(3);
				}
				else
				{
					PercentEncodeByte((byte)c, ref vsb);
					stringToEscape = stringToEscape.Slice(1);
				}
			}
			else
			{
				int num = stringToEscape.IndexOfAnyExcept(noEscape);
				if (num < 0)
				{
					num = stringToEscape.Length;
				}
				vsb.Append(stringToEscape.Slice(0, num));
				stringToEscape = stringToEscape.Slice(num);
			}
		}
	}

	internal unsafe static char[] UnescapeString(string input, int start, int end, char[] dest, ref int destPosition, char rsvd1, char rsvd2, char rsvd3, System.UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = input)
		{
			return UnescapeString(pStr, start, end, dest, ref destPosition, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static char[] UnescapeString(char* pStr, int start, int end, char[] dest, ref int destPosition, char rsvd1, char rsvd2, char rsvd3, System.UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		System.Text.ValueStringBuilder dest2 = new System.Text.ValueStringBuilder(dest.Length);
		dest2.Append(dest.AsSpan(0, destPosition));
		UnescapeString(pStr, start, end, ref dest2, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		if (dest2.Length > dest.Length)
		{
			dest = dest2.AsSpan().ToArray();
		}
		else
		{
			dest2.AsSpan(destPosition).TryCopyTo(dest.AsSpan(destPosition));
		}
		destPosition = dest2.Length;
		dest2.Dispose();
		return dest;
	}

	internal unsafe static void UnescapeString(string input, int start, int end, ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, System.UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = input)
		{
			UnescapeString(pStr, start, end, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static void UnescapeString(scoped ReadOnlySpan<char> input, scoped ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, System.UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = &MemoryMarshal.GetReference(input))
		{
			UnescapeString(pStr, 0, input.Length, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static void UnescapeString(char* pStr, int start, int end, ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, System.UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		if ((unescapeMode & System.UnescapeMode.EscapeUnescape) == 0)
		{
			dest.Append(pStr + start, end - start);
			return;
		}
		bool flag = false;
		bool flag2 = Uri.IriParsingStatic(syntax) && (unescapeMode & System.UnescapeMode.EscapeUnescape) == System.UnescapeMode.EscapeUnescape;
		int i = start;
		while (i < end)
		{
			char c = '\0';
			for (; i < end; i++)
			{
				if ((c = pStr[i]) == '%')
				{
					if ((unescapeMode & System.UnescapeMode.Unescape) == 0)
					{
						flag = true;
						break;
					}
					if (i + 2 < end)
					{
						c = DecodeHexChars(pStr[i + 1], pStr[i + 2]);
						if (unescapeMode < System.UnescapeMode.UnescapeAll)
						{
							switch (c)
							{
							case '\uffff':
								if ((unescapeMode & System.UnescapeMode.Escape) == 0)
								{
									continue;
								}
								flag = true;
								break;
							case '%':
								i += 2;
								continue;
							default:
								if (c == rsvd1 || c == rsvd2 || c == rsvd3)
								{
									i += 2;
									continue;
								}
								if ((unescapeMode & System.UnescapeMode.V1ToStringFlag) == 0 && IsNotSafeForUnescape(c))
								{
									i += 2;
									continue;
								}
								if (flag2 && ((c <= '\u009f' && IsNotSafeForUnescape(c)) || (c > '\u009f' && !System.IriHelper.CheckIriUnicodeRange(c, isQuery))))
								{
									i += 2;
									continue;
								}
								break;
							}
							break;
						}
						if (c != '\uffff')
						{
							break;
						}
						if (unescapeMode >= System.UnescapeMode.UnescapeAllOrThrow)
						{
							throw new UriFormatException(System.SR.net_uri_BadString);
						}
					}
					else
					{
						if (unescapeMode < System.UnescapeMode.UnescapeAll)
						{
							flag = true;
							break;
						}
						if (unescapeMode >= System.UnescapeMode.UnescapeAllOrThrow)
						{
							throw new UriFormatException(System.SR.net_uri_BadString);
						}
					}
				}
				else if ((unescapeMode & (System.UnescapeMode.Unescape | System.UnescapeMode.UnescapeAll)) != (System.UnescapeMode.Unescape | System.UnescapeMode.UnescapeAll) && (unescapeMode & System.UnescapeMode.Escape) != 0)
				{
					if (c == rsvd1 || c == rsvd2 || c == rsvd3)
					{
						flag = true;
						break;
					}
					if ((unescapeMode & System.UnescapeMode.V1ToStringFlag) == 0 && (c <= '\u001f' || (c >= '\u007f' && c <= '\u009f')))
					{
						flag = true;
						break;
					}
				}
			}
			while (start < i)
			{
				dest.Append(pStr[start++]);
			}
			if (i != end)
			{
				if (flag)
				{
					PercentEncodeByte((byte)pStr[i], ref dest);
					flag = false;
					i++;
				}
				else if (c <= '\u007f')
				{
					dest.Append(c);
					i += 3;
				}
				else
				{
					int num = System.PercentEncodingHelper.UnescapePercentEncodedUTF8Sequence(pStr + i, end - i, ref dest, isQuery, flag2);
					i += num;
				}
				start = i;
			}
		}
	}

	internal static void PercentEncodeByte(byte b, ref System.Text.ValueStringBuilder to)
	{
		to.Append('%');
		System.HexConverter.ToCharsBuffer(b, to.AppendSpan(2));
	}

	internal static char DecodeHexChars(int first, int second)
	{
		int num = System.HexConverter.FromChar(first);
		int num2 = System.HexConverter.FromChar(second);
		if ((num | num2) == 255)
		{
			return '\uffff';
		}
		return (char)((num << 4) | num2);
	}

	internal static bool IsNotSafeForUnescape(char ch)
	{
		if (ch <= '\u001f' || (ch >= '\u007f' && ch <= '\u009f'))
		{
			return true;
		}
		return ";/?:@&=+$,#[]!'()*%\\#".Contains(ch);
	}

	internal static bool IsGenDelim(char ch)
	{
		if (ch != ':' && ch != '/' && ch != '?' && ch != '#' && ch != '[' && ch != ']')
		{
			return ch == '@';
		}
		return true;
	}

	internal static bool IsLWS(char ch)
	{
		if (ch <= ' ')
		{
			if (ch != ' ' && ch != '\n' && ch != '\r')
			{
				return ch == '\t';
			}
			return true;
		}
		return false;
	}

	internal static bool IsBidiControlCharacter(char ch)
	{
		if (char.IsBetween(ch, '\u200e', '\u202e'))
		{
			return !char.IsBetween(ch, '‐', '\u2029');
		}
		return false;
	}

	internal unsafe static string StripBidiControlCharacters(ReadOnlySpan<char> strToClean, string backingString = null)
	{
		int num = 0;
		int num2 = strToClean.IndexOfAnyInRange('\u200e', '\u202e');
		if (num2 >= 0)
		{
			ReadOnlySpan<char> readOnlySpan = strToClean.Slice(num2);
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				char ch = readOnlySpan[i];
				if (IsBidiControlCharacter(ch))
				{
					num++;
				}
			}
		}
		if (num == 0)
		{
			return backingString ?? new string(strToClean);
		}
		ReadOnlySpan<char> readOnlySpan2 = strToClean;
		return string.Create(readOnlySpan2.Length - num, (nint)(&readOnlySpan2), delegate(Span<char> buffer, nint strToCleanPtr)
		{
			int num3 = 0;
			ReadOnlySpan<char> readOnlySpan3 = Unsafe.Read<ReadOnlySpan<char>>((void*)strToCleanPtr);
			for (int j = 0; j < readOnlySpan3.Length; j++)
			{
				char c = readOnlySpan3[j];
				if (!IsBidiControlCharacter(c))
				{
					buffer[num3++] = c;
				}
			}
		});
	}
}
