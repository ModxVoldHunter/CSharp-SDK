using System.Diagnostics;

namespace System.Buffers;

internal sealed class SearchValuesDebugView<T> where T : IEquatable<T>
{
	private readonly SearchValues<T> _values;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Values => _values.GetValues();

	public SearchValuesDebugView(SearchValues<T> values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		_values = values;
	}
}
