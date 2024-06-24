using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public class MeterOptions
{
	private string _name;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_name = value;
		}
	}

	public string? Version { get; set; }

	public IEnumerable<KeyValuePair<string, object?>>? Tags { get; set; }

	public object? Scope { get; set; }

	public MeterOptions(string name)
	{
		Name = name;
	}
}
