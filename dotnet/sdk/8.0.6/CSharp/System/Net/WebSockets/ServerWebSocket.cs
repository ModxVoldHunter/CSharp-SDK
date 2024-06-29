using System.IO;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets;

internal sealed class ServerWebSocket : WebSocketBase
{
	private readonly SafeHandle _sessionHandle;

	private readonly global::Interop.WebSocket.Property[] _properties;

	internal override SafeHandle SessionHandle => _sessionHandle;

	internal static WebSocket Create(Stream innerStream, string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
	{
		if (!WebSocketProtocolComponent.IsSupported)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		HttpWebSocket.ValidateInnerStream(innerStream);
		HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, 16, keepAliveInterval);
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(internalBuffer, "internalBuffer");
		WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, 16, isServerBuffer: true);
		return new ServerWebSocket(innerStream, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
	}

	public ServerWebSocket(Stream innerStream, string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
		: base(innerStream, subProtocol, keepAliveInterval, WebSocketBuffer.CreateServerBuffer(internalBuffer, receiveBufferSize))
	{
		_properties = base.InternalBuffer.CreateProperties(useZeroMaskingKey: false);
		SafeHandle safeHandle = CreateWebSocketHandle();
		if (safeHandle == null || safeHandle.IsInvalid)
		{
			safeHandle?.Dispose();
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		_sessionHandle = safeHandle;
		StartKeepAliveTimer();
	}

	private SafeWebSocketHandle CreateWebSocketHandle()
	{
		return WebSocketProtocolComponent.WebSocketCreateServerHandle(_properties, _properties.Length);
	}
}
