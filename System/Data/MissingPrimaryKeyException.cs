using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingPrimaryKeyException : DataException
{
	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected MissingPrimaryKeyException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public MissingPrimaryKeyException()
		: base(System.SR.DataSet_DefaultMissingPrimaryKeyException)
	{
		base.HResult = -2146232027;
	}

	public MissingPrimaryKeyException(string? s)
		: base(s)
	{
		base.HResult = -2146232027;
	}

	public MissingPrimaryKeyException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232027;
	}
}
