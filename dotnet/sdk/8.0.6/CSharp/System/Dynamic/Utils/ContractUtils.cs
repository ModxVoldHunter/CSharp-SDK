using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Dynamic.Utils;

internal static class ContractUtils
{
	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	public static Exception Unreachable => new UnreachableException();

	public static void Requires([DoesNotReturnIf(false)] bool precondition, string paramName)
	{
		if (!precondition)
		{
			throw Error.InvalidArgumentValue(paramName);
		}
	}

	public static void RequiresNotNull(object value, string paramName, int index)
	{
		if (value == null)
		{
			throw new ArgumentNullException(GetParamName(paramName, index));
		}
	}

	public static void RequiresNotEmpty<T>(ICollection<T> collection, string paramName)
	{
		ArgumentNullException.ThrowIfNull(collection, paramName);
		if (collection.Count == 0)
		{
			throw Error.NonEmptyCollectionRequired(paramName);
		}
	}

	public static void RequiresNotNullItems<T>(IList<T> array, string arrayName)
	{
		ArgumentNullException.ThrowIfNull(array, arrayName);
		int i = 0;
		for (int count = array.Count; i < count; i++)
		{
			if (array[i] == null)
			{
				throw new ArgumentNullException(GetParamName(arrayName, i));
			}
		}
	}

	private static string GetParamName(string paramName, int index)
	{
		if (index < 0)
		{
			return paramName;
		}
		return $"{paramName}[{index}]";
	}

	public static void RequiresArrayRange<T>(IList<T> array, int offset, int count, string offsetName, string countName)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, countName);
		ArgumentOutOfRangeException.ThrowIfNegative(offset, offsetName);
		ArgumentOutOfRangeException.ThrowIfLessThan(array.Count - offset, count, offsetName);
	}
}
