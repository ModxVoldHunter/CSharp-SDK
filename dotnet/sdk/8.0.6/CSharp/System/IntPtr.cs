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
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct IntPtr : IEquatable<nint>, IComparable, IComparable<nint>, ISpanFormattable, IFormattable, ISerializable, IBinaryInteger<nint>, IBinaryNumber<nint>, IBitwiseOperators<nint, nint, nint>, INumber<nint>, IComparisonOperators<nint, nint, bool>, IEqualityOperators<nint, nint, bool>, IModulusOperators<nint, nint, nint>, INumberBase<nint>, IAdditionOperators<nint, nint, nint>, IAdditiveIdentity<nint, nint>, IDecrementOperators<nint>, IDivisionOperators<nint, nint, nint>, IIncrementOperators<nint>, IMultiplicativeIdentity<nint, nint>, IMultiplyOperators<nint, nint, nint>, ISpanParsable<nint>, IParsable<nint>, ISubtractionOperators<nint, nint, nint>, IUnaryPlusOperators<nint, nint>, IUnaryNegationOperators<nint, nint>, IUtf8SpanFormattable, IUtf8SpanParsable<nint>, IShiftOperators<nint, int, nint>, IMinMaxValue<nint>, ISignedNumber<nint>
{
	private readonly nint _value;

	[Intrinsic]
	public static readonly nint Zero;

	public static int Size
	{
		[NonVersionable]
		get
		{
			return 8;
		}
	}

	public static nint MaxValue
	{
		[NonVersionable]
		get
		{
			return unchecked((nint)long.MaxValue);
		}
	}

	public static nint MinValue
	{
		[NonVersionable]
		get
		{
			return unchecked((nint)long.MinValue);
		}
	}

	static nint IAdditiveIdentity<nint, nint>.AdditiveIdentity => 0;

	static nint IBinaryNumber<nint>.AllBitsSet => -1;

	static nint IMinMaxValue<nint>.MinValue => MinValue;

	static nint IMinMaxValue<nint>.MaxValue => MaxValue;

	static nint IMultiplicativeIdentity<nint, nint>.MultiplicativeIdentity => 1;

	static nint INumberBase<nint>.One => 1;

	static int INumberBase<nint>.Radix => 2;

	static nint INumberBase<nint>.Zero => 0;

	static nint ISignedNumber<nint>.NegativeOne => -1;

	[NonVersionable]
	public IntPtr(int value)
	{
		_value = value;
	}

	[NonVersionable]
	public IntPtr(long value)
	{
		_value = (nint)value;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe IntPtr(void* value)
	{
		_value = (nint)value;
	}

	private IntPtr(SerializationInfo info, StreamingContext context)
	{
		long @int = info.GetInt64("value");
		_value = (nint)@int;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(info, "info");
		long value = (long)this;
		info.AddValue("value", value);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is nint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((long)this).GetHashCode();
	}

	[NonVersionable]
	public int ToInt32()
	{
		return checked((int)this);
	}

	[NonVersionable]
	public long ToInt64()
	{
		return (long)this;
	}

	[NonVersionable]
	public static explicit operator nint(int value)
	{
		return value;
	}

	[NonVersionable]
	public static explicit operator nint(long value)
	{
		return checked((nint)value);
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe static explicit operator nint(void* value)
	{
		return (nint)value;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe static explicit operator void*(nint value)
	{
		return (void*)value;
	}

	[NonVersionable]
	public static explicit operator int(nint value)
	{
		return checked((int)value);
	}

	[NonVersionable]
	public static explicit operator long(nint value)
	{
		return value;
	}

	[NonVersionable]
	public static bool operator ==(nint value1, nint value2)
	{
		return value1 == value2;
	}

	[NonVersionable]
	public static bool operator !=(nint value1, nint value2)
	{
		return value1 != value2;
	}

	[NonVersionable]
	public static nint Add(nint pointer, int offset)
	{
		return pointer + offset;
	}

	[NonVersionable]
	public static nint operator +(nint pointer, int offset)
	{
		return pointer + offset;
	}

	[NonVersionable]
	public static nint Subtract(nint pointer, int offset)
	{
		return pointer - offset;
	}

	[NonVersionable]
	public static nint operator -(nint pointer, int offset)
	{
		return pointer - offset;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe void* ToPointer()
	{
		return (void*)this;
	}

	public int CompareTo(object? value)
	{
		if (value is nint value2)
		{
			return CompareTo(value2);
		}
		if (value == null)
		{
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeIntPtr);
	}

	public int CompareTo(nint value)
	{
		if ((nint)this < value)
		{
			return -1;
		}
		if ((nint)this > value)
		{
			return 1;
		}
		return 0;
	}

	[NonVersionable]
	public bool Equals(nint other)
	{
		return this == (IntPtr)other;
	}

	public override string ToString()
	{
		return ((long)this).ToString();
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ((long)this).ToString(format);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ((long)this).ToString(provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return ((long)this).ToString(format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((long)this).TryFormat(destination, out charsWritten, format, provider);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((long)this).TryFormat(utf8Destination, out bytesWritten, format, provider);
	}

	public static nint Parse(string s)
	{
		return (nint)long.Parse(s);
	}

	public static nint Parse(string s, NumberStyles style)
	{
		return (nint)long.Parse(s, style);
	}

	public static nint Parse(string s, IFormatProvider? provider)
	{
		return (nint)long.Parse(s, provider);
	}

	public static nint Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		return (nint)long.Parse(s, style, provider);
	}

	public static nint Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return (nint)long.Parse(s, provider);
	}

	public static nint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (nint)long.Parse(s, style, provider);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, provider, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, style, provider, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(utf8Text, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, provider, out Unsafe.As<nint, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(s, style, provider, out Unsafe.As<nint, long>(ref result));
	}

	static nint IAdditionOperators<nint, nint, nint>.operator +(nint left, nint right)
	{
		return left + right;
	}

	static nint IAdditionOperators<nint, nint, nint>.operator checked +(nint left, nint right)
	{
		return checked(left + right);
	}

	public static (nint Quotient, nint Remainder) DivRem(nint left, nint right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static nint LeadingZeroCount(nint value)
	{
		return BitOperations.LeadingZeroCount((nuint)value);
	}

	[Intrinsic]
	public static nint PopCount(nint value)
	{
		return BitOperations.PopCount((nuint)value);
	}

	[Intrinsic]
	public static nint RotateLeft(nint value, int rotateAmount)
	{
		return (nint)BitOperations.RotateLeft((nuint)value, rotateAmount);
	}

	[Intrinsic]
	public static nint RotateRight(nint value, int rotateAmount)
	{
		return (nint)BitOperations.RotateRight((nuint)value, rotateAmount);
	}

	[Intrinsic]
	public static nint TrailingZeroCount(nint value)
	{
		return BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<nint>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nint value)
	{
		nint num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[0];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 8)
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(0, source.Length - 8).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[source.Length - 8]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<nint>(ref Unsafe.Add(ref reference, source.Length - 8));
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
				if (!isUnsigned)
				{
					num |= (nint)1 << (0x3F & 0x3F) >> (((8 - source.Length) * 8 - 1) & 0x3F);
				}
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<nint>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nint value)
	{
		nint num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[source.Length - 1];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 8)
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(8, source.Length - 8).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[7]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<nint>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_011c;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				num <<= 8;
				num |= Unsafe.Add(ref reference, i);
			}
			num <<= ((8 - source.Length) * 8) & 0x3F;
			num = BinaryPrimitives.ReverseEndianness(num);
			if (!isUnsigned)
			{
				num |= (nint)1 << (0x3F & 0x3F) >> (((8 - source.Length) * 8 - 1) & 0x3F);
			}
		}
		goto IL_011c;
		IL_011c:
		value = num;
		return true;
	}

	int IBinaryInteger<nint>.GetShortestBitLength()
	{
		nint num = this;
		if (num >= 0)
		{
			return 64 - BitOperations.LeadingZeroCount((nuint)num);
		}
		return 65 - BitOperations.LeadingZeroCount((nuint)(~num));
	}

	int IBinaryInteger<nint>.GetByteCount()
	{
		return 8;
	}

	bool IBinaryInteger<nint>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			long value = (long)this;
			_ = BitConverter.IsLittleEndian;
			value = BinaryPrimitives.ReverseEndianness(value);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<nint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			long value = (long)this;
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

	public static bool IsPow2(nint value)
	{
		return BitOperations.IsPow2(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static nint Log2(nint value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return BitOperations.Log2((nuint)value);
	}

	static nint IBitwiseOperators<nint, nint, nint>.operator &(nint left, nint right)
	{
		return left & right;
	}

	static nint IBitwiseOperators<nint, nint, nint>.operator |(nint left, nint right)
	{
		return left | right;
	}

	static nint IBitwiseOperators<nint, nint, nint>.operator ^(nint left, nint right)
	{
		return left ^ right;
	}

	static nint IBitwiseOperators<nint, nint, nint>.operator ~(nint value)
	{
		return ~value;
	}

	static bool IComparisonOperators<nint, nint, bool>.operator <(nint left, nint right)
	{
		return left < right;
	}

	static bool IComparisonOperators<nint, nint, bool>.operator <=(nint left, nint right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<nint, nint, bool>.operator >(nint left, nint right)
	{
		return left > right;
	}

	static bool IComparisonOperators<nint, nint, bool>.operator >=(nint left, nint right)
	{
		return left >= right;
	}

	static nint IDecrementOperators<nint>.operator --(nint value)
	{
		return --value;
	}

	static nint IDecrementOperators<nint>.operator checked --(nint value)
	{
		return value = checked(value - 1);
	}

	static nint IDivisionOperators<nint, nint, nint>.operator /(nint left, nint right)
	{
		return left / right;
	}

	static nint IIncrementOperators<nint>.operator ++(nint value)
	{
		return ++value;
	}

	static nint IIncrementOperators<nint>.operator checked ++(nint value)
	{
		return value = checked(value + 1);
	}

	static nint IModulusOperators<nint, nint, nint>.operator %(nint left, nint right)
	{
		return left % right;
	}

	static nint IMultiplyOperators<nint, nint, nint>.operator *(nint left, nint right)
	{
		return left * right;
	}

	static nint IMultiplyOperators<nint, nint, nint>.operator checked *(nint left, nint right)
	{
		return checked(left * right);
	}

	public static nint Clamp(nint value, nint min, nint max)
	{
		return Math.Clamp(value, min, max);
	}

	public static nint CopySign(nint value, nint sign)
	{
		nint num = value;
		if (num < 0)
		{
			num = -num;
		}
		if (sign >= 0)
		{
			if (num < 0)
			{
				Math.ThrowNegateTwosCompOverflow();
			}
			return num;
		}
		return -num;
	}

	public static nint Max(nint x, nint y)
	{
		return Math.Max(x, y);
	}

	static nint INumber<nint>.MaxNumber(nint x, nint y)
	{
		return Max(x, y);
	}

	public static nint Min(nint x, nint y)
	{
		return Math.Min(x, y);
	}

	static nint INumber<nint>.MinNumber(nint x, nint y)
	{
		return Min(x, y);
	}

	public static int Sign(nint value)
	{
		return Math.Sign(value);
	}

	public static nint Abs(nint value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nint))
		{
			return (nint)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToChecked<nint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nint))
		{
			return (nint)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToSaturating<nint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(nint))
		{
			return (nint)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !((INumberBase<TOther>)TOther).TryConvertToTruncating<nint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<nint>.IsCanonical(nint value)
	{
		return true;
	}

	static bool INumberBase<nint>.IsComplexNumber(nint value)
	{
		return false;
	}

	public static bool IsEvenInteger(nint value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<nint>.IsFinite(nint value)
	{
		return true;
	}

	static bool INumberBase<nint>.IsImaginaryNumber(nint value)
	{
		return false;
	}

	static bool INumberBase<nint>.IsInfinity(nint value)
	{
		return false;
	}

	static bool INumberBase<nint>.IsInteger(nint value)
	{
		return true;
	}

	static bool INumberBase<nint>.IsNaN(nint value)
	{
		return false;
	}

	public static bool IsNegative(nint value)
	{
		return value < 0;
	}

	static bool INumberBase<nint>.IsNegativeInfinity(nint value)
	{
		return false;
	}

	static bool INumberBase<nint>.IsNormal(nint value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(nint value)
	{
		return (value & 1) != 0;
	}

	public static bool IsPositive(nint value)
	{
		return value >= 0;
	}

	static bool INumberBase<nint>.IsPositiveInfinity(nint value)
	{
		return false;
	}

	static bool INumberBase<nint>.IsRealNumber(nint value)
	{
		return true;
	}

	static bool INumberBase<nint>.IsSubnormal(nint value)
	{
		return false;
	}

	static bool INumberBase<nint>.IsZero(nint value)
	{
		return value == 0;
	}

	public static nint MaxMagnitude(nint x, nint y)
	{
		nint num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return x;
			}
		}
		nint num2 = y;
		if (num2 < 0)
		{
			num2 = -num2;
			if (num2 < 0)
			{
				return y;
			}
		}
		if (num > num2)
		{
			return x;
		}
		if (num == num2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	static nint INumberBase<nint>.MaxMagnitudeNumber(nint x, nint y)
	{
		return MaxMagnitude(x, y);
	}

	public static nint MinMagnitude(nint x, nint y)
	{
		nint num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return y;
			}
		}
		nint num2 = y;
		if (num2 < 0)
		{
			num2 = -num2;
			if (num2 < 0)
			{
				return x;
			}
		}
		if (num < num2)
		{
			return x;
		}
		if (num == num2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	static nint INumberBase<nint>.MinMagnitudeNumber(nint x, nint y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertFromChecked<TOther>(TOther value, out nint result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out nint result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (nint)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (nint)half;
				return true;
			}
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)(object)value;
				result = num2;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num3 = (int)(object)value;
				result = num3;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num4 = (long)(object)value;
				result = (nint)num4;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)(object)value;
				result = (nint)@int;
				return true;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				sbyte b = (sbyte)(object)value;
				result = b;
				return true;
			}
			if (typeof(TOther) == typeof(float))
			{
				float num5 = (float)(object)value;
				result = (nint)num5;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertFromSaturating<TOther>(TOther value, out nint result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out nint result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = (nint)((num >= 9.223372036854776E+18) ? long.MaxValue : ((num <= -9.223372036854776E+18) ? long.MinValue : ((nint)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (nint)((half == Half.PositiveInfinity) ? long.MaxValue : ((half == Half.NegativeInfinity) ? long.MinValue : ((nint)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = num4 switch
			{
				long.MinValue => unchecked((nint)long.MinValue), 
				long.MaxValue => unchecked((nint)long.MaxValue), 
				_ => (nint)num4, 
			};
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (nint)((@int >= long.MaxValue) ? long.MaxValue : ((@int <= long.MinValue) ? long.MinValue : ((nint)@int)));
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (float)(object)value;
			result = (nint)((num5 >= 9.223372E+18f) ? long.MaxValue : ((num5 <= -9.223372E+18f) ? long.MinValue : ((nint)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertFromTruncating<TOther>(TOther value, out nint result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out nint result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = (nint)((num >= 9.223372036854776E+18) ? long.MaxValue : ((num <= -9.223372036854776E+18) ? long.MinValue : ((nint)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (nint)((half == Half.PositiveInfinity) ? long.MaxValue : ((half == Half.NegativeInfinity) ? long.MinValue : ((nint)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = (nint)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (nint)@int;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (float)(object)value;
			result = (nint)((num5 >= 9.223372E+18f) ? long.MaxValue : ((num5 <= -9.223372E+18f) ? long.MinValue : ((nint)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertToChecked<TOther>(nint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = checked((byte)value);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)checked((ushort)value);
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (long)value;
			result = (TOther)(object)num;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(ushort))
			{
				ushort num2 = (ushort)value;
				result = (TOther)(object)num2;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				uint num3 = (uint)value;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num4 = (ulong)value;
				result = (TOther)(object)num4;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)value;
				result = (TOther)(object)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				nuint num5 = (nuint)value;
				result = (TOther)(object)num5;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertToSaturating<TOther>(nint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= 255) ? byte.MaxValue : ((value > 0) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= 65535) ? '\uffff' : ((value > 0) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (long)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value >= 65535) ? ushort.MaxValue : ((value > 0) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = ((value >= uint.MaxValue) ? uint.MaxValue : (((long)value > 0L) ? ((uint)value) : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)((value <= 0) ? 0 : value);
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value <= 0) ? UInt128.MinValue : ((UInt128)value));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)((value <= 0) ? 0 : value);
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<nint>.TryConvertToTruncating<TOther>(nint value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)value;
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)value;
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (long)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)value;
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			result = (TOther)(object)(nuint)value;
			return true;
		}
		result = default(TOther);
		return false;
	}

	static nint IShiftOperators<nint, int, nint>.operator <<(nint value, int shiftAmount)
	{
		return value << (shiftAmount & 0x3F);
	}

	static nint IShiftOperators<nint, int, nint>.operator >>(nint value, int shiftAmount)
	{
		return value >> (shiftAmount & 0x3F);
	}

	static nint IShiftOperators<nint, int, nint>.operator >>>(nint value, int shiftAmount)
	{
		return value >>> (shiftAmount & 0x3F);
	}

	static nint ISubtractionOperators<nint, nint, nint>.operator -(nint left, nint right)
	{
		return left - right;
	}

	static nint ISubtractionOperators<nint, nint, nint>.operator checked -(nint left, nint right)
	{
		return checked(left - right);
	}

	static nint IUnaryNegationOperators<nint, nint>.operator -(nint value)
	{
		return -value;
	}

	static nint IUnaryNegationOperators<nint, nint>.operator checked -(nint value)
	{
		return checked(-value);
	}

	static nint IUnaryPlusOperators<nint, nint>.operator +(nint value)
	{
		return value;
	}

	public static nint Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (nint)long.Parse(utf8Text, style, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(utf8Text, style, provider, out Unsafe.As<nint, long>(ref result));
	}

	public static nint Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return (nint)long.Parse(utf8Text, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out nint result)
	{
		Unsafe.SkipInit<nint>(out result);
		return long.TryParse(utf8Text, provider, out Unsafe.As<nint, long>(ref result));
	}
}
