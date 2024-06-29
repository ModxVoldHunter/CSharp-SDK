using System.Runtime.CompilerServices;

namespace System;

internal static class ParseNumbers
{
	public static long StringToLong(ReadOnlySpan<char> s, int radix, int flags)
	{
		int currPos = 0;
		return StringToLong(s, radix, flags, ref currPos);
	}

	public static long StringToLong(ReadOnlySpan<char> s, int radix, int flags, ref int currPos)
	{
		int i = currPos;
		int num = ((-1 == radix) ? 10 : radix);
		if (num != 2 && num != 10 && num != 8 && num != 16)
		{
			throw new ArgumentException(SR.Arg_InvalidBase, "radix");
		}
		int length = s.Length;
		if (i < 0 || i >= length)
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_IndexMustBeLess);
		}
		if ((flags & 0x1000) == 0)
		{
			EatWhiteSpace(s, ref i);
			if (i == length)
			{
				throw new FormatException(SR.Format_EmptyInputString);
			}
		}
		int num2 = 1;
		if (s[i] == '-')
		{
			if (num != 10)
			{
				throw new ArgumentException(SR.Arg_CannotHaveNegativeValue);
			}
			if (((uint)flags & 0x200u) != 0)
			{
				throw new OverflowException(SR.Overflow_NegativeUnsigned);
			}
			num2 = -1;
			i++;
		}
		else if (s[i] == '+')
		{
			i++;
		}
		if ((radix == -1 || radix == 16) && i + 1 < length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
		{
			num = 16;
			i += 2;
		}
		int num3 = i;
		long num4 = GrabLongs(num, s, ref i, (flags & 0x200) != 0);
		if (i == num3)
		{
			throw new FormatException(SR.Format_NoParsibleDigits);
		}
		if (((uint)flags & 0x1000u) != 0 && i < length)
		{
			throw new FormatException(SR.Format_ExtraJunkAtEnd);
		}
		currPos = i;
		if (num4 == long.MinValue && num2 == 1 && num == 10 && (flags & 0x200) == 0)
		{
			Number.ThrowOverflowException<long>();
		}
		if (num == 10)
		{
			num4 *= num2;
		}
		return num4;
	}

	public static int StringToInt(ReadOnlySpan<char> s, int radix, int flags)
	{
		int currPos = 0;
		return StringToInt(s, radix, flags, ref currPos);
	}

	public static int StringToInt(ReadOnlySpan<char> s, int radix, int flags, ref int currPos)
	{
		int i = currPos;
		int num = ((-1 == radix) ? 10 : radix);
		if (num != 2 && num != 10 && num != 8 && num != 16)
		{
			throw new ArgumentException(SR.Arg_InvalidBase, "radix");
		}
		int length = s.Length;
		if (i < 0 || i >= length)
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_IndexMustBeLess);
		}
		if ((flags & 0x1000) == 0)
		{
			EatWhiteSpace(s, ref i);
			if (i == length)
			{
				throw new FormatException(SR.Format_EmptyInputString);
			}
		}
		int num2 = 1;
		if (s[i] == '-')
		{
			if (num != 10)
			{
				throw new ArgumentException(SR.Arg_CannotHaveNegativeValue);
			}
			if (((uint)flags & 0x200u) != 0)
			{
				throw new OverflowException(SR.Overflow_NegativeUnsigned);
			}
			num2 = -1;
			i++;
		}
		else if (s[i] == '+')
		{
			i++;
		}
		if ((radix == -1 || radix == 16) && i + 1 < length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
		{
			num = 16;
			i += 2;
		}
		int num3 = i;
		int num4 = GrabInts(num, s, ref i, (flags & 0x200) != 0);
		if (i == num3)
		{
			throw new FormatException(SR.Format_NoParsibleDigits);
		}
		if (((uint)flags & 0x1000u) != 0 && i < length)
		{
			throw new FormatException(SR.Format_ExtraJunkAtEnd);
		}
		currPos = i;
		if (((uint)flags & 0x400u) != 0)
		{
			if ((uint)num4 > 255u)
			{
				Number.ThrowOverflowException<sbyte>();
			}
		}
		else if (((uint)flags & 0x800u) != 0)
		{
			if ((uint)num4 > 65535u)
			{
				Number.ThrowOverflowException<short>();
			}
		}
		else if (num4 == int.MinValue && num2 == 1 && num == 10 && (flags & 0x200) == 0)
		{
			Number.ThrowOverflowException<int>();
		}
		if (num == 10)
		{
			num4 *= num2;
		}
		return num4;
	}

	private static void EatWhiteSpace(ReadOnlySpan<char> s, ref int i)
	{
		int j;
		for (j = i; j < s.Length && char.IsWhiteSpace(s[j]); j++)
		{
		}
		i = j;
	}

	private static long GrabLongs(int radix, ReadOnlySpan<char> s, ref int i, bool isUnsigned)
	{
		ulong num = 0uL;
		if (radix == 10 && !isUnsigned)
		{
			ulong num2 = 922337203685477580uL;
			int result;
			while (i < s.Length && IsDigit(s[i], radix, out result))
			{
				if (num > num2 || (long)num < 0L)
				{
					Number.ThrowOverflowException<long>();
				}
				num = (ulong)((long)num * (long)radix + result);
				i++;
			}
			if ((long)num < 0L && num != 9223372036854775808uL)
			{
				Number.ThrowOverflowException<long>();
			}
		}
		else
		{
			ulong num2 = radix switch
			{
				8 => 2305843009213693951uL, 
				16 => 1152921504606846975uL, 
				10 => 1844674407370955161uL, 
				_ => 9223372036854775807uL, 
			};
			int result2;
			while (i < s.Length && IsDigit(s[i], radix, out result2))
			{
				if (num > num2)
				{
					Number.ThrowOverflowException<ulong>();
				}
				ulong num3 = (ulong)((long)num * (long)radix + result2);
				if (num3 < num)
				{
					Number.ThrowOverflowException<ulong>();
				}
				num = num3;
				i++;
			}
		}
		return (long)num;
	}

	private static int GrabInts(int radix, ReadOnlySpan<char> s, ref int i, bool isUnsigned)
	{
		uint num = 0u;
		if (radix == 10 && !isUnsigned)
		{
			uint num2 = 214748364u;
			int result;
			while (i < s.Length && IsDigit(s[i], radix, out result))
			{
				if (num > num2 || (int)num < 0)
				{
					Number.ThrowOverflowException<int>();
				}
				num = (uint)((int)num * radix + result);
				i++;
			}
			if ((int)num < 0 && num != 2147483648u)
			{
				Number.ThrowOverflowException<int>();
			}
		}
		else
		{
			uint num2 = radix switch
			{
				8 => 536870911u, 
				16 => 268435455u, 
				10 => 429496729u, 
				_ => 2147483647u, 
			};
			int result2;
			while (i < s.Length && IsDigit(s[i], radix, out result2))
			{
				if (num > num2)
				{
					Number.ThrowOverflowException<uint>();
				}
				uint num3 = (uint)((int)num * radix + result2);
				if (num3 < num)
				{
					Number.ThrowOverflowException<uint>();
				}
				num = num3;
				i++;
			}
		}
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsDigit(char c, int radix, out int result)
	{
		int num;
		switch (c)
		{
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
			num = (result = c - 48);
			break;
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
			num = (result = c - 65 + 10);
			break;
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		case 'g':
		case 'h':
		case 'i':
		case 'j':
		case 'k':
		case 'l':
		case 'm':
		case 'n':
		case 'o':
		case 'p':
		case 'q':
		case 'r':
		case 's':
		case 't':
		case 'u':
		case 'v':
		case 'w':
		case 'x':
		case 'y':
		case 'z':
			num = (result = c - 97 + 10);
			break;
		default:
			result = -1;
			return false;
		}
		return num < radix;
	}
}
