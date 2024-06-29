using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TimeZoneNotFoundException : Exception
{
	public TimeZoneNotFoundException()
	{
	}

	public TimeZoneNotFoundException(string? message)
		: base(message)
	{
	}

	public TimeZoneNotFoundException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TimeZoneNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
