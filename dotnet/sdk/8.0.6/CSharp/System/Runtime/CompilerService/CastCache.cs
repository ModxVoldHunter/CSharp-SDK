using System.Numerics;
using System.Threading;

namespace System.Runtime.CompilerServices;

internal struct CastCache
{
	private struct CastCacheEntry
	{
		internal uint _version;

		internal nuint _source;

		internal nuint _targetAndResult;
	}

	private int[] _table;

	private int _lastFlushSize;

	private int _initialCacheSize;

	private int _maxCacheSize;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int KeyToBucket(ref int tableData, nuint source, nuint target)
	{
		int num = HashShift(ref tableData);
		ulong num2 = BitOperations.RotateLeft((ulong)source, 32) ^ target;
		return (int)((long)num2 * -7046029254386353131L >>> num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static ref int TableData(int[] table)
	{
		return ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref table.GetRawData(), sizeof(nint)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref int HashShift(ref int tableData)
	{
		return ref tableData;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref int TableMask(ref int tableData)
	{
		return ref Unsafe.Add(ref tableData, 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref CastCacheEntry Element(ref int tableData, int index)
	{
		return ref Unsafe.Add(ref Unsafe.As<int, CastCacheEntry>(ref tableData), index + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CastResult TryGet(int[] table, nuint source, nuint target)
	{
		ref int tableData = ref TableData(table);
		int num = KeyToBucket(ref tableData, source, target);
		int num2 = 0;
		while (num2 < 8)
		{
			ref CastCacheEntry reference = ref Element(ref tableData, num);
			uint num3 = Volatile.Read(ref reference._version);
			nuint source2 = reference._source;
			num3 &= 0xFFFFFFFEu;
			if (source2 == source)
			{
				nuint targetAndResult = reference._targetAndResult;
				targetAndResult ^= target;
				if (targetAndResult <= 1)
				{
					Interlocked.ReadMemoryBarrier();
					if (num3 != reference._version)
					{
						break;
					}
					return (CastResult)targetAndResult;
				}
			}
			if (num3 == 0)
			{
				break;
			}
			num2++;
			num = (num + num2) & TableMask(ref tableData);
		}
		return CastResult.MaybeCast;
	}
}
