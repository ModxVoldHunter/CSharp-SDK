using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

internal static class Scalar<T>
{
	public static T AllBitsSet
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (typeof(T) == typeof(byte))
			{
				return (T)(object)byte.MaxValue;
			}
			if (typeof(T) == typeof(double))
			{
				return (T)(object)BitConverter.Int64BitsToDouble(-1L);
			}
			if (typeof(T) == typeof(short))
			{
				return (T)(object)(short)(-1);
			}
			if (typeof(T) == typeof(int))
			{
				return (T)(object)(-1);
			}
			if (typeof(T) == typeof(long))
			{
				return (T)(object)(-1L);
			}
			if (typeof(T) == typeof(nint))
			{
				return (T)(object)(nint)(-1);
			}
			if (typeof(T) == typeof(nuint))
			{
				return (T)(object)UIntPtr.MaxValue;
			}
			if (typeof(T) == typeof(sbyte))
			{
				return (T)(object)(sbyte)(-1);
			}
			if (typeof(T) == typeof(float))
			{
				return (T)(object)BitConverter.Int32BitsToSingle(-1);
			}
			if (typeof(T) == typeof(ushort))
			{
				return (T)(object)ushort.MaxValue;
			}
			if (typeof(T) == typeof(uint))
			{
				return (T)(object)uint.MaxValue;
			}
			if (typeof(T) == typeof(ulong))
			{
				return (T)(object)ulong.MaxValue;
			}
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
			return default(T);
		}
	}

	public static T One
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (typeof(T) == typeof(byte))
			{
				return (T)(object)(byte)1;
			}
			if (typeof(T) == typeof(double))
			{
				return (T)(object)1.0;
			}
			if (typeof(T) == typeof(short))
			{
				return (T)(object)(short)1;
			}
			if (typeof(T) == typeof(int))
			{
				return (T)(object)1;
			}
			if (typeof(T) == typeof(long))
			{
				return (T)(object)1L;
			}
			if (typeof(T) == typeof(nint))
			{
				return (T)(object)(nint)1;
			}
			if (typeof(T) == typeof(nuint))
			{
				return (T)(object)(nuint)1u;
			}
			if (typeof(T) == typeof(sbyte))
			{
				return (T)(object)(sbyte)1;
			}
			if (typeof(T) == typeof(float))
			{
				return (T)(object)1f;
			}
			if (typeof(T) == typeof(ushort))
			{
				return (T)(object)(ushort)1;
			}
			if (typeof(T) == typeof(uint))
			{
				return (T)(object)1u;
			}
			if (typeof(T) == typeof(ulong))
			{
				return (T)(object)1uL;
			}
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
			return default(T);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Abs(T value)
	{
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Abs((double)(object)value);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)Math.Abs((short)(object)value);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)Math.Abs((int)(object)value);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)Math.Abs((long)(object)value);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)Math.Abs((nint)(object)value);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)Math.Abs((sbyte)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)Math.Abs((float)(object)value);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Add(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left + (byte)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left + (double)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left + (short)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left + (int)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left + (long)(object)right);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)left + (nint)(object)right);
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)left + (nuint)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left + (float)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left + (uint)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left + (ulong)(object)right);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Ceiling(T value)
	{
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Ceiling((double)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)MathF.Ceiling((float)(object)value);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Divide(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left / (byte)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left / (double)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left / (short)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left / (int)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left / (long)(object)right);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)left / (nint)(object)right);
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)left / (nuint)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left / (sbyte)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left / (float)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left / (ushort)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left / (uint)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left / (ulong)(object)right);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Equals(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left == (byte)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left == (double)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left == (short)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left == (int)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left == (long)(object)right;
		}
		if (typeof(T) == typeof(nint))
		{
			return (nint)(object)left == (nint)(object)right;
		}
		if (typeof(T) == typeof(nuint))
		{
			return (nuint)(object)left == (nuint)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left == (sbyte)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left == (float)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left == (ushort)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left == (uint)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left == (ulong)(object)right;
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ExtractMostSignificantBit(T value)
	{
		if (typeof(T) == typeof(byte))
		{
			uint num = (byte)(object)value;
			return num >> 7;
		}
		if (typeof(T) == typeof(double))
		{
			ulong num2 = BitConverter.DoubleToUInt64Bits((double)(object)value);
			return (uint)(num2 >> 63);
		}
		if (typeof(T) == typeof(short))
		{
			uint num3 = (ushort)(short)(object)value;
			return num3 >> 15;
		}
		if (typeof(T) == typeof(int))
		{
			uint num4 = (uint)(int)(object)value;
			return num4 >> 31;
		}
		if (typeof(T) == typeof(long))
		{
			ulong num5 = (ulong)(long)(object)value;
			return (uint)(num5 >> 63);
		}
		if (typeof(T) == typeof(nint))
		{
			ulong num6 = (ulong)(nint)(object)value;
			return (uint)(num6 >> 63);
		}
		if (typeof(T) == typeof(nuint))
		{
			ulong num7 = (nuint)(object)value;
			return (uint)(num7 >> 63);
		}
		if (typeof(T) == typeof(sbyte))
		{
			uint num8 = (byte)(sbyte)(object)value;
			return num8 >> 7;
		}
		if (typeof(T) == typeof(float))
		{
			uint num9 = BitConverter.SingleToUInt32Bits((float)(object)value);
			return num9 >> 31;
		}
		if (typeof(T) == typeof(ushort))
		{
			uint num10 = (ushort)(object)value;
			return num10 >> 15;
		}
		if (typeof(T) == typeof(uint))
		{
			uint num11 = (uint)(object)value;
			return num11 >> 31;
		}
		if (typeof(T) == typeof(ulong))
		{
			ulong num12 = (ulong)(object)value;
			return (uint)(num12 >> 63);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return 0u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Floor(T value)
	{
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Floor((double)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)MathF.Floor((float)(object)value);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left > (byte)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left > (double)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left > (short)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left > (int)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left > (long)(object)right;
		}
		if (typeof(T) == typeof(nint))
		{
			return (nint)(object)left > (nint)(object)right;
		}
		if (typeof(T) == typeof(nuint))
		{
			return (nuint)(object)left > (nuint)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left > (sbyte)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left > (float)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left > (ushort)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left > (uint)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left > (ulong)(object)right;
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanOrEqual(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left >= (byte)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left >= (double)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left >= (short)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left >= (int)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left >= (long)(object)right;
		}
		if (typeof(T) == typeof(nint))
		{
			return (nint)(object)left >= (nint)(object)right;
		}
		if (typeof(T) == typeof(nuint))
		{
			return (nuint)(object)left >= (nuint)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left >= (sbyte)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left >= (float)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left >= (ushort)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left >= (uint)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left >= (ulong)(object)right;
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left < (byte)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left < (double)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left < (short)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left < (int)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left < (long)(object)right;
		}
		if (typeof(T) == typeof(nint))
		{
			return (nint)(object)left < (nint)(object)right;
		}
		if (typeof(T) == typeof(nuint))
		{
			return (nuint)(object)left < (nuint)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left < (sbyte)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left < (float)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left < (ushort)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left < (uint)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left < (ulong)(object)right;
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanOrEqual(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left <= (byte)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left <= (double)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left <= (short)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left <= (int)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left <= (long)(object)right;
		}
		if (typeof(T) == typeof(nint))
		{
			return (nint)(object)left <= (nint)(object)right;
		}
		if (typeof(T) == typeof(nuint))
		{
			return (nuint)(object)left <= (nuint)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left <= (sbyte)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left <= (float)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left <= (ushort)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left <= (uint)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left <= (ulong)(object)right;
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Multiply(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left * (byte)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left * (double)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left * (short)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left * (int)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left * (long)(object)right);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)left * (nint)(object)right);
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)left * (nuint)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left * (sbyte)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left * (float)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left * (ushort)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left * (uint)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left * (ulong)(object)right);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	public static bool ObjectEquals(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return ((byte)(object)left).Equals((byte)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return ((double)(object)left).Equals((double)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return ((short)(object)left).Equals((short)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return ((int)(object)left).Equals((int)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return ((long)(object)left).Equals((long)(object)right);
		}
		if (typeof(T) == typeof(nint))
		{
			return ((IntPtr)(nint)(object)left).Equals((nint)(object)right);
		}
		if (typeof(T) == typeof(nuint))
		{
			return ((UIntPtr)(nuint)(object)left).Equals((nuint)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return ((sbyte)(object)left).Equals((sbyte)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return ((float)(object)left).Equals((float)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return ((ushort)(object)left).Equals((ushort)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return ((uint)(object)left).Equals((uint)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return ((ulong)(object)left).Equals((ulong)(object)right);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ShiftLeft(T value, int shiftCount)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)value << (shiftCount & 7));
		}
		if (typeof(T) == typeof(double))
		{
			long num = BitConverter.DoubleToInt64Bits((double)(object)value);
			double num2 = BitConverter.Int64BitsToDouble(num << shiftCount);
			return (T)(object)num2;
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)value << (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)value << shiftCount);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)value << shiftCount);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)value << (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)value << (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)value << (shiftCount & 7));
		}
		if (typeof(T) == typeof(float))
		{
			int num3 = BitConverter.SingleToInt32Bits((float)(object)value);
			float num4 = BitConverter.Int32BitsToSingle(num3 << shiftCount);
			return (T)(object)num4;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)value << (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)value << shiftCount);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)value << shiftCount);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ShiftRightArithmetic(T value, int shiftCount)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)value >> (shiftCount & 7));
		}
		if (typeof(T) == typeof(double))
		{
			long num = BitConverter.DoubleToInt64Bits((double)(object)value);
			double num2 = BitConverter.Int64BitsToDouble(num >> shiftCount);
			return (T)(object)num2;
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)value >> (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)value >> shiftCount);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)value >> shiftCount);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)value >> (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)value >> (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)value >> (shiftCount & 7));
		}
		if (typeof(T) == typeof(float))
		{
			int num3 = BitConverter.SingleToInt32Bits((float)(object)value);
			float num4 = BitConverter.Int32BitsToSingle(num3 >> shiftCount);
			return (T)(object)num4;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)value >> (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)value >> shiftCount);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)value >> shiftCount);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ShiftRightLogical(T value, int shiftCount)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((uint)(byte)(object)value >> (shiftCount & 7));
		}
		if (typeof(T) == typeof(double))
		{
			long num = BitConverter.DoubleToInt64Bits((double)(object)value);
			double num2 = BitConverter.Int64BitsToDouble(num >>> shiftCount);
			return (T)(object)num2;
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((ushort)(short)(object)value >>> (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)value >>> shiftCount);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)value >>> shiftCount);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)value >>> (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)value >> (shiftCount & 0x3F));
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((byte)(sbyte)(object)value >>> (shiftCount & 7));
		}
		if (typeof(T) == typeof(float))
		{
			int num3 = BitConverter.SingleToInt32Bits((float)(object)value);
			float num4 = BitConverter.Int32BitsToSingle(num3 >>> shiftCount);
			return (T)(object)num4;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((uint)(ushort)(object)value >> (shiftCount & 0xF));
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)value >> shiftCount);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)value >> shiftCount);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Sqrt(T value)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)MathF.Sqrt((int)(byte)(object)value);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Sqrt((double)(object)value);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)MathF.Sqrt((short)(object)value);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)(int)Math.Sqrt((int)(object)value);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)(long)Math.Sqrt((long)(object)value);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)(nint)Math.Sqrt((nint)(object)value);
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)(nuint)Math.Sqrt((nuint)(object)value);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)MathF.Sqrt((sbyte)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)MathF.Sqrt((float)(object)value);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)MathF.Sqrt((int)(ushort)(object)value);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)(uint)Math.Sqrt((uint)(object)value);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)(ulong)Math.Sqrt((ulong)(object)value);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Subtract(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left - (byte)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left - (double)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left - (short)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left - (int)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left - (long)(object)right);
		}
		if (typeof(T) == typeof(nint))
		{
			return (T)(object)((nint)(object)left - (nint)(object)right);
		}
		if (typeof(T) == typeof(nuint))
		{
			return (T)(object)((nuint)(object)left - (nuint)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left - (float)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left - (uint)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left - (ulong)(object)right);
		}
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		return default(T);
	}
}
