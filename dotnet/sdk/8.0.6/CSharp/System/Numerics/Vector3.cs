using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics;

[Intrinsic]
public struct Vector3 : IEquatable<Vector3>, IFormattable
{
	public float X;

	public float Y;

	public float Z;

	public static Vector3 Zero
	{
		[Intrinsic]
		get
		{
			return default(Vector3);
		}
	}

	public static Vector3 One
	{
		[Intrinsic]
		get
		{
			return new Vector3(1f);
		}
	}

	public static Vector3 UnitX
	{
		[Intrinsic]
		get
		{
			return new Vector3(1f, 0f, 0f);
		}
	}

	public static Vector3 UnitY
	{
		[Intrinsic]
		get
		{
			return new Vector3(0f, 1f, 0f);
		}
	}

	public static Vector3 UnitZ
	{
		[Intrinsic]
		get
		{
			return new Vector3(0f, 0f, 1f);
		}
	}

	public float this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		readonly get
		{
			return this.GetElement(index);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			this = this.WithElement(index, value);
		}
	}

	[Intrinsic]
	public Vector3(float value)
		: this(value, value, value)
	{
	}

	[Intrinsic]
	public Vector3(Vector2 value, float z)
		: this(value.X, value.Y, z)
	{
	}

	[Intrinsic]
	public Vector3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector3(ReadOnlySpan<float> values)
	{
		if (values.Length < 3)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
		}
		this = Unsafe.ReadUnaligned<Vector3>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator +(Vector3 left, Vector3 right)
	{
		return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator /(Vector3 left, Vector3 right)
	{
		return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator /(Vector3 value1, float value2)
	{
		return value1 / new Vector3(value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector3 left, Vector3 right)
	{
		if (left.X == right.X && left.Y == right.Y)
		{
			return left.Z == right.Z;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector3 left, Vector3 right)
	{
		return !(left == right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator *(Vector3 left, Vector3 right)
	{
		return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator *(Vector3 left, float right)
	{
		return left * new Vector3(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator *(float left, Vector3 right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator -(Vector3 left, Vector3 right)
	{
		return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 operator -(Vector3 value)
	{
		return Zero - value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Abs(Vector3 value)
	{
		return new Vector3(MathF.Abs(value.X), MathF.Abs(value.Y), MathF.Abs(value.Z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Add(Vector3 left, Vector3 right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
	{
		return Min(Max(value1, min), max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
	{
		return new Vector3(vector1.Y * vector2.Z - vector1.Z * vector2.Y, vector1.Z * vector2.X - vector1.X * vector2.Z, vector1.X * vector2.Y - vector1.Y * vector2.X);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Distance(Vector3 value1, Vector3 value2)
	{
		float x = DistanceSquared(value1, value2);
		return MathF.Sqrt(x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float DistanceSquared(Vector3 value1, Vector3 value2)
	{
		Vector3 vector = value1 - value2;
		return Dot(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Divide(Vector3 left, Vector3 right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Divide(Vector3 left, float divisor)
	{
		return left / divisor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Dot(Vector3 vector1, Vector3 vector2)
	{
		return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount)
	{
		return value1 * (1f - amount) + value2 * amount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Max(Vector3 value1, Vector3 value2)
	{
		return new Vector3((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y, (value1.Z > value2.Z) ? value1.Z : value2.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Min(Vector3 value1, Vector3 value2)
	{
		return new Vector3((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y, (value1.Z < value2.Z) ? value1.Z : value2.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Multiply(Vector3 left, Vector3 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Multiply(Vector3 left, float right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Multiply(float left, Vector3 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Negate(Vector3 value)
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Normalize(Vector3 value)
	{
		return value / value.Length();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Reflect(Vector3 vector, Vector3 normal)
	{
		float num = Dot(vector, normal);
		return vector - 2f * (num * normal);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 SquareRoot(Vector3 value)
	{
		return new Vector3(MathF.Sqrt(value.X), MathF.Sqrt(value.Y), MathF.Sqrt(value.Z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector3 Subtract(Vector3 left, Vector3 right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Transform(Vector3 position, Matrix4x4 matrix)
	{
		return Vector4.Transform(position, in matrix.AsImpl()).AsVector128().AsVector3();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Transform(Vector3 value, Quaternion rotation)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num;
		float num5 = rotation.W * num2;
		float num6 = rotation.W * num3;
		float num7 = rotation.X * num;
		float num8 = rotation.X * num2;
		float num9 = rotation.X * num3;
		float num10 = rotation.Y * num2;
		float num11 = rotation.Y * num3;
		float num12 = rotation.Z * num3;
		return new Vector3(value.X * (1f - num10 - num12) + value.Y * (num8 - num6) + value.Z * (num9 + num5), value.X * (num8 + num6) + value.Y * (1f - num7 - num12) + value.Z * (num11 - num4), value.X * (num9 - num5) + value.Y * (num11 + num4) + value.Z * (1f - num7 - num10));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 matrix)
	{
		return TransformNormal(normal, in matrix.AsImpl());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector3 TransformNormal(Vector3 normal, in Matrix4x4.Impl matrix)
	{
		Vector4 value = matrix.X * normal.X;
		value += matrix.Y * normal.Y;
		value += matrix.Z * normal.Z;
		return value.AsVector128().AsVector3();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(float[] array)
	{
		if (array.Length < 3)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[0]), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(float[] array, int index)
	{
		if ((uint)index >= (uint)array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
		}
		if (array.Length - index < 3)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[index]), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void CopyTo(Span<float> destination)
	{
		if (destination.Length < 3)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryCopyTo(Span<float> destination)
	{
		if (destination.Length < 3)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector3 other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(Vector3 other)
	{
		if (Vector128.IsHardwareAccelerated)
		{
			return this.AsVector128().Equals(other.AsVector128());
		}
		return SoftwareFallback(in this, other);
		static bool SoftwareFallback(in Vector3 self, Vector3 other)
		{
			if (self.X.Equals(other.X) && self.Y.Equals(other.Y))
			{
				return self.Z.Equals(other.Z);
			}
			return false;
		}
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(X, Y, Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public readonly float Length()
	{
		float x = LengthSquared();
		return MathF.Sqrt(x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public readonly float LengthSquared()
	{
		return Dot(this, this);
	}

	public override readonly string ToString()
	{
		return ToString("G", CultureInfo.CurrentCulture);
	}

	public readonly string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ToString(format, CultureInfo.CurrentCulture);
	}

	public readonly string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? formatProvider)
	{
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		return $"<{X.ToString(format, formatProvider)}{numberGroupSeparator} {Y.ToString(format, formatProvider)}{numberGroupSeparator} {Z.ToString(format, formatProvider)}>";
	}
}
