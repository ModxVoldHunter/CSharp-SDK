using System.Diagnostics.CodeAnalysis;
using Internal;

namespace System.Threading;

internal abstract class LowLevelLifoSemaphoreBase
{
	protected struct Counts : IEquatable<Counts>
	{
		private ulong _data;

		public uint SignalCount
		{
			get
			{
				return GetUInt32Value(0);
			}
			set
			{
				SetUInt32Value(value, 0);
			}
		}

		public ushort WaiterCount => GetUInt16Value(32);

		public byte SpinnerCount => GetByteValue(48);

		public byte CountOfWaitersSignaledToWake => GetByteValue(56);

		private Counts(ulong data)
		{
			_data = data;
		}

		private uint GetUInt32Value(byte shift)
		{
			return (uint)(_data >> (int)shift);
		}

		private void SetUInt32Value(uint value, byte shift)
		{
			_data = (_data & ~(4294967295uL << (int)shift)) | ((ulong)value << (int)shift);
		}

		private ushort GetUInt16Value(byte shift)
		{
			return (ushort)(_data >> (int)shift);
		}

		private byte GetByteValue(byte shift)
		{
			return (byte)(_data >> (int)shift);
		}

		public void AddSignalCount(uint value)
		{
			_data += value;
		}

		public void DecrementSignalCount()
		{
			_data--;
		}

		public void IncrementWaiterCount()
		{
			_data += 4294967296uL;
		}

		public void DecrementWaiterCount()
		{
			_data -= 4294967296uL;
		}

		public void InterlockedDecrementWaiterCount()
		{
			Counts counts = new Counts(Interlocked.Add(ref _data, 18446744069414584320uL));
		}

		public void IncrementSpinnerCount()
		{
			_data += 281474976710656uL;
		}

		public void DecrementSpinnerCount()
		{
			_data -= 281474976710656uL;
		}

		public void AddUpToMaxCountOfWaitersSignaledToWake(uint value)
		{
			uint num = (uint)(255 - CountOfWaitersSignaledToWake);
			if (value > num)
			{
				value = num;
			}
			_data += (ulong)value << 56;
		}

		public void DecrementCountOfWaitersSignaledToWake()
		{
			_data -= 72057594037927936uL;
		}

		public Counts InterlockedCompareExchange(Counts newCounts, Counts oldCounts)
		{
			return new Counts(Interlocked.CompareExchange(ref _data, newCounts._data, oldCounts._data));
		}

		public static bool operator ==(Counts lhs, Counts rhs)
		{
			return lhs.Equals(rhs);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is Counts other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(Counts other)
		{
			return _data == other._data;
		}

		public override int GetHashCode()
		{
			return (int)_data + (int)(_data >> 32);
		}
	}

	protected struct CacheLineSeparatedCounts
	{
		private readonly PaddingFor32 _pad1;

		public Counts _counts;

		private readonly PaddingFor32 _pad2;
	}

	protected CacheLineSeparatedCounts _separated;

	protected readonly int _maximumSignalCount;

	protected readonly int _spinCount;

	protected readonly Action _onWait;

	public LowLevelLifoSemaphoreBase(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
	{
		_separated = default(CacheLineSeparatedCounts);
		_separated._counts.SignalCount = (uint)initialSignalCount;
		_maximumSignalCount = maximumSignalCount;
		_spinCount = spinCount;
		_onWait = onWait;
	}

	protected abstract void ReleaseCore(int count);

	public void Release(int releaseCount)
	{
		Counts counts = _separated._counts;
		int num;
		while (true)
		{
			Counts newCounts = counts;
			newCounts.AddSignalCount((uint)releaseCount);
			num = (int)(Math.Min(newCounts.SignalCount, (uint)(counts.WaiterCount + counts.SpinnerCount)) - counts.SpinnerCount - counts.CountOfWaitersSignaledToWake);
			if (num > 0)
			{
				if (num > releaseCount)
				{
					num = releaseCount;
				}
				newCounts.AddUpToMaxCountOfWaitersSignaledToWake((uint)num);
			}
			Counts counts2 = _separated._counts.InterlockedCompareExchange(newCounts, counts);
			if (counts2 == counts)
			{
				break;
			}
			counts = counts2;
		}
		if (num > 0)
		{
			ReleaseCore(num);
		}
	}
}
