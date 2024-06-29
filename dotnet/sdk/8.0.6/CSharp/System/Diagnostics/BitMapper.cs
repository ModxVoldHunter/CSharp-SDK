namespace System.Diagnostics;

internal ref struct BitMapper
{
	private int _maxIndex;

	private Span<ulong> _bitMap;

	public BitMapper(Span<ulong> bitMap)
	{
		_bitMap = bitMap;
		_bitMap.Clear();
		_maxIndex = bitMap.Length * 8 * 8;
	}

	private static void GetIndexAndMask(int index, out int bitIndex, out ulong mask)
	{
		bitIndex = index >> 6;
		int num = index & 0x3F;
		mask = (ulong)(1L << num);
	}

	public bool SetBit(int index)
	{
		GetIndexAndMask(index, out var bitIndex, out var mask);
		ulong num = _bitMap[bitIndex];
		_bitMap[bitIndex] = num | mask;
		return true;
	}

	public bool IsSet(int index)
	{
		GetIndexAndMask(index, out var bitIndex, out var mask);
		ulong num = _bitMap[bitIndex];
		return (num & mask) != 0;
	}
}
