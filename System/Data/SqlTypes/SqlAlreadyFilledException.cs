using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class SqlAlreadyFilledException : SqlTypeException
{
	public SqlAlreadyFilledException()
		: this(SQLResource.AlreadyFilledMessage, null)
	{
	}

	public SqlAlreadyFilledException(string? message)
		: this(message, null)
	{
	}

	public SqlAlreadyFilledException(string? message, Exception? e)
		: base(message, e)
	{
		base.HResult = -2146232015;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private SqlAlreadyFilledException(SerializationInfo si, StreamingContext sc)
		: base(si, sc)
	{
	}
}
