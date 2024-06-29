using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable;

internal static class AllocFreeConcurrentStack<T>
{
	[ThreadStatic]
	private static Stack<RefAsValueType<T>> t_stack;

	public static void TryAdd(T item)
	{
		Stack<RefAsValueType<T>> stack = t_stack ?? (t_stack = new Stack<RefAsValueType<T>>(35));
		if (stack.Count < 35)
		{
			stack.Push(new RefAsValueType<T>(item));
		}
	}

	public static bool TryTake([MaybeNullWhen(false)] out T item)
	{
		Stack<RefAsValueType<T>> stack = t_stack;
		if (stack != null && stack.Count > 0)
		{
			item = stack.Pop().Value;
			return true;
		}
		item = default(T);
		return false;
	}
}
