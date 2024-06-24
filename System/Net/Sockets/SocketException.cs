using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.Sockets;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SocketException : Win32Exception
{
	private readonly SocketError _errorCode;

	public override string Message => base.Message;

	public SocketError SocketErrorCode => _errorCode;

	public override int ErrorCode => base.NativeErrorCode;

	public SocketException(int errorCode)
		: this((SocketError)errorCode)
	{
	}

	public SocketException(int errorCode, string? message)
		: this((SocketError)errorCode, message)
	{
	}

	internal SocketException(SocketError socketError)
		: base(GetNativeErrorForSocketError(socketError))
	{
		_errorCode = socketError;
	}

	internal SocketException(SocketError socketError, string message)
		: base(GetNativeErrorForSocketError(socketError), message)
	{
		_errorCode = socketError;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected SocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public SocketException()
		: this(Marshal.GetLastPInvokeError())
	{
	}

	private static int GetNativeErrorForSocketError(SocketError error)
	{
		return (int)error;
	}
}
