using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics;

internal static class BigNumber
{
	internal enum ParsingStatus
	{
		OK,
		Failed,
		Overflow
	}

	private struct BigNumberBuffer
	{
		public StringBuilder digits;

		public int precision;

		public int scale;

		public bool sign;

		public static BigNumberBuffer Create()
		{
			BigNumberBuffer result = default(BigNumberBuffer);
			result.digits = new StringBuilder();
			return result;
		}
	}

	private static int s_naiveThreshold = 20000;

	private static ReadOnlySpan<uint> UInt32PowersOfTen => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	[DoesNotReturn]
	internal static void ThrowOverflowOrFormatException(ParsingStatus status)
	{
		throw GetException(status);
	}

	private static Exception GetException(ParsingStatus status)
	{
		if (status != ParsingStatus.Failed)
		{
			return new OverflowException(System.SR.Overflow_ParseBigInteger);
		}
		return new FormatException(System.SR.Overflow_ParseBigInteger);
	}

	internal static bool TryValidateParseStyleInteger(NumberStyles style, [NotNullWhen(false)] out ArgumentException? e)
	{
		if (((uint)style & 0xFFFFFC00u) != 0)
		{
			e = new ArgumentException(System.SR.Argument_InvalidNumberStyles, "style");
			return false;
		}
		if ((style & NumberStyles.AllowHexSpecifier) != 0 && ((uint)style & 0xFFFFFDFCu) != 0)
		{
			e = new ArgumentException(System.SR.Argument_InvalidHexStyle, "style");
			return false;
		}
		e = null;
		return true;
	}

	internal static ParsingStatus TryParseBigInteger(string? value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
	{
		if (value == null)
		{
			result = default(BigInteger);
			return ParsingStatus.Failed;
		}
		return TryParseBigInteger(value.AsSpan(), style, info, out result);
	}

	internal static ParsingStatus TryParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
	{
		if (!TryValidateParseStyleInteger(style, out ArgumentException e))
		{
			throw e;
		}
		BigNumberBuffer number = BigNumberBuffer.Create();
		if (!FormatProvider.TryStringToBigInteger(value, style, info, number.digits, out number.precision, out number.scale, out number.sign))
		{
			result = default(BigInteger);
			return ParsingStatus.Failed;
		}
		if ((style & NumberStyles.AllowHexSpecifier) != 0)
		{
			return HexNumberToBigInteger(ref number, out result);
		}
		return NumberToBigInteger(ref number, out result);
	}

	internal static BigInteger ParseBigInteger(string value, NumberStyles style, NumberFormatInfo info)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		return ParseBigInteger(value.AsSpan(), style, info);
	}

	internal static BigInteger ParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info)
	{
		if (!TryValidateParseStyleInteger(style, out ArgumentException e))
		{
			throw e;
		}
		BigInteger result;
		ParsingStatus parsingStatus = TryParseBigInteger(value, style, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException(parsingStatus);
		}
		return result;
	}

	private static ParsingStatus HexNumberToBigInteger(ref BigNumberBuffer number, out BigInteger result)
	{
		if (number.digits == null || number.digits.Length == 0)
		{
			result = default(BigInteger);
			return ParsingStatus.Failed;
		}
		int a = number.digits.Length - 1;
		int result2;
		int num = Math.DivRem(a, 8, out result2);
		int num2;
		if (result2 == 0)
		{
			num2 = 0;
		}
		else
		{
			num++;
			num2 = 8 - result2;
		}
		bool flag = System.HexConverter.FromChar(number.digits[0]) >= 8;
		uint num3 = ((flag && num2 > 0) ? uint.MaxValue : 0u);
		uint[] array = null;
		Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		int num4 = num - 1;
		try
		{
			StringBuilder.ChunkEnumerator enumerator = number.digits.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlySpan<char> span2 = enumerator.Current.Span;
				for (int i = 0; i < span2.Length; i++)
				{
					char c = span2[i];
					if (c == '\0')
					{
						break;
					}
					int num5 = System.HexConverter.FromChar(c);
					num3 = (num3 << 4) | (uint)num5;
					num2++;
					if (num2 == 8)
					{
						span[num4] = num3;
						num4--;
						num3 = 0u;
						num2 = 0;
					}
				}
			}
			if (flag)
			{
				NumericsHelpers.DangerousMakeTwosComplement(span);
			}
			span = span.TrimEnd(0u);
			int n;
			uint[] rgu;
			if (span.IsEmpty)
			{
				n = 0;
				rgu = null;
			}
			else if (span.Length == 1 && span[0] <= int.MaxValue)
			{
				n = (int)span[0] * ((!flag) ? 1 : (-1));
				rgu = null;
			}
			else
			{
				n = ((!flag) ? 1 : (-1));
				rgu = span.ToArray();
			}
			result = new BigInteger(n, rgu);
			return ParsingStatus.OK;
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<uint>.Shared.Return(array);
			}
		}
	}

	private static ParsingStatus NumberToBigInteger(ref BigNumberBuffer number, out BigInteger result)
	{
		int currentBufferSize = 0;
		int totalDigitCount = 0;
		int numberScale = number.scale;
		int[] arrayFromPoolForResultBuffer = null;
		if (numberScale == int.MaxValue)
		{
			result = default(BigInteger);
			return ParsingStatus.Overflow;
		}
		if (numberScale < 0)
		{
			result = default(BigInteger);
			return ParsingStatus.Failed;
		}
		try
		{
			if (number.digits.Length <= s_naiveThreshold)
			{
				return Naive(ref number, out result);
			}
			return DivideAndConquer(ref number, out result);
		}
		finally
		{
			if (arrayFromPoolForResultBuffer != null)
			{
				ArrayPool<int>.Shared.Return(arrayFromPoolForResultBuffer);
			}
		}
		ParsingStatus DivideAndConquer(ref BigNumberBuffer number, out BigInteger result)
		{
			int[] array = null;
			try
			{
				totalDigitCount = Math.Min(number.digits.Length - 1, numberScale);
				int num = (totalDigitCount + 9 - 1) / 9;
				Span<uint> span = new uint[num];
				arrayFromPoolForResultBuffer = ArrayPool<int>.Shared.Rent(num);
				Span<uint> span2 = MemoryMarshal.Cast<int, uint>(arrayFromPoolForResultBuffer);
				Span<uint> span3 = span2.Slice(0, num);
				span3.Clear();
				int num2 = num - 1;
				uint num3 = 0u;
				int num4 = (totalDigitCount - 1) % 9;
				int num5 = totalDigitCount;
				StringBuilder.ChunkEnumerator enumerator = number.digits.GetChunks().GetEnumerator();
				while (enumerator.MoveNext())
				{
					ReadOnlySpan<char> span4 = enumerator.Current.Span;
					ReadOnlySpan<char> readOnlySpan = span4.Slice(0, Math.Min(num5, span4.Length));
					for (int i = 0; i < readOnlySpan.Length; i++)
					{
						char c = readOnlySpan[i];
						num3 *= 10;
						num3 += (uint)(c - 48);
						if (num4 == 0)
						{
							span[num2] = num3;
							num3 = 0u;
							num2--;
							num4 = 9;
						}
						num4--;
					}
					num5 -= readOnlySpan.Length;
					ReadOnlySpan<char> readOnlySpan2 = span4.Slice(readOnlySpan.Length);
					for (int j = 0; j < readOnlySpan2.Length; j++)
					{
						switch (readOnlySpan2[j])
						{
						default:
							result = default(BigInteger);
							return ParsingStatus.Failed;
						case '0':
							continue;
						case '\0':
							break;
						}
						break;
					}
				}
				int num6 = 1;
				array = ArrayPool<int>.Shared.Rent(num6);
				span2 = MemoryMarshal.Cast<int, uint>(array);
				Span<uint> span5 = span2.Slice(0, num6);
				span5[0] = 1000000000u;
				while (true)
				{
					for (int k = 0; k < num; k += num6 * 2)
					{
						Span<uint> span6 = span.Slice(k);
						Span<uint> span7 = span3.Slice(k);
						int num7 = Math.Min(num - k, num6 * 2);
						int num8 = Math.Min(num7, num6);
						int num9 = num7 - num8;
						if (num9 != 0)
						{
							BigIntegerCalculator.Multiply(span5, span6.Slice(num6, num9), span7.Slice(0, num7));
						}
						long num10 = 0L;
						int l;
						for (l = 0; l < num8; l++)
						{
							long num11 = span6[l] + num10 + span7[l];
							span7[l] = (uint)num11;
							num10 = num11 >> 32;
						}
						if (num10 != 0L)
						{
							while (true)
							{
								span7[l]++;
								if (span7[l] != 0)
								{
									break;
								}
								l++;
							}
						}
					}
					Span<uint> span8 = span;
					span = span3;
					span3 = span8;
					num6 *= 2;
					if (num <= num6)
					{
						break;
					}
					span3.Clear();
					int[] array2 = array;
					array = ArrayPool<int>.Shared.Rent(num6);
					span2 = MemoryMarshal.Cast<int, uint>(array);
					Span<uint> span9 = span2.Slice(0, num6);
					span9.Clear();
					BigIntegerCalculator.Square(span5, span9);
					span5 = span9;
					if (array2 != null)
					{
						ArrayPool<int>.Shared.Return(array2);
					}
				}
				for (currentBufferSize = Math.Min((int)((double)num * 0.9342922766870707) + 1, num); 0 < currentBufferSize && span[currentBufferSize - 1] == 0; currentBufferSize--)
				{
				}
				Span<uint> currentBuffer2 = span.Slice(0, currentBufferSize);
				result = NumberBufferToBigInteger(currentBuffer2, number.sign);
			}
			finally
			{
				if (array != null)
				{
					ArrayPool<int>.Shared.Return(array);
				}
			}
			return ParsingStatus.OK;
		}
		void MultiplyAdd(ref Span<uint> currentBuffer, uint multiplier, uint addValue)
		{
			Span<uint> span10 = currentBuffer.Slice(0, currentBufferSize);
			uint num13 = addValue;
			for (int m = 0; m < span10.Length; m++)
			{
				ulong num14 = (ulong)((long)multiplier * (long)span10[m] + num13);
				span10[m] = (uint)num14;
				num13 = (uint)(num14 >> 32);
			}
			if (num13 != 0)
			{
				if (currentBufferSize == currentBuffer.Length)
				{
					int[] array3 = arrayFromPoolForResultBuffer;
					arrayFromPoolForResultBuffer = ArrayPool<int>.Shared.Rent(checked(currentBufferSize * 2));
					Span<uint> span11 = MemoryMarshal.Cast<int, uint>(arrayFromPoolForResultBuffer);
					currentBuffer.CopyTo(span11);
					currentBuffer = span11;
					if (array3 != null)
					{
						ArrayPool<int>.Shared.Return(array3);
					}
				}
				currentBuffer[currentBufferSize] = num13;
				currentBufferSize++;
			}
		}
		ParsingStatus Naive(ref BigNumberBuffer number, out BigInteger result)
		{
			Span<uint> span12 = stackalloc uint[64];
			Span<uint> currentBuffer3 = span12;
			uint partialValue = 0u;
			int partialDigitCount = 0;
			StringBuilder.ChunkEnumerator enumerator2 = number.digits.GetChunks().GetEnumerator();
			while (enumerator2.MoveNext())
			{
				if (!ProcessChunk(enumerator2.Current.Span, ref currentBuffer3))
				{
					result = default(BigInteger);
					return ParsingStatus.Failed;
				}
			}
			if (partialDigitCount > 0)
			{
				MultiplyAdd(ref currentBuffer3, UInt32PowersOfTen[partialDigitCount], partialValue);
			}
			result = NumberBufferToBigInteger(currentBuffer3, number.sign);
			return ParsingStatus.OK;
			bool ProcessChunk(ReadOnlySpan<char> chunkDigits, ref Span<uint> currentBuffer)
			{
				int val = Math.Max(numberScale - totalDigitCount, 0);
				ReadOnlySpan<char> readOnlySpan3 = chunkDigits.Slice(0, Math.Min(val, chunkDigits.Length));
				bool flag = false;
				uint num15 = partialValue;
				int num16 = partialDigitCount;
				int num17 = totalDigitCount;
				for (int num18 = 0; num18 < readOnlySpan3.Length; num18++)
				{
					char c2 = chunkDigits[num18];
					if (c2 == '\0')
					{
						flag = true;
						break;
					}
					num15 = num15 * 10 + (uint)(c2 - 48);
					num16++;
					num17++;
					if (num16 == 9)
					{
						MultiplyAdd(ref currentBuffer, 1000000000u, num15);
						num15 = 0u;
						num16 = 0;
					}
				}
				if (!flag)
				{
					ReadOnlySpan<char> readOnlySpan4 = chunkDigits.Slice(readOnlySpan3.Length);
					for (int num19 = 0; num19 < readOnlySpan4.Length; num19++)
					{
						switch (readOnlySpan4[num19])
						{
						default:
							return false;
						case '0':
							continue;
						case '\0':
							break;
						}
						break;
					}
				}
				partialValue = num15;
				partialDigitCount = num16;
				totalDigitCount = num17;
				return true;
			}
		}
		BigInteger NumberBufferToBigInteger(Span<uint> currentBuffer, bool signa)
		{
			int num12;
			for (num12 = numberScale - totalDigitCount; num12 >= 9; num12 -= 9)
			{
				MultiplyAdd(ref currentBuffer, 1000000000u, 0u);
			}
			if (num12 > 0)
			{
				MultiplyAdd(ref currentBuffer, UInt32PowersOfTen[num12], 0u);
			}
			int n;
			uint[] rgu;
			if (currentBufferSize == 0)
			{
				n = 0;
				rgu = null;
			}
			else if (currentBufferSize == 1 && currentBuffer[0] <= int.MaxValue)
			{
				n = (int)(signa ? (0L - (long)currentBuffer[0]) : currentBuffer[0]);
				rgu = null;
			}
			else
			{
				n = ((!signa) ? 1 : (-1));
				rgu = currentBuffer.Slice(0, currentBufferSize).ToArray();
			}
			return new BigInteger(n, rgu);
		}
	}

	internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
	{
		digits = -1;
		if (format.Length == 0)
		{
			return 'R';
		}
		int num = 0;
		char c = format[num];
		if (char.IsAsciiLetter(c))
		{
			num++;
			int num2 = 0;
			while ((uint)num < (uint)format.Length && char.IsAsciiDigit(format[num]))
			{
				if (num2 >= 100000000)
				{
					throw new FormatException(System.SR.Argument_BadFormatSpecifier);
				}
				num2 = num2 * 10 + format[num++] - 48;
			}
			if (num >= format.Length || format[num] == '\0')
			{
				digits = num2;
				return c;
			}
		}
		return '\0';
	}

	private static string FormatBigIntegerToHex(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
	{
		byte[] array = null;
		Span<byte> destination2 = stackalloc byte[64];
		if (!value.TryWriteOrCountBytes(destination2, out var bytesWritten))
		{
			destination2 = (array = ArrayPool<byte>.Shared.Rent(bytesWritten));
			bool flag = value.TryWriteBytes(destination2, out bytesWritten);
		}
		destination2 = destination2.Slice(0, bytesWritten);
		Span<char> initialBuffer = stackalloc char[128];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		int num = destination2.Length - 1;
		if (num > -1)
		{
			bool flag2 = false;
			byte b = destination2[num];
			if (b > 247)
			{
				b -= 240;
				flag2 = true;
			}
			if (b < 8 || flag2)
			{
				valueStringBuilder.Append((b < 10) ? ((char)(b + 48)) : ((format == 'X') ? ((char)((b & 0xF) - 10 + 65)) : ((char)((b & 0xF) - 10 + 97))));
				num--;
			}
		}
		if (num > -1)
		{
			Span<char> span = valueStringBuilder.AppendSpan((num + 1) * 2);
			int num2 = 0;
			string text = ((format == 'x') ? "0123456789abcdef" : "0123456789ABCDEF");
			while (num > -1)
			{
				byte b2 = destination2[num--];
				span[num2++] = text[b2 >> 4];
				span[num2++] = text[b2 & 0xF];
			}
		}
		if (digits > valueStringBuilder.Length)
		{
			valueStringBuilder.Insert(0, (value._sign >= 0) ? '0' : ((format == 'x') ? 'f' : 'F'), digits - valueStringBuilder.Length);
		}
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		if (targetSpan)
		{
			spanSuccess = valueStringBuilder.TryCopyTo(destination, out charsWritten);
			return null;
		}
		charsWritten = 0;
		spanSuccess = false;
		return valueStringBuilder.ToString();
	}

	internal static string FormatBigInteger(BigInteger value, string? format, NumberFormatInfo info)
	{
		int charsWritten;
		bool spanSuccess;
		return FormatBigInteger(targetSpan: false, value, format, format, info, default(Span<char>), out charsWritten, out spanSuccess);
	}

	internal static bool TryFormatBigInteger(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out var spanSuccess);
		return spanSuccess;
	}

	private static string FormatBigInteger(bool targetSpan, BigInteger value, string formatString, ReadOnlySpan<char> formatSpan, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
	{
		int digits = 0;
		char c = ParseFormatSpecifier(formatSpan, out digits);
		if (c == 'x' || c == 'X')
		{
			return FormatBigIntegerToHex(targetSpan, value, c, digits, info, destination, out charsWritten, out spanSuccess);
		}
		if (value._bits == null)
		{
			if (c == 'g' || c == 'G' || c == 'r' || c == 'R')
			{
				formatSpan = (formatString = ((digits > 0) ? $"D{digits}" : "D"));
			}
			if (targetSpan)
			{
				spanSuccess = value._sign.TryFormat(destination, out charsWritten, formatSpan, info);
				return null;
			}
			charsWritten = 0;
			spanSuccess = false;
			return value._sign.ToString(formatString, info);
		}
		int num = value._bits.Length;
		uint[] array;
		int num3;
		int num4;
		checked
		{
			int num2;
			try
			{
				num2 = unchecked(checked(num * 10) / 9) + 2;
			}
			catch (OverflowException innerException)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException);
			}
			array = new uint[num2];
			num3 = 0;
			num4 = num;
		}
		while (--num4 >= 0)
		{
			uint num5 = value._bits[num4];
			for (int i = 0; i < num3; i++)
			{
				ulong num6 = NumericsHelpers.MakeUInt64(array[i], num5);
				array[i] = (uint)(num6 % 1000000000);
				num5 = (uint)(num6 / 1000000000);
			}
			if (num5 != 0)
			{
				array[num3++] = num5 % 1000000000;
				num5 /= 1000000000;
				if (num5 != 0)
				{
					array[num3++] = num5;
				}
			}
		}
		int num7;
		bool flag;
		char[] array2;
		int num9;
		checked
		{
			try
			{
				num7 = num3 * 9;
			}
			catch (OverflowException innerException2)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException2);
			}
			flag = c == 'g' || c == 'G' || c == 'd' || c == 'D' || c == 'r' || c == 'R';
			if (flag)
			{
				if (digits > 0 && digits > num7)
				{
					num7 = digits;
				}
				if (value._sign < 0)
				{
					try
					{
						num7 += info.NegativeSign.Length;
					}
					catch (OverflowException innerException3)
					{
						throw new FormatException(System.SR.Format_TooLarge, innerException3);
					}
				}
			}
			int num8;
			try
			{
				num8 = num7 + 1;
			}
			catch (OverflowException innerException4)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException4);
			}
			array2 = new char[num8];
			num9 = num7;
		}
		for (int j = 0; j < num3 - 1; j++)
		{
			uint num10 = array[j];
			int num11 = 9;
			while (--num11 >= 0)
			{
				array2[--num9] = (char)(48 + num10 % 10);
				num10 /= 10;
			}
		}
		for (uint num12 = array[num3 - 1]; num12 != 0; num12 /= 10)
		{
			array2[--num9] = (char)(48 + num12 % 10);
		}
		if (!flag)
		{
			bool sign = value._sign < 0;
			int precision = 29;
			int scale = num7 - num9;
			Span<char> initialBuffer = stackalloc char[128];
			System.Text.ValueStringBuilder sb = new System.Text.ValueStringBuilder(initialBuffer);
			FormatProvider.FormatBigInteger(ref sb, precision, scale, sign, formatSpan, info, array2, num9);
			if (targetSpan)
			{
				spanSuccess = sb.TryCopyTo(destination, out charsWritten);
				return null;
			}
			charsWritten = 0;
			spanSuccess = false;
			return sb.ToString();
		}
		int num13 = num7 - num9;
		while (digits > 0 && digits > num13)
		{
			array2[--num9] = '0';
			digits--;
		}
		if (value._sign < 0)
		{
			string negativeSign = info.NegativeSign;
			for (int num14 = negativeSign.Length - 1; num14 > -1; num14--)
			{
				array2[--num9] = negativeSign[num14];
			}
		}
		int num15 = num7 - num9;
		if (!targetSpan)
		{
			charsWritten = 0;
			spanSuccess = false;
			return new string(array2, num9, num7 - num9);
		}
		if (new ReadOnlySpan<char>(array2, num9, num7 - num9).TryCopyTo(destination))
		{
			charsWritten = num15;
			spanSuccess = true;
			return null;
		}
		charsWritten = 0;
		spanSuccess = false;
		return null;
	}
}
