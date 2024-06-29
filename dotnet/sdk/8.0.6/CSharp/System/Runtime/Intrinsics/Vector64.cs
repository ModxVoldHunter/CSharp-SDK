using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Intrinsics;

public static class Vector64
{
	public static bool IsHardwareAccelerated
	{
		[Intrinsic]
		get
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Abs<T>(Vector64<T> vector)
	{
		if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong) || typeof(T) == typeof(nuint))
		{
			return vector;
		}
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = Scalar<T>.Abs(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Add<T>(Vector64<T> left, Vector64<T> right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> AndNot<T>(Vector64<T> left, Vector64<T> right)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = left._00 & ~right._00;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<TTo> As<TFrom, TTo>(this Vector64<TFrom> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TTo>();
		return Unsafe.As<Vector64<TFrom>, Vector64<TTo>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> AsByte<T>(this Vector64<T> vector)
	{
		return vector.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> AsDouble<T>(this Vector64<T> vector)
	{
		return vector.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> AsInt16<T>(this Vector64<T> vector)
	{
		return vector.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> AsInt32<T>(this Vector64<T> vector)
	{
		return vector.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> AsInt64<T>(this Vector64<T> vector)
	{
		return vector.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> AsNInt<T>(this Vector64<T> vector)
	{
		return vector.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> AsNUInt<T>(this Vector64<T> vector)
	{
		return vector.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> AsSByte<T>(this Vector64<T> vector)
	{
		return vector.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> AsSingle<T>(this Vector64<T> vector)
	{
		return vector.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> AsUInt16<T>(this Vector64<T> vector)
	{
		return vector.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector)
	{
		return vector.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector)
	{
		return vector.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> BitwiseAnd<T>(Vector64<T> left, Vector64<T> right)
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> BitwiseOr<T>(Vector64<T> left, Vector64<T> right)
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> Ceiling(Vector64<float> vector)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<float>.Count; i++)
		{
			float value2 = Scalar<float>.Ceiling(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> Ceiling(Vector64<double> vector)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			double value2 = Scalar<double>.Ceiling(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> ConditionalSelect<T>(Vector64<T> condition, Vector64<T> left, Vector64<T> right)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = (left._00 & condition._00) | (right._00 & ~condition._00);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> ConvertToDouble(Vector64<long> vector)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			double value2 = GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<double> ConvertToDouble(Vector64<ulong> vector)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			double value2 = GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> ConvertToInt32(Vector64<float> vector)
	{
		Unsafe.SkipInit<Vector64<int>>(out var value);
		for (int i = 0; i < Vector64<int>.Count; i++)
		{
			int value2 = (int)GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> ConvertToInt64(Vector64<double> vector)
	{
		Unsafe.SkipInit<Vector64<long>>(out var value);
		for (int i = 0; i < Vector64<long>.Count; i++)
		{
			long value2 = (long)GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> ConvertToSingle(Vector64<int> vector)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<float>.Count; i++)
		{
			float value2 = GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<float> ConvertToSingle(Vector64<uint> vector)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<float>.Count; i++)
		{
			float value2 = GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> ConvertToUInt32(Vector64<float> vector)
	{
		Unsafe.SkipInit<Vector64<uint>>(out var value);
		for (int i = 0; i < Vector64<uint>.Count; i++)
		{
			uint value2 = (uint)GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> ConvertToUInt64(Vector64<double> vector)
	{
		Unsafe.SkipInit<Vector64<ulong>>(out var value);
		for (int i = 0; i < Vector64<ulong>.Count; i++)
		{
			ulong value2 = (ulong)GetElementUnsafe(in vector, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector64<T> vector, T[] destination)
	{
		if (destination.Length < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector64<T> vector, T[] destination, int startIndex)
	{
		if ((uint)startIndex >= (uint)destination.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (destination.Length - startIndex < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this Vector64<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Create<T>(T value)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value2);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			SetElementUnsafe(in value2, i, value);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> Create(byte value)
	{
		return Vector64.Create<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> Create(double value)
	{
		return Vector64.Create<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> Create(short value)
	{
		return Vector64.Create<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> Create(int value)
	{
		return Vector64.Create<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> Create(long value)
	{
		return Vector64.Create<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> Create(nint value)
	{
		return Vector64.Create<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> Create(nuint value)
	{
		return Vector64.Create<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> Create(sbyte value)
	{
		return Vector64.Create<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> Create(float value)
	{
		return Vector64.Create<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> Create(ushort value)
	{
		return Vector64.Create<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> Create(uint value)
	{
		return Vector64.Create<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> Create(ulong value)
	{
		return Vector64.Create<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector64<T> Create<T>(T[] values)
	{
		if (values.Length < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref values[0]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector64<T> Create<T>(T[] values, int index)
	{
		if (index < 0 || values.Length - index < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector64<T> Create<T>(ReadOnlySpan<T> values)
	{
		if (values.Length < Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public unsafe static Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
	{
		byte* source = stackalloc byte[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
		return Unsafe.AsRef<Vector64<byte>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public unsafe static Vector64<short> Create(short e0, short e1, short e2, short e3)
	{
		short* source = stackalloc short[4] { e0, e1, e2, e3 };
		return Unsafe.AsRef<Vector64<short>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public unsafe static Vector64<int> Create(int e0, int e1)
	{
		int* source = stackalloc int[2] { e0, e1 };
		return Unsafe.AsRef<Vector64<int>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
	{
		sbyte* source = stackalloc sbyte[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
		return Unsafe.AsRef<Vector64<sbyte>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public unsafe static Vector64<float> Create(float e0, float e1)
	{
		float* source = stackalloc float[2] { e0, e1 };
		return Unsafe.AsRef<Vector64<float>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
	{
		ushort* source = stackalloc ushort[4] { e0, e1, e2, e3 };
		return Unsafe.AsRef<Vector64<ushort>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<uint> Create(uint e0, uint e1)
	{
		uint* source = stackalloc uint[2] { e0, e1 };
		return Unsafe.AsRef<Vector64<uint>>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> CreateScalar<T>(T value)
	{
		Vector64<T> vector = Vector64<T>.Zero;
		SetElementUnsafe(in vector, 0, value);
		return vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> CreateScalar(byte value)
	{
		return Vector64.CreateScalar<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> CreateScalar(double value)
	{
		return Vector64.CreateScalar<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> CreateScalar(short value)
	{
		return Vector64.CreateScalar<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> CreateScalar(int value)
	{
		return Vector64.CreateScalar<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> CreateScalar(long value)
	{
		return Vector64.CreateScalar<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> CreateScalar(nint value)
	{
		return Vector64.CreateScalar<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> CreateScalar(nuint value)
	{
		return Vector64.CreateScalar<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> CreateScalar(sbyte value)
	{
		return Vector64.CreateScalar<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> CreateScalar(float value)
	{
		return Vector64.CreateScalar<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> CreateScalar(ushort value)
	{
		return Vector64.CreateScalar<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> CreateScalar(uint value)
	{
		return Vector64.CreateScalar<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> CreateScalar(ulong value)
	{
		return Vector64.CreateScalar<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> CreateScalarUnsafe<T>(T value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value2);
		SetElementUnsafe(in value2, 0, value);
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> CreateScalarUnsafe(byte value)
	{
		return Vector64.CreateScalarUnsafe<byte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> CreateScalarUnsafe(double value)
	{
		return Vector64.CreateScalarUnsafe<double>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> CreateScalarUnsafe(short value)
	{
		return Vector64.CreateScalarUnsafe<short>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> CreateScalarUnsafe(int value)
	{
		return Vector64.CreateScalarUnsafe<int>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> CreateScalarUnsafe(long value)
	{
		return Vector64.CreateScalarUnsafe<long>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> CreateScalarUnsafe(nint value)
	{
		return Vector64.CreateScalarUnsafe<nint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> CreateScalarUnsafe(nuint value)
	{
		return Vector64.CreateScalarUnsafe<nuint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> CreateScalarUnsafe(sbyte value)
	{
		return Vector64.CreateScalarUnsafe<sbyte>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> CreateScalarUnsafe(float value)
	{
		return Vector64.CreateScalarUnsafe<float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> CreateScalarUnsafe(ushort value)
	{
		return Vector64.CreateScalarUnsafe<ushort>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> CreateScalarUnsafe(uint value)
	{
		return Vector64.CreateScalarUnsafe<uint>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> CreateScalarUnsafe(ulong value)
	{
		return Vector64.CreateScalarUnsafe<ulong>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Divide<T>(Vector64<T> left, Vector64<T> right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Divide<T>(Vector64<T> left, T right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector64<T> left, Vector64<T> right)
	{
		T val = default(T);
		if (Vector64<T>.Count != 1)
		{
			for (int i = 0; i < Vector64<T>.Count; i += 2)
			{
				T right2 = Scalar<T>.Add(Scalar<T>.Multiply(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)), Scalar<T>.Multiply(GetElementUnsafe(in left, i + 1), GetElementUnsafe(in right, i + 1)));
				val = Scalar<T>.Add(val, right2);
			}
		}
		else
		{
			val = Scalar<T>.Multiply(GetElementUnsafe(in left, 0), GetElementUnsafe(in right, 0));
		}
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Equals<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.Equals(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAll<T>(Vector64<T> left, Vector64<T> right)
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAny<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (Scalar<T>.Equals(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint ExtractMostSignificantBits<T>(this Vector64<T> vector)
	{
		uint num = 0u;
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			uint num2 = Scalar<T>.ExtractMostSignificantBit(GetElementUnsafe(in vector, i));
			num |= num2 << i;
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> Floor(Vector64<float> vector)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<float>.Count; i++)
		{
			float value2 = Scalar<float>.Floor(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> Floor(Vector64<double> vector)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			double value2 = Scalar<double>.Floor(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T GetElement<T>(this Vector64<T> vector, int index)
	{
		if ((uint)index >= (uint)Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return GetElementUnsafe(in vector, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> GreaterThan<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAll<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (!Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAny<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> GreaterThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (!Scalar<T>.GreaterThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (Scalar<T>.GreaterThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> LessThan<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAll<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (!Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAny<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> LessThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (!Scalar<T>.LessThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			if (Scalar<T>.LessThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<T> Load<T>(T* source)
	{
		return LoadUnsafe(ref *source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<T> LoadAligned<T>(T* source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		if ((nuint)source % (nuint)8u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		return *(Vector64<T>*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector64<T> LoadAlignedNonTemporal<T>(T* source)
	{
		return LoadAligned(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> LoadUnsafe<T>([In][RequiresLocation] ref T source)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(ref source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<T> LoadUnsafe<T>([In][RequiresLocation] ref T source, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(ref source), (nint)elementOffset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Max<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? GetElementUnsafe(in left, i) : GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Min<T>(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? GetElementUnsafe(in left, i) : GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Multiply<T>(Vector64<T> left, Vector64<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Multiply<T>(Vector64<T> left, T right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Multiply<T>(T left, Vector64<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<float> Narrow(Vector64<double> lower, Vector64<double> upper)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			float value2 = (float)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<double>.Count; j < Vector64<float>.Count; j++)
		{
			float value3 = (float)GetElementUnsafe(in upper, j - Vector64<double>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> Narrow(Vector64<short> lower, Vector64<short> upper)
	{
		Unsafe.SkipInit<Vector64<sbyte>>(out var value);
		for (int i = 0; i < Vector64<short>.Count; i++)
		{
			sbyte value2 = (sbyte)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<short>.Count; j < Vector64<sbyte>.Count; j++)
		{
			sbyte value3 = (sbyte)GetElementUnsafe(in upper, j - Vector64<short>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> Narrow(Vector64<int> lower, Vector64<int> upper)
	{
		Unsafe.SkipInit<Vector64<short>>(out var value);
		for (int i = 0; i < Vector64<int>.Count; i++)
		{
			short value2 = (short)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<int>.Count; j < Vector64<short>.Count; j++)
		{
			short value3 = (short)GetElementUnsafe(in upper, j - Vector64<int>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> Narrow(Vector64<long> lower, Vector64<long> upper)
	{
		Unsafe.SkipInit<Vector64<int>>(out var value);
		for (int i = 0; i < Vector64<long>.Count; i++)
		{
			int value2 = (int)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<long>.Count; j < Vector64<int>.Count; j++)
		{
			int value3 = (int)GetElementUnsafe(in upper, j - Vector64<long>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<byte> Narrow(Vector64<ushort> lower, Vector64<ushort> upper)
	{
		Unsafe.SkipInit<Vector64<byte>>(out var value);
		for (int i = 0; i < Vector64<ushort>.Count; i++)
		{
			byte value2 = (byte)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<ushort>.Count; j < Vector64<byte>.Count; j++)
		{
			byte value3 = (byte)GetElementUnsafe(in upper, j - Vector64<ushort>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> Narrow(Vector64<uint> lower, Vector64<uint> upper)
	{
		Unsafe.SkipInit<Vector64<ushort>>(out var value);
		for (int i = 0; i < Vector64<uint>.Count; i++)
		{
			ushort value2 = (ushort)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<uint>.Count; j < Vector64<ushort>.Count; j++)
		{
			ushort value3 = (ushort)GetElementUnsafe(in upper, j - Vector64<uint>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> Narrow(Vector64<ulong> lower, Vector64<ulong> upper)
	{
		Unsafe.SkipInit<Vector64<uint>>(out var value);
		for (int i = 0; i < Vector64<ulong>.Count; i++)
		{
			uint value2 = (uint)GetElementUnsafe(in lower, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector64<ulong>.Count; j < Vector64<uint>.Count; j++)
		{
			uint value3 = (uint)GetElementUnsafe(in upper, j - Vector64<ulong>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Negate<T>(Vector64<T> vector)
	{
		return -vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> OnesComplement<T>(Vector64<T> vector)
	{
		return ~vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> ShiftLeft(Vector64<byte> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> ShiftLeft(Vector64<short> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> ShiftLeft(Vector64<int> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> ShiftLeft(Vector64<long> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> ShiftLeft(Vector64<nint> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> ShiftLeft(Vector64<nuint> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> ShiftLeft(Vector64<sbyte> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> ShiftLeft(Vector64<ushort> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> ShiftLeft(Vector64<uint> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> ShiftLeft(Vector64<ulong> vector, int shiftCount)
	{
		return vector << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> ShiftRightArithmetic(Vector64<short> vector, int shiftCount)
	{
		return vector >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> ShiftRightArithmetic(Vector64<int> vector, int shiftCount)
	{
		return vector >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> ShiftRightArithmetic(Vector64<long> vector, int shiftCount)
	{
		return vector >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> ShiftRightArithmetic(Vector64<nint> vector, int shiftCount)
	{
		return vector >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> ShiftRightArithmetic(Vector64<sbyte> vector, int shiftCount)
	{
		return vector >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<byte> ShiftRightLogical(Vector64<byte> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<short> ShiftRightLogical(Vector64<short> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> ShiftRightLogical(Vector64<int> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> ShiftRightLogical(Vector64<long> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<nint> ShiftRightLogical(Vector64<nint> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<nuint> ShiftRightLogical(Vector64<nuint> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> ShiftRightLogical(Vector64<sbyte> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> ShiftRightLogical(Vector64<ushort> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> ShiftRightLogical(Vector64<uint> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> ShiftRightLogical(Vector64<ulong> vector, int shiftCount)
	{
		return vector >>> shiftCount;
	}

	[Intrinsic]
	public static Vector64<byte> Shuffle(Vector64<byte> vector, Vector64<byte> indices)
	{
		Unsafe.SkipInit<Vector64<byte>>(out var value);
		for (int i = 0; i < Vector64<byte>.Count; i++)
		{
			byte elementUnsafe = GetElementUnsafe(in indices, i);
			byte value2 = 0;
			if (elementUnsafe < Vector64<byte>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> Shuffle(Vector64<sbyte> vector, Vector64<sbyte> indices)
	{
		Unsafe.SkipInit<Vector64<sbyte>>(out var value);
		for (int i = 0; i < Vector64<sbyte>.Count; i++)
		{
			byte b = (byte)GetElementUnsafe(in indices, i);
			sbyte value2 = 0;
			if (b < Vector64<sbyte>.Count)
			{
				value2 = GetElementUnsafe(in vector, b);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector64<short> Shuffle(Vector64<short> vector, Vector64<short> indices)
	{
		Unsafe.SkipInit<Vector64<short>>(out var value);
		for (int i = 0; i < Vector64<short>.Count; i++)
		{
			ushort num = (ushort)GetElementUnsafe(in indices, i);
			short value2 = 0;
			if (num < Vector64<short>.Count)
			{
				value2 = GetElementUnsafe(in vector, num);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> Shuffle(Vector64<ushort> vector, Vector64<ushort> indices)
	{
		Unsafe.SkipInit<Vector64<ushort>>(out var value);
		for (int i = 0; i < Vector64<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in indices, i);
			ushort value2 = 0;
			if (elementUnsafe < Vector64<ushort>.Count)
			{
				value2 = GetElementUnsafe(in vector, elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector64<int> Shuffle(Vector64<int> vector, Vector64<int> indices)
	{
		Unsafe.SkipInit<Vector64<int>>(out var value);
		for (int i = 0; i < Vector64<int>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			int value2 = 0;
			if (elementUnsafe < Vector64<int>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> Shuffle(Vector64<uint> vector, Vector64<uint> indices)
	{
		Unsafe.SkipInit<Vector64<uint>>(out var value);
		for (int i = 0; i < Vector64<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in indices, i);
			uint value2 = 0u;
			if (elementUnsafe < Vector64<uint>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[Intrinsic]
	public static Vector64<float> Shuffle(Vector64<float> vector, Vector64<int> indices)
	{
		Unsafe.SkipInit<Vector64<float>>(out var value);
		for (int i = 0; i < Vector64<float>.Count; i++)
		{
			uint elementUnsafe = (uint)GetElementUnsafe(in indices, i);
			float value2 = 0f;
			if (elementUnsafe < Vector64<float>.Count)
			{
				value2 = GetElementUnsafe(in vector, (int)elementUnsafe);
			}
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Sqrt<T>(Vector64<T> vector)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			T value2 = Scalar<T>.Sqrt(GetElementUnsafe(in vector, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void Store<T>(this Vector64<T> source, T* destination)
	{
		source.StoreUnsafe(ref *destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAligned<T>(this Vector64<T> source, T* destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		if ((nuint)destination % (nuint)8u != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		*(Vector64<T>*)destination = source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAlignedNonTemporal<T>(this Vector64<T> source, T* destination)
	{
		source.StoreAligned(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static void StoreUnsafe<T>(this Vector64<T> source, ref T destination)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static void StoreUnsafe<T>(this Vector64<T> source, ref T destination, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Subtract<T>(Vector64<T> left, Vector64<T> right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Sum<T>(Vector64<T> vector)
	{
		T val = default(T);
		for (int i = 0; i < Vector64<T>.Count; i++)
		{
			val = Scalar<T>.Add(val, GetElementUnsafe(in vector, i));
		}
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T ToScalar<T>(this Vector64<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		return GetElementUnsafe(in vector, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> ToVector128<T>(this Vector64<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Vector128<T> vector2 = default(Vector128<T>);
		vector2.SetLowerUnsafe<T>(vector);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<T> ToVector128Unsafe<T>(this Vector64<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector128<T>>(out var value);
		value.SetLowerUnsafe<T>(vector);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCopyTo<T>(this Vector64<T> vector, Span<T> destination)
	{
		if (destination.Length < Vector64<T>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector64<ushort> Lower, Vector64<ushort> Upper) Widen(Vector64<byte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector64<int> Lower, Vector64<int> Upper) Widen(Vector64<short> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector64<long> Lower, Vector64<long> Upper) Widen(Vector64<int> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector64<short> Lower, Vector64<short> Upper) Widen(Vector64<sbyte> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Vector64<double> Lower, Vector64<double> Upper) Widen(Vector64<float> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector64<uint> Lower, Vector64<uint> Upper) Widen(Vector64<ushort> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static (Vector64<ulong> Lower, Vector64<ulong> Upper) Widen(Vector64<uint> source)
	{
		return (Lower: WidenLower(source), Upper: WidenUpper(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> WidenLower(Vector64<byte> source)
	{
		Unsafe.SkipInit<Vector64<ushort>>(out var value);
		for (int i = 0; i < Vector64<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> WidenLower(Vector64<short> source)
	{
		Unsafe.SkipInit<Vector64<int>>(out var value);
		for (int i = 0; i < Vector64<int>.Count; i++)
		{
			int elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> WidenLower(Vector64<int> source)
	{
		Unsafe.SkipInit<Vector64<long>>(out var value);
		for (int i = 0; i < Vector64<long>.Count; i++)
		{
			long value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<short> WidenLower(Vector64<sbyte> source)
	{
		Unsafe.SkipInit<Vector64<short>>(out var value);
		for (int i = 0; i < Vector64<short>.Count; i++)
		{
			short elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> WidenLower(Vector64<float> source)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = 0; i < Vector64<double>.Count; i++)
		{
			double value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> WidenLower(Vector64<ushort> source)
	{
		Unsafe.SkipInit<Vector64<uint>>(out var value);
		for (int i = 0; i < Vector64<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> WidenLower(Vector64<uint> source)
	{
		Unsafe.SkipInit<Vector64<ulong>>(out var value);
		for (int i = 0; i < Vector64<ulong>.Count; i++)
		{
			ulong value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> WidenUpper(Vector64<byte> source)
	{
		Unsafe.SkipInit<Vector64<ushort>>(out var value);
		for (int i = Vector64<ushort>.Count; i < Vector64<byte>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<ushort>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<int> WidenUpper(Vector64<short> source)
	{
		Unsafe.SkipInit<Vector64<int>>(out var value);
		for (int i = Vector64<int>.Count; i < Vector64<short>.Count; i++)
		{
			int elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<int>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<long> WidenUpper(Vector64<int> source)
	{
		Unsafe.SkipInit<Vector64<long>>(out var value);
		for (int i = Vector64<long>.Count; i < Vector64<int>.Count; i++)
		{
			long value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<long>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<short> WidenUpper(Vector64<sbyte> source)
	{
		Unsafe.SkipInit<Vector64<short>>(out var value);
		for (int i = Vector64<short>.Count; i < Vector64<sbyte>.Count; i++)
		{
			short elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<short>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<double> WidenUpper(Vector64<float> source)
	{
		Unsafe.SkipInit<Vector64<double>>(out var value);
		for (int i = Vector64<double>.Count; i < Vector64<float>.Count; i++)
		{
			double value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<double>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> WidenUpper(Vector64<ushort> source)
	{
		Unsafe.SkipInit<Vector64<uint>>(out var value);
		for (int i = Vector64<uint>.Count; i < Vector64<ushort>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<uint>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> WidenUpper(Vector64<uint> source)
	{
		Unsafe.SkipInit<Vector64<ulong>>(out var value);
		for (int i = Vector64<ulong>.Count; i < Vector64<uint>.Count; i++)
		{
			ulong value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector64<ulong>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> WithElement<T>(this Vector64<T> vector, int index, T value)
	{
		if ((uint)index >= (uint)Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector64<T> vector2 = vector;
		SetElementUnsafe(in vector2, index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> Xor<T>(Vector64<T> left, Vector64<T> right)
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetElementUnsafe<T>(this in Vector64<T> vector, int index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(ref vector)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetElementUnsafe<T>(this in Vector64<T> vector, int index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(ref vector)), index) = value;
	}
}
[StructLayout(LayoutKind.Sequential, Size = 8)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector64DebugView<>))]
public readonly struct Vector64<T> : IEquatable<Vector64<T>>
{
	internal readonly ulong _00;

	public static Vector64<T> AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
			Unsafe.SkipInit<Vector64<T>>(out var value);
			Unsafe.AsRef(ref value._00) = ulong.MaxValue;
			return value;
		}
	}

	public static int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
			return 8 / Unsafe.SizeOf<T>();
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

	public static Vector64<T> One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			T one = Scalar<T>.One;
			return Vector64.Create(one);
		}
	}

	public static Vector64<T> Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
			return default(Vector64<T>);
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
	public static Vector64<T> operator +(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Add(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator &(Vector64<T> left, Vector64<T> right)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = left._00 & right._00;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator |(Vector64<T> left, Vector64<T> right)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = left._00 | right._00;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator /(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Divide(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator /(Vector64<T> left, T right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Divide(Vector64.GetElementUnsafe(in left, i), right);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Count; i++)
		{
			if (!Scalar<T>.Equals(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator ^(Vector64<T> left, Vector64<T> right)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = left._00 ^ right._00;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector64<T> left, Vector64<T> right)
	{
		for (int i = 0; i < Count; i++)
		{
			if (!Scalar<T>.Equals(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator <<(Vector64<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftLeft(Vector64.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator *(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Multiply(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator *(Vector64<T> left, T right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Multiply(Vector64.GetElementUnsafe(in left, i), right);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator *(T left, Vector64<T> right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator ~(Vector64<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Unsafe.SkipInit<Vector64<T>>(out var value);
		Unsafe.AsRef(ref value._00) = ~vector._00;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator >>(Vector64<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftRightArithmetic(Vector64.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator -(Vector64<T> left, Vector64<T> right)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Subtract(Vector64.GetElementUnsafe(in left, i), Vector64.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator -(Vector64<T> vector)
	{
		return Zero - vector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator +(Vector64<T> value)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<T> operator >>>(Vector64<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector64<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftRightLogical(Vector64.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector64<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector64<T> other)
	{
		if (false)
		{
		}
		return SoftwareFallback(in this, other);
		static bool SoftwareFallback(in Vector64<T> self, Vector64<T> other)
		{
			for (int i = 0; i < Count; i++)
			{
				if (!Scalar<T>.ObjectEquals(Vector64.GetElementUnsafe(in self, i), Vector64.GetElementUnsafe(in other, i)))
				{
					return false;
				}
			}
			return true;
		}
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		for (int i = 0; i < Count; i++)
		{
			T elementUnsafe = Vector64.GetElementUnsafe(in this, i);
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
		ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		valueStringBuilder.Append('<');
		valueStringBuilder.Append(((IFormattable)(object)Vector64.GetElementUnsafe(in this, 0)).ToString(format, formatProvider));
		for (int i = 1; i < Count; i++)
		{
			valueStringBuilder.Append(numberGroupSeparator);
			valueStringBuilder.Append(' ');
			valueStringBuilder.Append(((IFormattable)(object)Vector64.GetElementUnsafe(in this, i)).ToString(format, formatProvider));
		}
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}
}
