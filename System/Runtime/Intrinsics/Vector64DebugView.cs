using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

internal readonly struct Vector64DebugView<T>
{
	private readonly Vector64<T> _value;

	public byte[] ByteView
	{
		get
		{
			byte[] array = new byte[Vector64<byte>.Count];
			Unsafe.WriteUnaligned(ref array[0], _value);
			return array;
		}
	}

	public double[] DoubleView
	{
		get
		{
			double[] array = new double[Vector64<double>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[0]), _value);
			return array;
		}
	}

	public short[] Int16View
	{
		get
		{
			short[] array = new short[Vector64<short>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref array[0]), _value);
			return array;
		}
	}

	public int[] Int32View
	{
		get
		{
			int[] array = new int[Vector64<int>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref array[0]), _value);
			return array;
		}
	}

	public long[] Int64View
	{
		get
		{
			long[] array = new long[Vector64<long>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref array[0]), _value);
			return array;
		}
	}

	public nint[] NIntView
	{
		get
		{
			nint[] array = new nint[Vector64<nint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public nuint[] NUIntView
	{
		get
		{
			nuint[] array = new nuint[Vector64<nuint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public sbyte[] SByteView
	{
		get
		{
			sbyte[] array = new sbyte[Vector64<sbyte>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref array[0]), _value);
			return array;
		}
	}

	public float[] SingleView
	{
		get
		{
			float[] array = new float[Vector64<float>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref array[0]), _value);
			return array;
		}
	}

	public ushort[] UInt16View
	{
		get
		{
			ushort[] array = new ushort[Vector64<ushort>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref array[0]), _value);
			return array;
		}
	}

	public uint[] UInt32View
	{
		get
		{
			uint[] array = new uint[Vector64<uint>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref array[0]), _value);
			return array;
		}
	}

	public ulong[] UInt64View
	{
		get
		{
			ulong[] array = new ulong[Vector64<ulong>.Count];
			Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref array[0]), _value);
			return array;
		}
	}

	public Vector64DebugView(Vector64<T> value)
	{
		_value = value;
	}
}
