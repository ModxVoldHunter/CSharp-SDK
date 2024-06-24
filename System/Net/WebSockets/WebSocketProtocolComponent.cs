using System.Runtime.InteropServices;

namespace System.Net.WebSockets;

internal static class WebSocketProtocolComponent
{
	internal enum Action
	{
		NoAction,
		SendToNetwork,
		IndicateSendComplete,
		ReceiveFromNetwork,
		IndicateReceiveComplete
	}

	internal enum BufferType : uint
	{
		None = 0u,
		UTF8Message = 2147483648u,
		UTF8Fragment = 2147483649u,
		BinaryMessage = 2147483650u,
		BinaryFragment = 2147483651u,
		Close = 2147483652u,
		PingPong = 2147483653u,
		UnsolicitedPong = 2147483654u
	}

	internal enum PropertyType
	{
		ReceiveBufferSize,
		SendBufferSize,
		DisableMasking,
		AllocatedBuffer,
		DisableUtf8Verification,
		KeepAliveInterval
	}

	internal enum ActionQueue
	{
		Send = 1,
		Receive
	}

	private static readonly nint s_webSocketDllHandle;

	private static readonly string s_supportedVersion;

	private static readonly global::Interop.WebSocket.HttpHeader[] s_initialClientRequestHeaders;

	private static readonly global::Interop.WebSocket.HttpHeader[] s_serverFakeRequestHeaders;

	internal static string SupportedVersion
	{
		get
		{
			if (!IsSupported)
			{
				HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
			}
			return s_supportedVersion;
		}
	}

	internal static bool IsSupported => s_webSocketDllHandle != IntPtr.Zero;

	static WebSocketProtocolComponent()
	{
		s_initialClientRequestHeaders = new global::Interop.WebSocket.HttpHeader[2]
		{
			new global::Interop.WebSocket.HttpHeader
			{
				Name = "Connection",
				Value = "Upgrade"
			},
			new global::Interop.WebSocket.HttpHeader
			{
				Name = "Upgrade",
				Value = "websocket"
			}
		};
		s_webSocketDllHandle = global::Interop.Kernel32.LoadLibraryEx("websocket.dll", IntPtr.Zero, 0);
		if (s_webSocketDllHandle != IntPtr.Zero)
		{
			s_supportedVersion = GetSupportedVersion();
			s_serverFakeRequestHeaders = new global::Interop.WebSocket.HttpHeader[5]
			{
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Connection",
					Value = "Upgrade"
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Upgrade",
					Value = "websocket"
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Host",
					Value = string.Empty
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Sec-WebSocket-Version",
					Value = s_supportedVersion
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Sec-WebSocket-Key",
					Value = "AAAAAAAAAAAAAAAAAAAAAA=="
				}
			};
		}
	}

	internal unsafe static string GetSupportedVersion()
	{
		if (!IsSupported)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		SafeWebSocketHandle webSocketHandle = null;
		try
		{
			int errorCode = global::Interop.WebSocket.WebSocketCreateClientHandle(null, 0u, out webSocketHandle);
			ThrowOnError(errorCode);
			if (webSocketHandle == null || webSocketHandle.IsInvalid)
			{
				HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
			}
			errorCode = global::Interop.WebSocket.WebSocketBeginClientHandshake(webSocketHandle, IntPtr.Zero, 0u, IntPtr.Zero, 0u, s_initialClientRequestHeaders, (uint)s_initialClientRequestHeaders.Length, out var additionalHeadersPtr, out var additionalHeaderCount);
			ThrowOnError(errorCode);
			string result = null;
			for (uint num = 0u; num < additionalHeaderCount; num++)
			{
				global::Interop.WebSocket.HttpHeader httpHeader = MarshalAndVerifyHttpHeader(additionalHeadersPtr + num);
				if (string.Equals(httpHeader.Name, "Sec-WebSocket-Version", StringComparison.OrdinalIgnoreCase))
				{
					result = httpHeader.Value;
					break;
				}
			}
			return result;
		}
		finally
		{
			webSocketHandle?.Dispose();
		}
	}

	internal static SafeWebSocketHandle WebSocketCreateServerHandle(global::Interop.WebSocket.Property[] properties, int propertyCount)
	{
		if (!IsSupported)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		SafeWebSocketHandle webSocketHandle = null;
		try
		{
			int errorCode = global::Interop.WebSocket.WebSocketCreateServerHandle(properties, (uint)propertyCount, out webSocketHandle);
			ThrowOnError(errorCode);
			if (webSocketHandle.IsInvalid)
			{
				HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
			}
			errorCode = global::Interop.WebSocket.WebSocketBeginServerHandshake(webSocketHandle, IntPtr.Zero, IntPtr.Zero, 0u, s_serverFakeRequestHeaders, (uint)s_serverFakeRequestHeaders.Length, out var _, out var _);
			ThrowOnError(errorCode);
			errorCode = global::Interop.WebSocket.WebSocketEndServerHandshake(webSocketHandle);
			ThrowOnError(errorCode);
			return webSocketHandle;
		}
		catch
		{
			webSocketHandle?.Dispose();
			throw;
		}
	}

	internal static void WebSocketAbortHandle(SafeHandle webSocketHandle)
	{
		global::Interop.WebSocket.WebSocketAbortHandle(webSocketHandle);
		DrainActionQueue(webSocketHandle, ActionQueue.Send);
		DrainActionQueue(webSocketHandle, ActionQueue.Receive);
	}

	internal static void WebSocketDeleteHandle(nint webSocketPtr)
	{
		global::Interop.WebSocket.WebSocketDeleteHandle(webSocketPtr);
	}

	internal static void WebSocketSend(WebSocketBase webSocket, BufferType bufferType, global::Interop.WebSocket.Buffer buffer)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketSend_Raw(webSocket.SessionHandle, bufferType, ref buffer, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketSendWithoutBody(WebSocketBase webSocket, BufferType bufferType)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketSendWithoutBody_Raw(webSocket.SessionHandle, bufferType, IntPtr.Zero, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketReceive(WebSocketBase webSocket)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketReceive(webSocket.SessionHandle, IntPtr.Zero, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketGetAction(WebSocketBase webSocket, ActionQueue actionQueue, global::Interop.WebSocket.Buffer[] dataBuffers, ref uint dataBufferCount, out Action action, out BufferType bufferType, out nint actionContext)
	{
		action = Action.NoAction;
		bufferType = BufferType.None;
		actionContext = IntPtr.Zero;
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketGetAction(webSocket.SessionHandle, actionQueue, dataBuffers, ref dataBufferCount, out action, out bufferType, out var _, out actionContext);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
		webSocket.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
	}

	internal static void WebSocketCompleteAction(WebSocketBase webSocket, nint actionContext, int bytesTransferred)
	{
		if (webSocket.SessionHandle.IsClosed)
		{
			return;
		}
		try
		{
			global::Interop.WebSocket.WebSocketCompleteAction(webSocket.SessionHandle, actionContext, (uint)bytesTransferred);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static void DrainActionQueue(SafeHandle webSocketHandle, ActionQueue actionQueue)
	{
		while (true)
		{
			global::Interop.WebSocket.Buffer[] dataBuffers = new global::Interop.WebSocket.Buffer[1];
			uint dataBufferCount = 1u;
			Action action;
			BufferType bufferType;
			nint applicationContext;
			nint actionContext;
			int hr = global::Interop.WebSocket.WebSocketGetAction(webSocketHandle, actionQueue, dataBuffers, ref dataBufferCount, out action, out bufferType, out applicationContext, out actionContext);
			if (!Succeeded(hr) || action == Action.NoAction)
			{
				break;
			}
			global::Interop.WebSocket.WebSocketCompleteAction(webSocketHandle, actionContext, 0u);
		}
	}

	private unsafe static global::Interop.WebSocket.HttpHeader MarshalAndVerifyHttpHeader(global::Interop.WebSocket.WEB_SOCKET_HTTP_HEADER* httpHeaderPtr)
	{
		global::Interop.WebSocket.HttpHeader result = default(global::Interop.WebSocket.HttpHeader);
		nint name = httpHeaderPtr->Name;
		int nameLength = (int)httpHeaderPtr->NameLength;
		if (name != IntPtr.Zero)
		{
			result.Name = Marshal.PtrToStringAnsi(name, nameLength);
		}
		if ((result.Name == null && nameLength != 0) || (result.Name != null && nameLength != result.Name.Length))
		{
			throw new AccessViolationException();
		}
		nint value = httpHeaderPtr->Value;
		nameLength = (int)httpHeaderPtr->ValueLength;
		result.Value = Marshal.PtrToStringAnsi(value, nameLength);
		if ((result.Value == null && nameLength != 0) || (result.Value != null && nameLength != result.Value.Length))
		{
			throw new AccessViolationException();
		}
		return result;
	}

	public static bool Succeeded(int hr)
	{
		return hr >= 0;
	}

	private static void ThrowOnError(int errorCode)
	{
		if (Succeeded(errorCode))
		{
			return;
		}
		throw new WebSocketException(errorCode);
	}

	private static void ThrowIfSessionHandleClosed(WebSocketBase webSocket)
	{
		if (webSocket.SessionHandle.IsClosed)
		{
			throw new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State));
		}
	}

	private static WebSocketException ConvertObjectDisposedException(WebSocketBase webSocket, ObjectDisposedException innerException)
	{
		return new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State), innerException);
	}
}
