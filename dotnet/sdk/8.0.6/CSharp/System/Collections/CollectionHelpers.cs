namespace System.Collections;

internal static class CollectionHelpers
{
	internal static void ValidateCopyToArguments(int sourceCount, Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length, "index");
		if (array.Length - index < sourceCount)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
	}
}
