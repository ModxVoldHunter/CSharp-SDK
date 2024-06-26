using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

internal static class JsonSerializerOptionsUpdateHandler
{
	public static void ClearCache(Type[] types)
	{
		foreach (KeyValuePair<JsonSerializerOptions, object> item in (IEnumerable<KeyValuePair<JsonSerializerOptions, object>>)JsonSerializerOptions.TrackedOptionsInstances.All)
		{
			item.Key.ClearCaches();
		}
		if (RuntimeFeature.IsDynamicCodeSupported)
		{
			ReflectionEmitCachingMemberAccessor.Clear();
		}
	}
}
