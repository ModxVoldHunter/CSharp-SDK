using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public class SourceFilter : TraceFilter
{
	private string _src;

	public string Source
	{
		get
		{
			return _src;
		}
		[MemberNotNull("_src")]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "Source");
			_src = value;
		}
	}

	public SourceFilter(string source)
	{
		Source = source;
	}

	public override bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, [StringSyntax("CompositeFormat")] string? formatOrMessage, object?[]? args, object? data1, object?[]? data)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return string.Equals(_src, source);
	}
}
