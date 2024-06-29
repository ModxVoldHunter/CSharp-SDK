using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Char : IComparable, IComparable<char>, IEquatable<char>, IConvertible, ISpanFormattable, IFormattable, IBinaryInteger<char>, IBinaryNumber<char>, IBitwiseOperators<char, char, char>, INumber<char>, IComparisonOperators<char, char, bool>, IEqualityOperators<char, char, bool>, IModulusOperators<char, char, char>, INumberBase<char>, IAdditionOperators<char, char, char>, IAdditiveIdentity<char, char>, IDecrementOperators<char>, IDivisionOperators<char, char, char>, IIncrementOperators<char>, IMultiplicativeIdentity<char, char>, IMultiplyOperators<char, char, char>, ISpanParsable<char>, IParsable<char>, ISubtractionOperators<char, char, char>, IUnaryPlusOperators<char, char>, IUnaryNegationOperators<char, char>, IUtf8SpanFormattable, IUtf8SpanParsable<char>, IShiftOperators<char, int, char>, IMinMaxValue<char>, IUnsignedNumber<char>, IUtfChar<char>, IBinaryIntegerParseAndFormatInfo<char>
{
	private readonly char m_value;

	public const char MaxValue = '\uffff';

	public const char MinValue = '\0';

	private static ReadOnlySpan<byte> Latin1CharInfo => new byte[256]
	{
		14, 14, 14, 14, 14, 14, 14, 14, 14, 142,
		142, 142, 142, 142, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		14, 14, 139, 24, 24, 24, 26, 24, 24, 24,
		20, 21, 24, 25, 24, 19, 24, 24, 8, 8,
		8, 8, 8, 8, 8, 8, 8, 8, 24, 24,
		25, 25, 25, 24, 24, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 20, 24, 21, 27, 18, 27, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 20, 25, 21, 25, 14, 14, 14,
		14, 14, 14, 142, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		139, 24, 26, 26, 26, 26, 28, 24, 27, 28,
		4, 22, 25, 15, 28, 27, 28, 25, 10, 10,
		27, 33, 24, 24, 27, 10, 4, 23, 10, 10,
		10, 24, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 25, 64, 64, 64, 64,
		64, 64, 64, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 25, 33, 33,
		33, 33, 33, 33, 33, 33
	};

	static char IAdditiveIdentity<char, char>.AdditiveIdentity => '\0';

	static char IBinaryNumber<char>.AllBitsSet => '\uffff';

	static char IMinMaxValue<char>.MinValue => '\0';

	static char IMinMaxValue<char>.MaxValue => '\uffff';

	static char IMultiplicativeIdentity<char, char>.MultiplicativeIdentity => '\u0001';

	static char INumberBase<char>.One => '\u0001';

	static int INumberBase<char>.Radix => 2;

	static char INumberBase<char>.Zero => '\0';

	static bool IBinaryIntegerParseAndFormatInfo<char>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<char>.MaxDigitCount => 5;

	static int IBinaryIntegerParseAndFormatInfo<char>.MaxHexDigitCount => 4;

	static char IBinaryIntegerParseAndFormatInfo<char>.MaxValueDiv10 => 'ᦙ';

	static string IBinaryIntegerParseAndFormatInfo<char>.OverflowMessage => SR.Overflow_Char;

	private static bool IsLatin1(char c)
	{
		return (uint)c < (uint)Latin1CharInfo.Length;
	}

	public static bool IsAscii(char c)
	{
		return (uint)c <= 127u;
	}

	private static UnicodeCategory GetLatin1UnicodeCategory(char c)
	{
		return (UnicodeCategory)(Latin1CharInfo[c] & 0x1F);
	}

	public override int GetHashCode()
	{
		return (int)(this | ((uint)this << 16));
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is char))
		{
			return false;
		}
		return this == (char)obj;
	}

	[NonVersionable]
	public bool Equals(char obj)
	{
		return this == obj;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is char))
		{
			throw new ArgumentException(SR.Arg_MustBeChar);
		}
		return this - (char)value;
	}

	public int CompareTo(char value)
	{
		return this - value;
	}

	public override string ToString()
	{
		return ToString(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString(this);
	}

	public static string ToString(char c)
	{
		return string.CreateFromChar(c);
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		if (!destination.IsEmpty)
		{
			destination[0] = this;
			charsWritten = 1;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return new Rune(this).TryEncodeToUtf8(utf8Destination, out bytesWritten);
	}

	string IFormattable.ToString(string format, IFormatProvider formatProvider)
	{
		return ToString(this);
	}

	public static char Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan());
	}

	internal static char Parse(ReadOnlySpan<char> s)
	{
		if (s.Length != 1)
		{
			ThrowHelper.ThrowFormatException_NeedSingleChar();
		}
		return s[0];
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out char result)
	{
		if (s == null)
		{
			result = '\0';
			return false;
		}
		return TryParse(s.AsSpan(), out result);
	}

	internal static bool TryParse(ReadOnlySpan<char> s, out char result)
	{
		if (s.Length != 1)
		{
			result = '\0';
			return false;
		}
		result = s[0];
		return true;
	}

	public static bool IsAsciiLetter(char c)
	{
		return (uint)((c | 0x20) - 97) <= 25u;
	}

	public static bool IsAsciiLetterLower(char c)
	{
		return IsBetween(c, 'a', 'z');
	}

	public static bool IsAsciiLetterUpper(char c)
	{
		return IsBetween(c, 'A', 'Z');
	}

	public static bool IsAsciiDigit(char c)
	{
		return IsBetween(c, '0', '9');
	}

	public static bool IsAsciiLetterOrDigit(char c)
	{
		return IsAsciiLetter(c) | IsBetween(c, '0', '9');
	}

	public static bool IsAsciiHexDigit(char c)
	{
		return HexConverter.IsHexChar(c);
	}

	public static bool IsAsciiHexDigitUpper(char c)
	{
		return HexConverter.IsHexUpperChar(c);
	}

	public static bool IsAsciiHexDigitLower(char c)
	{
		return HexConverter.IsHexLowerChar(c);
	}

	public static bool IsDigit(char c)
	{
		if (IsLatin1(c))
		{
			return IsBetween(c, '0', '9');
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber;
	}

	public static bool IsBetween(char c, char minInclusive, char maxInclusive)
	{
		return (uint)(c - minInclusive) <= (uint)(maxInclusive - minInclusive);
	}

	private static bool IsBetween(UnicodeCategory c, UnicodeCategory min, UnicodeCategory max)
	{
		return (uint)(c - min) <= (uint)(max - min);
	}

	internal static bool CheckLetter(UnicodeCategory uc)
	{
		return IsBetween(uc, UnicodeCategory.UppercaseLetter, UnicodeCategory.OtherLetter);
	}

	public static bool IsLetter(char c)
	{
		if (IsAscii(c))
		{
			return (Latin1CharInfo[c] & 0x60) != 0;
		}
		return CheckLetter(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	private static bool IsWhiteSpaceLatin1(char c)
	{
		return (Latin1CharInfo[c] & 0x80) != 0;
	}

	public static bool IsWhiteSpace(char c)
	{
		if (IsLatin1(c))
		{
			return IsWhiteSpaceLatin1(c);
		}
		return CharUnicodeInfo.GetIsWhiteSpace(c);
	}

	public static bool IsUpper(char c)
	{
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x40) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter;
	}

	public static bool IsLower(char c)
	{
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x20) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter;
	}

	internal static bool CheckPunctuation(UnicodeCategory uc)
	{
		return IsBetween(uc, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.OtherPunctuation);
	}

	public static bool IsPunctuation(char c)
	{
		return CheckPunctuation(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategory(c));
	}

	internal static bool CheckLetterOrDigit(UnicodeCategory uc)
	{
		return (0x11F & (1 << (int)uc)) != 0;
	}

	public static bool IsLetterOrDigit(char c)
	{
		return CheckLetterOrDigit(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static char ToUpper(char c, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		return culture.TextInfo.ToUpper(c);
	}

	public static char ToUpper(char c)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToUpper(c);
	}

	public static char ToUpperInvariant(char c)
	{
		return TextInfo.ToUpperInvariant(c);
	}

	public static char ToLower(char c, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		return culture.TextInfo.ToLower(c);
	}

	public static char ToLower(char c)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToLower(c);
	}

	public static char ToLowerInvariant(char c)
	{
		return TextInfo.ToLowerInvariant(c);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Char;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Boolean"));
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return this;
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(this);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(this);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(this);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(this);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(this);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(this);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(this);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(this);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Single"));
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Double"));
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Decimal"));
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	public static bool IsControl(char c)
	{
		return (uint)((c + 1) & -129) <= 32u;
	}

	public static bool IsControl(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsControl(s[index]);
	}

	public static bool IsDigit(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return IsBetween(c, '0', '9');
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.DecimalDigitNumber;
	}

	public static bool IsLetter(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsAscii(c))
		{
			return (Latin1CharInfo[c] & 0x60) != 0;
		}
		return CheckLetter(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsLetterOrDigit(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		return CheckLetterOrDigit(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsLower(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x20) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.LowercaseLetter;
	}

	internal static bool CheckNumber(UnicodeCategory uc)
	{
		return IsBetween(uc, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherNumber);
	}

	public static bool IsNumber(char c)
	{
		if (IsLatin1(c))
		{
			if (IsAscii(c))
			{
				return IsBetween(c, '0', '9');
			}
			return CheckNumber(GetLatin1UnicodeCategory(c));
		}
		return CheckNumber(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsNumber(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			if (IsAscii(c))
			{
				return IsBetween(c, '0', '9');
			}
			return CheckNumber(GetLatin1UnicodeCategory(c));
		}
		return CheckNumber(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsPunctuation(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		return CheckPunctuation(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	internal static bool CheckSeparator(UnicodeCategory uc)
	{
		return IsBetween(uc, UnicodeCategory.SpaceSeparator, UnicodeCategory.ParagraphSeparator);
	}

	private static bool IsSeparatorLatin1(char c)
	{
		if (c != ' ')
		{
			return c == '\u00a0';
		}
		return true;
	}

	public static bool IsSeparator(char c)
	{
		if (IsLatin1(c))
		{
			return IsSeparatorLatin1(c);
		}
		return CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsSeparator(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return IsSeparatorLatin1(c);
		}
		return CheckSeparator(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsSurrogate(char c)
	{
		return IsBetween(c, '\ud800', '\udfff');
	}

	public static bool IsSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsSurrogate(s[index]);
	}

	internal static bool CheckSymbol(UnicodeCategory uc)
	{
		return IsBetween(uc, UnicodeCategory.MathSymbol, UnicodeCategory.OtherSymbol);
	}

	public static bool IsSymbol(char c)
	{
		return CheckSymbol(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsSymbol(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		return CheckSymbol(IsLatin1(c) ? GetLatin1UnicodeCategory(c) : CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsUpper(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x40) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.UppercaseLetter;
	}

	public static bool IsWhiteSpace(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsWhiteSpace(s[index]);
	}

	public static UnicodeCategory GetUnicodeCategory(char c)
	{
		if (IsLatin1(c))
		{
			return GetLatin1UnicodeCategory(c);
		}
		return CharUnicodeInfo.GetUnicodeCategory((int)c);
	}

	public static UnicodeCategory GetUnicodeCategory(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		if (IsLatin1(s[index]))
		{
			return GetLatin1UnicodeCategory(s[index]);
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index);
	}

	public static double GetNumericValue(char c)
	{
		return CharUnicodeInfo.GetNumericValue(c);
	}

	public static double GetNumericValue(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return CharUnicodeInfo.GetNumericValueInternal(s, index);
	}

	public static bool IsHighSurrogate(char c)
	{
		return IsBetween(c, '\ud800', '\udbff');
	}

	public static bool IsHighSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsHighSurrogate(s[index]);
	}

	public static bool IsLowSurrogate(char c)
	{
		return IsBetween(c, '\udc00', '\udfff');
	}

	public static bool IsLowSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsLowSurrogate(s[index]);
	}

	public static bool IsSurrogatePair(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		if ((uint)(index + 1) < (uint)s.Length)
		{
			return IsSurrogatePair(s[index], s[index + 1]);
		}
		return false;
	}

	public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate)
	{
		uint num = (uint)(highSurrogate - 55296);
		uint num2 = (uint)(lowSurrogate - 56320);
		return (num | num2) <= 1023;
	}

	public static string ConvertFromUtf32(int utf32)
	{
		if (!UnicodeUtility.IsValidUnicodeScalar((uint)utf32))
		{
			throw new ArgumentOutOfRangeException("utf32", SR.ArgumentOutOfRange_InvalidUTF32);
		}
		return Rune.UnsafeCreate((uint)utf32).ToString();
	}

	public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
	{
		uint num = (uint)(highSurrogate - 55296);
		uint num2 = (uint)(lowSurrogate - 56320);
		if ((num | num2) > 1023)
		{
			ConvertToUtf32_ThrowInvalidArgs(num);
		}
		return (int)(num << 10) + (lowSurrogate - 56320) + 65536;
	}

	[StackTraceHidden]
	private static void ConvertToUtf32_ThrowInvalidArgs(uint highSurrogateOffset)
	{
		if (highSurrogateOffset > 1023)
		{
			throw new ArgumentOutOfRangeException("highSurrogate", SR.ArgumentOutOfRange_InvalidHighSurrogate);
		}
		throw new ArgumentOutOfRangeException("lowSurrogate", SR.ArgumentOutOfRange_InvalidLowSurrogate);
	}

	public static int ConvertToUtf32(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		int num = s[index] - 55296;
		if ((uint)num <= 2047u)
		{
			bool flag = true;
			if (num <= 1023)
			{
				if ((uint)(index + 1) < (uint)s.Length)
				{
					int num2 = s[index + 1] - 56320;
					if ((uint)num2 <= 1023u)
					{
						return num * 1024 + num2 + 65536;
					}
				}
				flag = false;
			}
			throw new ArgumentException(SR.Format(flag ? SR.Argument_InvalidLowSurrogate : SR.Argument_InvalidHighSurrogate, index), "s");
		}
		return s[index];
	}

	static char IAdditionOperators<char, char, char>.operator +(char left, char right)
	{
		return (char)(left + right);
	}

	static char IAdditionOperators<char, char, char>.operator checked +(char left, char right)
	{
		return (char)checked((ushort)(left + right));
	}

	static char IBinaryInteger<char>.LeadingZeroCount(char value)
	{
		return (char)(BitOperations.LeadingZeroCount(value) - 16);
	}

	static char IBinaryInteger<char>.PopCount(char value)
	{
		return (char)BitOperations.PopCount(value);
	}

	static char IBinaryInteger<char>.RotateLeft(char value, int rotateAmount)
	{
		return (char)(((uint)value << (rotateAmount & 0xF)) | (uint)((int)value >> ((16 - rotateAmount) & 0xF)));
	}

	static char IBinaryInteger<char>.RotateRight(char value, int rotateAmount)
	{
		return (char)((uint)((int)value >> (rotateAmount & 0xF)) | ((uint)value << ((16 - rotateAmount) & 0xF)));
	}

	static char IBinaryInteger<char>.TrailingZeroCount(char value)
	{
		return (char)(BitOperations.TrailingZeroCount((int)((uint)value << 16)) - 16);
	}

	static bool IBinaryInteger<char>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out char value)
	{
		char c = '\0';
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = c;
				return false;
			}
			if (source.Length > 2)
			{
				if (source.Slice(0, source.Length - 2).ContainsAnyExcept<byte>(0))
				{
					value = c;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 2)
			{
				c = Unsafe.ReadUnaligned<char>(ref Unsafe.Add(ref reference, source.Length - 2));
				_ = BitConverter.IsLittleEndian;
				c = BinaryPrimitives.ReverseEndianness(c);
			}
			else
			{
				c = (char)reference;
			}
		}
		value = c;
		return true;
	}

	static bool IBinaryInteger<char>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out char value)
	{
		char c = '\0';
		if (source.Length != 0)
		{
			if (!isUnsigned)
			{
				if (sbyte.IsNegative((sbyte)source[source.Length - 1]))
				{
					value = c;
					return false;
				}
			}
			if (source.Length > 2)
			{
				if (source.Slice(2, source.Length - 2).ContainsAnyExcept<byte>(0))
				{
					value = c;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 2)
			{
				c = Unsafe.ReadUnaligned<char>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_0076;
				}
			}
			c = (char)reference;
		}
		goto IL_0076;
		IL_0076:
		value = c;
		return true;
	}

	int IBinaryInteger<char>.GetShortestBitLength()
	{
		return 16 - ushort.LeadingZeroCount(this);
	}

	int IBinaryInteger<char>.GetByteCount()
	{
		return 2;
	}

	bool IBinaryInteger<char>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ushort value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<char>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ushort value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	static bool IBinaryNumber<char>.IsPow2(char value)
	{
		return ushort.IsPow2(value);
	}

	static char IBinaryNumber<char>.Log2(char value)
	{
		return (char)ushort.Log2(value);
	}

	static char IBitwiseOperators<char, char, char>.operator &(char left, char right)
	{
		return (char)(left & right);
	}

	static char IBitwiseOperators<char, char, char>.operator |(char left, char right)
	{
		return (char)(left | right);
	}

	static char IBitwiseOperators<char, char, char>.operator ^(char left, char right)
	{
		return (char)(left ^ right);
	}

	static char IBitwiseOperators<char, char, char>.operator ~(char value)
	{
		return (char)(~(uint)value);
	}

	static bool IComparisonOperators<char, char, bool>.operator <(char left, char right)
	{
		return left < right;
	}

	static bool IComparisonOperators<char, char, bool>.operator <=(char left, char right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<char, char, bool>.operator >(char left, char right)
	{
		return left > right;
	}

	static bool IComparisonOperators<char, char, bool>.operator >=(char left, char right)
	{
		return left >= right;
	}

	static char IDecrementOperators<char>.operator --(char value)
	{
		return value = (char)(value - 1);
	}

	static char IDecrementOperators<char>.operator checked --(char value)
	{
		return value = (char)checked((ushort)(unchecked((uint)value) - 1u));
	}

	static char IDivisionOperators<char, char, char>.operator /(char left, char right)
	{
		return (char)(left / right);
	}

	static bool IEqualityOperators<char, char, bool>.operator ==(char left, char right)
	{
		return left == right;
	}

	static bool IEqualityOperators<char, char, bool>.operator !=(char left, char right)
	{
		return left != right;
	}

	static char IIncrementOperators<char>.operator ++(char value)
	{
		return value = (char)(value + 1);
	}

	static char IIncrementOperators<char>.operator checked ++(char value)
	{
		return value = (char)checked((ushort)(unchecked((uint)value) + 1u));
	}

	static char IModulusOperators<char, char, char>.operator %(char left, char right)
	{
		return (char)(left % right);
	}

	static char IMultiplyOperators<char, char, char>.operator *(char left, char right)
	{
		return (char)(left * right);
	}

	static char IMultiplyOperators<char, char, char>.operator checked *(char left, char right)
	{
		return (char)checked((ushort)(left * right));
	}

	static char INumberBase<char>.Abs(char value)
	{
		return value;
	}

	static bool INumberBase<char>.IsCanonical(char value)
	{
		return true;
	}

	static bool INumberBase<char>.IsComplexNumber(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsEvenInteger(char value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<char>.IsFinite(char value)
	{
		return true;
	}

	static bool INumberBase<char>.IsImaginaryNumber(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsInfinity(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsInteger(char value)
	{
		return true;
	}

	static bool INumberBase<char>.IsNaN(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsNegative(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsNegativeInfinity(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsNormal(char value)
	{
		return value != '\0';
	}

	static bool INumberBase<char>.IsOddInteger(char value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<char>.IsPositive(char value)
	{
		return true;
	}

	static bool INumberBase<char>.IsPositiveInfinity(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsRealNumber(char value)
	{
		return true;
	}

	static bool INumberBase<char>.IsSubnormal(char value)
	{
		return false;
	}

	static bool INumberBase<char>.IsZero(char value)
	{
		return value == '\0';
	}

	static char INumberBase<char>.MaxMagnitude(char x, char y)
	{
		return (char)Math.Max(x, y);
	}

	static char INumberBase<char>.MaxMagnitudeNumber(char x, char y)
	{
		return (char)Math.Max(x, y);
	}

	static char INumberBase<char>.MinMagnitude(char x, char y)
	{
		return (char)Math.Min(x, y);
	}

	static char INumberBase<char>.MinMagnitudeNumber(char x, char y)
	{
		return (char)Math.Min(x, y);
	}

	static char INumberBase<char>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s);
	}

	static char INumberBase<char>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertFromChecked<TOther>(TOther value, out char result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = (char)b;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = (char)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = (char)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = (char)checked((ushort)num3);
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = (char)checked((ushort)num4);
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = checked((char)uInt);
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = (char)checked((ushort)num5);
			return true;
		}
		result = '\0';
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertFromSaturating<TOther>(TOther value, out char result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = (char)b;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num >= 65535m) ? '\uffff' : ((!(num <= 0m)) ? ((char)num) : '\0'));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = (char)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = ((num3 >= 65535) ? '\uffff' : ((char)num3));
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = ((num4 >= 65535) ? '\uffff' : ((char)num4));
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= '\uffff') ? '\uffff' : ((char)uInt));
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = ((num5 >= 65535) ? '\uffff' : ((char)num5));
			return true;
		}
		result = '\0';
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertFromTruncating<TOther>(TOther value, out char result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = (char)b;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num >= 65535m) ? '\uffff' : ((!(num <= 0m)) ? ((char)num) : '\0'));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = (char)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = (char)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = (char)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (char)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = (char)num5;
			return true;
		}
		result = '\0';
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertToChecked<TOther>(char value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = checked((short)value);
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = checked((sbyte)value);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertToSaturating<TOther>(char value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = ((value >= '翿') ? short.MaxValue : ((short)value));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = ((value >= '\u007f') ? sbyte.MaxValue : ((sbyte)value));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<char>.TryConvertToTruncating<TOther>(char value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)value;
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	static bool INumberBase<char>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	static bool INumberBase<char>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	static char IParsable<char>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool IParsable<char>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	static char IShiftOperators<char, int, char>.operator <<(char value, int shiftAmount)
	{
		return (char)((uint)value << shiftAmount);
	}

	static char IShiftOperators<char, int, char>.operator >>(char value, int shiftAmount)
	{
		return (char)((int)value >> shiftAmount);
	}

	static char IShiftOperators<char, int, char>.operator >>>(char value, int shiftAmount)
	{
		return (char)((uint)value >> shiftAmount);
	}

	static char ISpanParsable<char>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool ISpanParsable<char>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	static char ISubtractionOperators<char, char, char>.operator -(char left, char right)
	{
		return (char)(left - right);
	}

	static char ISubtractionOperators<char, char, char>.operator checked -(char left, char right)
	{
		return (char)checked((ushort)(left - right));
	}

	static char IUnaryNegationOperators<char, char>.operator -(char value)
	{
		return (char)(0 - value);
	}

	static char IUnaryNegationOperators<char, char>.operator checked -(char value)
	{
		return (char)checked((ushort)(0 - value));
	}

	static char IUnaryPlusOperators<char, char>.operator +(char value)
	{
		return value;
	}

	static char IUtfChar<char>.CastFrom(byte value)
	{
		return (char)value;
	}

	static char IUtfChar<char>.CastFrom(char value)
	{
		return value;
	}

	static char IUtfChar<char>.CastFrom(int value)
	{
		return (char)value;
	}

	static char IUtfChar<char>.CastFrom(uint value)
	{
		return (char)value;
	}

	static char IUtfChar<char>.CastFrom(ulong value)
	{
		return (char)value;
	}

	static uint IUtfChar<char>.CastToUInt32(char value)
	{
		return value;
	}

	static bool IBinaryIntegerParseAndFormatInfo<char>.IsGreaterThanAsUnsigned(char left, char right)
	{
		return left > right;
	}

	static char IBinaryIntegerParseAndFormatInfo<char>.MultiplyBy10(char value)
	{
		return (char)(value * 10);
	}

	static char IBinaryIntegerParseAndFormatInfo<char>.MultiplyBy16(char value)
	{
		return (char)(value * 16);
	}
}
