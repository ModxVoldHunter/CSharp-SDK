using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class DataException : SystemException
{
	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected DataException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public DataException()
		: base(System.SR.DataSet_DefaultDataException)
	{
		base.HResult = -2146232032;
	}

	public DataException(string? s)
		: base(s)
	{
		base.HResult = -2146232032;
	}

	public DataException(string? s, Exception? innerException)
		: base(s, innerException)
	{
	}
}
