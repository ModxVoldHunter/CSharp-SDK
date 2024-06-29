using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Runtime.Serialization;

internal sealed class ContextAwareDictionary<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue> where TKey : Type where TValue : class
{
	private readonly ConcurrentDictionary<TKey, TValue> _fastDictionary = new ConcurrentDictionary<TKey, TValue>();

	private readonly ConditionalWeakTable<TKey, TValue> _collectibleTable = new ConditionalWeakTable<TKey, TValue>();

	internal TValue GetOrAdd(TKey t, Func<TKey, TValue> f)
	{
		if (_fastDictionary.TryGetValue(t, out var value))
		{
			return value;
		}
		if (_collectibleTable.TryGetValue(t, out value))
		{
			return value;
		}
		AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(t.Assembly);
		if (loadContext == null || !loadContext.IsCollectible)
		{
			if (!_fastDictionary.TryGetValue(t, out value))
			{
				lock (_fastDictionary)
				{
					return _fastDictionary.GetOrAdd(t, f);
				}
			}
		}
		else if (!_collectibleTable.TryGetValue(t, out value))
		{
			lock (_collectibleTable)
			{
				return _collectibleTable.GetValue(t, (TKey k) => f(k));
			}
		}
		return value;
	}
}
