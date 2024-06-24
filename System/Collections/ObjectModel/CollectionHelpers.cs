using System.Collections.Generic;

namespace System.Collections.ObjectModel;

internal static class CollectionHelpers
{
	internal static void ValidateCopyToArguments(int sourceCount, Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		if (array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(SR.Arg_NonZeroLowerBound, "array");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length, "index");
		if (array.Length - index < sourceCount)
		{
			throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
		}
	}

	internal static void CopyTo<T>(ICollection<T> collection, Array array, int index)
	{
		ValidateCopyToArguments(collection.Count, array, index);
		if (collection is ICollection collection2)
		{
			collection2.CopyTo(array, index);
			return;
		}
		if (array is T[] array2)
		{
			collection.CopyTo(array2, index);
			return;
		}
		if (!(array is object[] array3))
		{
			throw new ArgumentException(SR.Argument_IncompatibleArrayType, "array");
		}
		try
		{
			foreach (T item in collection)
			{
				array3[index++] = item;
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(SR.Argument_IncompatibleArrayType, "array");
		}
	}
}
