using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

[CollectionBuilder(typeof(ImmutableQueue), "Create")]
public interface IImmutableQueue<T> : IEnumerable<T>, IEnumerable
{
	bool IsEmpty { get; }

	IImmutableQueue<T> Clear();

	T Peek();

	IImmutableQueue<T> Enqueue(T value);

	IImmutableQueue<T> Dequeue();
}
