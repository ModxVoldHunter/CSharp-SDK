using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent;

internal interface IProducerConsumerQueue<T> : IEnumerable<T>, IEnumerable
{
	bool IsEmpty { get; }

	int Count { get; }

	void Enqueue(T item);

	bool TryDequeue([MaybeNullWhen(false)] out T result);

	int GetCountSafe(object syncObj);
}
