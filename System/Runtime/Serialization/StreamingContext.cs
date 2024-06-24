using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

public readonly struct StreamingContext
{
	private readonly object _additionalContext;

	[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private readonly StreamingContextStates _state;

	[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public StreamingContextStates State => _state;

	public object? Context => _additionalContext;

	[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public StreamingContext(StreamingContextStates state)
		: this(state, null)
	{
	}

	[Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public StreamingContext(StreamingContextStates state, object? additional)
	{
		_state = state;
		_additionalContext = additional;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is StreamingContext streamingContext))
		{
			return false;
		}
		if (streamingContext._additionalContext == _additionalContext)
		{
			return streamingContext._state == _state;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)_state;
	}
}
