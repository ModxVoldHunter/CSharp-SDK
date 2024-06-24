using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[NonVersionable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Guid : ISpanFormattable, IFormattable, IComparable, IComparable<Guid>, IEquatable<Guid>, ISpanParsable<Guid>, IParsable<Guid>, IUtf8SpanFormattable
{
	private enum GuidParseThrowStyle : byte
	{
		None,
		All,
		AllButOverflow
	}

	private enum ParseFailure
	{
		Format_ExtraJunkAtEnd,
		Format_GuidBraceAfterLastNumber,
		Format_GuidBrace,
		Format_GuidComma,
		Format_GuidDashes,
		Format_GuidEndBrace,
		Format_GuidHexPrefix,
		Format_GuidInvalidChar,
		Format_GuidInvLen,
		Format_GuidUnrecognized,
		Overflow_Byte,
		Overflow_UInt32
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct GuidResult
	{
		[FieldOffset(0)]
		internal uint _a;

		[FieldOffset(4)]
		internal uint _bc;

		[FieldOffset(4)]
		internal ushort _b;

		[FieldOffset(6)]
		internal ushort _c;

		[FieldOffset(8)]
		internal uint _defg;

		[FieldOffset(8)]
		internal ushort _de;

		[FieldOffset(8)]
		internal byte _d;

		[FieldOffset(10)]
		internal ushort _fg;

		[FieldOffset(12)]
		internal uint _hijk;

		[FieldOffset(16)]
		private readonly GuidParseThrowStyle _throwStyle;

		internal GuidResult(GuidParseThrowStyle canThrow)
		{
			this = default(GuidResult);
			_throwStyle = canThrow;
		}

		internal readonly void SetFailure(ParseFailure failureKind)
		{
			if (_throwStyle == GuidParseThrowStyle.None)
			{
				return;
			}
			if (failureKind == ParseFailure.Overflow_UInt32 && _throwStyle == GuidParseThrowStyle.All)
			{
				throw new OverflowException(SR.Overflow_UInt32);
			}
			throw new FormatException(failureKind switch
			{
				ParseFailure.Format_ExtraJunkAtEnd => SR.Format_ExtraJunkAtEnd, 
				ParseFailure.Format_GuidBraceAfterLastNumber => SR.Format_GuidBraceAfterLastNumber, 
				ParseFailure.Format_GuidBrace => SR.Format_GuidBrace, 
				ParseFailure.Format_GuidComma => SR.Format_GuidComma, 
				ParseFailure.Format_GuidDashes => SR.Format_GuidDashes, 
				ParseFailure.Format_GuidEndBrace => SR.Format_GuidEndBrace, 
				ParseFailure.Format_GuidHexPrefix => SR.Format_GuidHexPrefix, 
				ParseFailure.Format_GuidInvalidChar => SR.Format_GuidInvalidChar, 
				ParseFailure.Format_GuidInvLen => SR.Format_GuidInvLen, 
				_ => SR.Format_GuidUnrecognized, 
			});
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Guid ToGuid()
		{
			return Unsafe.As<GuidResult, Guid>(ref Unsafe.AsRef(ref this));
		}
	}

	public static readonly Guid Empty;

	private readonly int _a;

	private readonly short _b;

	private readonly short _c;

	private readonly byte _d;

	private readonly byte _e;

	private readonly byte _f;

	private readonly byte _g;

	private readonly byte _h;

	private readonly byte _i;

	private readonly byte _j;

	private readonly byte _k;

	public Guid(byte[] b)
		: this(new ReadOnlySpan<byte>(b ?? throw new ArgumentNullException("b")))
	{
	}

	public Guid(ReadOnlySpan<byte> b)
	{
		if (b.Length != 16)
		{
			ThrowGuidArrayCtorArgumentException();
		}
		this = MemoryMarshal.Read<Guid>(b);
		if (BitConverter.IsLittleEndian)
		{
		}
	}

	public Guid(ReadOnlySpan<byte> b, bool bigEndian)
	{
		if (b.Length != 16)
		{
			ThrowGuidArrayCtorArgumentException();
		}
		this = MemoryMarshal.Read<Guid>(b);
		if (BitConverter.IsLittleEndian == bigEndian)
		{
			_a = BinaryPrimitives.ReverseEndianness(_a);
			_b = BinaryPrimitives.ReverseEndianness(_b);
			_c = BinaryPrimitives.ReverseEndianness(_c);
		}
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private static void ThrowGuidArrayCtorArgumentException()
	{
		throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "16"), "b");
	}

	[CLSCompliant(false)]
	public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
	{
		_a = (int)a;
		_b = (short)b;
		_c = (short)c;
		_d = d;
		_e = e;
		_f = f;
		_g = g;
		_h = h;
		_i = i;
		_j = j;
		_k = k;
	}

	public Guid(int a, short b, short c, byte[] d)
	{
		ArgumentNullException.ThrowIfNull(d, "d");
		if (d.Length != 8)
		{
			throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "8"), "d");
		}
		_a = a;
		_b = b;
		_c = c;
		_k = d[7];
		_d = d[0];
		_e = d[1];
		_f = d[2];
		_g = d[3];
		_h = d[4];
		_i = d[5];
		_j = d[6];
	}

	public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
	{
		_a = a;
		_b = b;
		_c = c;
		_d = d;
		_e = e;
		_f = f;
		_g = g;
		_h = h;
		_i = i;
		_j = j;
		_k = k;
	}

	public Guid(string g)
	{
		ArgumentNullException.ThrowIfNull(g, "g");
		GuidResult result = new GuidResult(GuidParseThrowStyle.All);
		bool flag = TryParseGuid(g, ref result);
		this = result.ToGuid();
	}

	public static Guid Parse(string input)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		return Parse((ReadOnlySpan<char>)input);
	}

	public static Guid Parse(ReadOnlySpan<char> input)
	{
		GuidResult result = new GuidResult(GuidParseThrowStyle.AllButOverflow);
		bool flag = TryParseGuid(input, ref result);
		return result.ToGuid();
	}

	public static bool TryParse([NotNullWhen(true)] string? input, out Guid result)
	{
		if (input == null)
		{
			result = default(Guid);
			return false;
		}
		return TryParse((ReadOnlySpan<char>)input, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> input, out Guid result)
	{
		GuidResult result2 = new GuidResult(GuidParseThrowStyle.None);
		if (TryParseGuid(input, ref result2))
		{
			result = result2.ToGuid();
			return true;
		}
		result = default(Guid);
		return false;
	}

	public static Guid ParseExact(string input, [StringSyntax("GuidFormat")] string format)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		ArgumentNullException.ThrowIfNull(format, "format");
		return ParseExact((ReadOnlySpan<char>)input, (ReadOnlySpan<char>)format);
	}

	public static Guid ParseExact(ReadOnlySpan<char> input, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format)
	{
		if (format.Length != 1)
		{
			ThrowBadGuidFormatSpecification();
		}
		input = input.Trim();
		GuidResult result = new GuidResult(GuidParseThrowStyle.AllButOverflow);
		bool flag = (char)(ushort)(format[0] | 0x20) switch
		{
			'd' => TryParseExactD(input, ref result), 
			'n' => TryParseExactN(input, ref result), 
			'b' => TryParseExactB(input, ref result), 
			'p' => TryParseExactP(input, ref result), 
			'x' => TryParseExactX(input, ref result), 
			_ => throw new FormatException(SR.Format_InvalidGuidFormatSpecification), 
		};
		return result.ToGuid();
	}

	public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)][StringSyntax("GuidFormat")] string? format, out Guid result)
	{
		if (input == null)
		{
			result = default(Guid);
			return false;
		}
		return TryParseExact((ReadOnlySpan<char>)input, (ReadOnlySpan<char>)format, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> input, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format, out Guid result)
	{
		if (format.Length != 1 || input.Length < 32)
		{
			result = default(Guid);
			return false;
		}
		input = input.Trim();
		GuidResult result2 = new GuidResult(GuidParseThrowStyle.None);
		bool flag = false;
		switch ((char)(ushort)(format[0] | 0x20))
		{
		case 'd':
			flag = TryParseExactD(input, ref result2);
			break;
		case 'n':
			flag = TryParseExactN(input, ref result2);
			break;
		case 'b':
			flag = TryParseExactB(input, ref result2);
			break;
		case 'p':
			flag = TryParseExactP(input, ref result2);
			break;
		case 'x':
			flag = TryParseExactX(input, ref result2);
			break;
		}
		if (flag)
		{
			result = result2.ToGuid();
			return true;
		}
		result = default(Guid);
		return false;
	}

	private static bool TryParseGuid(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		guidString = guidString.Trim();
		if (guidString.Length < 32)
		{
			result.SetFailure(ParseFailure.Format_GuidUnrecognized);
			return false;
		}
		return guidString[0] switch
		{
			'(' => TryParseExactP(guidString, ref result), 
			'{' => (guidString[9] == '-') ? TryParseExactB(guidString, ref result) : TryParseExactX(guidString, ref result), 
			_ => (guidString[8] == '-') ? TryParseExactD(guidString, ref result) : TryParseExactN(guidString, ref result), 
		};
	}

	private static bool TryParseExactB(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		if (guidString.Length != 38 || guidString[0] != '{' || guidString[37] != '}')
		{
			result.SetFailure(ParseFailure.Format_GuidInvLen);
			return false;
		}
		return TryParseExactD(guidString.Slice(1, 36), ref result);
	}

	private static bool TryParseExactD(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		if (guidString.Length != 36 || guidString[8] != '-' || guidString[13] != '-' || guidString[18] != '-' || guidString[23] != '-')
		{
			result.SetFailure((guidString.Length != 36) ? ParseFailure.Format_GuidInvLen : ParseFailure.Format_GuidDashes);
			return false;
		}
		Span<byte> span = MemoryMarshal.AsBytes(new Span<GuidResult>(ref result));
		int invalidIfNegative = 0;
		span[0] = DecodeByte(guidString[6], guidString[7], ref invalidIfNegative);
		span[1] = DecodeByte(guidString[4], guidString[5], ref invalidIfNegative);
		span[2] = DecodeByte(guidString[2], guidString[3], ref invalidIfNegative);
		span[3] = DecodeByte(guidString[0], guidString[1], ref invalidIfNegative);
		span[4] = DecodeByte(guidString[11], guidString[12], ref invalidIfNegative);
		span[5] = DecodeByte(guidString[9], guidString[10], ref invalidIfNegative);
		span[6] = DecodeByte(guidString[16], guidString[17], ref invalidIfNegative);
		span[7] = DecodeByte(guidString[14], guidString[15], ref invalidIfNegative);
		span[8] = DecodeByte(guidString[19], guidString[20], ref invalidIfNegative);
		span[9] = DecodeByte(guidString[21], guidString[22], ref invalidIfNegative);
		span[10] = DecodeByte(guidString[24], guidString[25], ref invalidIfNegative);
		span[11] = DecodeByte(guidString[26], guidString[27], ref invalidIfNegative);
		span[12] = DecodeByte(guidString[28], guidString[29], ref invalidIfNegative);
		span[13] = DecodeByte(guidString[30], guidString[31], ref invalidIfNegative);
		span[14] = DecodeByte(guidString[32], guidString[33], ref invalidIfNegative);
		span[15] = DecodeByte(guidString[34], guidString[35], ref invalidIfNegative);
		if (invalidIfNegative >= 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return true;
		}
		if (guidString.ContainsAny('X', 'x', '+') && TryCompatParsing(guidString, ref result))
		{
			return true;
		}
		result.SetFailure(ParseFailure.Format_GuidInvalidChar);
		return false;
		static bool TryCompatParsing(ReadOnlySpan<char> guidString, ref GuidResult result)
		{
			if (TryParseHex(guidString.Slice(0, 8), out result._a) && TryParseHex(guidString.Slice(9, 4), out var result2))
			{
				result._b = (ushort)result2;
				if (TryParseHex(guidString.Slice(14, 4), out result2))
				{
					result._c = (ushort)result2;
					if (TryParseHex(guidString.Slice(19, 4), out result2))
					{
						if (!BitConverter.IsLittleEndian)
						{
						}
						result._de = BinaryPrimitives.ReverseEndianness((ushort)result2);
						if (TryParseHex(guidString.Slice(24, 4), out result2))
						{
							if (!BitConverter.IsLittleEndian)
							{
							}
							result._fg = BinaryPrimitives.ReverseEndianness((ushort)result2);
							if (Number.TryParseBinaryIntegerHexNumberStyle<char, uint>(guidString.Slice(28, 8), NumberStyles.AllowHexSpecifier, out result2) == Number.ParsingStatus.OK)
							{
								if (!BitConverter.IsLittleEndian)
								{
								}
								result._hijk = BinaryPrimitives.ReverseEndianness(result2);
								return true;
							}
						}
					}
				}
			}
			return false;
		}
	}

	private static bool TryParseExactN(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		if (guidString.Length != 32)
		{
			result.SetFailure(ParseFailure.Format_GuidInvLen);
			return false;
		}
		Span<byte> span = MemoryMarshal.AsBytes(new Span<GuidResult>(ref result));
		int invalidIfNegative = 0;
		span[0] = DecodeByte(guidString[6], guidString[7], ref invalidIfNegative);
		span[1] = DecodeByte(guidString[4], guidString[5], ref invalidIfNegative);
		span[2] = DecodeByte(guidString[2], guidString[3], ref invalidIfNegative);
		span[3] = DecodeByte(guidString[0], guidString[1], ref invalidIfNegative);
		span[4] = DecodeByte(guidString[10], guidString[11], ref invalidIfNegative);
		span[5] = DecodeByte(guidString[8], guidString[9], ref invalidIfNegative);
		span[6] = DecodeByte(guidString[14], guidString[15], ref invalidIfNegative);
		span[7] = DecodeByte(guidString[12], guidString[13], ref invalidIfNegative);
		span[8] = DecodeByte(guidString[16], guidString[17], ref invalidIfNegative);
		span[9] = DecodeByte(guidString[18], guidString[19], ref invalidIfNegative);
		span[10] = DecodeByte(guidString[20], guidString[21], ref invalidIfNegative);
		span[11] = DecodeByte(guidString[22], guidString[23], ref invalidIfNegative);
		span[12] = DecodeByte(guidString[24], guidString[25], ref invalidIfNegative);
		span[13] = DecodeByte(guidString[26], guidString[27], ref invalidIfNegative);
		span[14] = DecodeByte(guidString[28], guidString[29], ref invalidIfNegative);
		span[15] = DecodeByte(guidString[30], guidString[31], ref invalidIfNegative);
		if (invalidIfNegative >= 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return true;
		}
		result.SetFailure(ParseFailure.Format_GuidInvalidChar);
		return false;
	}

	private static bool TryParseExactP(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		if (guidString.Length != 38 || guidString[0] != '(' || guidString[37] != ')')
		{
			result.SetFailure(ParseFailure.Format_GuidInvLen);
			return false;
		}
		return TryParseExactD(guidString.Slice(1, 36), ref result);
	}

	private static bool TryParseExactX(ReadOnlySpan<char> guidString, ref GuidResult result)
	{
		guidString = EatAllWhitespace(guidString);
		if (guidString.Length == 0 || guidString[0] != '{')
		{
			result.SetFailure(ParseFailure.Format_GuidBrace);
			return false;
		}
		if (!IsHexPrefix(guidString, 1))
		{
			result.SetFailure(ParseFailure.Format_GuidHexPrefix);
			return false;
		}
		int num = 3;
		int num2 = guidString.Slice(num).IndexOf(',');
		if (num2 <= 0)
		{
			result.SetFailure(ParseFailure.Format_GuidComma);
			return false;
		}
		bool overflow = false;
		if (!TryParseHex(guidString.Slice(num, num2), out result._a, ref overflow) || overflow)
		{
			result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ParseFailure.Format_GuidInvalidChar);
			return false;
		}
		if (!IsHexPrefix(guidString, num + num2 + 1))
		{
			result.SetFailure(ParseFailure.Format_GuidHexPrefix);
			return false;
		}
		num = num + num2 + 3;
		num2 = guidString.Slice(num).IndexOf(',');
		if (num2 <= 0)
		{
			result.SetFailure(ParseFailure.Format_GuidComma);
			return false;
		}
		if (!TryParseHex(guidString.Slice(num, num2), out result._b, ref overflow) || overflow)
		{
			result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ParseFailure.Format_GuidInvalidChar);
			return false;
		}
		if (!IsHexPrefix(guidString, num + num2 + 1))
		{
			result.SetFailure(ParseFailure.Format_GuidHexPrefix);
			return false;
		}
		num = num + num2 + 3;
		num2 = guidString.Slice(num).IndexOf(',');
		if (num2 <= 0)
		{
			result.SetFailure(ParseFailure.Format_GuidComma);
			return false;
		}
		if (!TryParseHex(guidString.Slice(num, num2), out result._c, ref overflow) || overflow)
		{
			result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ParseFailure.Format_GuidInvalidChar);
			return false;
		}
		if ((uint)guidString.Length <= (uint)(num + num2 + 1) || guidString[num + num2 + 1] != '{')
		{
			result.SetFailure(ParseFailure.Format_GuidBrace);
			return false;
		}
		num2++;
		for (int i = 0; i < 8; i++)
		{
			if (!IsHexPrefix(guidString, num + num2 + 1))
			{
				result.SetFailure(ParseFailure.Format_GuidHexPrefix);
				return false;
			}
			num = num + num2 + 3;
			if (i < 7)
			{
				num2 = guidString.Slice(num).IndexOf(',');
				if (num2 <= 0)
				{
					result.SetFailure(ParseFailure.Format_GuidComma);
					return false;
				}
			}
			else
			{
				num2 = guidString.Slice(num).IndexOf('}');
				if (num2 <= 0)
				{
					result.SetFailure(ParseFailure.Format_GuidBraceAfterLastNumber);
					return false;
				}
			}
			if (!TryParseHex(guidString.Slice(num, num2), out uint result2, ref overflow) || overflow || result2 > 255)
			{
				result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ((result2 > 255) ? ParseFailure.Overflow_Byte : ParseFailure.Format_GuidInvalidChar));
				return false;
			}
			Unsafe.Add(ref result._d, i) = (byte)result2;
		}
		if (num + num2 + 1 >= guidString.Length || guidString[num + num2 + 1] != '}')
		{
			result.SetFailure(ParseFailure.Format_GuidEndBrace);
			return false;
		}
		if (num + num2 + 1 != guidString.Length - 1)
		{
			result.SetFailure(ParseFailure.Format_ExtraJunkAtEnd);
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte DecodeByte(char ch1, char ch2, ref int invalidIfNegative)
	{
		ReadOnlySpan<byte> charToHexLookup = HexConverter.CharToHexLookup;
		int num = (sbyte)charToHexLookup[(byte)ch1];
		int num2 = (sbyte)charToHexLookup[(byte)ch2];
		int num3 = (num << 4) | num2;
		num3 = (((ch1 | ch2) >> 8 == 0) ? num3 : (-1));
		invalidIfNegative |= num3;
		return (byte)num3;
	}

	private static bool TryParseHex(ReadOnlySpan<char> guidString, out ushort result, ref bool overflow)
	{
		uint result2;
		bool result3 = TryParseHex(guidString, out result2, ref overflow);
		result = (ushort)result2;
		return result3;
	}

	private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result)
	{
		bool overflow = false;
		return TryParseHex(guidString, out result, ref overflow);
	}

	private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result, ref bool overflow)
	{
		if (guidString.Length > 0)
		{
			if (guidString[0] == '+')
			{
				guidString = guidString.Slice(1);
			}
			if (guidString.Length > 1 && guidString[0] == '0' && (guidString[1] | 0x20) == 120)
			{
				guidString = guidString.Slice(2);
			}
		}
		int i;
		for (i = 0; i < guidString.Length && guidString[i] == '0'; i++)
		{
		}
		int num = 0;
		uint num2 = 0u;
		for (; i < guidString.Length; i++)
		{
			char c = guidString[i];
			int num3 = HexConverter.FromChar(c);
			if (num3 == 255)
			{
				if (num > 8)
				{
					overflow = true;
				}
				result = 0u;
				return false;
			}
			num2 = num2 * 16 + (uint)num3;
			num++;
		}
		if (num > 8)
		{
			overflow = true;
		}
		result = num2;
		return true;
	}

	private static ReadOnlySpan<char> EatAllWhitespace(ReadOnlySpan<char> str)
	{
		int i;
		for (i = 0; i < str.Length && !char.IsWhiteSpace(str[i]); i++)
		{
		}
		if (i == str.Length)
		{
			return str;
		}
		char[] array = new char[str.Length];
		int length = 0;
		if (i > 0)
		{
			length = i;
			str.Slice(0, i).CopyTo(array);
		}
		for (; i < str.Length; i++)
		{
			char c = str[i];
			if (!char.IsWhiteSpace(c))
			{
				array[length++] = c;
			}
		}
		return new ReadOnlySpan<char>(array, 0, length);
	}

	private static bool IsHexPrefix(ReadOnlySpan<char> str, int i)
	{
		if (i + 1 < str.Length && str[i] == '0')
		{
			return (str[i + 1] | 0x20) == 120;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static ReadOnlySpan<byte> AsBytes(in Guid source)
	{
		return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(ref source)), sizeof(Guid));
	}

	public byte[] ToByteArray()
	{
		byte[] array = new byte[16];
		_ = BitConverter.IsLittleEndian;
		MemoryMarshal.TryWrite(array, in this);
		return array;
	}

	public byte[] ToByteArray(bool bigEndian)
	{
		byte[] array = new byte[16];
		if (BitConverter.IsLittleEndian != bigEndian)
		{
			MemoryMarshal.TryWrite(array, in this);
		}
		else
		{
			Guid value = new Guid(AsBytes(in this), bigEndian);
			MemoryMarshal.TryWrite(array, in value);
		}
		return array;
	}

	public bool TryWriteBytes(Span<byte> destination)
	{
		if (destination.Length < 16)
		{
			return false;
		}
		_ = BitConverter.IsLittleEndian;
		MemoryMarshal.TryWrite(destination, in this);
		return true;
	}

	public bool TryWriteBytes(Span<byte> destination, bool bigEndian, out int bytesWritten)
	{
		if (destination.Length < 16)
		{
			bytesWritten = 0;
			return false;
		}
		if (BitConverter.IsLittleEndian != bigEndian)
		{
			MemoryMarshal.TryWrite(destination, in this);
		}
		else
		{
			Guid value = new Guid(AsBytes(in this), bigEndian);
			MemoryMarshal.TryWrite(destination, in value);
		}
		bytesWritten = 16;
		return true;
	}

	public override int GetHashCode()
	{
		ref int reference = ref Unsafe.AsRef(ref _a);
		return reference ^ Unsafe.Add(ref reference, 1) ^ Unsafe.Add(ref reference, 2) ^ Unsafe.Add(ref reference, 3);
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is Guid right)
		{
			return EqualsCore(in this, in right);
		}
		return false;
	}

	public bool Equals(Guid g)
	{
		return EqualsCore(in this, in g);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool EqualsCore(in Guid left, in Guid right)
	{
		if (Vector128.IsHardwareAccelerated)
		{
			return Vector128.LoadUnsafe(ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(ref left))) == Vector128.LoadUnsafe(ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(ref right)));
		}
		ref int reference = ref Unsafe.AsRef(ref left._a);
		ref int reference2 = ref Unsafe.AsRef(ref right._a);
		if (reference == reference2 && Unsafe.Add(ref reference, 1) == Unsafe.Add(ref reference2, 1) && Unsafe.Add(ref reference, 2) == Unsafe.Add(ref reference2, 2))
		{
			return Unsafe.Add(ref reference, 3) == Unsafe.Add(ref reference2, 3);
		}
		return false;
	}

	private static int GetResult(uint me, uint them)
	{
		if (me >= them)
		{
			return 1;
		}
		return -1;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is Guid guid))
		{
			throw new ArgumentException(SR.Arg_MustBeGuid, "value");
		}
		if (guid._a != _a)
		{
			return GetResult((uint)_a, (uint)guid._a);
		}
		if (guid._b != _b)
		{
			return GetResult((uint)_b, (uint)guid._b);
		}
		if (guid._c != _c)
		{
			return GetResult((uint)_c, (uint)guid._c);
		}
		if (guid._d != _d)
		{
			return GetResult(_d, guid._d);
		}
		if (guid._e != _e)
		{
			return GetResult(_e, guid._e);
		}
		if (guid._f != _f)
		{
			return GetResult(_f, guid._f);
		}
		if (guid._g != _g)
		{
			return GetResult(_g, guid._g);
		}
		if (guid._h != _h)
		{
			return GetResult(_h, guid._h);
		}
		if (guid._i != _i)
		{
			return GetResult(_i, guid._i);
		}
		if (guid._j != _j)
		{
			return GetResult(_j, guid._j);
		}
		if (guid._k != _k)
		{
			return GetResult(_k, guid._k);
		}
		return 0;
	}

	public int CompareTo(Guid value)
	{
		if (value._a != _a)
		{
			return GetResult((uint)_a, (uint)value._a);
		}
		if (value._b != _b)
		{
			return GetResult((uint)_b, (uint)value._b);
		}
		if (value._c != _c)
		{
			return GetResult((uint)_c, (uint)value._c);
		}
		if (value._d != _d)
		{
			return GetResult(_d, value._d);
		}
		if (value._e != _e)
		{
			return GetResult(_e, value._e);
		}
		if (value._f != _f)
		{
			return GetResult(_f, value._f);
		}
		if (value._g != _g)
		{
			return GetResult(_g, value._g);
		}
		if (value._h != _h)
		{
			return GetResult(_h, value._h);
		}
		if (value._i != _i)
		{
			return GetResult(_i, value._i);
		}
		if (value._j != _j)
		{
			return GetResult(_j, value._j);
		}
		if (value._k != _k)
		{
			return GetResult(_k, value._k);
		}
		return 0;
	}

	public static bool operator ==(Guid a, Guid b)
	{
		return EqualsCore(in a, in b);
	}

	public static bool operator !=(Guid a, Guid b)
	{
		return !EqualsCore(in a, in b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int HexsToChars<TChar>(TChar* guidChars, int a, int b) where TChar : unmanaged, IUtfChar<TChar>
	{
		*guidChars = TChar.CastFrom(HexConverter.ToCharLower(a >> 4));
		guidChars[1] = TChar.CastFrom(HexConverter.ToCharLower(a));
		guidChars[2] = TChar.CastFrom(HexConverter.ToCharLower(b >> 4));
		guidChars[3] = TChar.CastFrom(HexConverter.ToCharLower(b));
		return 4;
	}

	private unsafe static int HexsToCharsHexOutput<TChar>(TChar* guidChars, int a, int b) where TChar : unmanaged, IUtfChar<TChar>
	{
		*guidChars = TChar.CastFrom('0');
		guidChars[1] = TChar.CastFrom('x');
		guidChars[2] = TChar.CastFrom(HexConverter.ToCharLower(a >> 4));
		guidChars[3] = TChar.CastFrom(HexConverter.ToCharLower(a));
		guidChars[4] = TChar.CastFrom(',');
		guidChars[5] = TChar.CastFrom('0');
		guidChars[6] = TChar.CastFrom('x');
		guidChars[7] = TChar.CastFrom(HexConverter.ToCharLower(b >> 4));
		guidChars[8] = TChar.CastFrom(HexConverter.ToCharLower(b));
		return 9;
	}

	public override string ToString()
	{
		return ToString("d", null);
	}

	public string ToString([StringSyntax("GuidFormat")] string? format)
	{
		return ToString(format, null);
	}

	public string ToString([StringSyntax("GuidFormat")] string? format, IFormatProvider? provider)
	{
		int length;
		if (string.IsNullOrEmpty(format))
		{
			length = 36;
		}
		else
		{
			if (format.Length != 1)
			{
				ThrowBadGuidFormatSpecification();
			}
			switch (format[0] | 0x20)
			{
			case 100:
				length = 36;
				break;
			case 110:
				length = 32;
				break;
			case 98:
			case 112:
				length = 38;
				break;
			case 120:
				length = 68;
				break;
			default:
				length = 0;
				ThrowBadGuidFormatSpecification();
				break;
			}
		}
		string text = string.FastAllocateString(length);
		int charsWritten;
		bool flag = TryFormatCore(new Span<char>(ref text.GetRawStringData(), text.Length), out charsWritten, format);
		return text;
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>))
	{
		return TryFormatCore(destination, out charsWritten, format);
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormatCore(destination, out charsWritten, format);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>))
	{
		return TryFormatCore(utf8Destination, out bytesWritten, format);
	}

	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormatCore(utf8Destination, out bytesWritten, format);
	}

	private bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format) where TChar : unmanaged, IUtfChar<TChar>
	{
		int flags;
		if (format.Length == 0)
		{
			flags = -2147483612;
		}
		else
		{
			if (format.Length != 1)
			{
				ThrowBadGuidFormatSpecification();
			}
			switch (format[0] | 0x20)
			{
			case 100:
				flags = -2147483612;
				break;
			case 112:
				flags = -2144786394;
				break;
			case 98:
				flags = -2139260122;
				break;
			case 110:
				flags = 32;
				break;
			case 120:
				return TryFormatX(destination, out charsWritten);
			default:
				flags = 0;
				ThrowBadGuidFormatSpecification();
				break;
			}
		}
		return TryFormatCore(destination, out charsWritten, flags);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, int flags) where TChar : unmanaged, IUtfChar<TChar>
	{
		if ((byte)flags > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = (byte)flags;
		flags >>= 8;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = ptr;
			if ((byte)flags != 0)
			{
				*(ptr2++) = TChar.CastFrom((byte)flags);
			}
			flags >>= 8;
			if (Ssse3.IsSupported ? true : false)
			{
				_ = BitConverter.IsLittleEndian;
				var (source, source2, source3) = FormatGuidVector128Utf8(this, flags < 0);
				if (typeof(TChar) == typeof(byte))
				{
					byte* ptr3 = (byte*)ptr2;
					if (flags < 0)
					{
						source.Store(ptr3);
						source2.Store(ptr3 + 20);
						source3.Store(ptr3 + 8);
						ptr2 += 36;
					}
					else
					{
						source.Store(ptr3);
						source2.Store(ptr3 + 16);
						ptr2 += 32;
					}
				}
				else
				{
					(Vector128<ushort> Lower, Vector128<ushort> Upper) tuple2 = Vector128.Widen(source);
					Vector128<ushort> item = tuple2.Lower;
					Vector128<ushort> item2 = tuple2.Upper;
					(Vector128<ushort> Lower, Vector128<ushort> Upper) tuple3 = Vector128.Widen(source2);
					Vector128<ushort> item3 = tuple3.Lower;
					Vector128<ushort> item4 = tuple3.Upper;
					ushort* ptr4 = (ushort*)ptr2;
					if (flags < 0)
					{
						var (source4, source5) = Vector128.Widen(source3);
						item.Store(ptr4);
						item3.Store(ptr4 + 20);
						item4.Store(ptr4 + 28);
						source4.Store(ptr4 + 8);
						source5.Store(ptr4 + 16);
						ptr2 += 36;
					}
					else
					{
						item.Store(ptr4);
						item2.Store(ptr4 + 8);
						item3.Store(ptr4 + 16);
						item4.Store(ptr4 + 24);
						ptr2 += 32;
					}
				}
			}
			else
			{
				ptr2 += HexsToChars(ptr2, _a >> 24, _a >> 16);
				ptr2 += HexsToChars(ptr2, _a >> 8, _a);
				if (flags < 0)
				{
					*(ptr2++) = TChar.CastFrom('-');
				}
				ptr2 += HexsToChars(ptr2, _b >> 8, _b);
				if (flags < 0)
				{
					*(ptr2++) = TChar.CastFrom('-');
				}
				ptr2 += HexsToChars(ptr2, _c >> 8, _c);
				if (flags < 0)
				{
					*(ptr2++) = TChar.CastFrom('-');
				}
				ptr2 += HexsToChars(ptr2, _d, _e);
				if (flags < 0)
				{
					*(ptr2++) = TChar.CastFrom('-');
				}
				ptr2 += HexsToChars(ptr2, _f, _g);
				ptr2 += HexsToChars(ptr2, _h, _i);
				ptr2 += HexsToChars(ptr2, _j, _k);
			}
			if ((byte)flags != 0)
			{
				*ptr2 = TChar.CastFrom((byte)flags);
			}
		}
		return true;
	}

	private unsafe bool TryFormatX<TChar>(Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 68)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 68;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = ptr;
			*(ptr2++) = TChar.CastFrom('{');
			*(ptr2++) = TChar.CastFrom('0');
			*(ptr2++) = TChar.CastFrom('x');
			ptr2 += HexsToChars(ptr2, _a >> 24, _a >> 16);
			ptr2 += HexsToChars(ptr2, _a >> 8, _a);
			*(ptr2++) = TChar.CastFrom(',');
			*(ptr2++) = TChar.CastFrom('0');
			*(ptr2++) = TChar.CastFrom('x');
			ptr2 += HexsToChars(ptr2, _b >> 8, _b);
			*(ptr2++) = TChar.CastFrom(',');
			*(ptr2++) = TChar.CastFrom('0');
			*(ptr2++) = TChar.CastFrom('x');
			ptr2 += HexsToChars(ptr2, _c >> 8, _c);
			*(ptr2++) = TChar.CastFrom(',');
			*(ptr2++) = TChar.CastFrom('{');
			ptr2 += HexsToCharsHexOutput(ptr2, _d, _e);
			*(ptr2++) = TChar.CastFrom(',');
			ptr2 += HexsToCharsHexOutput(ptr2, _f, _g);
			*(ptr2++) = TChar.CastFrom(',');
			ptr2 += HexsToCharsHexOutput(ptr2, _h, _i);
			*(ptr2++) = TChar.CastFrom(',');
			ptr2 += HexsToCharsHexOutput(ptr2, _j, _k);
			*(ptr2++) = TChar.CastFrom('}');
			*ptr2 = TChar.CastFrom('}');
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	private static (Vector128<byte>, Vector128<byte>, Vector128<byte>) FormatGuidVector128Utf8(Guid value, bool useDashes)
	{
		Vector128<byte> hexMap = Vector128.Create((byte)48, (byte)49, (byte)50, (byte)51, (byte)52, (byte)53, (byte)54, (byte)55, (byte)56, (byte)57, (byte)97, (byte)98, (byte)99, (byte)100, (byte)101, (byte)102);
		Vector128<byte> src = Unsafe.As<Guid, Vector128<byte>>(ref value);
		(Vector128<byte>, Vector128<byte>) tuple = HexConverter.AsciiToHexVector128(src, hexMap);
		Vector128<byte> item = tuple.Item1;
		Vector128<byte> item2 = tuple.Item2;
		item = Vector128.Shuffle(item.AsInt16(), Vector128.Create(3, 2, 1, 0, 5, 4, 7, 6)).AsByte();
		if (useDashes)
		{
			Vector128<byte> item3 = Vector128.Shuffle(item, Vector128.Create(506097522914230528L, 940406845091678463L).AsByte());
			Vector128<byte> item4 = Vector128.Shuffle(item2, Vector128.Create(506097527142154753L, 1084818905618843912L).AsByte());
			Vector128<byte> vector = Vector128.Create(49478023249965L, 3242591731709706240L).AsByte();
			if (false)
			{
			}
			Vector128<byte> vector2 = Vector128.Shuffle(item, Vector128.Create(940406845091678463uL, 18446744073709489934uL).AsByte());
			Vector128<byte> vector3 = Vector128.Shuffle(item2, Vector128.Create(ulong.MaxValue, 18375533107936755711uL).AsByte());
			Vector128<byte> item5 = vector2 | vector3 | vector;
			return (item3, item4, item5);
		}
		return (item, item2, default(Vector128<byte>));
	}

	public static bool operator <(Guid left, Guid right)
	{
		if (left._a != right._a)
		{
			return (uint)left._a < (uint)right._a;
		}
		if (left._b != right._b)
		{
			return (uint)left._b < (uint)right._b;
		}
		if (left._c != right._c)
		{
			return (uint)left._c < (uint)right._c;
		}
		if (left._d != right._d)
		{
			return left._d < right._d;
		}
		if (left._e != right._e)
		{
			return left._e < right._e;
		}
		if (left._f != right._f)
		{
			return left._f < right._f;
		}
		if (left._g != right._g)
		{
			return left._g < right._g;
		}
		if (left._h != right._h)
		{
			return left._h < right._h;
		}
		if (left._i != right._i)
		{
			return left._i < right._i;
		}
		if (left._j != right._j)
		{
			return left._j < right._j;
		}
		if (left._k != right._k)
		{
			return left._k < right._k;
		}
		return false;
	}

	public static bool operator <=(Guid left, Guid right)
	{
		if (left._a != right._a)
		{
			return (uint)left._a < (uint)right._a;
		}
		if (left._b != right._b)
		{
			return (uint)left._b < (uint)right._b;
		}
		if (left._c != right._c)
		{
			return (uint)left._c < (uint)right._c;
		}
		if (left._d != right._d)
		{
			return left._d < right._d;
		}
		if (left._e != right._e)
		{
			return left._e < right._e;
		}
		if (left._f != right._f)
		{
			return left._f < right._f;
		}
		if (left._g != right._g)
		{
			return left._g < right._g;
		}
		if (left._h != right._h)
		{
			return left._h < right._h;
		}
		if (left._i != right._i)
		{
			return left._i < right._i;
		}
		if (left._j != right._j)
		{
			return left._j < right._j;
		}
		if (left._k != right._k)
		{
			return left._k < right._k;
		}
		return true;
	}

	public static bool operator >(Guid left, Guid right)
	{
		if (left._a != right._a)
		{
			return (uint)left._a > (uint)right._a;
		}
		if (left._b != right._b)
		{
			return (uint)left._b > (uint)right._b;
		}
		if (left._c != right._c)
		{
			return (uint)left._c > (uint)right._c;
		}
		if (left._d != right._d)
		{
			return left._d > right._d;
		}
		if (left._e != right._e)
		{
			return left._e > right._e;
		}
		if (left._f != right._f)
		{
			return left._f > right._f;
		}
		if (left._g != right._g)
		{
			return left._g > right._g;
		}
		if (left._h != right._h)
		{
			return left._h > right._h;
		}
		if (left._i != right._i)
		{
			return left._i > right._i;
		}
		if (left._j != right._j)
		{
			return left._j > right._j;
		}
		if (left._k != right._k)
		{
			return left._k > right._k;
		}
		return false;
	}

	public static bool operator >=(Guid left, Guid right)
	{
		if (left._a != right._a)
		{
			return (uint)left._a > (uint)right._a;
		}
		if (left._b != right._b)
		{
			return (uint)left._b > (uint)right._b;
		}
		if (left._c != right._c)
		{
			return (uint)left._c > (uint)right._c;
		}
		if (left._d != right._d)
		{
			return left._d > right._d;
		}
		if (left._e != right._e)
		{
			return left._e > right._e;
		}
		if (left._f != right._f)
		{
			return left._f > right._f;
		}
		if (left._g != right._g)
		{
			return left._g > right._g;
		}
		if (left._h != right._h)
		{
			return left._h > right._h;
		}
		if (left._i != right._i)
		{
			return left._i > right._i;
		}
		if (left._j != right._j)
		{
			return left._j > right._j;
		}
		if (left._k != right._k)
		{
			return left._k > right._k;
		}
		return true;
	}

	public static Guid Parse(string s, IFormatProvider? provider)
	{
		return Parse(s);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Guid result)
	{
		return TryParse(s, out result);
	}

	public static Guid Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Guid result)
	{
		return TryParse(s, out result);
	}

	[DoesNotReturn]
	private static void ThrowBadGuidFormatSpecification()
	{
		throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
	}

	public static Guid NewGuid()
	{
		Guid guid;
		int num = Interop.Ole32.CoCreateGuid(out guid);
		if (num != 0)
		{
			Exception ex = new Exception();
			ex.HResult = num;
			throw ex;
		}
		return guid;
	}
}
