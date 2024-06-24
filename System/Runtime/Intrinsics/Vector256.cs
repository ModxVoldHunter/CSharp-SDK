using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System.Runtime.Intrinsics;

public static class Vector256
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
	public static Vector256<T> Abs<T>(Vector256<T> vector)
	{
		return Create(Vector128.Abs(vector._lower), Vector128.Abs(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Add<T>(Vector256<T> left, Vector256<T> right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> AndNot<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.AndNot(left._lower, right._lower), Vector128.AndNot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<TTo> As<TFrom, TTo>(this Vector256<TFrom> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<TTo>();
		return Unsafe.As<Vector256<TFrom>, Vector256<TTo>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> AsByte<T>(this Vector256<T> vector)
	{
		return vector.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> AsDouble<T>(this Vector256<T> vector)
	{
		return vector.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> AsInt16<T>(this Vector256<T> vector)
	{
		return vector.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> AsInt32<T>(this Vector256<T> vector)
	{
		return vector.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> AsInt64<T>(this Vector256<T> vector)
	{
		return vector.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> AsNInt<T>(this Vector256<T> vector)
	{
		return vector.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> AsNUInt<T>(this Vector256<T> vector)
	{
		return vector.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> AsSByte<T>(this Vector256<T> vector)
	{
		return vector.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> AsSingle<T>(this Vector256<T> vector)
	{
		return vector.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> AsUInt16<T>(this Vector256<T> vector)
	{
		return vector.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> AsUInt32<T>(this Vector256<T> vector)
	{
		return vector.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> AsUInt64<T>(this Vector256<T> vector)
	{
		return vector.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> AsVector256<T>(this Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Vector256<T> source = default(Vector256<T>);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<T>, byte>(ref source), value);
		return source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> AsVector<T>(this Vector256<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<Vector256<T>, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> BitwiseAnd<T>(Vector256<T> left, Vector256<T> right)
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> BitwiseOr<T>(Vector256<T> left, Vector256<T> right)
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> Ceiling(Vector256<float> vector)
	{
		return Create(Vector128.Ceiling(vector._lower), Vector128.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> Ceiling(Vector256<double> vector)
	{
		return Create(Vector128.Ceiling(vector._lower), Vector128.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> ConditionalSelect<T>(Vector256<T> condition, Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.ConditionalSelect(condition._lower, left._lower, right._lower), Vector128.ConditionalSelect(condition._upper, left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> ConvertToDouble(Vector256<long> vector)
	{
		if (Avx2.IsSupported)
		{
			Vector256<int> left = vector.AsInt32();
			left = Avx2.Blend(left, Create(4841369599423283200L).AsInt32(), 170);
			Vector256<long> left2 = Avx2.ShiftRightLogical(vector, 32);
			left2 = Avx2.Xor(left2, Create(4985484789646622720L));
			Vector256<double> left3 = Avx.Subtract(left2.AsDouble(), Create(4985484789647671296L).AsDouble());
			return Avx.Add(left3, left.AsDouble());
		}
		return Create(Vector128.ConvertToDouble(vector._lower), Vector128.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<double> ConvertToDouble(Vector256<ulong> vector)
	{
		if (Avx2.IsSupported)
		{
			Vector256<uint> left = vector.AsUInt32();
			left = Avx2.Blend(left, Create(4841369599423283200uL).AsUInt32(), 170);
			Vector256<ulong> left2 = Avx2.ShiftRightLogical(vector, 32);
			left2 = Avx2.Xor(left2, Create(4985484787499139072uL));
			Vector256<double> left3 = Avx.Subtract(left2.AsDouble(), Create(4985484787500187648uL).AsDouble());
			return Avx.Add(left3, left.AsDouble());
		}
		return Create(Vector128.ConvertToDouble(vector._lower), Vector128.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> ConvertToInt32(Vector256<float> vector)
	{
		return Create(Vector128.ConvertToInt32(vector._lower), Vector128.ConvertToInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> ConvertToInt64(Vector256<double> vector)
	{
		return Create(Vector128.ConvertToInt64(vector._lower), Vector128.ConvertToInt64(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> ConvertToSingle(Vector256<int> vector)
	{
		return Create(Vector128.ConvertToSingle(vector._lower), Vector128.ConvertToSingle(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<float> ConvertToSingle(Vector256<uint> vector)
	{
		if (Avx2.IsSupported)
		{
			Vector256<int> value = Avx2.And(vector, Create(65535u)).AsInt32();
			Vector256<int> value2 = Avx2.ShiftRightLogical(vector, 16).AsInt32();
			Vector256<float> vector2 = Avx.ConvertToVector256Single(value);
			Vector256<float> vector3 = Avx.ConvertToVector256Single(value2);
			if (Fma.IsSupported)
			{
				return Fma.MultiplyAdd(vector3, Create(65536f), vector2);
			}
			Vector256<float> left = Avx.Multiply(vector3, Create(65536f));
			return Avx.Add(left, vector2);
		}
		return Create(Vector128.ConvertToSingle(vector._lower), Vector128.ConvertToSingle(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> ConvertToUInt32(Vector256<float> vector)
	{
		return Create(Vector128.ConvertToUInt32(vector._lower), Vector128.ConvertToUInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> ConvertToUInt64(Vector256<double> vector)
	{
		return Create(Vector128.ConvertToUInt64(vector._lower), Vector128.ConvertToUInt64(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector256<T> vector, T[] destination)
	{
		if (destination.Length < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector256<T> vector, T[] destination, int startIndex)
	{
		if ((uint)startIndex >= (uint)destination.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (destination.Length - startIndex < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector256<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Create<T>(T value)
	{
		Vector128<T> vector = Vector128.Create(value);
		return Create(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> Create(byte value)
	{
		return Vector256.Create<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> Create(double value)
	{
		return Vector256.Create<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> Create(short value)
	{
		return Vector256.Create<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> Create(int value)
	{
		return Vector256.Create<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> Create(long value)
	{
		return Vector256.Create<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> Create(nint value)
	{
		return Vector256.Create<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> Create(nuint value)
	{
		return Vector256.Create<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(sbyte value)
	{
		return Vector256.Create<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> Create(float value)
	{
		return Vector256.Create<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(ushort value)
	{
		return Vector256.Create<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(uint value)
	{
		return Vector256.Create<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(ulong value)
	{
		return Vector256.Create<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> Create<T>(T[] values)
	{
		if (values.Length < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref values[0]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> Create<T>(T[] values, int index)
	{
		if (index < 0 || values.Length - index < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> Create<T>(ReadOnlySpan<T> values)
	{
		if (values.Length < Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
	{
		return Create(Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15), Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> Create(double e0, double e1, double e2, double e3)
	{
		return Create(Vector128.Create(e0, e1), Vector128.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
	{
		return Create(Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
	{
		return Create(Vector128.Create(e0, e1, e2, e3), Vector128.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> Create(long e0, long e1, long e2, long e3)
	{
		return Create(Vector128.Create(e0, e1), Vector128.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
	{
		return Create(Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15), Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
	{
		return Create(Vector128.Create(e0, e1, e2, e3), Vector128.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
	{
		return Create(Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
	{
		return Create(Vector128.Create(e0, e1, e2, e3), Vector128.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3)
	{
		return Create(Vector128.Create(e0, e1), Vector128.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> Create<T>(Vector128<T> lower, Vector128<T> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<T> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Unsafe.SkipInit<Vector256<T>>(out var value);
		value.SetLowerUnsafe<T>(lower);
		value.SetUpperUnsafe<T>(upper);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<byte> Create(Vector128<byte> lower, Vector128<byte> upper)
	{
		return Vector256.Create<byte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<double> Create(Vector128<double> lower, Vector128<double> upper)
	{
		return Vector256.Create<double>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<short> Create(Vector128<short> lower, Vector128<short> upper)
	{
		return Vector256.Create<short>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<int> Create(Vector128<int> lower, Vector128<int> upper)
	{
		return Vector256.Create<int>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<long> Create(Vector128<long> lower, Vector128<long> upper)
	{
		return Vector256.Create<long>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<nint> Create(Vector128<nint> lower, Vector128<nint> upper)
	{
		return Vector256.Create<nint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<nuint> Create(Vector128<nuint> lower, Vector128<nuint> upper)
	{
		return Vector256.Create<nuint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(Vector128<sbyte> lower, Vector128<sbyte> upper)
	{
		return Vector256.Create<sbyte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<float> Create(Vector128<float> lower, Vector128<float> upper)
	{
		return Vector256.Create<float>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(Vector128<ushort> lower, Vector128<ushort> upper)
	{
		return Vector256.Create<ushort>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(Vector128<uint> lower, Vector128<uint> upper)
	{
		return Vector256.Create<uint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(Vector128<ulong> lower, Vector128<ulong> upper)
	{
		return Vector256.Create<ulong>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> CreateScalar<T>(T value)
	{
		return Vector128.CreateScalar(value).ToVector256();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> CreateScalar(byte value)
	{
		return Vector256.CreateScalar<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> CreateScalar(double value)
	{
		return Vector256.CreateScalar<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> CreateScalar(short value)
	{
		return Vector256.CreateScalar<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> CreateScalar(int value)
	{
		return Vector256.CreateScalar<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> CreateScalar(long value)
	{
		return Vector256.CreateScalar<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> CreateScalar(nint value)
	{
		return Vector256.CreateScalar<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> CreateScalar(nuint value)
	{
		return Vector256.CreateScalar<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> CreateScalar(sbyte value)
	{
		return Vector256.CreateScalar<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> CreateScalar(float value)
	{
		return Vector256.CreateScalar<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> CreateScalar(ushort value)
	{
		return Vector256.CreateScalar<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> CreateScalar(uint value)
	{
		return Vector256.CreateScalar<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> CreateScalar(ulong value)
	{
		return Vector256.CreateScalar<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> CreateScalarUnsafe<T>(T value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Unsafe.SkipInit<Vector256<T>>(out var value2);
		SetElementUnsafe(in value2, 0, value);
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> CreateScalarUnsafe(byte value)
	{
		return Vector256.CreateScalarUnsafe<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> CreateScalarUnsafe(double value)
	{
		return Vector256.CreateScalarUnsafe<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> CreateScalarUnsafe(short value)
	{
		return Vector256.CreateScalarUnsafe<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> CreateScalarUnsafe(int value)
	{
		return Vector256.CreateScalarUnsafe<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> CreateScalarUnsafe(long value)
	{
		return Vector256.CreateScalarUnsafe<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> CreateScalarUnsafe(nint value)
	{
		return Vector256.CreateScalarUnsafe<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> CreateScalarUnsafe(nuint value)
	{
		return Vector256.CreateScalarUnsafe<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> CreateScalarUnsafe(sbyte value)
	{
		return Vector256.CreateScalarUnsafe<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> CreateScalarUnsafe(float value)
	{
		return Vector256.CreateScalarUnsafe<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> CreateScalarUnsafe(ushort value)
	{
		return Vector256.CreateScalarUnsafe<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> CreateScalarUnsafe(uint value)
	{
		return Vector256.CreateScalarUnsafe<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> CreateScalarUnsafe(ulong value)
	{
		return Vector256.CreateScalarUnsafe<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Divide<T>(Vector256<T> left, Vector256<T> right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Divide<T>(Vector256<T> left, T right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector256<T> left, Vector256<T> right)
	{
		T left2 = Vector128.Dot(left._lower, right._lower);
		return Scalar<T>.Add(left2, Vector128.Dot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Equals<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.Equals(left._lower, right._lower), Vector128.Equals(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAll<T>(Vector256<T> left, Vector256<T> right)
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAny<T>(Vector256<T> left, Vector256<T> right)
	{
		if (!Vector128.EqualsAny(left._lower, right._lower))
		{
			return Vector128.EqualsAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint ExtractMostSignificantBits<T>(this Vector256<T> vector)
	{
		uint num = vector._lower.ExtractMostSignificantBits();
		return num | (vector._upper.ExtractMostSignificantBits() << Vector128<T>.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> Floor(Vector256<float> vector)
	{
		return Create(Vector128.Floor(vector._lower), Vector128.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> Floor(Vector256<double> vector)
	{
		return Create(Vector128.Floor(vector._lower), Vector128.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T GetElement<T>(this Vector256<T> vector, int index)
	{
		if ((uint)index >= (uint)Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return GetElementUnsafe(in vector, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> GetLower<T>(this Vector256<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return vector._lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> GetUpper<T>(this Vector256<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return vector._upper;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> GreaterThan<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.GreaterThan(left._lower, right._lower), Vector128.GreaterThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAll<T>(Vector256<T> left, Vector256<T> right)
	{
		if (Vector128.GreaterThanAll(left._lower, right._lower))
		{
			return Vector128.GreaterThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAny<T>(Vector256<T> left, Vector256<T> right)
	{
		if (!Vector128.GreaterThanAny(left._lower, right._lower))
		{
			return Vector128.GreaterThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> GreaterThanOrEqual<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.GreaterThanOrEqual(left._lower, right._lower), Vector128.GreaterThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAll<T>(Vector256<T> left, Vector256<T> right)
	{
		if (Vector128.GreaterThanOrEqualAll(left._lower, right._lower))
		{
			return Vector128.GreaterThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAny<T>(Vector256<T> left, Vector256<T> right)
	{
		if (!Vector128.GreaterThanOrEqualAny(left._lower, right._lower))
		{
			return Vector128.GreaterThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> LessThan<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.LessThan(left._lower, right._lower), Vector128.LessThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAll<T>(Vector256<T> left, Vector256<T> right)
	{
		if (Vector128.LessThanAll(left._lower, right._lower))
		{
			return Vector128.LessThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAny<T>(Vector256<T> left, Vector256<T> right)
	{
		if (!Vector128.LessThanAny(left._lower, right._lower))
		{
			return Vector128.LessThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> LessThanOrEqual<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.LessThanOrEqual(left._lower, right._lower), Vector128.LessThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAll<T>(Vector256<T> left, Vector256<T> right)
	{
		if (Vector128.LessThanOrEqualAll(left._lower, right._lower))
		{
			return Vector128.LessThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAny<T>(Vector256<T> left, Vector256<T> right)
	{
		if (!Vector128.LessThanOrEqualAny(left._lower, right._lower))
		{
			return Vector128.LessThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<T> Load<T>(T* source)
	{
		return LoadUnsafe(ref *source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<T> LoadAligned<T>(T* source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		if ((nuint)source % (nuint)32u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		return *(Vector256<T>*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<T> LoadAlignedNonTemporal<T>(T* source)
	{
		return LoadAligned(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> LoadUnsafe<T>([In][RequiresLocation] ref T source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(ref source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<T> LoadUnsafe<T>([In][RequiresLocation] ref T source, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(ref source), (nint)elementOffset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector256<ushort> LoadUnsafe(ref char source)
	{
		return LoadUnsafe(ref Unsafe.As<char, ushort>(ref source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector256<ushort> LoadUnsafe(ref char source, nuint elementOffset)
	{
		return LoadUnsafe(ref Unsafe.As<char, ushort>(ref source), elementOffset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Max<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.Max(left._lower, right._lower), Vector128.Max(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Min<T>(Vector256<T> left, Vector256<T> right)
	{
		return Create(Vector128.Min(left._lower, right._lower), Vector128.Min(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Multiply<T>(Vector256<T> left, Vector256<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Multiply<T>(Vector256<T> left, T right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Multiply<T>(T left, Vector256<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<float> Narrow(Vector256<double> lower, Vector256<double> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Narrow(Vector256<short> lower, Vector256<short> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> Narrow(Vector256<int> lower, Vector256<int> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> Narrow(Vector256<long> lower, Vector256<long> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<byte> Narrow(Vector256<ushort> lower, Vector256<ushort> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Narrow(Vector256<uint> lower, Vector256<uint> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Narrow(Vector256<ulong> lower, Vector256<ulong> upper)
	{
		return Create(Vector128.Narrow(lower._lower, lower._upper), Vector128.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Negate<T>(Vector256<T> vector)
	{
		return -vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> OnesComplement<T>(Vector256<T> vector)
	{
		return Create(Vector128.OnesComplement(vector._lower), Vector128.OnesComplement(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> ShiftLeft(Vector256<byte> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> ShiftLeft(Vector256<short> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> ShiftLeft(Vector256<int> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> ShiftLeft(Vector256<long> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> ShiftLeft(Vector256<nint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> ShiftLeft(Vector256<nuint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> ShiftLeft(Vector256<sbyte> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> ShiftLeft(Vector256<ushort> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> ShiftLeft(Vector256<uint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> ShiftLeft(Vector256<ulong> vector, int shiftCount)
	{
		return Create(Vector128.ShiftLeft(vector._lower, shiftCount), Vector128.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> ShiftRightArithmetic(Vector256<short> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightArithmetic(vector._lower, shiftCount), Vector128.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> ShiftRightArithmetic(Vector256<int> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightArithmetic(vector._lower, shiftCount), Vector128.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> ShiftRightArithmetic(Vector256<long> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightArithmetic(vector._lower, shiftCount), Vector128.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> ShiftRightArithmetic(Vector256<nint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightArithmetic(vector._lower, shiftCount), Vector128.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightArithmetic(vector._lower, shiftCount), Vector128.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<byte> ShiftRightLogical(Vector256<byte> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<short> ShiftRightLogical(Vector256<short> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> ShiftRightLogical(Vector256<int> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> ShiftRightLogical(Vector256<long> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<nint> ShiftRightLogical(Vector256<nint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<nuint> ShiftRightLogical(Vector256<nuint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> ShiftRightLogical(Vector256<uint> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> vector, int shiftCount)
	{
		return Create(Vector128.ShiftRightLogical(vector._lower, shiftCount), Vector128.ShiftRightLogical(vector._upper, shiftCount));
	}

	[Intrinsic]
	public static Vector256<byte> Shuffle(Vector256<byte> vector, Vector256<byte> indices)
	{
		Unsafe.SkipInit<Vector256<byte>>(out var value);
		for (int i = 0; i < Vector256<byte>.Count; i++)
		{
			byte elementUnsafe = GetElementUnsafe(in indices, i);
			byte value2 = 0;
			if (elementUnsafe < Vector256<byte>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Shuffle(Vector256<sbyte> vector, Vector256<sbyte> indices)
	{
		Unsafe.SkipInit<Vector256<sbyte>>(out var value);
		for (int i = 0; i < Vector256<sbyte>.Count; i++)
		{
			byte b = (byte)GetElementUnsafe(in indices, i);
			sbyte value2 = 0;
			if (b < Vector256<sbyte>.Count)
			{
				value2 = GetElementUnsafe(in vector, b);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector256<short> Shuffle(Vector256<short> vector, Vector256<short> indices)
	{
		Unsafe.SkipInit<Vector256<short>>(out var value);
		for (int i = 0; i < Vector256<short>.Count; i++)
		{
			ushort num = (ushort)GetElementUnsafe(in indices, i);
			short value2 = 0;
			if (num < Vector256<short>.Count)
			{
				value2 = GetElementUnsafe(in vector, num);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Shuffle(Vector256<ushort> vector, Vector256<ushort> indices)
	{
		Unsafe.SkipInit<Vector256<ushort>>(out var value);
		for (int i = 0; i < Vector256<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in indices, i);
			ushort value2 = 0;
			if (elementUnsafe < Vector256<ushort>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector256<int> Shuffle(Vector256<int> vector, Vector256<int> indices)
	{
		Unsafe.SkipInit<Vector256<int>>(out var value);
		for (int i = 0; i < Vector256<int>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			int value2 = 0;
			if (elementUnsafe < Vector256<int>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Shuffle(Vector256<uint> vector, Vector256<uint> indices)
	{
		Unsafe.SkipInit<Vector256<uint>>(out var value);
		for (int i = 0; i < Vector256<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in indices, i);
			uint value2 = 0u;
			if (elementUnsafe < Vector256<uint>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector256<float> Shuffle(Vector256<float> vector, Vector256<int> indices)
	{
		Unsafe.SkipInit<Vector256<float>>(out var value);
		for (int i = 0; i < Vector256<float>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			float value2 = 0f;
			if (elementUnsafe < Vector256<float>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector256<long> Shuffle(Vector256<long> vector, Vector256<long> indices)
	{
		Unsafe.SkipInit<Vector256<long>>(out var value);
		for (int i = 0; i < Vector256<long>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			long value2 = 0L;
			if (elementUnsafe < (uint)Vector256<long>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> Shuffle(Vector256<ulong> vector, Vector256<ulong> indices)
	{
		Unsafe.SkipInit<Vector256<ulong>>(out var value);
		for (int i = 0; i < Vector256<ulong>.Count; i++)
		{
			ulong elementUnsafe = GetElementUnsafe(in indices, i);
			ulong value2 = 0uL;
			if (elementUnsafe < (uint)Vector256<ulong>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector256<double> Shuffle(Vector256<double> vector, Vector256<long> indices)
	{
		Unsafe.SkipInit<Vector256<double>>(out var value);
		for (int i = 0; i < Vector256<double>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			double value2 = 0.0;
			if (elementUnsafe < (uint)Vector256<double>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Sqrt<T>(Vector256<T> vector)
	{
		return Create(Vector128.Sqrt(vector._lower), Vector128.Sqrt(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void Store<T>(this Vector256<T> source, T* destination)
	{
		source.StoreUnsafe(ref *destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAligned<T>(this Vector256<T> source, T* destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		if ((nuint)destination % (nuint)32u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		*(Vector256<T>*)destination = source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAlignedNonTemporal<T>(this Vector256<T> source, T* destination)
	{
		source.StoreAligned(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static void StoreUnsafe<T>(this Vector256<T> source, ref T destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static void StoreUnsafe<T>(this Vector256<T> source, ref T destination, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Subtract<T>(Vector256<T> left, Vector256<T> right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Sum<T>(Vector256<T> vector)
	{
		T left = Vector128.Sum(vector._lower);
		return Scalar<T>.Add(left, Vector128.Sum(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T ToScalar<T>(this Vector256<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return GetElementUnsafe(in vector, 0);
	}

	[Intrinsic]
	public static Vector512<T> ToVector512<T>(this Vector256<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Vector512<T> vector2 = default(Vector512<T>);
		vector2.SetLowerUnsafe<T>(vector);
		return vector2;
	}

	[Intrinsic]
	public static Vector512<T> ToVector512Unsafe<T>(this Vector256<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Unsafe.SkipInit<Vector512<T>>(out var value);
		value.SetLowerUnsafe<T>(vector);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCopyTo<T>(this Vector256<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector256<T>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector256<ushort> Lower, Vector256<ushort> Upper) Widen(Vector256<byte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector256<int> Lower, Vector256<int> Upper) Widen(Vector256<short> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector256<long> Lower, Vector256<long> Upper) Widen(Vector256<int> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector256<short> Lower, Vector256<short> Upper) Widen(Vector256<sbyte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector256<double> Lower, Vector256<double> Upper) Widen(Vector256<float> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector256<uint> Lower, Vector256<uint> Upper) Widen(Vector256<ushort> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector256<ulong> Lower, Vector256<ulong> Upper) Widen(Vector256<uint> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> WidenLower(Vector256<byte> source)
	{
		Vector128<byte> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> WidenLower(Vector256<short> source)
	{
		Vector128<short> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> WidenLower(Vector256<int> source)
	{
		Vector128<int> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<short> WidenLower(Vector256<sbyte> source)
	{
		Vector128<sbyte> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> WidenLower(Vector256<float> source)
	{
		Vector128<float> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> WidenLower(Vector256<ushort> source)
	{
		Vector128<ushort> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> WidenLower(Vector256<uint> source)
	{
		Vector128<uint> lower = source._lower;
		return Create(Vector128.WidenLower(lower), Vector128.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> WidenUpper(Vector256<byte> source)
	{
		Vector128<byte> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<int> WidenUpper(Vector256<short> source)
	{
		Vector128<short> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<long> WidenUpper(Vector256<int> source)
	{
		Vector128<int> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<short> WidenUpper(Vector256<sbyte> source)
	{
		Vector128<sbyte> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<double> WidenUpper(Vector256<float> source)
	{
		Vector128<float> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> WidenUpper(Vector256<ushort> source)
	{
		Vector128<ushort> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> WidenUpper(Vector256<uint> source)
	{
		Vector128<uint> upper = source._upper;
		return Create(Vector128.WidenLower(upper), Vector128.WidenUpper(upper));
	}

	[Intrinsic]
	public static Vector256<T> WithElement<T>(this Vector256<T> vector, int index, T value)
	{
		if ((uint)index >= (uint)Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector256<T> vector2 = vector;
		SetElementUnsafe(in vector2, index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> WithLower<T>(this Vector256<T> vector, Vector128<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Vector256<T> vector2 = vector;
		vector2.SetLowerUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> WithUpper<T>(this Vector256<T> vector, Vector128<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Vector256<T> vector2 = vector;
		vector2.SetUpperUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> Xor<T>(Vector256<T> left, Vector256<T> right)
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetElementUnsafe<T>(this in Vector256<T> vector, int index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(ref vector)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetElementUnsafe<T>(this in Vector256<T> vector, int index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(ref vector)), index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetLowerUnsafe<T>(this in Vector256<T> vector, Vector128<T> value)
	{
		Unsafe.AsRef(ref vector._lower) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetUpperUnsafe<T>(this in Vector256<T> vector, Vector128<T> value)
	{
		Unsafe.AsRef(ref vector._upper) = value;
	}
}
[StructLayout(LayoutKind.Sequential, Size = 32)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector256DebugView<>))]
public readonly struct Vector256<T> : IEquatable<Vector256<T>>
{
	internal readonly Vector128<T> _lower;

	internal readonly Vector128<T> _upper;

	public static Vector256<T> AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector128<T> allBitsSet = Vector128<T>.AllBitsSet;
			return Vector256.Create(allBitsSet, allBitsSet);
		}
	}

	public static int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			return Vector128<T>.Count * 2;
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

	public static Vector256<T> One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector128<T> one = Vector128<T>.One;
			return Vector256.Create(one, one);
		}
	}

	public static Vector256<T> Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
			return default(Vector256<T>);
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
	public static Vector256<T> operator +(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower + right._lower, left._upper + right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator &(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower & right._lower, left._upper & right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator |(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower | right._lower, left._upper | right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator /(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower / right._lower, left._upper / right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator /(Vector256<T> left, T right)
	{
		return Vector256.Create(left._lower / right, left._upper / right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector256<T> left, Vector256<T> right)
	{
		if (left._lower == right._lower)
		{
			return left._upper == right._upper;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator ^(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower ^ right._lower, left._upper ^ right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector256<T> left, Vector256<T> right)
	{
		if (!(left._lower != right._lower))
		{
			return left._upper != right._upper;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator <<(Vector256<T> value, int shiftCount)
	{
		return Vector256.Create(value._lower << shiftCount, value._upper << shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator *(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower * right._lower, left._upper * right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator *(Vector256<T> left, T right)
	{
		return Vector256.Create(left._lower * right, left._upper * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator *(T left, Vector256<T> right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator ~(Vector256<T> vector)
	{
		return Vector256.Create(~vector._lower, ~vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator >>(Vector256<T> value, int shiftCount)
	{
		return Vector256.Create(value._lower >> shiftCount, value._upper >> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator -(Vector256<T> left, Vector256<T> right)
	{
		return Vector256.Create(left._lower - right._lower, left._upper - right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator -(Vector256<T> vector)
	{
		return Vector256.Create(-vector._lower, -vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator +(Vector256<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> operator >>>(Vector256<T> value, int shiftCount)
	{
		return Vector256.Create(value._lower >>> shiftCount, value._upper >>> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector256<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector256<T> other)
	{
		if (Vector256.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
			{
				Vector256<T> vector = Vector256.Equals(this, other) | ~(Vector256.Equals(this, this) | Vector256.Equals(other, other));
				return vector.AsInt32() == Vector256<int>.AllBitsSet;
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
			T elementUnsafe = Vector256.GetElementUnsafe(in this, i);
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
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		valueStringBuilder.Append('<');
		valueStringBuilder.Append(((IFormattable)(object)Vector256.GetElementUnsafe(in this, 0)).ToString(format, formatProvider));
		for (int i = 1; i < Count; i++)
		{
			valueStringBuilder.Append(numberGroupSeparator);
			valueStringBuilder.Append(' ');
			valueStringBuilder.Append(((IFormattable)(object)Vector256.GetElementUnsafe(in this, i)).ToString(format, formatProvider));
		}
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}
}
