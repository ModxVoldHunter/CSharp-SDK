using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class HttpListenerException : Win32Exception
{
	public override int ErrorCode => base.NativeErrorCode;

	public HttpListenerException()
		: base(Marshal.GetLastPInvokeError())
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public HttpListenerException(int errorCode)
		: base(errorCode)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public HttpListenerException(int errorCode, string message)
		: base(errorCode, message)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected HttpListenerException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}
}
