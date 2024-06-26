using System.Net.Quic;

namespace System.Net.Http;

public sealed class HttpProtocolException : HttpIOException
{
	public long ErrorCode { get; }

	public HttpProtocolException(long errorCode, string message, Exception? innerException)
		: base(HttpRequestError.HttpProtocolError, message, innerException)
	{
		ErrorCode = errorCode;
	}

	internal static HttpProtocolException CreateHttp2StreamException(Http2ProtocolErrorCode protocolError)
	{
		string net_http_http2_stream_error = System.SR.net_http_http2_stream_error;
		string name = GetName(protocolError);
		int num = (int)protocolError;
		string message = System.SR.Format(net_http_http2_stream_error, name, num.ToString("x"));
		return new HttpProtocolException((long)protocolError, message, null);
	}

	internal static HttpProtocolException CreateHttp2ConnectionException(Http2ProtocolErrorCode protocolError, string message = null)
	{
		string resourceFormat = message ?? System.SR.net_http_http2_connection_error;
		string name = GetName(protocolError);
		int num = (int)protocolError;
		message = System.SR.Format(resourceFormat, name, num.ToString("x"));
		return new HttpProtocolException((long)protocolError, message, null);
	}

	internal static HttpProtocolException CreateHttp3StreamException(Http3ErrorCode protocolError, QuicException innerException)
	{
		string message = System.SR.Format(System.SR.net_http_http3_stream_error, GetName(protocolError), ((int)protocolError).ToString("x"));
		return new HttpProtocolException((long)protocolError, message, innerException);
	}

	internal static HttpProtocolException CreateHttp3ConnectionException(Http3ErrorCode protocolError, string message = null)
	{
		message = System.SR.Format(message ?? System.SR.net_http_http3_connection_error, GetName(protocolError), ((int)protocolError).ToString("x"));
		return new HttpProtocolException((long)protocolError, message, null);
	}

	private static string GetName(Http2ProtocolErrorCode code)
	{
		return code switch
		{
			Http2ProtocolErrorCode.NoError => "NO_ERROR", 
			Http2ProtocolErrorCode.ProtocolError => "PROTOCOL_ERROR", 
			Http2ProtocolErrorCode.InternalError => "INTERNAL_ERROR", 
			Http2ProtocolErrorCode.FlowControlError => "FLOW_CONTROL_ERROR", 
			Http2ProtocolErrorCode.SettingsTimeout => "SETTINGS_TIMEOUT", 
			Http2ProtocolErrorCode.StreamClosed => "STREAM_CLOSED", 
			Http2ProtocolErrorCode.FrameSizeError => "FRAME_SIZE_ERROR", 
			Http2ProtocolErrorCode.RefusedStream => "REFUSED_STREAM", 
			Http2ProtocolErrorCode.Cancel => "CANCEL", 
			Http2ProtocolErrorCode.CompressionError => "COMPRESSION_ERROR", 
			Http2ProtocolErrorCode.ConnectError => "CONNECT_ERROR", 
			Http2ProtocolErrorCode.EnhanceYourCalm => "ENHANCE_YOUR_CALM", 
			Http2ProtocolErrorCode.InadequateSecurity => "INADEQUATE_SECURITY", 
			Http2ProtocolErrorCode.Http11Required => "HTTP_1_1_REQUIRED", 
			_ => "(unknown error)", 
		};
	}

	private static string GetName(Http3ErrorCode code)
	{
		Http3ErrorCode num = code - 256;
		if ((ulong)num <= 16uL)
		{
			switch (num)
			{
			case (Http3ErrorCode)0L:
				return "H3_NO_ERROR";
			case (Http3ErrorCode)1L:
				return "H3_GENERAL_PROTOCOL_ERROR";
			case (Http3ErrorCode)2L:
				return "H3_INTERNAL_ERROR";
			case (Http3ErrorCode)3L:
				return "H3_STREAM_CREATION_ERROR";
			case (Http3ErrorCode)4L:
				return "H3_CLOSED_CRITICAL_STREAM";
			case (Http3ErrorCode)5L:
				return "H3_FRAME_UNEXPECTED";
			case (Http3ErrorCode)6L:
				return "H3_FRAME_ERROR";
			case (Http3ErrorCode)7L:
				return "H3_EXCESSIVE_LOAD";
			case (Http3ErrorCode)8L:
				return "H3_ID_ERROR";
			case (Http3ErrorCode)9L:
				return "H3_SETTINGS_ERROR";
			case (Http3ErrorCode)10L:
				return "H3_MISSING_SETTINGS";
			case (Http3ErrorCode)11L:
				return "H3_REQUEST_REJECTED";
			case (Http3ErrorCode)12L:
				return "H3_REQUEST_CANCELLED";
			case (Http3ErrorCode)13L:
				return "H3_REQUEST_INCOMPLETE";
			case (Http3ErrorCode)15L:
				return "H3_CONNECT_ERROR";
			case (Http3ErrorCode)16L:
				return "H3_VERSION_FALLBACK";
			}
		}
		return "(unknown error)";
	}
}
