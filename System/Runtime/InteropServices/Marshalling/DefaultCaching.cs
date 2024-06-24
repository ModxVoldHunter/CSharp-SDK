using System.Collections.Concurrent;

namespace System.Runtime.InteropServices.Marshalling;

internal sealed class DefaultCaching : IIUnknownCacheStrategy
{
	private readonly ConcurrentDictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new ConcurrentDictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo>(1, 16);

	unsafe IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails details, void* ptr)
	{
		IIUnknownCacheStrategy.TableInfo result = default(IIUnknownCacheStrategy.TableInfo);
		result.ThisPtr = ptr;
		result.Table = *(void***)ptr;
		result.ManagedType = details.Implementation.TypeHandle;
		return result;
	}

	bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
	{
		return _cache.TryGetValue(handle, out info);
	}

	bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
	{
		return _cache.TryAdd(handle, info);
	}

	unsafe void IIUnknownCacheStrategy.Clear(IIUnknownStrategy unknownStrategy)
	{
		foreach (IIUnknownCacheStrategy.TableInfo value in _cache.Values)
		{
			unknownStrategy.Release(value.ThisPtr);
		}
		_cache.Clear();
	}
}
