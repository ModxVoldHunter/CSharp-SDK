using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent;

[DebuggerDisplay("Count = {Count}")]
internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, System.Collections.Concurrent.IProducerConsumerQueue<T>, IEnumerable<T>, IEnumerable
{
	bool System.Collections.Concurrent.IProducerConsumerQueue<T>.IsEmpty => base.IsEmpty;

	int System.Collections.Concurrent.IProducerConsumerQueue<T>.Count => base.Count;

	void System.Collections.Concurrent.IProducerConsumerQueue<T>.Enqueue(T item)
	{
		Enqueue(item);
	}

	bool System.Collections.Concurrent.IProducerConsumerQueue<T>.TryDequeue([MaybeNullWhen(false)] out T result)
	{
		return TryDequeue(out result);
	}

	int System.Collections.Concurrent.IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
	{
		return base.Count;
	}
}
