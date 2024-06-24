using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Boolean : IComparable, IConvertible, IComparable<bool>, IEquatable<bool>, ISpanParsable<bool>, IParsable<bool>
{
	private readonly bool m_value;

	public static readonly string TrueString = "True";

	public static readonly string FalseString = "False";

	public override int GetHashCode()
	{
		return this ? 1 : 0;
	}

	public override string ToString()
	{
		if (!this)
		{
			return "False";
		}
		return "True";
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString();
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		if (this)
		{
			if (destination.Length > 3)
			{
				if (!BitConverter.IsLittleEndian)
				{
				}
				ulong value = 28429475166421076uL;
				MemoryMarshal.Write(MemoryMarshal.AsBytes(destination), in value);
				charsWritten = 4;
				return true;
			}
		}
		else if (destination.Length > 4)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ulong value2 = 32370086184550470uL;
			MemoryMarshal.Write(MemoryMarshal.AsBytes(destination), in value2);
			destination[4] = 'e';
			charsWritten = 5;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is bool))
		{
			return false;
		}
		return this == (bool)obj;
	}

	[NonVersionable]
	public bool Equals(bool obj)
	{
		return this == obj;
	}

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is bool))
		{
			throw new ArgumentException(SR.Arg_MustBeBoolean);
		}
		if (this == (bool)obj)
		{
			return 0;
		}
		if (!this)
		{
			return -1;
		}
		return 1;
	}

	public int CompareTo(bool value)
	{
		if (this == value)
		{
			return 0;
		}
		if (!this)
		{
			return -1;
		}
		return 1;
	}

	internal static bool IsTrueStringIgnoreCase(ReadOnlySpan<char> value)
	{
		return MemoryExtensions.Equals(value, "True", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
	{
		return MemoryExtensions.Equals(value, "False", StringComparison.OrdinalIgnoreCase);
	}

	public static bool Parse(string value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		return Parse(value.AsSpan());
	}

	public static bool Parse(ReadOnlySpan<char> value)
	{
		if (!TryParse(value, out var result))
		{
			ThrowHelper.ThrowFormatException_BadBoolean(value);
		}
		return result;
	}

	public static bool TryParse([NotNullWhen(true)] string? value, out bool result)
	{
		return TryParse(value.AsSpan(), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> value, out bool result)
	{
		if (IsTrueStringIgnoreCase(value))
		{
			result = true;
			return true;
		}
		if (IsFalseStringIgnoreCase(value))
		{
			result = false;
			return true;
		}
		return TryParseUncommon(value, out result);
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool TryParseUncommon(ReadOnlySpan<char> value, out bool result)
		{
			int length = value.Length;
			if (length >= 5)
			{
				value = TrimWhiteSpaceAndNull(value);
				if (value.Length != length)
				{
					if (IsTrueStringIgnoreCase(value))
					{
						result = true;
						return true;
					}
					result = false;
					return IsFalseStringIgnoreCase(value);
				}
			}
			result = false;
			return false;
		}
	}

	private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
	{
		int i;
		for (i = 0; i < value.Length && (char.IsWhiteSpace(value[i]) || value[i] == '\0'); i++)
		{
		}
		int num = value.Length - 1;
		while (num >= i && (char.IsWhiteSpace(value[num]) || value[num] == '\0'))
		{
			num--;
		}
		return value.Slice(i, num - i + 1);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Boolean;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return this;
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "Char"));
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
		return Convert.ToSingle(this);
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return Convert.ToDouble(this);
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(this);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static bool IParsable<bool>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool IParsable<bool>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out bool result)
	{
		return TryParse(s, out result);
	}

	static bool ISpanParsable<bool>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool ISpanParsable<bool>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out bool result)
	{
		return TryParse(s, out result);
	}
}
