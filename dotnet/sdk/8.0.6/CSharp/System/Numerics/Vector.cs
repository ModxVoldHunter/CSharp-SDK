using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System.Numerics;

[Intrinsic]
public static class Vector
{
	internal unsafe static readonly nuint Alignment = ((sizeof(Vector<byte>) == sizeof(Vector128<byte>)) ? 16u : 32u);

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
	internal static float GetElement(this Quaternion quaternion, int index)
	{
		if ((uint)index >= 4u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return quaternion.GetElementUnsafe(index);
	}

	[Intrinsic]
	internal static Quaternion WithElement(this Quaternion quaternion, int index, float value)
	{
		if ((uint)index >= 4u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Quaternion quaternion2 = quaternion;
		quaternion2.SetElementUnsafe(index, value);
		return quaternion2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetElementUnsafe(this in Quaternion quaternion, int index)
	{
		return Unsafe.Add(ref Unsafe.AsRef(ref quaternion.X), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetElementUnsafe(this ref Quaternion quaternion, int index, float value)
	{
		Unsafe.Add(ref quaternion.X, index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Abs<T>(Vector<T> value)
	{
		if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong) || typeof(T) == typeof(nuint))
		{
			return value;
		}
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value3 = Scalar<T>.Abs(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Add<T>(Vector<T> left, Vector<T> right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right)
	{
		return left & ~right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<TTo> As<TFrom, TTo>(this Vector<TFrom> vector)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TTo>();
		return Unsafe.As<Vector<TFrom>, Vector<TTo>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<byte> AsVectorByte<T>(Vector<T> value)
	{
		return value.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> AsVectorDouble<T>(Vector<T> value)
	{
		return value.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<short> AsVectorInt16<T>(Vector<T> value)
	{
		return value.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> AsVectorInt32<T>(Vector<T> value)
	{
		return value.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> AsVectorInt64<T>(Vector<T> value)
	{
		return value.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<nint> AsVectorNInt<T>(Vector<T> value)
	{
		return value.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value)
	{
		return value.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value)
	{
		return value.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> AsVectorSingle<T>(Vector<T> value)
	{
		return value.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value)
	{
		return value.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> AsVectorUInt32<T>(Vector<T> value)
	{
		return value.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value)
	{
		return value.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right)
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right)
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> Ceiling(Vector<double> value)
	{
		Unsafe.SkipInit<Vector<double>>(out var value2);
		for (int i = 0; i < Vector<double>.Count; i++)
		{
			double value3 = Scalar<double>.Ceiling(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> Ceiling(Vector<float> value)
	{
		Unsafe.SkipInit<Vector<float>>(out var value2);
		for (int i = 0; i < Vector<float>.Count; i++)
		{
			float value3 = Scalar<float>.Ceiling(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right)
	{
		return (left & condition) | (right & ~condition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right)
	{
		return ConditionalSelect(condition.As<int, float>(), left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right)
	{
		return ConditionalSelect(condition.As<long, double>(), left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> ConvertToDouble(Vector<long> value)
	{
		if (Avx2.IsSupported)
		{
			return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
		}
		return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<double> ConvertToDouble(Vector<ulong> value)
	{
		if (Avx2.IsSupported)
		{
			return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
		}
		return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
	}

	[Intrinsic]
	public static Vector<int> ConvertToInt32(Vector<float> value)
	{
		Unsafe.SkipInit<Vector<int>>(out var value2);
		for (int i = 0; i < Vector<int>.Count; i++)
		{
			int value3 = (int)GetElementUnsafe(in value, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[Intrinsic]
	public static Vector<long> ConvertToInt64(Vector<double> value)
	{
		Unsafe.SkipInit<Vector<long>>(out var value2);
		for (int i = 0; i < Vector<long>.Count; i++)
		{
			long value3 = (long)GetElementUnsafe(in value, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[Intrinsic]
	public static Vector<float> ConvertToSingle(Vector<int> value)
	{
		Unsafe.SkipInit<Vector<float>>(out var value2);
		for (int i = 0; i < Vector<float>.Count; i++)
		{
			float value3 = GetElementUnsafe(in value, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<float> ConvertToSingle(Vector<uint> value)
	{
		if (Avx2.IsSupported)
		{
			return Vector256.ConvertToSingle(value.AsVector256()).AsVector();
		}
		return Vector128.ConvertToSingle(value.AsVector128()).AsVector();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> ConvertToUInt32(Vector<float> value)
	{
		Unsafe.SkipInit<Vector<uint>>(out var value2);
		for (int i = 0; i < Vector<uint>.Count; i++)
		{
			uint value3 = (uint)GetElementUnsafe(in value, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> ConvertToUInt64(Vector<double> value)
	{
		Unsafe.SkipInit<Vector<ulong>>(out var value2);
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value3 = (ulong)GetElementUnsafe(in value, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Divide<T>(Vector<T> left, T right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector<T> left, Vector<T> right)
	{
		T val = default(T);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T right2 = Scalar<T>.Multiply(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i));
			val = Scalar<T>.Add(val, right2);
		}
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.Equals(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> Equals(Vector<double> left, Vector<double> right)
	{
		return Vector.Equals<double>(left, right).As<double, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> Equals(Vector<int> left, Vector<int> right)
	{
		return Vector.Equals<int>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> Equals(Vector<long> left, Vector<long> right)
	{
		return Vector.Equals<long>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> Equals(Vector<float> left, Vector<float> right)
	{
		return Vector.Equals<float>(left, right).As<float, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAll<T>(Vector<T> left, Vector<T> right)
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool EqualsAny<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static Vector<double> Floor(Vector<double> value)
	{
		Unsafe.SkipInit<Vector<double>>(out var value2);
		for (int i = 0; i < Vector<double>.Count; i++)
		{
			double value3 = Scalar<double>.Floor(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> Floor(Vector<float> value)
	{
		Unsafe.SkipInit<Vector<float>>(out var value2);
		for (int i = 0; i < Vector<float>.Count; i++)
		{
			float value3 = Scalar<float>.Floor(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T GetElement<T>(this Vector<T> vector, int index)
	{
		if ((uint)index >= (uint)Vector<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return GetElementUnsafe(in vector, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
	{
		return Vector.GreaterThan<double>(left, right).As<double, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
	{
		return Vector.GreaterThan<int>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
	{
		return Vector.GreaterThan<long>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
	{
		return Vector.GreaterThan<float>(left, right).As<float, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return Vector.GreaterThanOrEqual<double>(left, right).As<double, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector.GreaterThanOrEqual<int>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector.GreaterThanOrEqual<long>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return Vector.GreaterThanOrEqual<float>(left, right).As<float, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
	{
		return Vector.LessThan<double>(left, right).As<double, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
	{
		return Vector.LessThan<int>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
	{
		return Vector.LessThan<long>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
	{
		return Vector.LessThan<float>(left, right).As<float, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanAll<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static bool LessThanAny<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThanOrEqual(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? Scalar<T>.AllBitsSet : default(T));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return Vector.LessThanOrEqual<double>(left, right).As<double, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector.LessThanOrEqual<int>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector.LessThanOrEqual<long>(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return Vector.LessThanOrEqual<float>(left, right).As<float, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Vector<T>.Count; i++)
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
	public unsafe static Vector<T> Load<T>(T* source)
	{
		return LoadUnsafe(ref *source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector<T> LoadAligned<T>(T* source)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((nuint)source % Alignment != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		return *(Vector<T>*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector<T> LoadAlignedNonTemporal<T>(T* source)
	{
		return LoadAligned(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> LoadUnsafe<T>([In][RequiresLocation] ref T source)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(ref source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<T> LoadUnsafe<T>([In][RequiresLocation] ref T source, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(ref source), (nint)elementOffset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Max<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.GreaterThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? GetElementUnsafe(in left, i) : GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Min<T>(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value2 = (Scalar<T>.LessThan(GetElementUnsafe(in left, i), GetElementUnsafe(in right, i)) ? GetElementUnsafe(in left, i) : GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Multiply<T>(Vector<T> left, T right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Multiply<T>(T left, Vector<T> right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> Narrow(Vector<double> low, Vector<double> high)
	{
		Unsafe.SkipInit<Vector<float>>(out var value);
		for (int i = 0; i < Vector<double>.Count; i++)
		{
			float value2 = (float)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<double>.Count; j < Vector<float>.Count; j++)
		{
			float value3 = (float)GetElementUnsafe(in high, j - Vector<double>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
	{
		Unsafe.SkipInit<Vector<sbyte>>(out var value);
		for (int i = 0; i < Vector<short>.Count; i++)
		{
			sbyte value2 = (sbyte)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<short>.Count; j < Vector<sbyte>.Count; j++)
		{
			sbyte value3 = (sbyte)GetElementUnsafe(in high, j - Vector<short>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<short> Narrow(Vector<int> low, Vector<int> high)
	{
		Unsafe.SkipInit<Vector<short>>(out var value);
		for (int i = 0; i < Vector<int>.Count; i++)
		{
			short value2 = (short)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<int>.Count; j < Vector<short>.Count; j++)
		{
			short value3 = (short)GetElementUnsafe(in high, j - Vector<int>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> Narrow(Vector<long> low, Vector<long> high)
	{
		Unsafe.SkipInit<Vector<int>>(out var value);
		for (int i = 0; i < Vector<long>.Count; i++)
		{
			int value2 = (int)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<long>.Count; j < Vector<int>.Count; j++)
		{
			int value3 = (int)GetElementUnsafe(in high, j - Vector<long>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
	{
		Unsafe.SkipInit<Vector<byte>>(out var value);
		for (int i = 0; i < Vector<ushort>.Count; i++)
		{
			byte value2 = (byte)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<ushort>.Count; j < Vector<byte>.Count; j++)
		{
			byte value3 = (byte)GetElementUnsafe(in high, j - Vector<ushort>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
	{
		Unsafe.SkipInit<Vector<ushort>>(out var value);
		for (int i = 0; i < Vector<uint>.Count; i++)
		{
			ushort value2 = (ushort)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<uint>.Count; j < Vector<ushort>.Count; j++)
		{
			ushort value3 = (ushort)GetElementUnsafe(in high, j - Vector<uint>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
	{
		Unsafe.SkipInit<Vector<uint>>(out var value);
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			uint value2 = (uint)GetElementUnsafe(in low, i);
			value.SetElementUnsafe(i, value2);
		}
		for (int j = Vector<ulong>.Count; j < Vector<uint>.Count; j++)
		{
			uint value3 = (uint)GetElementUnsafe(in high, j - Vector<ulong>.Count);
			value.SetElementUnsafe(j, value3);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Negate<T>(Vector<T> value)
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> OnesComplement<T>(Vector<T> value)
	{
		return ~value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<byte> ShiftLeft(Vector<byte> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<short> ShiftLeft(Vector<short> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> ShiftLeft(Vector<int> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> ShiftLeft(Vector<long> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<nint> ShiftLeft(Vector<nint> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<nuint> ShiftLeft(Vector<nuint> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<sbyte> ShiftLeft(Vector<sbyte> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> ShiftLeft(Vector<ushort> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> ShiftLeft(Vector<uint> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> ShiftLeft(Vector<ulong> value, int shiftCount)
	{
		return value << shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<short> ShiftRightArithmetic(Vector<short> value, int shiftCount)
	{
		return value >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> ShiftRightArithmetic(Vector<int> value, int shiftCount)
	{
		return value >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> ShiftRightArithmetic(Vector<long> value, int shiftCount)
	{
		return value >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<nint> ShiftRightArithmetic(Vector<nint> value, int shiftCount)
	{
		return value >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> value, int shiftCount)
	{
		return value >> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<byte> ShiftRightLogical(Vector<byte> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<short> ShiftRightLogical(Vector<short> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> ShiftRightLogical(Vector<int> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> ShiftRightLogical(Vector<long> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<nint> ShiftRightLogical(Vector<nint> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<nuint> ShiftRightLogical(Vector<nuint> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<sbyte> ShiftRightLogical(Vector<sbyte> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> ShiftRightLogical(Vector<ushort> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> ShiftRightLogical(Vector<uint> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> ShiftRightLogical(Vector<ulong> value, int shiftCount)
	{
		return value >>> shiftCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> SquareRoot<T>(Vector<T> value)
	{
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			T value3 = Scalar<T>.Sqrt(GetElementUnsafe(in value, i));
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void Store<T>(this Vector<T> source, T* destination)
	{
		source.StoreUnsafe(ref *destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAligned<T>(this Vector<T> source, T* destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((nuint)destination % Alignment != 0)
		{
			ThrowHelper.ThrowAccessViolationException();
		}
		*(Vector<T>*)destination = source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static void StoreAlignedNonTemporal<T>(this Vector<T> source, T* destination)
	{
		source.StoreAligned(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static void StoreUnsafe<T>(this Vector<T> source, ref T destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static void StoreUnsafe<T>(this Vector<T> source, ref T destination, nuint elementOffset)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Sum<T>(Vector<T> value)
	{
		T val = default(T);
		for (int i = 0; i < Vector<T>.Count; i++)
		{
			val = Scalar<T>.Add(val, GetElementUnsafe(in value, i));
		}
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T ToScalar<T>(this Vector<T> vector)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return GetElementUnsafe(in vector, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
	{
		low = WidenLower(source);
		high = WidenUpper(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> WidenLower(Vector<byte> source)
	{
		Unsafe.SkipInit<Vector<ushort>>(out var value);
		for (int i = 0; i < Vector<ushort>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> WidenLower(Vector<short> source)
	{
		Unsafe.SkipInit<Vector<int>>(out var value);
		for (int i = 0; i < Vector<int>.Count; i++)
		{
			int elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> WidenLower(Vector<int> source)
	{
		Unsafe.SkipInit<Vector<long>>(out var value);
		for (int i = 0; i < Vector<long>.Count; i++)
		{
			long value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<short> WidenLower(Vector<sbyte> source)
	{
		Unsafe.SkipInit<Vector<short>>(out var value);
		for (int i = 0; i < Vector<short>.Count; i++)
		{
			short elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> WidenLower(Vector<float> source)
	{
		Unsafe.SkipInit<Vector<double>>(out var value);
		for (int i = 0; i < Vector<double>.Count; i++)
		{
			double value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> WidenLower(Vector<ushort> source)
	{
		Unsafe.SkipInit<Vector<uint>>(out var value);
		for (int i = 0; i < Vector<uint>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> WidenLower(Vector<uint> source)
	{
		Unsafe.SkipInit<Vector<ulong>>(out var value);
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ushort> WidenUpper(Vector<byte> source)
	{
		Unsafe.SkipInit<Vector<ushort>>(out var value);
		for (int i = Vector<ushort>.Count; i < Vector<byte>.Count; i++)
		{
			ushort elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<ushort>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> WidenUpper(Vector<short> source)
	{
		Unsafe.SkipInit<Vector<int>>(out var value);
		for (int i = Vector<int>.Count; i < Vector<short>.Count; i++)
		{
			int elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<int>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> WidenUpper(Vector<int> source)
	{
		Unsafe.SkipInit<Vector<long>>(out var value);
		for (int i = Vector<long>.Count; i < Vector<int>.Count; i++)
		{
			long value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<long>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<short> WidenUpper(Vector<sbyte> source)
	{
		Unsafe.SkipInit<Vector<short>>(out var value);
		for (int i = Vector<short>.Count; i < Vector<sbyte>.Count; i++)
		{
			short elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<short>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> WidenUpper(Vector<float> source)
	{
		Unsafe.SkipInit<Vector<double>>(out var value);
		for (int i = Vector<double>.Count; i < Vector<float>.Count; i++)
		{
			double value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<double>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<uint> WidenUpper(Vector<ushort> source)
	{
		Unsafe.SkipInit<Vector<uint>>(out var value);
		for (int i = Vector<uint>.Count; i < Vector<ushort>.Count; i++)
		{
			uint elementUnsafe = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<uint>.Count, elementUnsafe);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector<ulong> WidenUpper(Vector<uint> source)
	{
		Unsafe.SkipInit<Vector<ulong>>(out var value);
		for (int i = Vector<ulong>.Count; i < Vector<uint>.Count; i++)
		{
			ulong value2 = GetElementUnsafe(in source, i);
			value.SetElementUnsafe(i - Vector<ulong>.Count, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> WithElement<T>(this Vector<T> vector, int index, T value)
	{
		if ((uint)index >= (uint)Vector<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector<T> vector2 = vector;
		SetElementUnsafe(in vector2, index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right)
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetElementUnsafe<T>(this in Vector<T> vector, int index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(ref vector)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetElementUnsafe<T>(this in Vector<T> vector, int index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(ref vector)), index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static float GetElement(this Vector2 vector, int index)
	{
		if ((uint)index >= 2u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return vector.GetElementUnsafe(index);
	}

	[Intrinsic]
	internal static Vector2 WithElement(this Vector2 vector, int index, float value)
	{
		if ((uint)index >= 2u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector2 vector2 = vector;
		vector2.SetElementUnsafe(index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetElementUnsafe(this in Vector2 vector, int index)
	{
		return Unsafe.Add(ref Unsafe.AsRef(ref vector.X), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetElementUnsafe(this ref Vector2 vector, int index, float value)
	{
		Unsafe.Add(ref vector.X, index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static float GetElement(this Vector3 vector, int index)
	{
		if ((uint)index >= 3u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return vector.GetElementUnsafe(index);
	}

	[Intrinsic]
	internal static Vector3 WithElement(this Vector3 vector, int index, float value)
	{
		if ((uint)index >= 3u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector3 vector2 = vector;
		vector2.SetElementUnsafe(index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetElementUnsafe(this in Vector3 vector, int index)
	{
		return Unsafe.Add(ref Unsafe.AsRef(ref vector.X), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetElementUnsafe(this ref Vector3 vector, int index, float value)
	{
		Unsafe.Add(ref vector.X, index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static float GetElement(this Vector4 vector, int index)
	{
		if ((uint)index >= 4u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return vector.GetElementUnsafe(index);
	}

	[Intrinsic]
	internal static Vector4 WithElement(this Vector4 vector, int index, float value)
	{
		if ((uint)index >= 4u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector4 vector2 = vector;
		vector2.SetElementUnsafe(index, value);
		return vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetElementUnsafe(this in Vector4 vector, int index)
	{
		return Unsafe.Add(ref Unsafe.AsRef(ref vector.X), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetElementUnsafe(this ref Vector4 vector, int index, float value)
	{
		Unsafe.Add(ref vector.X, index) = value;
	}
}
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(VectorDebugView<>))]
public readonly struct Vector<T> : IEquatable<Vector<T>>, IFormattable
{
	internal readonly ulong _00;

	internal readonly ulong _01;

	public static Vector<T> AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			T allBitsSet = Scalar<T>.AllBitsSet;
			return new Vector<T>(allBitsSet);
		}
	}

	public unsafe static int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return sizeof(Vector<T>) / Unsafe.SizeOf<T>();
		}
	}

	public static bool IsSupported
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!(typeof(T) == typeof(byte)) && !(typeof(T) == typeof(double)) && !(typeof(T) == typeof(short)) && !(typeof(T) == typeof(int)) && !(typeof(T) == typeof(long)) && !(typeof(T) == typeof(nint)) && !(typeof(T) == typeof(nuint)) && !(typeof(T) == typeof(sbyte)) && !(typeof(T) == typeof(float)) && !(typeof(T) == typeof(ushort)) && !(typeof(T) == typeof(uint)))
			{
				return typeof(T) == typeof(ulong);
			}
			return true;
		}
	}

	public static Vector<T> One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			T one = Scalar<T>.One;
			return new Vector<T>(one);
		}
	}

	public static Vector<T> Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return default(Vector<T>);
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
		[Intrinsic]
		get
		{
			return this.GetElement(index);
		}
	}

	[Intrinsic]
	public Vector(T value)
	{
		Unsafe.SkipInit<Vector<T>>(out this);
		for (int i = 0; i < Count; i++)
		{
			Vector.SetElementUnsafe(in this, i, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(T[] values)
	{
		if (values.Length < Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[0]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(T[] values, int index)
	{
		if (index < 0 || values.Length - index < Count)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(ReadOnlySpan<T> values)
	{
		if (values.Length < Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(ReadOnlySpan<byte> values)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (values.Length < Vector<byte>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(values));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(Span<T> values)
		: this((ReadOnlySpan<T>)values)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator +(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Add(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator &(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.SkipInit<Vector<ulong>>(out var value);
		Vector<ulong> vector = left.As<T, ulong>();
		Vector<ulong> vector2 = right.As<T, ulong>();
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value2 = Vector.GetElementUnsafe(in vector, i) & Vector.GetElementUnsafe(in vector2, i);
			value.SetElementUnsafe(i, value2);
		}
		return value.As<ulong, T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator |(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.SkipInit<Vector<ulong>>(out var value);
		Vector<ulong> vector = left.As<T, ulong>();
		Vector<ulong> vector2 = right.As<T, ulong>();
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value2 = Vector.GetElementUnsafe(in vector, i) | Vector.GetElementUnsafe(in vector2, i);
			value.SetElementUnsafe(i, value2);
		}
		return value.As<ulong, T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator /(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Divide(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator /(Vector<T> left, T right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Divide(Vector.GetElementUnsafe(in left, i), right);
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Count; i++)
		{
			if (!Scalar<T>.Equals(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator ^(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.SkipInit<Vector<ulong>>(out var value);
		Vector<ulong> vector = left.As<T, ulong>();
		Vector<ulong> vector2 = right.As<T, ulong>();
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value2 = Vector.GetElementUnsafe(in vector, i) ^ Vector.GetElementUnsafe(in vector2, i);
			value.SetElementUnsafe(i, value2);
		}
		return value.As<ulong, T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<byte>(Vector<T> value)
	{
		return value.As<T, byte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<double>(Vector<T> value)
	{
		return value.As<T, double>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<short>(Vector<T> value)
	{
		return value.As<T, short>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<int>(Vector<T> value)
	{
		return value.As<T, int>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<long>(Vector<T> value)
	{
		return value.As<T, long>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<nint>(Vector<T> value)
	{
		return value.As<T, nint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static explicit operator Vector<nuint>(Vector<T> value)
	{
		return value.As<T, nuint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static explicit operator Vector<sbyte>(Vector<T> value)
	{
		return value.As<T, sbyte>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static explicit operator Vector<float>(Vector<T> value)
	{
		return value.As<T, float>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static explicit operator Vector<ushort>(Vector<T> value)
	{
		return value.As<T, ushort>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static explicit operator Vector<uint>(Vector<T> value)
	{
		return value.As<T, uint>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static explicit operator Vector<ulong>(Vector<T> value)
	{
		return value.As<T, ulong>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector<T> left, Vector<T> right)
	{
		for (int i = 0; i < Count; i++)
		{
			if (!Scalar<T>.Equals(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i)))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator <<(Vector<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftLeft(Vector.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator *(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Multiply(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator *(Vector<T> value, T factor)
	{
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.Multiply(Vector.GetElementUnsafe(in value, i), factor);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator *(T factor, Vector<T> value)
	{
		return value * factor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator ~(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.SkipInit<Vector<ulong>>(out var value2);
		Vector<ulong> vector = value.As<T, ulong>();
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			ulong value3 = ~Vector.GetElementUnsafe(in vector, i);
			value2.SetElementUnsafe(i, value3);
		}
		return value2.As<ulong, T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator >>(Vector<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftRightArithmetic(Vector.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator -(Vector<T> left, Vector<T> right)
	{
		Unsafe.SkipInit<Vector<T>>(out var value);
		for (int i = 0; i < Count; i++)
		{
			T value2 = Scalar<T>.Subtract(Vector.GetElementUnsafe(in left, i), Vector.GetElementUnsafe(in right, i));
			value.SetElementUnsafe(i, value2);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator -(Vector<T> value)
	{
		return Zero - value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator +(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator >>>(Vector<T> value, int shiftCount)
	{
		Unsafe.SkipInit<Vector<T>>(out var value2);
		for (int i = 0; i < Count; i++)
		{
			T value3 = Scalar<T>.ShiftRightLogical(Vector.GetElementUnsafe(in value, i), shiftCount);
			value2.SetElementUnsafe(i, value3);
		}
		return value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(T[] destination)
	{
		if (destination.Length < Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(T[] destination, int startIndex)
	{
		if ((uint)startIndex >= (uint)destination.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (destination.Length - startIndex < Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(Span<byte> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (destination.Length < Vector<byte>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(Span<T> destination)
	{
		if (destination.Length < Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector<T> other)
	{
		if (Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
			{
				Vector<T> vector = Vector.Equals(this, other) | ~(Vector.Equals(this, this) | Vector.Equals(other, other));
				return vector.As<T, int>() == Vector<int>.AllBitsSet;
			}
			return this == other;
		}
		return SoftwareFallback(in this, other);
		static bool SoftwareFallback(in Vector<T> self, Vector<T> other)
		{
			for (int i = 0; i < Count; i++)
			{
				if (!Scalar<T>.ObjectEquals(Vector.GetElementUnsafe(in self, i), Vector.GetElementUnsafe(in other, i)))
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
			T elementUnsafe = Vector.GetElementUnsafe(in this, i);
			hashCode.Add(elementUnsafe);
		}
		return hashCode.ToHashCode();
	}

	public override string ToString()
	{
		return ToString("G", CultureInfo.CurrentCulture);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ToString(format, CultureInfo.CurrentCulture);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? formatProvider)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		valueStringBuilder.Append('<');
		valueStringBuilder.Append(((IFormattable)(object)Vector.GetElementUnsafe(in this, 0)).ToString(format, formatProvider));
		for (int i = 1; i < Count; i++)
		{
			valueStringBuilder.Append(numberGroupSeparator);
			valueStringBuilder.Append(' ');
			valueStringBuilder.Append(((IFormattable)(object)Vector.GetElementUnsafe(in this, i)).ToString(format, formatProvider));
		}
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryCopyTo(Span<byte> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (destination.Length < Vector<byte>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryCopyTo(Span<T> destination)
	{
		if (destination.Length < Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}
}
