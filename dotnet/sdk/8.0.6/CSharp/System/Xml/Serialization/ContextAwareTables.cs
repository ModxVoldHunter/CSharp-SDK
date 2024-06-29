using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Xml.Serialization;

internal sealed class ContextAwareTables<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> where T : class
{
	private readonly Hashtable _defaultTable;

	private readonly ConditionalWeakTable<Type, T> _collectibleTable;

	public ContextAwareTables()
	{
		_defaultTable = new Hashtable();
		_collectibleTable = new ConditionalWeakTable<Type, T>();
	}

	internal T GetOrCreateValue(Type t, Func<Type, T> f)
	{
		T value = (T)_defaultTable[t];
		if (value != null)
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
			lock (_defaultTable)
			{
				if ((value = (T)_defaultTable[t]) == null)
				{
					value = f(t);
					_defaultTable[t] = value;
				}
			}
		}
		else
		{
			lock (_collectibleTable)
			{
				if (!_collectibleTable.TryGetValue(t, out value))
				{
					value = f(t);
					_collectibleTable.AddOrUpdate(t, value);
				}
			}
		}
		return value;
	}
}
