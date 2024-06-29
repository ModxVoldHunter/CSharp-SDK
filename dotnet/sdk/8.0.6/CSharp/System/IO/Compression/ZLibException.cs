using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO.Compression;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ZLibException : IOException, ISerializable
{
	private readonly string _zlibErrorContext = string.Empty;

	private readonly string _zlibErrorMessage = string.Empty;

	private readonly ZLibNative.ErrorCode _zlibErrorCode;

	public ZLibException(string? message, string? zlibErrorContext, int zlibErrorCode, string? zlibErrorMessage)
		: base(message)
	{
		_zlibErrorContext = zlibErrorContext;
		_zlibErrorCode = (ZLibNative.ErrorCode)zlibErrorCode;
		_zlibErrorMessage = zlibErrorMessage;
	}

	public ZLibException()
	{
	}

	public ZLibException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ZLibException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_zlibErrorContext = info.GetString("zlibErrorContext");
		_zlibErrorCode = (ZLibNative.ErrorCode)info.GetInt32("zlibErrorCode");
		_zlibErrorMessage = info.GetString("zlibErrorMessage");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
	{
		base.GetObjectData(si, context);
		si.AddValue("zlibErrorContext", _zlibErrorContext);
		si.AddValue("zlibErrorCode", (int)_zlibErrorCode);
		si.AddValue("zlibErrorMessage", _zlibErrorMessage);
	}
}
