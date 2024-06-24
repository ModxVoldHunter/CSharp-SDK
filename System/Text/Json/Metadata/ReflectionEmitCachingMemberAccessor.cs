using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class ReflectionEmitCachingMemberAccessor : MemberAccessor
{
	private sealed class Cache<TKey>
	{
		private sealed class CacheEntry
		{
			public readonly object Value;

			public long LastUsedTicks;

			public CacheEntry(object value)
			{
				Value = value;
			}
		}

		private int _evictLock;

		private long _lastEvictedTicks;

		private readonly long _evictionIntervalTicks;

		private readonly long _slidingExpirationTicks;

		private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new ConcurrentDictionary<TKey, CacheEntry>();

		public Cache(TimeSpan slidingExpiration, TimeSpan evictionInterval)
		{
			_slidingExpirationTicks = slidingExpiration.Ticks;
			_evictionIntervalTicks = evictionInterval.Ticks;
			_lastEvictedTicks = DateTime.UtcNow.Ticks;
		}

		public TValue GetOrAdd<TValue>(TKey key, Func<TKey, TValue> valueFactory) where TValue : class
		{
			CacheEntry orAdd = _cache.GetOrAdd(key, (TKey key, Func<TKey, TValue> valueFactory) => new CacheEntry(valueFactory(key)), valueFactory);
			long ticks = DateTime.UtcNow.Ticks;
			Volatile.Write(ref orAdd.LastUsedTicks, ticks);
			if (ticks - Volatile.Read(ref _lastEvictedTicks) >= _evictionIntervalTicks && Interlocked.CompareExchange(ref _evictLock, 1, 0) == 0)
			{
				if (ticks - _lastEvictedTicks >= _evictionIntervalTicks)
				{
					EvictStaleCacheEntries(ticks);
					Volatile.Write(ref _lastEvictedTicks, ticks);
				}
				Volatile.Write(ref _evictLock, 0);
			}
			return (TValue)orAdd.Value;
		}

		public void Clear()
		{
			_cache.Clear();
			_lastEvictedTicks = DateTime.UtcNow.Ticks;
		}

		private void EvictStaleCacheEntries(long utcNowTicks)
		{
			foreach (KeyValuePair<TKey, CacheEntry> item in _cache)
			{
				if (utcNowTicks - Volatile.Read(ref item.Value.LastUsedTicks) >= _slidingExpirationTicks)
				{
					_cache.TryRemove(item.Key, out var _);
				}
			}
		}
	}

	private static readonly ReflectionEmitMemberAccessor s_sourceAccessor = new ReflectionEmitMemberAccessor();

	private static readonly Cache<(string id, Type declaringType, MemberInfo member)> s_cache = new Cache<(string, Type, MemberInfo)>(TimeSpan.FromMilliseconds(1000.0), TimeSpan.FromMilliseconds(200.0));

	public static void Clear()
	{
		s_cache.Clear();
	}

	public override Action<TCollection, object> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
	{
		return s_cache.GetOrAdd(("CreateAddMethodDelegate", typeof(TCollection), null), ((string id, Type declaringType, MemberInfo member) _) => s_sourceAccessor.CreateAddMethodDelegate<TCollection>());
	}

	public override Func<object> CreateParameterlessConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ConstructorInfo ctorInfo)
	{
		return s_cache.GetOrAdd(("CreateParameterlessConstructor", type, ctorInfo), [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern", Justification = "Cannot apply DynamicallyAccessedMembersAttribute to tuple properties.")] ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreateParameterlessConstructor(key.declaringType, (ConstructorInfo)key.member));
	}

	public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo)
	{
		return s_cache.GetOrAdd(("CreateFieldGetter", typeof(TProperty), fieldInfo), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreateFieldGetter<TProperty>((FieldInfo)key.member));
	}

	public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo)
	{
		return s_cache.GetOrAdd(("CreateFieldSetter", typeof(TProperty), fieldInfo), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreateFieldSetter<TProperty>((FieldInfo)key.member));
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>()
	{
		return s_cache.GetOrAdd(("CreateImmutableDictionaryCreateRangeDelegate", typeof((TCollection, TKey, TValue)), null), ((string id, Type declaringType, MemberInfo member) _) => s_sourceAccessor.CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>());
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>()
	{
		return s_cache.GetOrAdd(("CreateImmutableEnumerableCreateRangeDelegate", typeof((TCollection, TElement)), null), ((string id, Type declaringType, MemberInfo member) _) => s_sourceAccessor.CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>());
	}

	public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
	{
		return s_cache.GetOrAdd(("CreateParameterizedConstructor", typeof(T), constructor), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreateParameterizedConstructor<T>((ConstructorInfo)key.member));
	}

	public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3> CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
	{
		return s_cache.GetOrAdd(("CreateParameterizedConstructor", typeof(T), constructor), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>((ConstructorInfo)key.member));
	}

	public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
	{
		return s_cache.GetOrAdd(("CreatePropertyGetter", typeof(TProperty), propertyInfo), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreatePropertyGetter<TProperty>((PropertyInfo)key.member));
	}

	public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
	{
		return s_cache.GetOrAdd(("CreatePropertySetter", typeof(TProperty), propertyInfo), ((string id, Type declaringType, MemberInfo member) key) => s_sourceAccessor.CreatePropertySetter<TProperty>((PropertyInfo)key.member));
	}
}
