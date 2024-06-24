using System.Buffers;
using System.Collections.Generic;

namespace System.Collections.Frozen;

internal static class LengthBuckets
{
	internal const int MaxPerLength = 5;

	internal static int[]? CreateLengthBucketsArrayIfAppropriate(string[] keys, IEqualityComparer<string> comparer, int minLength, int maxLength)
	{
		int num = maxLength - minLength + 1;
		if (keys.Length / num > 5)
		{
			return null;
		}
		int num2 = num * 5;
		if (num2 > Array.MaxLength)
		{
			return null;
		}
		int[] array = ArrayPool<int>.Shared.Rent(num2);
		array.AsSpan(0, num2).Fill(-1);
		int num3 = 0;
		for (int i = 0; i < keys.Length; i++)
		{
			string text = keys[i];
			int num4 = (text.Length - minLength) * 5;
			int num5 = num4 + 5;
			int j;
			for (j = num4; j < num5; j++)
			{
				ref int reference = ref array[j];
				if (reference < 0)
				{
					if (j == num4)
					{
						num3++;
					}
					reference = i;
					break;
				}
			}
			if (j == num5)
			{
				ArrayPool<int>.Shared.Return(array);
				return null;
			}
		}
		if ((double)num3 / (double)num < 0.2)
		{
			ArrayPool<int>.Shared.Return(array);
			return null;
		}
		int[] array2 = GC.AllocateUninitializedArray<int>(num2);
		Array.Copy(array, array2, num2);
		ArrayPool<int>.Shared.Return(array);
		return array2;
	}
}
