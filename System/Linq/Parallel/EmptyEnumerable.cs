using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class EmptyEnumerable<T> : ParallelQuery<T>
{
	private static volatile EmptyEnumerable<T> s_instance;

	private static volatile EmptyEnumerator<T> s_enumeratorInstance;

	internal static EmptyEnumerable<T> Instance => s_instance ?? (s_instance = new EmptyEnumerable<T>());

	private EmptyEnumerable()
		: base(QuerySettings.Empty)
	{
	}

	public override IEnumerator<T> GetEnumerator()
	{
		return s_enumeratorInstance ?? (s_enumeratorInstance = new EmptyEnumerator<T>());
	}
}
