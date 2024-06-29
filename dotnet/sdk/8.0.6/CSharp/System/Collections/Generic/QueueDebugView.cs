using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class QueueDebugView<T>
{
	private readonly Queue<T> _queue;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _queue.ToArray();

	public QueueDebugView(Queue<T> queue)
	{
		ArgumentNullException.ThrowIfNull(queue, "queue");
		_queue = queue;
	}
}
