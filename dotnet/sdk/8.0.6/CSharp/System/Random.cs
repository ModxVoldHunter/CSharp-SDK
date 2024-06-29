using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public class Random
{
	private sealed class ThreadSafeRandom : Random
	{
		[ThreadStatic]
		private static XoshiroImpl t_random;

		private static XoshiroImpl LocalRandom => t_random ?? Create();

		public ThreadSafeRandom()
			: base(isThreadSafeRandom: true)
		{
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static XoshiroImpl Create()
		{
			return t_random = new XoshiroImpl();
		}

		public override int Next()
		{
			return LocalRandom.Next();
		}

		public override int Next(int maxValue)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(maxValue, "maxValue");
			return LocalRandom.Next(maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			if (minValue > maxValue)
			{
				ThrowMinMaxValueSwapped();
			}
			return LocalRandom.Next(minValue, maxValue);
		}

		public override long NextInt64()
		{
			return LocalRandom.NextInt64();
		}

		public override long NextInt64(long maxValue)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(maxValue, "maxValue");
			return LocalRandom.NextInt64(maxValue);
		}

		public override long NextInt64(long minValue, long maxValue)
		{
			if (minValue > maxValue)
			{
				ThrowMinMaxValueSwapped();
			}
			return LocalRandom.NextInt64(minValue, maxValue);
		}

		public override float NextSingle()
		{
			return LocalRandom.NextSingle();
		}

		public override double NextDouble()
		{
			return LocalRandom.NextDouble();
		}

		public override void NextBytes(byte[] buffer)
		{
			if (buffer == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
			}
			LocalRandom.NextBytes(buffer);
		}

		public override void NextBytes(Span<byte> buffer)
		{
			LocalRandom.NextBytes(buffer);
		}

		protected override double Sample()
		{
			throw new NotSupportedException();
		}
	}

	internal abstract class ImplBase
	{
		public abstract double Sample();

		public abstract int Next();

		public abstract int Next(int maxValue);

		public abstract int Next(int minValue, int maxValue);

		public abstract long NextInt64();

		public abstract long NextInt64(long maxValue);

		public abstract long NextInt64(long minValue, long maxValue);

		public abstract float NextSingle();

		public abstract double NextDouble();

		public abstract void NextBytes(byte[] buffer);

		public abstract void NextBytes(Span<byte> buffer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint NextUInt32(uint maxValue, XoshiroImpl xoshiro)
		{
			ulong num = (ulong)maxValue * (ulong)xoshiro.NextUInt32();
			uint num2 = (uint)num;
			if (num2 < maxValue)
			{
				uint num3 = (0 - maxValue) % maxValue;
				while (num2 < num3)
				{
					num = (ulong)maxValue * (ulong)xoshiro.NextUInt32();
					num2 = (uint)num;
				}
			}
			return (uint)(num >> 32);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ulong NextUInt64(ulong maxValue, XoshiroImpl xoshiro)
		{
			ulong low;
			ulong result = Math.BigMul(maxValue, xoshiro.NextUInt64(), out low);
			if (low < maxValue)
			{
				ulong num = (0 - maxValue) % maxValue;
				while (low < num)
				{
					result = Math.BigMul(maxValue, xoshiro.NextUInt64(), out low);
				}
			}
			return result;
		}
	}

	private sealed class Net5CompatSeedImpl : ImplBase
	{
		private CompatPrng _prng;

		public Net5CompatSeedImpl(int seed)
		{
			_prng.EnsureInitialized(seed);
		}

		public override double Sample()
		{
			return _prng.Sample();
		}

		public override int Next()
		{
			return _prng.InternalSample();
		}

		public override int Next(int maxValue)
		{
			return (int)(_prng.Sample() * (double)maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			long num = (long)maxValue - (long)minValue;
			if (num > int.MaxValue)
			{
				return (int)((long)(_prng.GetSampleForLargeRange() * (double)num) + minValue);
			}
			return (int)(_prng.Sample() * (double)num) + minValue;
		}

		public override long NextInt64()
		{
			ulong num;
			do
			{
				num = NextUInt64() >> 1;
			}
			while (num == long.MaxValue);
			return (long)num;
		}

		public override long NextInt64(long maxValue)
		{
			return NextInt64(0L, maxValue);
		}

		public override long NextInt64(long minValue, long maxValue)
		{
			ulong num = (ulong)(maxValue - minValue);
			if (num > 1)
			{
				int num2 = BitOperations.Log2Ceiling(num);
				ulong num3;
				do
				{
					num3 = NextUInt64() >> 64 - num2;
				}
				while (num3 >= num);
				return (long)num3 + minValue;
			}
			return minValue;
		}

		private ulong NextUInt64()
		{
			return (uint)Next(4194304) | ((ulong)(uint)Next(4194304) << 22) | ((ulong)(uint)Next(1048576) << 44);
		}

		public override double NextDouble()
		{
			return _prng.Sample();
		}

		public override float NextSingle()
		{
			float num;
			do
			{
				num = (float)_prng.Sample();
			}
			while (!(num < 1f));
			return num;
		}

		public override void NextBytes(byte[] buffer)
		{
			_prng.NextBytes(buffer);
		}

		public override void NextBytes(Span<byte> buffer)
		{
			_prng.NextBytes(buffer);
		}
	}

	private sealed class Net5CompatDerivedImpl : ImplBase
	{
		private readonly Random _parent;

		private readonly int _seed;

		private CompatPrng _prng;

		public Net5CompatDerivedImpl(Random parent)
			: this(parent, Shared.Next())
		{
		}

		public Net5CompatDerivedImpl(Random parent, int seed)
		{
			_parent = parent;
			_seed = seed;
		}

		public override double Sample()
		{
			_prng.EnsureInitialized(_seed);
			return _prng.Sample();
		}

		public override int Next()
		{
			_prng.EnsureInitialized(_seed);
			return _prng.InternalSample();
		}

		public override int Next(int maxValue)
		{
			_prng.EnsureInitialized(_seed);
			return (int)(_parent.Sample() * (double)maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			_prng.EnsureInitialized(_seed);
			long num = (long)maxValue - (long)minValue;
			if (num > int.MaxValue)
			{
				return (int)((long)(_prng.GetSampleForLargeRange() * (double)num) + minValue);
			}
			return (int)(_parent.Sample() * (double)num) + minValue;
		}

		public override long NextInt64()
		{
			_prng.EnsureInitialized(_seed);
			ulong num;
			do
			{
				num = NextUInt64() >> 1;
			}
			while (num == long.MaxValue);
			return (long)num;
		}

		public override long NextInt64(long maxValue)
		{
			return NextInt64(0L, maxValue);
		}

		public override long NextInt64(long minValue, long maxValue)
		{
			ulong num = (ulong)(maxValue - minValue);
			if (num > 1)
			{
				_prng.EnsureInitialized(_seed);
				int num2 = BitOperations.Log2Ceiling(num);
				ulong num3;
				do
				{
					num3 = NextUInt64() >> 64 - num2;
				}
				while (num3 >= num);
				return (long)num3 + minValue;
			}
			return minValue;
		}

		private ulong NextUInt64()
		{
			return (uint)_parent.Next(4194304) | ((ulong)(uint)_parent.Next(4194304) << 22) | ((ulong)(uint)_parent.Next(1048576) << 44);
		}

		public override double NextDouble()
		{
			_prng.EnsureInitialized(_seed);
			return _parent.Sample();
		}

		public override float NextSingle()
		{
			_prng.EnsureInitialized(_seed);
			float num;
			do
			{
				num = (float)_parent.Sample();
			}
			while (!(num < 1f));
			return num;
		}

		public override void NextBytes(byte[] buffer)
		{
			_prng.EnsureInitialized(_seed);
			_prng.NextBytes(buffer);
		}

		public override void NextBytes(Span<byte> buffer)
		{
			_prng.EnsureInitialized(_seed);
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = (byte)_parent.Next();
			}
		}
	}

	private struct CompatPrng
	{
		private int[] _seedArray;

		private int _inext;

		private int _inextp;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[MemberNotNull("_seedArray")]
		internal void EnsureInitialized(int seed)
		{
			if (_seedArray == null)
			{
				Initialize(seed);
			}
		}

		[MemberNotNull("_seedArray")]
		private void Initialize(int seed)
		{
			int[] array = new int[56];
			int num = ((seed == int.MinValue) ? int.MaxValue : Math.Abs(seed));
			int num2 = (array[55] = 161803398 - num);
			int num3 = 1;
			int num4 = 0;
			for (int i = 1; i < 55; i++)
			{
				if ((num4 += 21) >= 55)
				{
					num4 -= 55;
				}
				array[num4] = num3;
				num3 = num2 - num3;
				if (num3 < 0)
				{
					num3 += int.MaxValue;
				}
				num2 = array[num4];
			}
			for (int j = 1; j < 5; j++)
			{
				for (int k = 1; k < 56; k++)
				{
					int num5 = k + 30;
					if (num5 >= 55)
					{
						num5 -= 55;
					}
					array[k] -= array[1 + num5];
					if (array[k] < 0)
					{
						array[k] += int.MaxValue;
					}
				}
			}
			_seedArray = array;
			_inext = 0;
			_inextp = 21;
		}

		internal double Sample()
		{
			return (double)InternalSample() * 4.656612875245797E-10;
		}

		internal void NextBytes(Span<byte> buffer)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = (byte)InternalSample();
			}
		}

		internal int InternalSample()
		{
			int inext = _inext;
			if (++inext >= 56)
			{
				inext = 1;
			}
			int inextp = _inextp;
			if (++inextp >= 56)
			{
				inextp = 1;
			}
			int[] seedArray = _seedArray;
			int num = seedArray[inext] - seedArray[inextp];
			if (num == int.MaxValue)
			{
				num--;
			}
			if (num < 0)
			{
				num += int.MaxValue;
			}
			seedArray[inext] = num;
			_inext = inext;
			_inextp = inextp;
			return num;
		}

		internal double GetSampleForLargeRange()
		{
			int num = InternalSample();
			if (InternalSample() % 2 == 0)
			{
				num = -num;
			}
			double num2 = num;
			num2 += 2147483646.0;
			return num2 / 4294967293.0;
		}
	}

	internal sealed class XoshiroImpl : ImplBase
	{
		private ulong _s0;

		private ulong _s1;

		private ulong _s2;

		private ulong _s3;

		public unsafe XoshiroImpl()
		{
			ulong* ptr = stackalloc ulong[4];
			do
			{
				Interop.GetRandomBytes((byte*)ptr, 32);
				_s0 = *ptr;
				_s1 = ptr[1];
				_s2 = ptr[2];
				_s3 = ptr[3];
			}
			while ((_s0 | _s1 | _s2 | _s3) == 0L);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal uint NextUInt32()
		{
			return (uint)(NextUInt64() >> 32);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong NextUInt64()
		{
			ulong s = _s0;
			ulong s2 = _s1;
			ulong s3 = _s2;
			ulong s4 = _s3;
			ulong result = BitOperations.RotateLeft(s2 * 5, 7) * 9;
			ulong num = s2 << 17;
			s3 ^= s;
			s4 ^= s2;
			s2 ^= s3;
			s ^= s4;
			s3 ^= num;
			s4 = BitOperations.RotateLeft(s4, 45);
			_s0 = s;
			_s1 = s2;
			_s2 = s3;
			_s3 = s4;
			return result;
		}

		public override int Next()
		{
			ulong num;
			do
			{
				num = NextUInt64() >> 33;
			}
			while (num == int.MaxValue);
			return (int)num;
		}

		public override int Next(int maxValue)
		{
			return (int)ImplBase.NextUInt32((uint)maxValue, this);
		}

		public override int Next(int minValue, int maxValue)
		{
			return (int)ImplBase.NextUInt32((uint)(maxValue - minValue), this) + minValue;
		}

		public override long NextInt64()
		{
			ulong num;
			do
			{
				num = NextUInt64() >> 1;
			}
			while (num == long.MaxValue);
			return (long)num;
		}

		public override long NextInt64(long maxValue)
		{
			return (long)ImplBase.NextUInt64((ulong)maxValue, this);
		}

		public override long NextInt64(long minValue, long maxValue)
		{
			return (long)ImplBase.NextUInt64((ulong)(maxValue - minValue), this) + minValue;
		}

		public override void NextBytes(byte[] buffer)
		{
			NextBytes((Span<byte>)buffer);
		}

		public unsafe override void NextBytes(Span<byte> buffer)
		{
			ulong num = _s0;
			ulong num2 = _s1;
			ulong num3 = _s2;
			ulong num4 = _s3;
			while (buffer.Length >= 8)
			{
				Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), BitOperations.RotateLeft(num2 * 5, 7) * 9);
				ulong num5 = num2 << 17;
				num3 ^= num;
				num4 ^= num2;
				num2 ^= num3;
				num ^= num4;
				num3 ^= num5;
				num4 = BitOperations.RotateLeft(num4, 45);
				buffer = buffer.Slice(8);
			}
			if (!buffer.IsEmpty)
			{
				ulong num6 = BitOperations.RotateLeft(num2 * 5, 7) * 9;
				byte* ptr = (byte*)(&num6);
				for (int i = 0; i < buffer.Length; i++)
				{
					buffer[i] = ptr[i];
				}
				ulong num7 = num2 << 17;
				num3 ^= num;
				num4 ^= num2;
				num2 ^= num3;
				num ^= num4;
				num3 ^= num7;
				num4 = BitOperations.RotateLeft(num4, 45);
			}
			_s0 = num;
			_s1 = num2;
			_s2 = num3;
			_s3 = num4;
		}

		public override double NextDouble()
		{
			return (double)(NextUInt64() >> 11) * 1.1102230246251565E-16;
		}

		public override float NextSingle()
		{
			return (float)(NextUInt64() >> 40) * 5.9604645E-08f;
		}

		public override double Sample()
		{
			throw new NotSupportedException();
		}
	}

	private readonly ImplBase _impl;

	public static Random Shared { get; } = new ThreadSafeRandom();


	public Random()
	{
		_impl = ((GetType() == typeof(Random)) ? ((ImplBase)new XoshiroImpl()) : ((ImplBase)new Net5CompatDerivedImpl(this)));
	}

	public Random(int Seed)
	{
		_impl = ((GetType() == typeof(Random)) ? ((ImplBase)new Net5CompatSeedImpl(Seed)) : ((ImplBase)new Net5CompatDerivedImpl(this, Seed)));
	}

	private protected Random(bool isThreadSafeRandom)
	{
		_impl = null;
	}

	public virtual int Next()
	{
		return _impl.Next();
	}

	public virtual int Next(int maxValue)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(maxValue, "maxValue");
		return _impl.Next(maxValue);
	}

	public virtual int Next(int minValue, int maxValue)
	{
		if (minValue > maxValue)
		{
			ThrowMinMaxValueSwapped();
		}
		return _impl.Next(minValue, maxValue);
	}

	public virtual long NextInt64()
	{
		return _impl.NextInt64();
	}

	public virtual long NextInt64(long maxValue)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(maxValue, "maxValue");
		return _impl.NextInt64(maxValue);
	}

	public virtual long NextInt64(long minValue, long maxValue)
	{
		if (minValue > maxValue)
		{
			ThrowMinMaxValueSwapped();
		}
		return _impl.NextInt64(minValue, maxValue);
	}

	public virtual float NextSingle()
	{
		return _impl.NextSingle();
	}

	public virtual double NextDouble()
	{
		return _impl.NextDouble();
	}

	public virtual void NextBytes(byte[] buffer)
	{
		if (buffer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
		}
		_impl.NextBytes(buffer);
	}

	public virtual void NextBytes(Span<byte> buffer)
	{
		_impl.NextBytes(buffer);
	}

	public void GetItems<T>(ReadOnlySpan<T> choices, Span<T> destination)
	{
		if (choices.IsEmpty)
		{
			throw new ArgumentException(SR.Arg_EmptySpan, "choices");
		}
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = choices[Next(choices.Length)];
		}
	}

	public T[] GetItems<T>(T[] choices, int length)
	{
		ArgumentNullException.ThrowIfNull(choices, "choices");
		return GetItems(new ReadOnlySpan<T>(choices), length);
	}

	public T[] GetItems<T>(ReadOnlySpan<T> choices, int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		T[] array = new T[length];
		GetItems(choices, array.AsSpan());
		return array;
	}

	public void Shuffle<T>(T[] values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		Shuffle(values.AsSpan());
	}

	public void Shuffle<T>(Span<T> values)
	{
		int length = values.Length;
		for (int i = 0; i < length - 1; i++)
		{
			int num = Next(i, length);
			if (num != i)
			{
				T val = values[i];
				values[i] = values[num];
				values[num] = val;
			}
		}
	}

	protected virtual double Sample()
	{
		return _impl.Sample();
	}

	private static void ThrowMinMaxValueSwapped()
	{
		throw new ArgumentOutOfRangeException("minValue", SR.Format(SR.Argument_MinMaxValue, "minValue", "maxValue"));
	}
}
