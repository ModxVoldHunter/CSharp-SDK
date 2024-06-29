using System.Collections.Generic;

namespace System.Reflection.Internal;

internal static class EnumerableExtensions
{
	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		foreach (TSource item in source)
		{
			yield return selector(item);
		}
	}

	public static IEnumerable<T> OrderBy<T>(this List<T> source, Comparison<T> comparison)
	{
		Comparison<T> comparison = comparison;
		List<T> source = source;
		int[] array = new int[source.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		Array.Sort(array, delegate(int left, int right)
		{
			if (left == right)
			{
				return 0;
			}
			int num = comparison(source[left], source[right]);
			return (num == 0) ? (left - right) : num;
		});
		int[] array2 = array;
		foreach (int index in array2)
		{
			yield return source[index];
		}
	}
}
