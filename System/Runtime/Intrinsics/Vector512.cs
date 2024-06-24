using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Intrinsics;

public static class Vector512
{
	public static bool IsHardwareAccelerated
	{
		[Intrinsic]
		get
		{
			return IsHardwareAccelerated;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Abs<T>(Vector512<T> vector)
	{
		return Create(Vector256.Abs(vector._lower), Vector256.Abs(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Add<T>(Vector512<T> left, Vector512<T> right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> AndNot<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.AndNot(left._lower, right._lower), Vector256.AndNot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<TTo> As<TFrom, TTo>(this Vector512<TFrom> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TTo>();
		return Unsafe.As<Vector512<TFrom>, Vector512<TTo>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> AsByte<T>(this Vector512<T> vector)
	{
		return vector.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> AsDouble<T>(this Vector512<T> vector)
	{
		return vector.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> AsInt16<T>(this Vector512<T> vector)
	{
		return vector.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> AsInt32<T>(this Vector512<T> vector)
	{
		return vector.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> AsInt64<T>(this Vector512<T> vector)
	{
		return vector.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> AsNInt<T>(this Vector512<T> vector)
	{
		return vector.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<nuint> AsNUInt<T>(this Vector512<T> vector)
	{
		return vector.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> AsSByte<T>(this Vector512<T> vector)
	{
		return vector.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> AsSingle<T>(this Vector512<T> vector)
	{
		return vector.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> AsUInt16<T>(this Vector512<T> vector)
	{
		return vector.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> AsUInt32<T>(this Vector512<T> vector)
	{
		return vector.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> AsUInt64<T>(this Vector512<T> vector)
	{
		return vector.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> AsVector512<T>(this Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Vector512<T> source = default(Vector512<T>);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector512<T>, byte>(ref source), value);
		return source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> AsVector<T>(this Vector512<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<Vector512<T>, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> BitwiseAnd<T>(Vector512<T> left, Vector512<T> right)
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> BitwiseOr<T>(Vector512<T> left, Vector512<T> right)
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> Ceiling(Vector512<float> vector)
	{
		return Create(Vector256.Ceiling(vector._lower), Vector256.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> Ceiling(Vector512<double> vector)
	{
		return Create(Vector256.Ceiling(vector._lower), Vector256.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> ConditionalSelect<T>(Vector512<T> condition, Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.ConditionalSelect(condition._lower, left._lower, right._lower), Vector256.ConditionalSelect(condition._upper, left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> ConvertToDouble(Vector512<long> vector)
	{
		return Create(Vector256.ConvertToDouble(vector._lower), Vector256.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<double> ConvertToDouble(Vector512<ulong> vector)
	{
		return Create(Vector256.ConvertToDouble(vector._lower), Vector256.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> ConvertToInt32(Vector512<float> vector)
	{
		return Create(Vector256.ConvertToInt32(vector._lower), Vector256.ConvertToInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> ConvertToInt64(Vector512<double> vector)
	{
		return Create(Vector256.ConvertToInt64(vector._lower), Vector256.ConvertToInt64(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> ConvertToSingle(Vector512<int> vector)
	{
		return Create(Vector256.ConvertToSingle(vector._lower), Vector256.ConvertToSingle(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<float> ConvertToSingle(Vector512<uint> vector)
	{
		return Create(Vector256.ConvertToSingle(vector._lower), Vector256.ConvertToSingle(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> ConvertToUInt32(Vector512<float> vector)
	{
		return Create(Vector256.ConvertToUInt32(vector._lower), Vector256.ConvertToUInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> ConvertToUInt64(Vector512<double> vector)
	{
		return Create(Vector256.ConvertToUInt64(vector._lower), Vector256.ConvertToUInt64(vector._upper));
	}

	public static void CopyTo<T>(this Vector512<T> vector, T[] destination)
	{
		if (destination.Length < Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
	}

	public static void CopyTo<T>(this Vector512<T> vector, T[] destination, int startIndex)
	{
		if ((uint)startIndex >= (uint)destination.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (destination.Length - startIndex < Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
	}

	public static void CopyTo<T>(this Vector512<T> vector, Span<T> destination)
	{
		if ((uint)destination.Length < (uint)Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Create<T>(T value)
	{
		Vector256<T> vector = Vector256.Create(value);
		return Create(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> Create(byte value)
	{
		return Vector512.Create<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> Create(double value)
	{
		return Vector512.Create<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> Create(short value)
	{
		return Vector512.Create<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> Create(int value)
	{
		return Vector512.Create<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> Create(long value)
	{
		return Vector512.Create<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> Create(nint value)
	{
		return Vector512.Create<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<nuint> Create(nuint value)
	{
		return Vector512.Create<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> Create(sbyte value)
	{
		return Vector512.Create<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> Create(float value)
	{
		return Vector512.Create<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> Create(ushort value)
	{
		return Vector512.Create<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> Create(uint value)
	{
		return Vector512.Create<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> Create(ulong value)
	{
		return Vector512.Create<ulong>(value);
	}

	public static Vector512<T> Create<T>(T[] values)
	{
		if (values.Length < Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref values[0]));
	}

	public static Vector512<T> Create<T>(T[] values, int index)
	{
		if (index < 0 || values.Length - index < Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<T> Create<T>(ReadOnlySpan<T> values)
	{
		if (values.Length < Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31, byte e32, byte e33, byte e34, byte e35, byte e36, byte e37, byte e38, byte e39, byte e40, byte e41, byte e42, byte e43, byte e44, byte e45, byte e46, byte e47, byte e48, byte e49, byte e50, byte e51, byte e52, byte e53, byte e54, byte e55, byte e56, byte e57, byte e58, byte e59, byte e60, byte e61, byte e62, byte e63)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31), Vector256.Create(e32, e33, e34, e35, e36, e37, e38, e39, e40, e41, e42, e43, e44, e45, e46, e47, e48, e49, e50, e51, e52, e53, e54, e55, e56, e57, e58, e59, e60, e61, e62, e63));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> Create(double e0, double e1, double e2, double e3, double e4, double e5, double e6, double e7)
	{
		return Create(Vector256.Create(e0, e1, e2, e3), Vector256.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15, short e16, short e17, short e18, short e19, short e20, short e21, short e22, short e23, short e24, short e25, short e26, short e27, short e28, short e29, short e30, short e31)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15), Vector256.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8, int e9, int e10, int e11, int e12, int e13, int e14, int e15)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> Create(long e0, long e1, long e2, long e3, long e4, long e5, long e6, long e7)
	{
		return Create(Vector256.Create(e0, e1, e2, e3), Vector256.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31, sbyte e32, sbyte e33, sbyte e34, sbyte e35, sbyte e36, sbyte e37, sbyte e38, sbyte e39, sbyte e40, sbyte e41, sbyte e42, sbyte e43, sbyte e44, sbyte e45, sbyte e46, sbyte e47, sbyte e48, sbyte e49, sbyte e50, sbyte e51, sbyte e52, sbyte e53, sbyte e54, sbyte e55, sbyte e56, sbyte e57, sbyte e58, sbyte e59, sbyte e60, sbyte e61, sbyte e62, sbyte e63)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31), Vector256.Create(e32, e33, e34, e35, e36, e37, e38, e39, e40, e41, e42, e43, e44, e45, e46, e47, e48, e49, e50, e51, e52, e53, e54, e55, e56, e57, e58, e59, e60, e61, e62, e63));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7, float e8, float e9, float e10, float e11, float e12, float e13, float e14, float e15)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15, ushort e16, ushort e17, ushort e18, ushort e19, ushort e20, ushort e21, ushort e22, ushort e23, ushort e24, ushort e25, ushort e26, ushort e27, ushort e28, ushort e29, ushort e30, ushort e31)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15), Vector256.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7, uint e8, uint e9, uint e10, uint e11, uint e12, uint e13, uint e14, uint e15)
	{
		return Create(Vector256.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3, ulong e4, ulong e5, ulong e6, ulong e7)
	{
		return Create(Vector256.Create(e0, e1, e2, e3), Vector256.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<T> Create<T>(Vector256<T> lower, Vector256<T> upper)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Unsafe.SkipInit<Vector512<T>>(out var value);
		value.SetLowerUnsafe<T>(lower);
		value.SetUpperUnsafe<T>(upper);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<byte> Create(Vector256<byte> lower, Vector256<byte> upper)
	{
		return Vector512.Create<byte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<double> Create(Vector256<double> lower, Vector256<double> upper)
	{
		return Vector512.Create<double>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<short> Create(Vector256<short> lower, Vector256<short> upper)
	{
		return Vector512.Create<short>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<int> Create(Vector256<int> lower, Vector256<int> upper)
	{
		return Vector512.Create<int>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<long> Create(Vector256<long> lower, Vector256<long> upper)
	{
		return Vector512.Create<long>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<nint> Create(Vector256<nint> lower, Vector256<nint> upper)
	{
		return Vector512.Create<nint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<nuint> Create(Vector256<nuint> lower, Vector256<nuint> upper)
	{
		return Vector512.Create<nuint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<sbyte> Create(Vector256<sbyte> lower, Vector256<sbyte> upper)
	{
		return Vector512.Create<sbyte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<float> Create(Vector256<float> lower, Vector256<float> upper)
	{
		return Vector512.Create<float>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<ushort> Create(Vector256<ushort> lower, Vector256<ushort> upper)
	{
		return Vector512.Create<ushort>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<uint> Create(Vector256<uint> lower, Vector256<uint> upper)
	{
		return Vector512.Create<uint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<ulong> Create(Vector256<ulong> lower, Vector256<ulong> upper)
	{
		return Vector512.Create<ulong>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<T> CreateScalar<T>(T value)
	{
		return Vector256.CreateScalar(value).ToVector512();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<byte> CreateScalar(byte value)
	{
		return Vector512.CreateScalar<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<double> CreateScalar(double value)
	{
		return Vector512.CreateScalar<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<short> CreateScalar(short value)
	{
		return Vector512.CreateScalar<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<int> CreateScalar(int value)
	{
		return Vector512.CreateScalar<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<long> CreateScalar(long value)
	{
		return Vector512.CreateScalar<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<nint> CreateScalar(nint value)
	{
		return Vector512.CreateScalar<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<nuint> CreateScalar(nuint value)
	{
		return Vector512.CreateScalar<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<sbyte> CreateScalar(sbyte value)
	{
		return Vector512.CreateScalar<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector512<float> CreateScalar(float value)
	{
		return Vector512.CreateScalar<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<ushort> CreateScalar(ushort value)
	{
		return Vector512.CreateScalar<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<uint> CreateScalar(uint value)
	{
		return Vector512.CreateScalar<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector512<ulong> CreateScalar(ulong value)
	{
		return Vector512.CreateScalar<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> CreateScalarUnsafe<T>(T value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Unsafe.SkipInit<Vector512<T>>(out var value2);
		SetElementUnsafe(in value2, 0, value);
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> CreateScalarUnsafe(byte value)
	{
		return Vector512.CreateScalarUnsafe<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> CreateScalarUnsafe(double value)
	{
		return Vector512.CreateScalarUnsafe<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> CreateScalarUnsafe(short value)
	{
		return Vector512.CreateScalarUnsafe<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> CreateScalarUnsafe(int value)
	{
		return Vector512.CreateScalarUnsafe<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> CreateScalarUnsafe(long value)
	{
		return Vector512.CreateScalarUnsafe<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> CreateScalarUnsafe(nint value)
	{
		return Vector512.CreateScalarUnsafe<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<nuint> CreateScalarUnsafe(nuint value)
	{
		return Vector512.CreateScalarUnsafe<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> CreateScalarUnsafe(sbyte value)
	{
		return Vector512.CreateScalarUnsafe<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> CreateScalarUnsafe(float value)
	{
		return Vector512.CreateScalarUnsafe<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> CreateScalarUnsafe(ushort value)
	{
		return Vector512.CreateScalarUnsafe<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> CreateScalarUnsafe(uint value)
	{
		return Vector512.CreateScalarUnsafe<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> CreateScalarUnsafe(ulong value)
	{
		return Vector512.CreateScalarUnsafe<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Divide<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.Divide(left._lower, right._lower), Vector256.Divide(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Divide<T>(Vector512<T> left, T right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector512<T> left, Vector512<T> right)
	{
		T left2 = Vector256.Dot(left._lower, right._lower);
		return Scalar<T>.Add(left2, Vector256.Dot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Equals<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.Equals(left._lower, right._lower), Vector256.Equals(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAll<T>(Vector512<T> left, Vector512<T> right)
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAny<T>(Vector512<T> left, Vector512<T> right)
	{
		if (!Vector256.EqualsAny(left._lower, right._lower))
		{
			return Vector256.EqualsAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static ulong ExtractMostSignificantBits<T>(this Vector512<T> vector)
	{
		ulong num = vector._lower.ExtractMostSignificantBits();
		return num | ((ulong)vector._upper.ExtractMostSignificantBits() << Vector256<T>.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> Floor(Vector512<float> vector)
	{
		return Create(Vector256.Floor(vector._lower), Vector256.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> Floor(Vector512<double> vector)
	{
		return Create(Vector256.Floor(vector._lower), Vector256.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T GetElement<T>(this Vector512<T> vector, int index)
	{
		if ((uint)index >= (uint)Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return GetElementUnsafe(in vector, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> GetLower<T>(this Vector512<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return vector._lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> GetUpper<T>(this Vector512<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return vector._upper;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> GreaterThan<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.GreaterThan(left._lower, right._lower), Vector256.GreaterThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAll<T>(Vector512<T> left, Vector512<T> right)
	{
		if (Vector256.GreaterThanAll(left._lower, right._lower))
		{
			return Vector256.GreaterThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAny<T>(Vector512<T> left, Vector512<T> right)
	{
		if (!Vector256.GreaterThanAny(left._lower, right._lower))
		{
			return Vector256.GreaterThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> GreaterThanOrEqual<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.GreaterThanOrEqual(left._lower, right._lower), Vector256.GreaterThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAll<T>(Vector512<T> left, Vector512<T> right)
	{
		if (Vector256.GreaterThanOrEqualAll(left._lower, right._lower))
		{
			return Vector256.GreaterThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAny<T>(Vector512<T> left, Vector512<T> right)
	{
		if (!Vector256.GreaterThanOrEqualAny(left._lower, right._lower))
		{
			return Vector256.GreaterThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> LessThan<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.LessThan(left._lower, right._lower), Vector256.LessThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAll<T>(Vector512<T> left, Vector512<T> right)
	{
		if (Vector256.LessThanAll(left._lower, right._lower))
		{
			return Vector256.LessThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAny<T>(Vector512<T> left, Vector512<T> right)
	{
		if (!Vector256.LessThanAny(left._lower, right._lower))
		{
			return Vector256.LessThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> LessThanOrEqual<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.LessThanOrEqual(left._lower, right._lower), Vector256.LessThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAll<T>(Vector512<T> left, Vector512<T> right)
	{
		if (Vector256.LessThanOrEqualAll(left._lower, right._lower))
		{
			return Vector256.LessThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAny<T>(Vector512<T> left, Vector512<T> right)
	{
		if (!Vector256.LessThanOrEqualAny(left._lower, right._lower))
		{
			return Vector256.LessThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector512<T> Load<T>(T* source)
	{
		return LoadUnsafe(ref *source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector512<T> LoadAligned<T>(T* source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		if ((nuint)source % (nuint)64u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		return *(Vector512<T>*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector512<T> LoadAlignedNonTemporal<T>(T* source)
	{
		return LoadAligned(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> LoadUnsafe<T>([In][RequiresLocation] ref T source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(ref source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<T> LoadUnsafe<T>([In][RequiresLocation] ref T source, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(ref source), (nint)elementOffset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector512<ushort> LoadUnsafe(ref char source, nuint elementOffset)
	{
		return LoadUnsafe(ref Unsafe.As<char, ushort>(ref source), elementOffset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Max<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.Max(left._lower, right._lower), Vector256.Max(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Min<T>(Vector512<T> left, Vector512<T> right)
	{
		return Create(Vector256.Min(left._lower, right._lower), Vector256.Min(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Multiply<T>(Vector512<T> left, Vector512<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Multiply<T>(Vector512<T> left, T right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Multiply<T>(T left, Vector512<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<float> Narrow(Vector512<double> lower, Vector512<double> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> Narrow(Vector512<short> lower, Vector512<short> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> Narrow(Vector512<int> lower, Vector512<int> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> Narrow(Vector512<long> lower, Vector512<long> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<byte> Narrow(Vector512<ushort> lower, Vector512<ushort> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> Narrow(Vector512<uint> lower, Vector512<uint> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> Narrow(Vector512<ulong> lower, Vector512<ulong> upper)
	{
		return Create(Vector256.Narrow(lower._lower, lower._upper), Vector256.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Negate<T>(Vector512<T> vector)
	{
		return -vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> OnesComplement<T>(Vector512<T> vector)
	{
		return Create(Vector256.OnesComplement(vector._lower), Vector256.OnesComplement(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> ShiftLeft(Vector512<byte> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> ShiftLeft(Vector512<short> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> ShiftLeft(Vector512<int> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> ShiftLeft(Vector512<long> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> ShiftLeft(Vector512<nint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<nuint> ShiftLeft(Vector512<nuint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> ShiftLeft(Vector512<sbyte> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> ShiftLeft(Vector512<ushort> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> ShiftLeft(Vector512<uint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> ShiftLeft(Vector512<ulong> vector, int shiftCount)
	{
		return Create(Vector256.ShiftLeft(vector._lower, shiftCount), Vector256.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> ShiftRightArithmetic(Vector512<short> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightArithmetic(vector._lower, shiftCount), Vector256.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> ShiftRightArithmetic(Vector512<int> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightArithmetic(vector._lower, shiftCount), Vector256.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> ShiftRightArithmetic(Vector512<long> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightArithmetic(vector._lower, shiftCount), Vector256.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> ShiftRightArithmetic(Vector512<nint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightArithmetic(vector._lower, shiftCount), Vector256.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> ShiftRightArithmetic(Vector512<sbyte> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightArithmetic(vector._lower, shiftCount), Vector256.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<byte> ShiftRightLogical(Vector512<byte> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<short> ShiftRightLogical(Vector512<short> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> ShiftRightLogical(Vector512<int> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> ShiftRightLogical(Vector512<long> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<nint> ShiftRightLogical(Vector512<nint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<nuint> ShiftRightLogical(Vector512<nuint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> ShiftRightLogical(Vector512<sbyte> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> ShiftRightLogical(Vector512<uint> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> vector, int shiftCount)
	{
		return Create(Vector256.ShiftRightLogical(vector._lower, shiftCount), Vector256.ShiftRightLogical(vector._upper, shiftCount));
	}

	[Intrinsic]
	public static Vector512<byte> Shuffle(Vector512<byte> vector, Vector512<byte> indices)
	{
		Unsafe.SkipInit<Vector512<byte>>(out var value);
		for (int i = 0; i < Vector512<byte>.Count; i++)
		{
			byte elementUnsafe = GetElementUnsafe(in indices, i);
			byte value2 = 0;
			if (elementUnsafe < Vector512<byte>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<sbyte> Shuffle(Vector512<sbyte> vector, Vector512<sbyte> indices)
	{
		Unsafe.SkipInit<Vector512<sbyte>>(out var value);
		for (int i = 0; i < Vector512<sbyte>.Count; i++)
		{
			byte b = (byte)GetElementUnsafe(in indices, i);
			sbyte value2 = 0;
			if (b < Vector512<sbyte>.Count)
			{
				value2 = GetElementUnsafe(in vector, b);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector512<short> Shuffle(Vector512<short> vector, Vector512<short> indices)
	{
		Unsafe.SkipInit<Vector512<short>>(out var value);
		for (int i = 0; i < Vector512<short>.Count; i++)
		{
			ushort num = (ushort)GetElementUnsafe(in indices, i);
			short value2 = 0;
			if (num < Vector512<short>.Count)
			{
				value2 = GetElementUnsafe(in vector, num);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> Shuffle(Vector512<ushort> vector, Vector512<ushort> indices)
	{
		Unsafe.SkipInit<Vector512<ushort>>(out var value);
		for (int i = 0; i < Vector512<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in indices, i);
			ushort value2 = 0;
			if (elementUnsafe < Vector512<ushort>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector512<int> Shuffle(Vector512<int> vector, Vector512<int> indices)
	{
		Unsafe.SkipInit<Vector512<int>>(out var value);
		for (int i = 0; i < Vector512<int>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			int value2 = 0;
			if (elementUnsafe < Vector512<int>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> Shuffle(Vector512<uint> vector, Vector512<uint> indices)
	{
		Unsafe.SkipInit<Vector512<uint>>(out var value);
		for (int i = 0; i < Vector512<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in indices, i);
			uint value2 = 0u;
			if (elementUnsafe < Vector512<uint>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector512<float> Shuffle(Vector512<float> vector, Vector512<int> indices)
	{
		Unsafe.SkipInit<Vector512<float>>(out var value);
		for (int i = 0; i < Vector512<float>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			float value2 = 0f;
			if (elementUnsafe < Vector512<float>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector512<long> Shuffle(Vector512<long> vector, Vector512<long> indices)
	{
		Unsafe.SkipInit<Vector512<long>>(out var value);
		for (int i = 0; i < Vector512<long>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			long value2 = 0L;
			if (elementUnsafe < (uint)Vector512<long>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> Shuffle(Vector512<ulong> vector, Vector512<ulong> indices)
	{
		Unsafe.SkipInit<Vector512<ulong>>(out var value);
		for (int i = 0; i < Vector512<ulong>.Count; i++)
		{
			ulong elementUnsafe = GetElementUnsafe(in indices, i);
			ulong value2 = 0uL;
			if (elementUnsafe < (uint)Vector512<ulong>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector512<double> Shuffle(Vector512<double> vector, Vector512<long> indices)
	{
		Unsafe.SkipInit<Vector512<double>>(out var value);
		for (int i = 0; i < Vector512<double>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			double value2 = 0.0;
			if (elementUnsafe < (uint)Vector512<double>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Sqrt<T>(Vector512<T> vector)
	{
		return Create(Vector256.Sqrt(vector._lower), Vector256.Sqrt(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void Store<T>(this Vector512<T> source, T* destination)
	{
		source.StoreUnsafe(ref *destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAligned<T>(this Vector512<T> source, T* destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		if ((nuint)destination % (nuint)64u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		*(Vector512<T>*)destination = source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAlignedNonTemporal<T>(this Vector512<T> source, T* destination)
	{
		source.StoreAligned(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static void StoreUnsafe<T>(this Vector512<T> source, ref T destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static void StoreUnsafe<T>(this Vector512<T> source, ref T destination, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Subtract<T>(Vector512<T> left, Vector512<T> right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Sum<T>(Vector512<T> vector)
	{
		T left = Vector256.Sum(vector._lower);
		return Scalar<T>.Add(left, Vector256.Sum(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T ToScalar<T>(this Vector512<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return GetElementUnsafe(in vector, 0);
	}

	public static bool TryCopyTo<T>(this Vector512<T> vector, Span<T> destination)
	{
		if ((uint)destination.Length < (uint)Vector512<T>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector512<ushort> Lower, Vector512<ushort> Upper) Widen(Vector512<byte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector512<int> Lower, Vector512<int> Upper) Widen(Vector512<short> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector512<long> Lower, Vector512<long> Upper) Widen(Vector512<int> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector512<short> Lower, Vector512<short> Upper) Widen(Vector512<sbyte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector512<double> Lower, Vector512<double> Upper) Widen(Vector512<float> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector512<uint> Lower, Vector512<uint> Upper) Widen(Vector512<ushort> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector512<ulong> Lower, Vector512<ulong> Upper) Widen(Vector512<uint> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> WidenLower(Vector512<byte> source)
	{
		Vector256<byte> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> WidenLower(Vector512<short> source)
	{
		Vector256<short> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> WidenLower(Vector512<int> source)
	{
		Vector256<int> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<short> WidenLower(Vector512<sbyte> source)
	{
		Vector256<sbyte> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> WidenLower(Vector512<float> source)
	{
		Vector256<float> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> WidenLower(Vector512<ushort> source)
	{
		Vector256<ushort> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> WidenLower(Vector512<uint> source)
	{
		Vector256<uint> lower = source._lower;
		return Create(Vector256.WidenLower(lower), Vector256.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ushort> WidenUpper(Vector512<byte> source)
	{
		Vector256<byte> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<int> WidenUpper(Vector512<short> source)
	{
		Vector256<short> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<long> WidenUpper(Vector512<int> source)
	{
		Vector256<int> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<short> WidenUpper(Vector512<sbyte> source)
	{
		Vector256<sbyte> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<double> WidenUpper(Vector512<float> source)
	{
		Vector256<float> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<uint> WidenUpper(Vector512<ushort> source)
	{
		Vector256<ushort> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector512<ulong> WidenUpper(Vector512<uint> source)
	{
		Vector256<uint> upper = source._upper;
		return Create(Vector256.WidenLower(upper), Vector256.WidenUpper(upper));
	}

	[Intrinsic]
	public static Vector512<T> WithElement<T>(this Vector512<T> vector, int index, T value)
	{
		if ((uint)index >= (uint)Vector512<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector512<T> vector2 = vector;
		SetElementUnsafe(in vector2, index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> WithLower<T>(this Vector512<T> vector, Vector256<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Vector512<T> vector2 = vector;
		vector2.SetLowerUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> WithUpper<T>(this Vector512<T> vector, Vector256<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Vector512<T> vector2 = vector;
		vector2.SetUpperUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> Xor<T>(Vector512<T> left, Vector512<T> right)
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetElementUnsafe<T>(this in Vector512<T> vector, int index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector512<T>, T>(ref Unsafe.AsRef(ref vector)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetElementUnsafe<T>(this in Vector512<T> vector, int index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector512<T>, T>(ref Unsafe.AsRef(ref vector)), index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetLowerUnsafe<T>(this in Vector512<T> vector, Vector256<T> value)
	{
		Unsafe.AsRef(ref vector._lower) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetUpperUnsafe<T>(this in Vector512<T> vector, Vector256<T> value)
	{
		Unsafe.AsRef(ref vector._upper) = value;
	}
}
[StructLayout(LayoutKind.Sequential, Size = 64)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector512DebugView<>))]
public readonly struct Vector512<T> : IEquatable<Vector512<T>>
{
	internal readonly Vector256<T> _lower;

	internal readonly Vector256<T> _upper;

	public static Vector512<T> AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector256<T> allBitsSet = Vector256<T>.AllBitsSet;
			return Vector512.Create(allBitsSet, allBitsSet);
		}
	}

	public static int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			return Vector256<T>.Count * 2;
		}
	}

	public static bool IsSupported
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			if (!(typeof(T) == typeof(byte)) && !(typeof(T) == typeof(double)) && !(typeof(T) == typeof(short)) && !(typeof(T) == typeof(int)) && !(typeof(T) == typeof(long)) && !(typeof(T) == typeof(nint)) && !(typeof(T) == typeof(sbyte)) && !(typeof(T) == typeof(float)) && !(typeof(T) == typeof(ushort)) && !(typeof(T) == typeof(uint)) && !(typeof(T) == typeof(ulong)))
			{
				return typeof(T) == typeof(nuint);
			}
			return true;
		}
	}

	public static Vector512<T> One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector256<T> one = Vector256<T>.One;
			return Vector512.Create(one, one);
		}
	}

	public static Vector512<T> Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
			return default(Vector512<T>);
		}
	}

	internal string DisplayString
	{
		get
		{
			if (!IsSupported)
			{
				return SR.NotSupported_Type;
			}
			return ToString();
		}
	}

	public T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return this.GetElement(index);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator +(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower + right._lower, left._upper + right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator &(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower & right._lower, left._upper & right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator |(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower | right._lower, left._upper | right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator /(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower / right._lower, left._upper / right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator /(Vector512<T> left, T right)
	{
		return Vector512.Create(left._lower / right, left._upper / right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector512<T> left, Vector512<T> right)
	{
		if (left._lower == right._lower)
		{
			return left._upper == right._upper;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator ^(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower ^ right._lower, left._upper ^ right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector512<T> left, Vector512<T> right)
	{
		if (!(left._lower != right._lower))
		{
			return left._upper != right._upper;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator <<(Vector512<T> value, int shiftCount)
	{
		return Vector512.Create(value._lower << shiftCount, value._upper << shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator *(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower * right._lower, left._upper * right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator *(Vector512<T> left, T right)
	{
		return Vector512.Create(left._lower * right, left._upper * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator *(T left, Vector512<T> right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator ~(Vector512<T> vector)
	{
		return Vector512.Create(~vector._lower, ~vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator >>(Vector512<T> value, int shiftCount)
	{
		return Vector512.Create(value._lower >> shiftCount, value._upper >> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator -(Vector512<T> left, Vector512<T> right)
	{
		return Vector512.Create(left._lower - right._lower, left._upper - right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator -(Vector512<T> vector)
	{
		return Vector512.Create(-vector._lower, -vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator +(Vector512<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector512<T> operator >>>(Vector512<T> value, int shiftCount)
	{
		return Vector512.Create(value._lower >>> shiftCount, value._upper >>> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector512<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector512<T> other)
	{
		if (Vector512.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
			{
				Vector512<T> vector = Vector512.Equals(this, other) | ~(Vector512.Equals(this, this) | Vector512.Equals(other, other));
				return vector.AsInt32() == Vector512<int>.AllBitsSet;
			}
			return this == other;
		}
		if (_lower.Equals(other._lower))
		{
			return _upper.Equals(other._upper);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		for (int i = 0; i < Count; i++)
		{
			T elementUnsafe = Vector512.GetElementUnsafe(in this, i);
			hashCode.Add(elementUnsafe);
		}
		return hashCode.ToHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString()
	{
		return ToString("G", CultureInfo.InvariantCulture);
	}

	private string ToString([StringSyntax("NumericFormat")] string format, IFormatProvider formatProvider)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		valueStringBuilder.Append('<');
		valueStringBuilder.Append(((IFormattable)(object)Vector512.GetElementUnsafe(in this, 0)).ToString(format, formatProvider));
		for (int i = 1; i < Count; i++)
		{
			valueStringBuilder.Append(numberGroupSeparator);
			valueStringBuilder.Append(' ');
			valueStringBuilder.Append(((IFormattable)(object)Vector512.GetElementUnsafe(in this, i)).ToString(format, formatProvider));
		}
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}
}
