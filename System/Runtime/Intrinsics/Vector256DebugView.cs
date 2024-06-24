using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

internal readonly struct Vector256DebugView<T>
{
	private readonly Vector256<T> _value;

	public byte[] ByteView
	{
		get
		{
			byte[] array = new byte[Vector256<byte>.Count];
			Unsafe.WriteUnaligned(ref array[0], _value);
			return array;
		}
	}

	public double[] DoubleView
	{
		get
		{
			double[] array = new double[Vector256<double>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[0]), _value);
			return array;
		}
	}

	public short[] Int16View
	{
		get
		{
			short[] array = new short[Vector256<short>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref array[0]), _value);
			return array;
		}
	}

	public int[] Int32View
	{
		get
		{
			int[] array = new int[Vector256<int>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref array[0]), _value);
			return array;
		}
	}

	public long[] Int64View
	{
		get
		{
			long[] array = new long[Vector256<long>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref array[0]), _value);
			return array;
		}
	}

	public nint[] NIntView
	{
		get
		{
			nint[] array = new nint[Vector256<nint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public nuint[] NUIntView
	{
		get
		{
			nuint[] array = new nuint[Vector256<nuint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public sbyte[] SByteView
	{
		get
		{
			sbyte[] array = new sbyte[Vector256<sbyte>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref array[0]), _value);
			return array;
		}
	}

	public float[] SingleView
	{
		get
		{
			float[] array = new float[Vector256<float>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[0]), _value);
			return array;
		}
	}

	public ushort[] UInt16View
	{
		get
		{
			ushort[] array = new ushort[Vector256<ushort>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref array[0]), _value);
			return array;
		}
	}

	public uint[] UInt32View
	{
		get
		{
			uint[] array = new uint[Vector256<uint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public ulong[] UInt64View
	{
		get
		{
			ulong[] array = new ulong[Vector256<ulong>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref array[0]), _value);
			return array;
		}
	}

	public Vector256DebugView(Vector256<T> value)
	{
		_value = value;
	}
}
