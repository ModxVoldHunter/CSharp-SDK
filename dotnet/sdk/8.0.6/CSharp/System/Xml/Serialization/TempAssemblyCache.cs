using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Xml.Serialization;

internal sealed class TempAssemblyCache
{
	private readonly ConditionalWeakTable<Assembly, Dictionary<TempAssemblyCacheKey, TempAssembly>> _collectibleCaches = new ConditionalWeakTable<Assembly, Dictionary<TempAssemblyCacheKey, TempAssembly>>();

	private Dictionary<TempAssemblyCacheKey, TempAssembly> _fastCache = new Dictionary<TempAssemblyCacheKey, TempAssembly>();

	internal TempAssembly this[string ns, Type t]
	{
		get
		{
			TempAssemblyCacheKey key = new TempAssemblyCacheKey(ns, t);
			if (_fastCache.TryGetValue(key, out var value))
			{
				return value;
			}
			if (_collectibleCaches.TryGetValue(t.Assembly, out var value2))
			{
				value2.TryGetValue(key, out value);
			}
			return value;
		}
	}

	internal void Add(string ns, Type t, TempAssembly assembly)
	{
		lock (this)
		{
			TempAssembly tempAssembly = this[ns, t];
			if (tempAssembly != assembly)
			{
				AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(t.Assembly);
				TempAssemblyCacheKey key = new TempAssemblyCacheKey(ns, t);
				if (loadContext != null && loadContext.IsCollectible)
				{
					Dictionary<TempAssemblyCacheKey, TempAssembly> value;
					Dictionary<TempAssemblyCacheKey, TempAssembly> dictionary = (_collectibleCaches.TryGetValue(t.Assembly, out value) ? new Dictionary<TempAssemblyCacheKey, TempAssembly>(value) : new Dictionary<TempAssemblyCacheKey, TempAssembly>());
					dictionary[key] = assembly;
					_collectibleCaches.AddOrUpdate(t.Assembly, dictionary);
				}
				else
				{
					Dictionary<TempAssemblyCacheKey, TempAssembly> dictionary = new Dictionary<TempAssemblyCacheKey, TempAssembly>(_fastCache);
					dictionary[key] = assembly;
					_fastCache = dictionary;
				}
			}
		}
	}
}
