using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System.Runtime.Intrinsics;

public static class Vector128
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
	public static Vector128<T> Abs<T>(Vector128<T> vector)
	{
		return Create(Vector64.Abs(vector._lower), Vector64.Abs(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Add<T>(Vector128<T> left, Vector128<T> right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> AndNot<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.AndNot(left._lower, right._lower), Vector64.AndNot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<TTo> As<TFrom, TTo>(this Vector128<TFrom> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<TTo>();
		return Unsafe.As<Vector128<TFrom>, Vector128<TTo>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> AsByte<T>(this Vector128<T> vector)
	{
		return vector.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> AsDouble<T>(this Vector128<T> vector)
	{
		return vector.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> AsInt16<T>(this Vector128<T> vector)
	{
		return vector.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> AsInt32<T>(this Vector128<T> vector)
	{
		return vector.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> AsInt64<T>(this Vector128<T> vector)
	{
		return vector.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> AsNInt<T>(this Vector128<T> vector)
	{
		return vector.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> AsNUInt<T>(this Vector128<T> vector)
	{
		return vector.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector)
	{
		return vector.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> AsSingle<T>(this Vector128<T> vector)
	{
		return vector.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector)
	{
		return vector.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector)
	{
		return vector.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector)
	{
		return vector.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static Vector128<float> AsVector128(this Plane value)
	{
		return Unsafe.As<Plane, Vector128<float>>(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static Vector128<float> AsVector128(this Quaternion value)
	{
		return Unsafe.As<Quaternion, Vector128<float>>(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> AsVector128(this Vector2 value)
	{
		return new Vector4(value, 0f, 0f).AsVector128();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> AsVector128(this Vector3 value)
	{
		return new Vector4(value, 0f).AsVector128();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> AsVector128(this Vector4 value)
	{
		return Unsafe.As<Vector4, Vector128<float>>(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> AsVector128<T>(this Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<Vector<T>, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 AsVector2(this Vector128<float> value)
	{
		return Unsafe.ReadUnaligned<Vector2>(ref Unsafe.As<Vector128<float>, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 AsVector3(this Vector128<float> value)
	{
		return Unsafe.ReadUnaligned<Vector3>(ref Unsafe.As<Vector128<float>, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 AsVector4(this Vector128<float> value)
	{
		return Unsafe.As<Vector128<float>, Vector4>(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> AsVector<T>(this Vector128<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Vector<T> source = default(Vector<T>);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector<T>, byte>(ref source), value);
		return source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> BitwiseAnd<T>(Vector128<T> left, Vector128<T> right)
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> BitwiseOr<T>(Vector128<T> left, Vector128<T> right)
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> Ceiling(Vector128<float> vector)
	{
		return Create(Vector64.Ceiling(vector._lower), Vector64.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> Ceiling(Vector128<double> vector)
	{
		return Create(Vector64.Ceiling(vector._lower), Vector64.Ceiling(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> ConditionalSelect<T>(Vector128<T> condition, Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.ConditionalSelect(condition._lower, left._lower, right._lower), Vector64.ConditionalSelect(condition._upper, left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> ConvertToDouble(Vector128<long> vector)
	{
		if (Sse2.IsSupported)
		{
			Vector128<int> left;
			if (Avx2.IsSupported)
			{
				left = vector.AsInt32();
				left = Avx2.Blend(left, Create(4841369599423283200L).AsInt32(), 10);
			}
			else
			{
				left = Sse2.And(vector, Create(4294967295L)).AsInt32();
				left = Sse2.Or(left, Create(4841369599423283200L).AsInt32());
			}
			Vector128<long> left2 = Sse2.ShiftRightLogical(vector, 32);
			left2 = Sse2.Xor(left2, Create(4985484789646622720L));
			Vector128<double> left3 = Sse2.Subtract(left2.AsDouble(), Create(4985484789647671296L).AsDouble());
			return Sse2.Add(left3, left.AsDouble());
		}
		return Create(Vector64.ConvertToDouble(vector._lower), Vector64.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<double> ConvertToDouble(Vector128<ulong> vector)
	{
		if (Sse2.IsSupported)
		{
			Vector128<uint> left;
			if (Avx2.IsSupported)
			{
				left = vector.AsUInt32();
				left = Avx2.Blend(left, Create(4841369599423283200uL).AsUInt32(), 10);
			}
			else
			{
				left = Sse2.And(vector, Create(4294967295uL)).AsUInt32();
				left = Sse2.Or(left, Create(4841369599423283200uL).AsUInt32());
			}
			Vector128<ulong> left2 = Sse2.ShiftRightLogical(vector, 32);
			left2 = Sse2.Xor(left2, Create(4985484787499139072uL));
			Vector128<double> left3 = Sse2.Subtract(left2.AsDouble(), Create(4985484787500187648uL).AsDouble());
			return Sse2.Add(left3, left.AsDouble());
		}
		return Create(Vector64.ConvertToDouble(vector._lower), Vector64.ConvertToDouble(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> ConvertToInt32(Vector128<float> vector)
	{
		return Create(Vector64.ConvertToInt32(vector._lower), Vector64.ConvertToInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> ConvertToInt64(Vector128<double> vector)
	{
		return Create(Vector64.ConvertToInt64(vector._lower), Vector64.ConvertToInt64(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> ConvertToSingle(Vector128<int> vector)
	{
		return Create(Vector64.ConvertToSingle(vector._lower), Vector64.ConvertToSingle(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<float> ConvertToSingle(Vector128<uint> vector)
	{
		if (Sse2.IsSupported)
		{
			Vector128<int> value = Sse2.And(vector, Create(65535u)).AsInt32();
			Vector128<int> value2 = Sse2.ShiftRightLogical(vector, 16).AsInt32();
			Vector128<float> vector2 = Sse2.ConvertToVector128Single(value);
			Vector128<float> vector3 = Sse2.ConvertToVector128Single(value2);
			if (Fma.IsSupported)
			{
				return Fma.MultiplyAdd(vector3, Create(65536f), vector2);
			}
			Vector128<float> left = Sse.Multiply(vector3, Create(65536f));
			return Sse.Add(left, vector2);
		}
		return SoftwareFallback(vector);
		static Vector128<float> SoftwareFallback(Vector128<uint> vector)
		{
			Unsafe.SkipInit<Vector128<float>>(out var value3);
			for (int i = 0; i < Vector128<float>.Count; i++)
			{
				float value4 = GetElementUnsafe(in vector, i);
				value3.SetElementUnsafe(i, value4);
			}
			return value3;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> ConvertToUInt32(Vector128<float> vector)
	{
		return Create(Vector64.ConvertToUInt32(vector._lower), Vector64.ConvertToUInt32(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> ConvertToUInt64(Vector128<double> vector)
	{
		return Create(Vector64.ConvertToUInt64(vector._lower), Vector64.ConvertToUInt64(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector128<T> vector, T[] destination)
	{
		if (destination.Length < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector128<T> vector, T[] destination, int startIndex)
	{
		if ((uint)startIndex >= (uint)destination.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (destination.Length - startIndex < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector128<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Create<T>(T value)
	{
		Vector64<T> vector = Vector64.Create(value);
		return Create(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> Create(byte value)
	{
		return Vector128.Create<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> Create(double value)
	{
		return Vector128.Create<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> Create(short value)
	{
		return Vector128.Create<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> Create(int value)
	{
		return Vector128.Create<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> Create(long value)
	{
		return Vector128.Create<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> Create(nint value)
	{
		return Vector128.Create<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> Create(nuint value)
	{
		return Vector128.Create<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(sbyte value)
	{
		return Vector128.Create<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> Create(float value)
	{
		return Vector128.Create<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(ushort value)
	{
		return Vector128.Create<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(uint value)
	{
		return Vector128.Create<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(ulong value)
	{
		return Vector128.Create<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> Create<T>(T[] values)
	{
		if (values.Length < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref values[0]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> Create<T>(T[] values, int index)
	{
		if (index < 0 || values.Length - index < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> Create<T>(ReadOnlySpan<T> values)
	{
		if (values.Length < Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
	{
		return Create(Vector64.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector64.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> Create(double e0, double e1)
	{
		return Create(Vector64.Create(e0), Vector64.Create(e1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
	{
		return Create(Vector64.Create(e0, e1, e2, e3), Vector64.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> Create(int e0, int e1, int e2, int e3)
	{
		return Create(Vector64.Create(e0, e1), Vector64.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> Create(long e0, long e1)
	{
		return Create(Vector64.Create(e0), Vector64.Create(e1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
	{
		return Create(Vector64.Create(e0, e1, e2, e3, e4, e5, e6, e7), Vector64.Create(e8, e9, e10, e11, e12, e13, e14, e15));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> Create(float e0, float e1, float e2, float e3)
	{
		return Create(Vector64.Create(e0, e1), Vector64.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
	{
		return Create(Vector64.Create(e0, e1, e2, e3), Vector64.Create(e4, e5, e6, e7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(uint e0, uint e1, uint e2, uint e3)
	{
		return Create(Vector64.Create(e0, e1), Vector64.Create(e2, e3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(ulong e0, ulong e1)
	{
		return Create(Vector64.Create(e0), Vector64.Create(e1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> Create<T>(Vector64<T> lower, Vector64<T> upper)
	{
		if (false)
		{
		}
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
		Unsafe.SkipInit<Vector128<T>>(out var value);
		value.SetLowerUnsafe<T>(lower);
		value.SetUpperUnsafe<T>(upper);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
	{
		return Vector128.Create<byte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<double> Create(Vector64<double> lower, Vector64<double> upper)
	{
		return Vector128.Create<double>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<short> Create(Vector64<short> lower, Vector64<short> upper)
	{
		return Vector128.Create<short>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<int> Create(Vector64<int> lower, Vector64<int> upper)
	{
		return Vector128.Create<int>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<long> Create(Vector64<long> lower, Vector64<long> upper)
	{
		return Vector128.Create<long>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<nint> Create(Vector64<nint> lower, Vector64<nint> upper)
	{
		return Vector128.Create<nint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<nuint> Create(Vector64<nuint> lower, Vector64<nuint> upper)
	{
		return Vector128.Create<nuint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(Vector64<sbyte> lower, Vector64<sbyte> upper)
	{
		return Vector128.Create<sbyte>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Create(Vector64<float> lower, Vector64<float> upper)
	{
		return Vector128.Create<float>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(Vector64<ushort> lower, Vector64<ushort> upper)
	{
		return Vector128.Create<ushort>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(Vector64<uint> lower, Vector64<uint> upper)
	{
		return Vector128.Create<uint>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(Vector64<ulong> lower, Vector64<ulong> upper)
	{
		return Vector128.Create<ulong>(lower, upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> CreateScalar<T>(T value)
	{
		return Vector64.CreateScalar(value).ToVector128();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> CreateScalar(byte value)
	{
		return Vector128.CreateScalar<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> CreateScalar(double value)
	{
		return Vector128.CreateScalar<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> CreateScalar(short value)
	{
		return Vector128.CreateScalar<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> CreateScalar(int value)
	{
		return Vector128.CreateScalar<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> CreateScalar(long value)
	{
		return Vector128.CreateScalar<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> CreateScalar(nint value)
	{
		return Vector128.CreateScalar<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> CreateScalar(nuint value)
	{
		return Vector128.CreateScalar<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> CreateScalar(sbyte value)
	{
		return Vector128.CreateScalar<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> CreateScalar(float value)
	{
		return Vector128.CreateScalar<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> CreateScalar(ushort value)
	{
		return Vector128.CreateScalar<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> CreateScalar(uint value)
	{
		return Vector128.CreateScalar<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> CreateScalar(ulong value)
	{
		return Vector128.CreateScalar<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> CreateScalarUnsafe<T>(T value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Unsafe.SkipInit<Vector128<T>>(out var value2);
		SetElementUnsafe(in value2, 0, value);
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> CreateScalarUnsafe(byte value)
	{
		return Vector128.CreateScalarUnsafe<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> CreateScalarUnsafe(double value)
	{
		return Vector128.CreateScalarUnsafe<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> CreateScalarUnsafe(short value)
	{
		return Vector128.CreateScalarUnsafe<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> CreateScalarUnsafe(int value)
	{
		return Vector128.CreateScalarUnsafe<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> CreateScalarUnsafe(long value)
	{
		return Vector128.CreateScalarUnsafe<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> CreateScalarUnsafe(nint value)
	{
		return Vector128.CreateScalarUnsafe<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> CreateScalarUnsafe(nuint value)
	{
		return Vector128.CreateScalarUnsafe<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> CreateScalarUnsafe(sbyte value)
	{
		return Vector128.CreateScalarUnsafe<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> CreateScalarUnsafe(float value)
	{
		return Vector128.CreateScalarUnsafe<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> CreateScalarUnsafe(ushort value)
	{
		return Vector128.CreateScalarUnsafe<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> CreateScalarUnsafe(uint value)
	{
		return Vector128.CreateScalarUnsafe<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> CreateScalarUnsafe(ulong value)
	{
		return Vector128.CreateScalarUnsafe<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Divide<T>(Vector128<T> left, Vector128<T> right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Divide<T>(Vector128<T> left, T right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector128<T> left, Vector128<T> right)
	{
		T left2 = Vector64.Dot(left._lower, right._lower);
		return Scalar<T>.Add(left2, Vector64.Dot(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Equals<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.Equals(left._lower, right._lower), Vector64.Equals(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAll<T>(Vector128<T> left, Vector128<T> right)
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAny<T>(Vector128<T> left, Vector128<T> right)
	{
		if (!Vector64.EqualsAny(left._lower, right._lower))
		{
			return Vector64.EqualsAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint ExtractMostSignificantBits<T>(this Vector128<T> vector)
	{
		uint num = vector._lower.ExtractMostSignificantBits();
		return num | (vector._upper.ExtractMostSignificantBits() << Vector64<T>.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> Floor(Vector128<float> vector)
	{
		return Create(Vector64.Floor(vector._lower), Vector64.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> Floor(Vector128<double> vector)
	{
		return Create(Vector64.Floor(vector._lower), Vector64.Floor(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T GetElement<T>(this Vector128<T> vector, int index)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		if ((uint)index >= (uint)Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return GetElementUnsafe(in vector, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> GetLower<T>(this Vector128<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return vector._lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> GetUpper<T>(this Vector128<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return vector._upper;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> GreaterThan<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.GreaterThan(left._lower, right._lower), Vector64.GreaterThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAll<T>(Vector128<T> left, Vector128<T> right)
	{
		if (Vector64.GreaterThanAll(left._lower, right._lower))
		{
			return Vector64.GreaterThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAny<T>(Vector128<T> left, Vector128<T> right)
	{
		if (!Vector64.GreaterThanAny(left._lower, right._lower))
		{
			return Vector64.GreaterThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> GreaterThanOrEqual<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.GreaterThanOrEqual(left._lower, right._lower), Vector64.GreaterThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right)
	{
		if (Vector64.GreaterThanOrEqualAll(left._lower, right._lower))
		{
			return Vector64.GreaterThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right)
	{
		if (!Vector64.GreaterThanOrEqualAny(left._lower, right._lower))
		{
			return Vector64.GreaterThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> LessThan<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.LessThan(left._lower, right._lower), Vector64.LessThan(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAll<T>(Vector128<T> left, Vector128<T> right)
	{
		if (Vector64.LessThanAll(left._lower, right._lower))
		{
			return Vector64.LessThanAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAny<T>(Vector128<T> left, Vector128<T> right)
	{
		if (!Vector64.LessThanAny(left._lower, right._lower))
		{
			return Vector64.LessThanAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> LessThanOrEqual<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.LessThanOrEqual(left._lower, right._lower), Vector64.LessThanOrEqual(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAll<T>(Vector128<T> left, Vector128<T> right)
	{
		if (Vector64.LessThanOrEqualAll(left._lower, right._lower))
		{
			return Vector64.LessThanOrEqualAll(left._upper, right._upper);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAny<T>(Vector128<T> left, Vector128<T> right)
	{
		if (!Vector64.LessThanOrEqualAny(left._lower, right._lower))
		{
			return Vector64.LessThanOrEqualAny(left._upper, right._upper);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<T> Load<T>(T* source)
	{
		return LoadUnsafe(ref *source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<T> LoadAligned<T>(T* source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		if ((nuint)source % (nuint)16u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		return *(Vector128<T>*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<T> LoadAlignedNonTemporal<T>(T* source)
	{
		return LoadAligned(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> LoadUnsafe<T>([In][RequiresLocation] ref T source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(ref source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<T> LoadUnsafe<T>([In][RequiresLocation] ref T source, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(ref source), (nint)elementOffset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector128<ushort> LoadUnsafe(ref char source)
	{
		return LoadUnsafe(ref Unsafe.As<char, ushort>(ref source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector128<ushort> LoadUnsafe(ref char source, nuint elementOffset)
	{
		return LoadUnsafe(ref Unsafe.As<char, ushort>(ref source), elementOffset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Max<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.Max(left._lower, right._lower), Vector64.Max(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Min<T>(Vector128<T> left, Vector128<T> right)
	{
		return Create(Vector64.Min(left._lower, right._lower), Vector64.Min(left._upper, right._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Multiply<T>(Vector128<T> left, Vector128<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Multiply<T>(Vector128<T> left, T right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Multiply<T>(T left, Vector128<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<float> Narrow(Vector128<double> lower, Vector128<double> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Narrow(Vector128<short> lower, Vector128<short> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> Narrow(Vector128<int> lower, Vector128<int> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> Narrow(Vector128<long> lower, Vector128<long> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<byte> Narrow(Vector128<ushort> lower, Vector128<ushort> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Narrow(Vector128<uint> lower, Vector128<uint> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Narrow(Vector128<ulong> lower, Vector128<ulong> upper)
	{
		return Create(Vector64.Narrow(lower._lower, lower._upper), Vector64.Narrow(upper._lower, upper._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Negate<T>(Vector128<T> vector)
	{
		return -vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> OnesComplement<T>(Vector128<T> vector)
	{
		return ~vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> ShiftLeft(Vector128<byte> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> ShiftLeft(Vector128<short> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> ShiftLeft(Vector128<int> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> ShiftLeft(Vector128<long> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> ShiftLeft(Vector128<nint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> ShiftLeft(Vector128<nuint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> ShiftLeft(Vector128<sbyte> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> ShiftLeft(Vector128<ushort> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> ShiftLeft(Vector128<uint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> ShiftLeft(Vector128<ulong> vector, int shiftCount)
	{
		return Create(Vector64.ShiftLeft(vector._lower, shiftCount), Vector64.ShiftLeft(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> ShiftRightArithmetic(Vector128<short> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightArithmetic(vector._lower, shiftCount), Vector64.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> ShiftRightArithmetic(Vector128<int> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightArithmetic(vector._lower, shiftCount), Vector64.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> ShiftRightArithmetic(Vector128<long> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightArithmetic(vector._lower, shiftCount), Vector64.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> ShiftRightArithmetic(Vector128<nint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightArithmetic(vector._lower, shiftCount), Vector64.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightArithmetic(vector._lower, shiftCount), Vector64.ShiftRightArithmetic(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<byte> ShiftRightLogical(Vector128<byte> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<short> ShiftRightLogical(Vector128<short> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> ShiftRightLogical(Vector128<int> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> ShiftRightLogical(Vector128<long> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<nint> ShiftRightLogical(Vector128<nint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<nuint> ShiftRightLogical(Vector128<nuint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> ShiftRightLogical(Vector128<uint> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> vector, int shiftCount)
	{
		return Create(Vector64.ShiftRightLogical(vector._lower, shiftCount), Vector64.ShiftRightLogical(vector._upper, shiftCount));
	}

	[Intrinsic]
	public static Vector128<byte> Shuffle(Vector128<byte> vector, Vector128<byte> indices)
	{
		Unsafe.SkipInit<Vector128<byte>>(out var value);
		for (int i = 0; i < Vector128<byte>.Count; i++)
		{
			byte elementUnsafe = GetElementUnsafe(in indices, i);
			byte value2 = 0;
			if (elementUnsafe < Vector128<byte>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Shuffle(Vector128<sbyte> vector, Vector128<sbyte> indices)
	{
		Unsafe.SkipInit<Vector128<sbyte>>(out var value);
		for (int i = 0; i < Vector128<sbyte>.Count; i++)
		{
			byte b = (byte)GetElementUnsafe(in indices, i);
			sbyte value2 = 0;
			if (b < Vector128<sbyte>.Count)
			{
				value2 = GetElementUnsafe(in vector, b);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static Vector128<byte> ShuffleUnsafe(Vector128<byte> vector, Vector128<byte> indices)
	{
		if (Ssse3.IsSupported)
		{
			return Ssse3.Shuffle(vector, indices);
		}
		if (false)
		{
		}
		if (PackedSimd.IsSupported)
		{
			return PackedSimd.Swizzle(vector, indices);
		}
		return Shuffle(vector, indices);
	}

	[Intrinsic]
	public static Vector128<short> Shuffle(Vector128<short> vector, Vector128<short> indices)
	{
		Unsafe.SkipInit<Vector128<short>>(out var value);
		for (int i = 0; i < Vector128<short>.Count; i++)
		{
			ushort num = (ushort)GetElementUnsafe(in indices, i);
			short value2 = 0;
			if (num < Vector128<short>.Count)
			{
				value2 = GetElementUnsafe(in vector, num);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Shuffle(Vector128<ushort> vector, Vector128<ushort> indices)
	{
		Unsafe.SkipInit<Vector128<ushort>>(out var value);
		for (int i = 0; i < Vector128<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in indices, i);
			ushort value2 = 0;
			if (elementUnsafe < Vector128<ushort>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector128<int> Shuffle(Vector128<int> vector, Vector128<int> indices)
	{
		Unsafe.SkipInit<Vector128<int>>(out var value);
		for (int i = 0; i < Vector128<int>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			int value2 = 0;
			if (elementUnsafe < Vector128<int>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Shuffle(Vector128<uint> vector, Vector128<uint> indices)
	{
		Unsafe.SkipInit<Vector128<uint>>(out var value);
		for (int i = 0; i < Vector128<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in indices, i);
			uint value2 = 0u;
			if (elementUnsafe < Vector128<uint>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector128<float> Shuffle(Vector128<float> vector, Vector128<int> indices)
	{
		Unsafe.SkipInit<Vector128<float>>(out var value);
		for (int i = 0; i < Vector128<float>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			float value2 = 0f;
			if (elementUnsafe < Vector128<float>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector128<long> Shuffle(Vector128<long> vector, Vector128<long> indices)
	{
		Unsafe.SkipInit<Vector128<long>>(out var value);
		for (int i = 0; i < Vector128<long>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			long value2 = 0L;
			if (elementUnsafe < (uint)Vector128<long>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> Shuffle(Vector128<ulong> vector, Vector128<ulong> indices)
	{
		Unsafe.SkipInit<Vector128<ulong>>(out var value);
		for (int i = 0; i < Vector128<ulong>.Count; i++)
		{
			ulong elementUnsafe = GetElementUnsafe(in indices, i);
			ulong value2 = 0uL;
			if (elementUnsafe < (uint)Vector128<ulong>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector128<double> Shuffle(Vector128<double> vector, Vector128<long> indices)
	{
		Unsafe.SkipInit<Vector128<double>>(out var value);
		for (int i = 0; i < Vector128<double>.Count; i++)
		{
			ulong elementUnsafe = (ulong)GetElementUnsafe(in indices, i);
			double value2 = 0.0;
			if (elementUnsafe < (uint)Vector128<double>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector128<T> Sqrt<T>(Vector128<T> vector)
	{
		return Create(Vector64.Sqrt(vector._lower), Vector64.Sqrt(vector._upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void Store<T>(this Vector128<T> source, T* destination)
	{
		source.StoreUnsafe(ref *destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAligned<T>(this Vector128<T> source, T* destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		if ((nuint)destination % (nuint)16u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		*(Vector128<T>*)destination = source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAlignedNonTemporal<T>(this Vector128<T> source, T* destination)
	{
		source.StoreAligned(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StoreLowerUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset = 0u)
	{
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref destination, elementOffset)), source.AsDouble().ToScalar());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static void StoreUnsafe<T>(this Vector128<T> source, ref T destination, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Subtract<T>(Vector128<T> left, Vector128<T> right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Sum<T>(Vector128<T> vector)
	{
		T val = default(T);
		for (int i = 0; i < Vector128<T>.Count; i++)
		{
			val = Scalar<T>.Add(val, GetElementUnsafe(in vector, i));
		}
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T ToScalar<T>(this Vector128<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return GetElementUnsafe(in vector, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> ToVector256<T>(this Vector128<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Vector256<T> vector2 = default(Vector256<T>);
		vector2.SetLowerUnsafe<T>(vector);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<T> ToVector256Unsafe<T>(this Vector128<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Unsafe.SkipInit<Vector256<T>>(out var value);
		value.SetLowerUnsafe<T>(vector);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCopyTo<T>(this Vector128<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector128<T>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector128<ushort> Lower, Vector128<ushort> Upper) Widen(Vector128<byte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector128<int> Lower, Vector128<int> Upper) Widen(Vector128<short> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector128<long> Lower, Vector128<long> Upper) Widen(Vector128<int> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector128<short> Lower, Vector128<short> Upper) Widen(Vector128<sbyte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector128<double> Lower, Vector128<double> Upper) Widen(Vector128<float> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector128<uint> Lower, Vector128<uint> Upper) Widen(Vector128<ushort> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector128<ulong> Lower, Vector128<ulong> Upper) Widen(Vector128<uint> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> WidenLower(Vector128<byte> source)
	{
		Vector64<byte> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> WidenLower(Vector128<short> source)
	{
		Vector64<short> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> WidenLower(Vector128<int> source)
	{
		Vector64<int> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<short> WidenLower(Vector128<sbyte> source)
	{
		Vector64<sbyte> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> WidenLower(Vector128<float> source)
	{
		Vector64<float> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> WidenLower(Vector128<ushort> source)
	{
		Vector64<ushort> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> WidenLower(Vector128<uint> source)
	{
		Vector64<uint> lower = source._lower;
		return Create(Vector64.WidenLower(lower), Vector64.WidenUpper(lower));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> WidenUpper(Vector128<byte> source)
	{
		Vector64<byte> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<int> WidenUpper(Vector128<short> source)
	{
		Vector64<short> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<long> WidenUpper(Vector128<int> source)
	{
		Vector64<int> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<short> WidenUpper(Vector128<sbyte> source)
	{
		Vector64<sbyte> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<double> WidenUpper(Vector128<float> source)
	{
		Vector64<float> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> WidenUpper(Vector128<ushort> source)
	{
		Vector64<ushort> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> WidenUpper(Vector128<uint> source)
	{
		Vector64<uint> upper = source._upper;
		return Create(Vector64.WidenLower(upper), Vector64.WidenUpper(upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> WithElement<T>(this Vector128<T> vector, int index, T value)
	{
		if ((uint)index >= (uint)Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector128<T> vector2 = vector;
		SetElementUnsafe(in vector2, index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> WithLower<T>(this Vector128<T> vector, Vector64<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Vector128<T> vector2 = vector;
		vector2.SetLowerUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> WithUpper<T>(this Vector128<T> vector, Vector64<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Vector128<T> vector2 = vector;
		vector2.SetUpperUnsafe<T>(value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> Xor<T>(Vector128<T> left, Vector128<T> right)
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetElementUnsafe<T>(this in Vector128<T> vector, int index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(ref vector)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetElementUnsafe<T>(this in Vector128<T> vector, int index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(ref vector)), index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetLowerUnsafe<T>(this in Vector128<T> vector, Vector64<T> value)
	{
		Unsafe.AsRef(ref vector._lower) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetUpperUnsafe<T>(this in Vector128<T> vector, Vector64<T> value)
	{
		Unsafe.AsRef(ref vector._upper) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	internal static Vector128<byte> UnpackLow(Vector128<byte> left, Vector128<byte> right)
	{
		if (Sse2.IsSupported)
		{
			return Sse2.UnpackLow(left, right);
		}
		_ = 0;
		ThrowHelper.ThrowNotSupportedException();
		return AdvSimd.Arm64.ZipLow(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	internal static Vector128<byte> UnpackHigh(Vector128<byte> left, Vector128<byte> right)
	{
		if (Sse2.IsSupported)
		{
			return Sse2.UnpackHigh(left, right);
		}
		_ = 0;
		ThrowHelper.ThrowNotSupportedException();
		return AdvSimd.Arm64.ZipHigh(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	internal static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right)
	{
		if (Sse2.IsSupported)
		{
			return Sse2.AddSaturate(left, right);
		}
		_ = 0;
		ThrowHelper.ThrowNotSupportedException();
		return AdvSimd.AddSaturate(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	internal static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right)
	{
		if (Sse2.IsSupported)
		{
			return Sse2.SubtractSaturate(left, right);
		}
		_ = 0;
		ThrowHelper.ThrowNotSupportedException();
		return AdvSimd.SubtractSaturate(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	internal static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right)
	{
		if (Sse2.IsSupported)
		{
			return Sse2.AddSaturate(left, right);
		}
		_ = 0;
		ThrowHelper.ThrowNotSupportedException();
		return AdvSimd.AddSaturate(left, right);
	}
}
[StructLayout(LayoutKind.Sequential, Size = 16)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector128DebugView<>))]
public readonly struct Vector128<T> : IEquatable<Vector128<T>>
{
	internal readonly Vector64<T> _lower;

	internal readonly Vector64<T> _upper;

	public static Vector128<T> AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector64<T> allBitsSet = Vector64<T>.AllBitsSet;
			return Vector128.Create(allBitsSet, allBitsSet);
		}
	}

	public static int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			return Vector64<T>.Count * 2;
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

	public static Vector128<T> One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			Vector64<T> one = Vector64<T>.One;
			return Vector128.Create(one, one);
		}
	}

	public static Vector128<T> Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
			return default(Vector128<T>);
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
	public static Vector128<T> operator +(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower + right._lower, left._upper + right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator &(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower & right._lower, left._upper & right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator |(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower | right._lower, left._upper | right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator /(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower / right._lower, left._upper / right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator /(Vector128<T> left, T right)
	{
		return Vector128.Create(left._lower / right, left._upper / right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector128<T> left, Vector128<T> right)
	{
		if (left._lower == right._lower)
		{
			return left._upper == right._upper;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator ^(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower ^ right._lower, left._upper ^ right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector128<T> left, Vector128<T> right)
	{
		if (!(left._lower != right._lower))
		{
			return left._upper != right._upper;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator <<(Vector128<T> value, int shiftCount)
	{
		return Vector128.Create(value._lower << shiftCount, value._upper << shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator *(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower * right._lower, left._upper * right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator *(Vector128<T> left, T right)
	{
		return Vector128.Create(left._lower * right, left._upper * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator *(T left, Vector128<T> right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator ~(Vector128<T> vector)
	{
		return Vector128.Create(~vector._lower, ~vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator >>(Vector128<T> value, int shiftCount)
	{
		return Vector128.Create(value._lower >> shiftCount, value._upper >> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator -(Vector128<T> left, Vector128<T> right)
	{
		return Vector128.Create(left._lower - right._lower, left._upper - right._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator -(Vector128<T> vector)
	{
		return Vector128.Create(-vector._lower, -vector._upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator +(Vector128<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> operator >>>(Vector128<T> value, int shiftCount)
	{
		return Vector128.Create(value._lower >>> shiftCount, value._upper >>> shiftCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector128<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static bool EqualsFloatingPoint(Vector128<T> lhs, Vector128<T> rhs)
	{
		Vector128<T> vector = Vector128.Equals(lhs, rhs) | ~(Vector128.Equals(lhs, lhs) | Vector128.Equals(rhs, rhs));
		return vector.AsInt32() == Vector128<int>.AllBitsSet;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector128<T> other)
	{
		if (Vector128.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
			{
				return EqualsFloatingPoint(this, other);
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
			T elementUnsafe = Vector128.GetElementUnsafe(in this, i);
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
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		valueStringBuilder.Append('<');
		valueStringBuilder.Append(((IFormattable)(object)Vector128.GetElementUnsafe(in this, 0)).ToString(format, formatProvider));
		for (int i = 1; i < Count; i++)
		{
			valueStringBuilder.Append(numberGroupSeparator);
			valueStringBuilder.Append(' ');
			valueStringBuilder.Append(((IFormattable)(object)Vector128.GetElementUnsafe(in this, i)).ToString(format, formatProvider));
		}
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}
}
