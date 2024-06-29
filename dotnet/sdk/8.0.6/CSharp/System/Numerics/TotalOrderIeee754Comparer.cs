using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct TotalOrderIeee754Comparer<T> : IComparer<T>, IEqualityComparer<T>, IEquatable<TotalOrderIeee754Comparer<T>> where T : IFloatingPointIeee754<T>?
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Compare(T? x, T? y)
	{
		if (typeof(T) == typeof(float))
		{
			return CompareIntegerSemantic<int>(BitConverter.SingleToInt32Bits((float)(object)x), BitConverter.SingleToInt32Bits((float)(object)y));
		}
		if (typeof(T) == typeof(double))
		{
			return CompareIntegerSemantic<long>(BitConverter.DoubleToInt64Bits((double)(object)x), BitConverter.DoubleToInt64Bits((double)(object)y));
		}
		if (typeof(T) == typeof(Half))
		{
			return CompareIntegerSemantic<short>(BitConverter.HalfToInt16Bits((Half)(object)x), BitConverter.HalfToInt16Bits((Half)(object)y));
		}
		return CompareGeneric(x, y);
		static int CompareGeneric(T x, T y)
		{
			if (x == null)
			{
				if (y != null)
				{
					return -1;
				}
				return 0;
			}
			if (y == null)
			{
				return 1;
			}
			if (x < y)
			{
				return -1;
			}
			if (x > y)
			{
				return 1;
			}
			if (x == y)
			{
				if (T.IsZero(x))
				{
					if (T.IsNegative(x))
					{
						if (!T.IsNegative(y))
						{
							return -1;
						}
						return 0;
					}
					return (!T.IsPositive(y)) ? 1 : 0;
				}
				return 0;
			}
			if (T.IsNaN(x))
			{
				if (T.IsNaN(y))
				{
					if (T.IsNegative(x))
					{
						if (!T.IsPositive(y))
						{
							return CompareSignificand(y, x);
						}
						return -1;
					}
					if (!T.IsNegative(y))
					{
						return CompareSignificand(x, y);
					}
					return 1;
				}
				if (!T.IsPositive(x))
				{
					return -1;
				}
				return 1;
			}
			if (T.IsNaN(y))
			{
				if (!T.IsPositive(y))
				{
					return 1;
				}
				return -1;
			}
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
			return 0;
		}
		static int CompareIntegerSemantic<TInteger>(TInteger x, TInteger y) where TInteger : struct, IBinaryInteger<TInteger?>?, ISignedNumber<TInteger?>?
		{
			if (!((INumberBase<TInteger>)TInteger).IsNegative(x) || !((INumberBase<TInteger>)TInteger).IsNegative(y))
			{
				return ((IComparable<TInteger>)x).CompareTo(y);
			}
			return ((IComparable<TInteger>)y).CompareTo(x);
		}
		static int CompareSignificand(T x, T y)
		{
			int significandBitLength = x.GetSignificandBitLength();
			int significandBitLength2 = y.GetSignificandBitLength();
			if (significandBitLength == significandBitLength2)
			{
				int significandByteCount = x.GetSignificandByteCount();
				int significandByteCount2 = y.GetSignificandByteCount();
				Span<byte> span = (((uint)significandByteCount > 256u) ? ((Span<byte>)new byte[significandByteCount]) : stackalloc byte[significandByteCount]);
				Span<byte> span2 = span;
				Span<byte> span3 = (((uint)significandByteCount2 > 256u) ? ((Span<byte>)new byte[significandByteCount2]) : stackalloc byte[significandByteCount2]);
				Span<byte> span4 = span3;
				x.WriteSignificandBigEndian(span2);
				y.WriteSignificandBigEndian(span4);
				return span2.SequenceCompareTo(span4);
			}
			return significandBitLength.CompareTo(significandBitLength2);
		}
	}

	public bool Equals(T? x, T? y)
	{
		return Compare(x, y) == 0;
	}

	public int GetHashCode([DisallowNull] T obj)
	{
		ArgumentNullException.ThrowIfNull(obj, "obj");
		return obj.GetHashCode();
	}

	public bool Equals(TotalOrderIeee754Comparer<T> other)
	{
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is TotalOrderIeee754Comparer<T>;
	}

	public override int GetHashCode()
	{
		return EqualityComparer<T>.Default.GetHashCode();
	}
}
