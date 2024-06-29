using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Json;

internal static class JsonReaderHelper
{
	private static readonly SearchValues<char> s_specialCharacters = SearchValues.Create(". '/\"[]()\t\n\r\f\b\\\u0085\u2028\u2029");

	public static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly SearchValues<byte> s_controlQuoteBackslash = SearchValues.Create(new byte[34]
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
		10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
		20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
		30, 31, 34, 92
	});

	public static bool ContainsSpecialCharacters(this ReadOnlySpan<char> text)
	{
		return text.ContainsAny(s_specialCharacters);
	}

	public static (int, int) CountNewLines(ReadOnlySpan<byte> data)
	{
		int num = data.LastIndexOf<byte>(10);
		int item = 0;
		if (num >= 0)
		{
			item = 1;
			data = data.Slice(0, num);
			item += data.Count<byte>(10);
		}
		return (item, num);
	}

	internal static JsonValueKind ToValueKind(this JsonTokenType tokenType)
	{
		switch (tokenType)
		{
		case JsonTokenType.None:
			return JsonValueKind.Undefined;
		case JsonTokenType.StartArray:
			return JsonValueKind.Array;
		case JsonTokenType.StartObject:
			return JsonValueKind.Object;
		case JsonTokenType.String:
		case JsonTokenType.Number:
		case JsonTokenType.True:
		case JsonTokenType.False:
		case JsonTokenType.Null:
			return (JsonValueKind)(tokenType - 4);
		default:
			return JsonValueKind.Undefined;
		}
	}

	public static bool IsTokenTypePrimitive(JsonTokenType tokenType)
	{
		return (int)(tokenType - 7) <= 4;
	}

	public static bool IsHexDigit(byte nextByte)
	{
		return System.HexConverter.IsHexChar(nextByte);
	}

	public static bool TryGetEscapedDateTime(ReadOnlySpan<byte> source, out DateTime value)
	{
		Span<byte> span = stackalloc byte[252];
		Unescape(source, span, out var written);
		span = span.Slice(0, written);
		if (JsonHelpers.IsValidUnescapedDateTimeOffsetParseLength(span.Length) && JsonHelpers.TryParseAsISO((ReadOnlySpan<byte>)span, out DateTime value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTime);
		return false;
	}

	public static bool TryGetEscapedDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
	{
		Span<byte> span = stackalloc byte[252];
		Unescape(source, span, out var written);
		span = span.Slice(0, written);
		if (JsonHelpers.IsValidUnescapedDateTimeOffsetParseLength(span.Length) && JsonHelpers.TryParseAsISO((ReadOnlySpan<byte>)span, out DateTimeOffset value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTimeOffset);
		return false;
	}

	public static bool TryGetEscapedGuid(ReadOnlySpan<byte> source, out Guid value)
	{
		Span<byte> span = stackalloc byte[216];
		Unescape(source, span, out var written);
		span = span.Slice(0, written);
		if (span.Length == 36 && Utf8Parser.TryParse((ReadOnlySpan<byte>)span, out Guid value2, out int _, 'D'))
		{
			value = value2;
			return true;
		}
		value = default(Guid);
		return false;
	}

	public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out Half value)
	{
		if (span.Length == 3)
		{
			if (span.SequenceEqual(JsonConstants.NaNValue))
			{
				value = Half.NaN;
				return true;
			}
		}
		else if (span.Length == 8)
		{
			if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
			{
				value = Half.PositiveInfinity;
				return true;
			}
		}
		else if (span.Length == 9 && span.SequenceEqual(JsonConstants.NegativeInfinityValue))
		{
			value = Half.NegativeInfinity;
			return true;
		}
		value = default(Half);
		return false;
	}

	public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out float value)
	{
		if (span.Length == 3)
		{
			if (span.SequenceEqual(JsonConstants.NaNValue))
			{
				value = float.NaN;
				return true;
			}
		}
		else if (span.Length == 8)
		{
			if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
			{
				value = float.PositiveInfinity;
				return true;
			}
		}
		else if (span.Length == 9 && span.SequenceEqual(JsonConstants.NegativeInfinityValue))
		{
			value = float.NegativeInfinity;
			return true;
		}
		value = 0f;
		return false;
	}

	public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out double value)
	{
		if (span.Length == 3)
		{
			if (span.SequenceEqual(JsonConstants.NaNValue))
			{
				value = double.NaN;
				return true;
			}
		}
		else if (span.Length == 8)
		{
			if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
			{
				value = double.PositiveInfinity;
				return true;
			}
		}
		else if (span.Length == 9 && span.SequenceEqual(JsonConstants.NegativeInfinityValue))
		{
			value = double.NegativeInfinity;
			return true;
		}
		value = 0.0;
		return false;
	}

	public static bool TryGetUnescapedBase64Bytes(ReadOnlySpan<byte> utf8Source, [NotNullWhen(true)] out byte[] bytes)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Source.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Source.Length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, out var written);
		span2 = span2.Slice(0, written);
		bool result = TryDecodeBase64InPlace(span2, out bytes);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static string GetUnescapedString(ReadOnlySpan<byte> utf8Source)
	{
		int length = utf8Source.Length;
		byte[] array = null;
		Span<byte> span = ((length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, out var written);
		span2 = span2.Slice(0, written);
		string result = TranscodeHelper(span2);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static ReadOnlySpan<byte> GetUnescapedSpan(ReadOnlySpan<byte> utf8Source)
	{
		int length = utf8Source.Length;
		byte[] array = null;
		Span<byte> span = ((length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(length))) : stackalloc byte[256]);
		Span<byte> destination = span;
		Unescape(utf8Source, destination, out var written);
		ReadOnlySpan<byte> result = destination.Slice(0, written).ToArray();
		if (array != null)
		{
			new Span<byte>(array, 0, written).Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool UnescapeAndCompare(ReadOnlySpan<byte> utf8Source, ReadOnlySpan<byte> other)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Source.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Source.Length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, 0, out var written);
		span2 = span2.Slice(0, written);
		bool result = other.SequenceEqual(span2);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool UnescapeAndCompare(ReadOnlySequence<byte> utf8Source, ReadOnlySpan<byte> other)
	{
		byte[] array = null;
		byte[] array2 = null;
		int num = checked((int)utf8Source.Length);
		Span<byte> span = ((num > 256) ? ((Span<byte>)(array2 = ArrayPool<byte>.Shared.Rent(num))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Span<byte> span3 = ((num > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(num))) : stackalloc byte[256]);
		Span<byte> span4 = span3;
		utf8Source.CopyTo(span4);
		span4 = span4.Slice(0, num);
		Unescape(span4, span2, 0, out var written);
		span2 = span2.Slice(0, written);
		bool result = other.SequenceEqual(span2);
		if (array2 != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array2);
			span4.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool TryDecodeBase64InPlace(Span<byte> utf8Unescaped, [NotNullWhen(true)] out byte[] bytes)
	{
		if (Base64.DecodeFromUtf8InPlace(utf8Unescaped, out var bytesWritten) != 0)
		{
			bytes = null;
			return false;
		}
		bytes = utf8Unescaped.Slice(0, bytesWritten).ToArray();
		return true;
	}

	public static bool TryDecodeBase64(ReadOnlySpan<byte> utf8Unescaped, [NotNullWhen(true)] out byte[] bytes)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Unescaped.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Unescaped.Length))) : stackalloc byte[256]);
		Span<byte> bytes2 = span;
		if (Base64.DecodeFromUtf8(utf8Unescaped, bytes2, out var _, out var bytesWritten) != 0)
		{
			bytes = null;
			if (array != null)
			{
				bytes2.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
			return false;
		}
		bytes = bytes2.Slice(0, bytesWritten).ToArray();
		if (array != null)
		{
			bytes2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return true;
	}

	public static string TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped)
	{
		try
		{
			return s_utf8Encoding.GetString(utf8Unescaped);
		}
		catch (DecoderFallbackException innerException)
		{
			throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(innerException);
		}
	}

	public static int TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped, Span<char> destination)
	{
		try
		{
			return s_utf8Encoding.GetChars(utf8Unescaped, destination);
		}
		catch (DecoderFallbackException innerException)
		{
			throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(innerException);
		}
		catch (ArgumentException)
		{
			destination.Clear();
			throw;
		}
	}

	public static void ValidateUtf8(ReadOnlySpan<byte> utf8Buffer)
	{
		if (!Utf8.IsValid(utf8Buffer))
		{
			throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8();
		}
	}

	internal static int GetUtf8ByteCount(ReadOnlySpan<char> text)
	{
		try
		{
			return s_utf8Encoding.GetByteCount(text);
		}
		catch (EncoderFallbackException innerException)
		{
			throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(innerException);
		}
	}

	internal static int GetUtf8FromText(ReadOnlySpan<char> text, Span<byte> dest)
	{
		try
		{
			return s_utf8Encoding.GetBytes(text, dest);
		}
		catch (EncoderFallbackException innerException)
		{
			throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(innerException);
		}
	}

	internal static string GetTextFromUtf8(ReadOnlySpan<byte> utf8Text)
	{
		return s_utf8Encoding.GetString(utf8Text);
	}

	internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
	{
		int idx = source.IndexOf<byte>(92);
		bool flag = TryUnescape(source, destination, idx, out written);
	}

	internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
	{
		bool flag = TryUnescape(source, destination, idx, out written);
	}

	internal static bool TryUnescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
	{
		int idx = source.IndexOf<byte>(92);
		return TryUnescape(source, destination, idx, out written);
	}

	private static bool TryUnescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
	{
		if (!source.Slice(0, idx).TryCopyTo(destination))
		{
			written = 0;
		}
		else
		{
			written = idx;
			while (written != destination.Length)
			{
				byte b = source[++idx];
				if ((uint)b <= 98u)
				{
					if ((uint)b <= 47u)
					{
						if (b != 34)
						{
							if (b != 47)
							{
								goto IL_0179;
							}
							destination[written++] = 47;
						}
						else
						{
							destination[written++] = 34;
						}
					}
					else if (b != 92)
					{
						if (b != 98)
						{
							goto IL_0179;
						}
						destination[written++] = 8;
					}
					else
					{
						destination[written++] = 92;
					}
				}
				else if ((uint)b <= 110u)
				{
					if (b != 102)
					{
						if (b != 110)
						{
							goto IL_0179;
						}
						destination[written++] = 10;
					}
					else
					{
						destination[written++] = 12;
					}
				}
				else if (b != 114)
				{
					if (b != 116)
					{
						goto IL_0179;
					}
					destination[written++] = 9;
				}
				else
				{
					destination[written++] = 13;
				}
				goto IL_0264;
				IL_0264:
				if (++idx != source.Length)
				{
					if (source[idx] == 92)
					{
						continue;
					}
					ReadOnlySpan<byte> span = source.Slice(idx);
					int num = span.IndexOf<byte>(92);
					if (num < 0)
					{
						num = span.Length;
					}
					if ((uint)(written + num) >= (uint)destination.Length)
					{
						break;
					}
					switch (num)
					{
					case 1:
						destination[written++] = source[idx++];
						break;
					case 2:
						destination[written++] = source[idx++];
						destination[written++] = source[idx++];
						break;
					case 3:
						destination[written++] = source[idx++];
						destination[written++] = source[idx++];
						destination[written++] = source[idx++];
						break;
					default:
						span.Slice(0, num).CopyTo(destination.Slice(written));
						written += num;
						idx += num;
						break;
					}
					if (idx != source.Length)
					{
						continue;
					}
				}
				return true;
				IL_0179:
				bool flag = Utf8Parser.TryParse(source.Slice(idx + 1, 4), out int value, out int bytesConsumed, 'x');
				idx += 4;
				if (JsonHelpers.IsInRangeInclusive((uint)value, 55296u, 57343u))
				{
					if (value >= 56320)
					{
						ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(value);
					}
					if (source.Length < idx + 7 || source[idx + 1] != 92 || source[idx + 2] != 117)
					{
						ThrowHelper.ThrowInvalidOperationException_ReadIncompleteUTF16();
					}
					flag = Utf8Parser.TryParse(source.Slice(idx + 3, 4), out int value2, out bytesConsumed, 'x');
					idx += 6;
					if (!JsonHelpers.IsInRangeInclusive((uint)value2, 56320u, 57343u))
					{
						ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(value2);
					}
					value = 1024 * (value - 55296) + (value2 - 56320) + 65536;
				}
				if (!new Rune(value).TryEncodeToUtf8(destination.Slice(written), out var bytesWritten))
				{
					break;
				}
				written += bytesWritten;
				goto IL_0264;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
	{
		return span.IndexOfAny(s_controlQuoteBackslash);
	}
}
