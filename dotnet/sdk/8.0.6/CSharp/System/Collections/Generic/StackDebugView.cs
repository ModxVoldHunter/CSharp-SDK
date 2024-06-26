using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class StackDebugView<T>
{
	private readonly Stack<T> _stack;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _stack.ToArray();

	public StackDebugView(Stack<T> stack)
	{
		ArgumentNullException.ThrowIfNull(stack, "stack");
		_stack = stack;
	}
}
