using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public static class MeterFactoryExtensions
{
	public static Meter Create(this IMeterFactory meterFactory, string name, string? version = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)
	{
		if (meterFactory == null)
		{
			throw new ArgumentNullException("meterFactory");
		}
		return meterFactory.Create(new MeterOptions(name)
		{
			Version = version,
			Tags = tags,
			Scope = meterFactory
		});
	}
}
