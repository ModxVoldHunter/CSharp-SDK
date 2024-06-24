using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct UIntPtr : IEquatable<nuint>, IComparable, IComparable<nuint>, ISpanFormattable, IFormattable, ISerializable, IBinaryInteger<nuint>, IBinaryNumber<nuint>, IBitwiseOperators<nuint, nuint, nuint>, INumber<nuint>, IComparisonOperators<nuint, nuint, bool>, IEqualityOperators<nuint, nuint, bool>, IModulusOperators<nuint, nuint, nuint>, INumberBase<nuint>, IAdditionOperators<nuint, nuint, nuint>, IAdditiveIdentity<nuint, nuint>, IDecrementOperators<nuint>, IDivisionOperators<nuint, nuint, nuint>, IIncrementOperators<nuint>, IMultiplicativeIdentity<nuint, nuint>, IMultiplyOperators<nuint, nuint, nuint>, ISpanParsable<nuint>, IParsable<nuint>, ISubtractionOperators<nuint, nuint, nuint>, IUnaryPlusOperators<nuint, nuint>, IUnaryNegationOperators<nuint, nuint>, IUtf8SpanFormattable, IUtf8SpanParsable<nuint>, IShiftOperators<nuint, int, nuint>, IMinMaxValue<nuint>, IUnsignedNumber<nuint>
{
	private readonly nuint _value;

	[Intrinsic]
	public static readonly nuint Zero;

	public static int Size
	{
		[NonVersionable]
		get
		{
			return 8;
		}
	}

	public static nuint MaxValue
	{
		[NonVersionable]
		get
		{
			return unchecked((nuint)(-1));
		}
	}

	public static nuint MinValue
	{
		[NonVersionable]
		get
		{
			return 0u;
		}
	}

	static nuint IAdditiveIdentity<nuint, nuint>.AdditiveIdentity => 0u;

	static nuint IBinaryNumber<nuint>.AllBitsSet
	{
		[NonVersionable]
		get
		{
			return unchecked((nuint)(-1));
		}
	}

	static nuint IMinMaxValue<nuint>.MinValue => MinValue;

	static nuint IMinMaxValue<nuint>.MaxValue => MaxValue;

	static nuint IMultiplicativeIdentity<nuint, nuint>.MultiplicativeIdentity => 1u;

	static nuint INumberBase<nuint>.One => 1u;

	static int INumberBase<nuint>.Radix => 2;

	static nuint INumberBase<nuint>.Zero => 0u;

	[NonVersionable]
	public UIntPtr(uint value)
	{
		_value = value;
	}

	[NonVersionable]
	public UIntPtr(ulong value)
	{
		_value = (nuint)value;
	}

	[NonVersionable]
	public unsafe UIntPtr(void* value)
	{
		_value = (nuint)value;
	}

	private UIntPtr(SerializationInfo info, StreamingContext context)
	{
		ulong uInt = info.GetUInt64("value");
		_value = (nuint)uInt;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		ulong value = (ulong)this;
		info.AddValue("value", value);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is nuint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((ulong)this).GetHashCode();
	}

	[NonVersionable]
	public uint ToUInt32()
	{
		return checked((uint)this);
	}

	[NonVersionable]
	public ulong ToUInt64()
	{
		return (ulong)this;
	}

	[NonVersionable]
	public static explicit operator nuint(uint value)
	{
		return value;
	}

	[NonVersionable]
	public static explicit operator nuint(ulong value)
	{
		return checked((nuint)value);
	}

	[NonVersionable]
	public unsafe static explicit operator nuint(void* value)
	{
		return (nuint)value;
	}

	[NonVersionable]
	public unsafe static explicit operator void*(nuint value)
	{
		return (void*)value;
	}

	[NonVersionable]
	public static explicit operator uint(nuint value)
	{
		return checked((uint)value);
	}

	[NonVersionable]
	public static explicit operator ulong(nuint value)
	{
		return value;
	}

	[NonVersionable]
	public static bool operator ==(nuint value1, nuint value2)
	{
		return value1 == value2;
	}

	[NonVersionable]
	public static bool operator !=(nuint value1, nuint value2)
	{
		return value1 != value2;
	}

	[NonVersionable]
	public static nuint Add(nuint pointer, int offset)
	{
		return pointer + (nuint)offset;
	}

	[NonVersionable]
	public static nuint operator +(nuint pointer, int offset)
	{
		return pointer + (nuint)offset;
	}

	[NonVersionable]
	public static nuint Subtract(nuint pointer, int offset)
	{
		return pointer - (nuint)offset;
	}

	[NonVersionable]
	public static nuint operator -(nuint pointer, int offset)
	{
		return pointer - (nuint)offset;
	}

	[NonVersionable]
	public unsafe void* ToPointer()
	{
		return (void*)this;
	}

	public int CompareTo(object? value)
	{
		if (value is nuint value2)
		{
			return CompareTo(value2);
		}
		if (value == null)
		{
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeUIntPtr);
	}

	public int CompareTo(nuint value)
	{
		if ((nuint)this < value)
		{
			return -1;
		}
		if ((nuint)this > value)
		{
			return 1;
		}
		return 0;
	}

	[NonVersionable]
	public bool Equals(nuint other)
	{
		return this == (UIntPtr)other;
	}

	public override string ToString()
	{
		return ((ulong)this).ToString();
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ((ulong)this).ToString(format);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ((ulong)this).ToString(provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return ((ulong)this).ToString(format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((ulong)this).TryFormat(destination, out charsWritten, format, provider);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((ulong)this).TryFormat(utf8Destination, out bytesWritten, format, provider);
	}

	public static nuint Parse(string s)
	{
		return (nuint)ulong.Parse(s);
	}

	public static nuint Parse(string s, NumberStyles style)
	{
		return (nuint)ulong.Parse(s, style);
	}

	public static nuint Parse(string s, IFormatProvider? provider)
	{
		return (nuint)ulong.Parse(s, provider);
	}

	public static nuint Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		return (nuint)ulong.Parse(s, style, provider);
	}

	public static nuint Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return (nuint)ulong.Parse(s, provider);
	}

	public static nuint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (nuint)ulong.Parse(s, style, provider);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, provider, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, style, provider, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(utf8Text, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, provider, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(s, style, provider, out Unsafe.As<nuint, ulong>(ref result));
	}

	static nuint IAdditionOperators<nuint, nuint, nuint>.operator +(nuint left, nuint right)
	{
		return left + right;
	}

	static nuint IAdditionOperators<nuint, nuint, nuint>.operator checked +(nuint left, nuint right)
	{
		return checked(left + right);
	}

	public static (nuint Quotient, nuint Remainder) DivRem(nuint left, nuint right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static nuint LeadingZeroCount(nuint value)
	{
		return (nuint)BitOperations.LeadingZeroCount(value);
	}

	[Intrinsic]
	public static nuint PopCount(nuint value)
	{
		return (nuint)BitOperations.PopCount(value);
	}

	[Intrinsic]
	public static nuint RotateLeft(nuint value, int rotateAmount)
	{
		return BitOperations.RotateLeft(value, rotateAmount);
	}

	[Intrinsic]
	public static nuint RotateRight(nuint value, int rotateAmount)
	{
		return BitOperations.RotateRight(value, rotateAmount);
	}

	[Intrinsic]
	public static nuint TrailingZeroCount(nuint value)
	{
		return (nuint)BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<nuint>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nuint value)
	{
		nuint num = 0u;
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(0, source.Length - 8).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<nuint>(ref Unsafe.Add(ref reference, source.Length - 8));
				_ = BitConverter.IsLittleEndian;
				num = BinaryPrimitives.ReverseEndianness(num);
			}
			else
			{
				for (int i = 0; i < source.Length; i++)
				{
					num <<= 8;
					num |= Unsafe.Add(ref reference, i);
				}
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<nuint>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nuint value)
	{
		nuint num = 0u;
		if (source.Length != 0)
		{
			if (!isUnsigned)
			{
				if (sbyte.IsNegative((sbyte)source[source.Length - 1]))
				{
					value = num;
					return false;
				}
			}
			if (source.Length > 8)
			{
				if (source.Slice(8, source.Length - 8).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<nuint>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_00ab;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				nuint num2 = Unsafe.Add(ref reference, i);
				num2 <<= (i * 8) & 0x3F;
				num |= num2;
			}
		}
		goto IL_00ab;
		IL_00ab:
		value = num;
		return true;
	}

	int IBinaryInteger<nuint>.GetShortestBitLength()
	{
		return 64 - BitOperations.LeadingZeroCount(this);
	}

	int IBinaryInteger<nuint>.GetByteCount()
	{
		return 8;
	}

	bool IBinaryInteger<nuint>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong value = (ulong)this;
			_ = BitConverter.IsLittleEndian;
			value = BinaryPrimitives.ReverseEndianness(value);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<nuint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong value = (ulong)this;
			if (!BitConverter.IsLittleEndian)
			{
			}
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(nuint value)
	{
		return BitOperations.IsPow2(value);
	}

	[Intrinsic]
	public static nuint Log2(nuint value)
	{
		return (nuint)BitOperations.Log2(value);
	}

	static nuint IBitwiseOperators<nuint, nuint, nuint>.operator &(nuint left, nuint right)
	{
		return left & right;
	}

	static nuint IBitwiseOperators<nuint, nuint, nuint>.operator |(nuint left, nuint right)
	{
		return left | right;
	}

	static nuint IBitwiseOperators<nuint, nuint, nuint>.operator ^(nuint left, nuint right)
	{
		return left ^ right;
	}

	static nuint IBitwiseOperators<nuint, nuint, nuint>.operator ~(nuint value)
	{
		return ~value;
	}

	static bool IComparisonOperators<nuint, nuint, bool>.operator <(nuint left, nuint right)
	{
		return left < right;
	}

	static bool IComparisonOperators<nuint, nuint, bool>.operator <=(nuint left, nuint right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<nuint, nuint, bool>.operator >(nuint left, nuint right)
	{
		return left > right;
	}

	static bool IComparisonOperators<nuint, nuint, bool>.operator >=(nuint left, nuint right)
	{
		return left >= right;
	}

	static nuint IDecrementOperators<nuint>.operator --(nuint value)
	{
		return --value;
	}

	static nuint IDecrementOperators<nuint>.operator checked --(nuint value)
	{
		return value = checked(value - 1);
	}

	static nuint IDivisionOperators<nuint, nuint, nuint>.operator /(nuint left, nuint right)
	{
		return left / right;
	}

	static nuint IIncrementOperators<nuint>.operator ++(nuint value)
	{
		return ++value;
	}

	static nuint IIncrementOperators<nuint>.operator checked ++(nuint value)
	{
		return value = checked(value + 1);
	}

	static nuint IModulusOperators<nuint, nuint, nuint>.operator %(nuint left, nuint right)
	{
		return left % right;
	}

	static nuint IMultiplyOperators<nuint, nuint, nuint>.operator *(nuint left, nuint right)
	{
		return left * right;
	}

	static nuint IMultiplyOperators<nuint, nuint, nuint>.operator checked *(nuint left, nuint right)
	{
		return checked(left * right);
	}

	public static nuint Clamp(nuint value, nuint min, nuint max)
	{
		return Math.Clamp(value, min, max);
	}

	static nuint INumber<nuint>.CopySign(nuint value, nuint sign)
	{
		return value;
	}

	public static nuint Max(nuint x, nuint y)
	{
		return Math.Max(x, y);
	}

	static nuint INumber<nuint>.MaxNumber(nuint x, nuint y)
	{
		return Max(x, y);
	}

	public static nuint Min(nuint x, nuint y)
	{
		return Math.Min(x, y);
	}

	static nuint INumber<nuint>.MinNumber(nuint x, nuint y)
	{
		return Min(x, y);
	}

	public static int Sign(nuint value)
	{
		return (value != 0) ? 1 : 0;
	}

	static nuint INumberBase<nuint>.Abs(nuint value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nuint))
		{
			return (nuint)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToChecked<nuint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nuint))
		{
			return (nuint)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToSaturating<nuint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nuint))
		{
			return (nuint)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToTruncating<nuint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<nuint>.IsCanonical(nuint value)
	{
		return true;
	}

	static bool INumberBase<nuint>.IsComplexNumber(nuint value)
	{
		return false;
	}

	public static bool IsEvenInteger(nuint value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<nuint>.IsFinite(nuint value)
	{
		return true;
	}

	static bool INumberBase<nuint>.IsImaginaryNumber(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsInfinity(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsInteger(nuint value)
	{
		return true;
	}

	static bool INumberBase<nuint>.IsNaN(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsNegative(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsNegativeInfinity(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsNormal(nuint value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(nuint value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<nuint>.IsPositive(nuint value)
	{
		return true;
	}

	static bool INumberBase<nuint>.IsPositiveInfinity(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsRealNumber(nuint value)
	{
		return true;
	}

	static bool INumberBase<nuint>.IsSubnormal(nuint value)
	{
		return false;
	}

	static bool INumberBase<nuint>.IsZero(nuint value)
	{
		return value == 0;
	}

	static nuint INumberBase<nuint>.MaxMagnitude(nuint x, nuint y)
	{
		return Max(x, y);
	}

	static nuint INumberBase<nuint>.MaxMagnitudeNumber(nuint x, nuint y)
	{
		return Max(x, y);
	}

	static nuint INumberBase<nuint>.MinMagnitude(nuint x, nuint y)
	{
		return Min(x, y);
	}

	static nuint INumberBase<nuint>.MinMagnitudeNumber(nuint x, nuint y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertFromChecked<TOther>(TOther value, out nuint result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out nuint result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(decimal))
			{
				decimal num = (decimal)(object)value;
				result = (nuint)(ulong)num;
				return true;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				ushort num2 = (ushort)(object)value;
				result = num2;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				uint num3 = (uint)(object)value;
				result = num3;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num4 = (ulong)(object)value;
				result = (nuint)num4;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)(object)value;
				result = (nuint)uInt;
				return true;
			}
			result = 0u;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertFromSaturating<TOther>(TOther value, out nuint result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out nuint result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num >= 18446744073709551615m) ? unchecked((nuint)(-1)) : ((nuint)((num <= 0m) ? 0 : ((ulong)num))));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = ((num4 >= ulong.MaxValue) ? unchecked((nuint)(-1)) : ((nuint)num4));
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= ulong.MaxValue) ? unchecked((nuint)(-1)) : ((nuint)uInt));
			return true;
		}
		result = 0u;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertFromTruncating<TOther>(TOther value, out nuint result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out nuint result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num >= 18446744073709551615m) ? unchecked((nuint)(-1)) : ((nuint)((num <= 0m) ? 0 : ((ulong)num))));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = (nuint)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (nuint)uInt;
			return true;
		}
		result = 0u;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertToChecked<TOther>(nuint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)value;
				result = (TOther)(object)num2;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num3 = (int)value;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num4 = (long)value;
				result = (TOther)(object)num4;
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
				nint num5 = (nint)value;
				result = (TOther)(object)num5;
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
				float num6 = value;
				result = (TOther)(object)num6;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertToSaturating<TOther>(nuint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
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
			short num2 = ((value >= 32767) ? short.MaxValue : ((short)value));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)((value >= int.MaxValue) ? int.MaxValue : value);
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (((ulong)value >= 9223372036854775807uL) ? long.MaxValue : ((long)value));
			result = (TOther)(object)num4;
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
			nint num5 = ((value >= (nuint)IntPtr.MaxValue) ? IntPtr.MaxValue : ((nint)value));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = ((value >= 127) ? sbyte.MaxValue : ((sbyte)value));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = value;
			result = (TOther)(object)num6;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nuint>.TryConvertToTruncating<TOther>(nuint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
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
			int num3 = (int)value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)value;
			result = (TOther)(object)num4;
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
			result = (TOther)(object)(nint)value;
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
			float num5 = value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	static nuint IShiftOperators<nuint, int, nuint>.operator <<(nuint value, int shiftAmount)
	{
		return value << (shiftAmount & 0x3F);
	}

	static nuint IShiftOperators<nuint, int, nuint>.operator >>(nuint value, int shiftAmount)
	{
		return value >> (shiftAmount & 0x3F);
	}

	static nuint IShiftOperators<nuint, int, nuint>.operator >>>(nuint value, int shiftAmount)
	{
		return value >> (shiftAmount & 0x3F);
	}

	static nuint ISubtractionOperators<nuint, nuint, nuint>.operator -(nuint left, nuint right)
	{
		return left - right;
	}

	static nuint ISubtractionOperators<nuint, nuint, nuint>.operator checked -(nuint left, nuint right)
	{
		return checked(left - right);
	}

	static nuint IUnaryNegationOperators<nuint, nuint>.operator -(nuint value)
	{
		return 0 - value;
	}

	static nuint IUnaryNegationOperators<nuint, nuint>.operator checked -(nuint value)
	{
		return checked(0 - value);
	}

	static nuint IUnaryPlusOperators<nuint, nuint>.operator +(nuint value)
	{
		return value;
	}

	public static nuint Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (nuint)ulong.Parse(utf8Text, style, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(utf8Text, style, provider, out Unsafe.As<nuint, ulong>(ref result));
	}

	public static nuint Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return (nuint)ulong.Parse(utf8Text, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out nuint result)
	{
		Unsafe.SkipInit<nuint>(out result);
		return ulong.TryParse(utf8Text, provider, out Unsafe.As<nuint, ulong>(ref result));
	}
}
