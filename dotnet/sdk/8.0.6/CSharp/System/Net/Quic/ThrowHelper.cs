using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using Microsoft.Quic;

namespace System.Net.Quic;

internal static class ThrowHelper
{
	internal static QuicException GetConnectionAbortedException(long errorCode)
	{
		return new QuicException(QuicError.ConnectionAborted, errorCode, System.SR.Format(System.SR.net_quic_connectionaborted, errorCode));
	}

	internal static QuicException GetStreamAbortedException(long errorCode)
	{
		return new QuicException(QuicError.StreamAborted, errorCode, System.SR.Format(System.SR.net_quic_streamaborted, errorCode));
	}

	internal static QuicException GetOperationAbortedException(string message = null)
	{
		return new QuicException(QuicError.OperationAborted, null, message ?? System.SR.net_quic_operationaborted);
	}

	internal static bool TryGetStreamExceptionForMsQuicStatus(int status, [NotNullWhen(true)] out Exception exception)
	{
		if (status == MsQuic.QUIC_STATUS_ABORTED)
		{
			exception = null;
			return false;
		}
		if (status == MsQuic.QUIC_STATUS_INVALID_STATE)
		{
			exception = GetOperationAbortedException();
			return true;
		}
		if (MsQuic.StatusFailed(status))
		{
			exception = GetExceptionForMsQuicStatus(status);
			return true;
		}
		exception = null;
		return false;
	}

	internal static Exception GetExceptionForMsQuicStatus(int status, long? errorCode = null, string message = null)
	{
		Exception ex = GetExceptionInternal(status, errorCode, message);
		if (status != 0)
		{
			ex.HResult = status;
		}
		return ex;
		static Exception GetExceptionInternal(int status, long? errorCode, string message)
		{
			if (status == MsQuic.QUIC_STATUS_CONNECTION_REFUSED)
			{
				return new QuicException(QuicError.ConnectionRefused, null, errorCode, System.SR.net_quic_connection_refused);
			}
			if (status == MsQuic.QUIC_STATUS_CONNECTION_TIMEOUT)
			{
				return new QuicException(QuicError.ConnectionTimeout, null, errorCode, System.SR.net_quic_timeout);
			}
			if (status == MsQuic.QUIC_STATUS_VER_NEG_ERROR)
			{
				return new QuicException(QuicError.VersionNegotiationError, null, errorCode, System.SR.net_quic_ver_neg_error);
			}
			if (status == MsQuic.QUIC_STATUS_CONNECTION_IDLE)
			{
				return new QuicException(QuicError.ConnectionIdle, null, errorCode, System.SR.net_quic_connection_idle);
			}
			if (status == MsQuic.QUIC_STATUS_PROTOCOL_ERROR)
			{
				return new QuicException(QuicError.TransportError, null, errorCode, System.SR.net_quic_protocol_error);
			}
			if (status == MsQuic.QUIC_STATUS_ALPN_IN_USE)
			{
				return new QuicException(QuicError.AlpnInUse, null, errorCode, System.SR.net_quic_protocol_error);
			}
			if (status == MsQuic.QUIC_STATUS_INVALID_ADDRESS)
			{
				return new SocketException(10049);
			}
			if (status == MsQuic.QUIC_STATUS_ADDRESS_IN_USE)
			{
				return new SocketException(10048);
			}
			if (status == MsQuic.QUIC_STATUS_UNREACHABLE)
			{
				return new SocketException(10065);
			}
			if (status == MsQuic.QUIC_STATUS_ADDRESS_NOT_AVAILABLE)
			{
				return new SocketException(10047);
			}
			if (status == MsQuic.QUIC_STATUS_TLS_ERROR || status == MsQuic.QUIC_STATUS_CERT_EXPIRED || status == MsQuic.QUIC_STATUS_CERT_UNTRUSTED_ROOT || status == MsQuic.QUIC_STATUS_CERT_NO_CERT)
			{
				return new AuthenticationException(System.SR.Format(System.SR.net_quic_auth, GetErrorMessageForStatus(status, message)));
			}
			if (status == MsQuic.QUIC_STATUS_ALPN_NEG_FAILURE)
			{
				return new AuthenticationException(System.SR.net_quic_alpn_neg_error);
			}
			if (status == MsQuic.QUIC_STATUS_USER_CANCELED)
			{
				return new AuthenticationException(System.SR.Format(System.SR.net_auth_tls_alert, System.Net.Security.TlsAlertMessage.UserCanceled));
			}
			if ((uint)status >= (uint)MsQuic.QUIC_STATUS_CLOSE_NOTIFY && (uint)status < (uint)(MsQuic.QUIC_STATUS_CLOSE_NOTIFY + 256))
			{
				System.Net.Security.TlsAlertMessage tlsAlertMessage = (System.Net.Security.TlsAlertMessage)(status - MsQuic.QUIC_STATUS_CLOSE_NOTIFY);
				return new AuthenticationException(System.SR.Format(System.SR.net_auth_tls_alert, tlsAlertMessage));
			}
			return new QuicException(QuicError.InternalError, null, System.SR.Format(System.SR.net_quic_internal_error, GetErrorMessageForStatus(status, message)));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfMsQuicError(int status, string message = null)
	{
		if (MsQuic.StatusFailed(status))
		{
			ThrowMsQuicException(status, message);
		}
	}

	internal static void ThrowMsQuicException(int status, string message = null)
	{
		throw GetExceptionForMsQuicStatus(status, null, message);
	}

	internal static string GetErrorMessageForStatus(int status, string message)
	{
		return (message ?? "Status code") + ": " + GetErrorMessageForStatus(status);
	}

	internal static string GetErrorMessageForStatus(int status)
	{
		if (status == MsQuic.QUIC_STATUS_SUCCESS)
		{
			return "QUIC_STATUS_SUCCESS";
		}
		if (status == MsQuic.QUIC_STATUS_PENDING)
		{
			return "QUIC_STATUS_PENDING";
		}
		if (status == MsQuic.QUIC_STATUS_CONTINUE)
		{
			return "QUIC_STATUS_CONTINUE";
		}
		if (status == MsQuic.QUIC_STATUS_OUT_OF_MEMORY)
		{
			return "QUIC_STATUS_OUT_OF_MEMORY";
		}
		if (status == MsQuic.QUIC_STATUS_INVALID_PARAMETER)
		{
			return "QUIC_STATUS_INVALID_PARAMETER";
		}
		if (status == MsQuic.QUIC_STATUS_INVALID_STATE)
		{
			return "QUIC_STATUS_INVALID_STATE";
		}
		if (status == MsQuic.QUIC_STATUS_NOT_SUPPORTED)
		{
			return "QUIC_STATUS_NOT_SUPPORTED";
		}
		if (status == MsQuic.QUIC_STATUS_NOT_FOUND)
		{
			return "QUIC_STATUS_NOT_FOUND";
		}
		if (status == MsQuic.QUIC_STATUS_BUFFER_TOO_SMALL)
		{
			return "QUIC_STATUS_BUFFER_TOO_SMALL";
		}
		if (status == MsQuic.QUIC_STATUS_HANDSHAKE_FAILURE)
		{
			return "QUIC_STATUS_HANDSHAKE_FAILURE";
		}
		if (status == MsQuic.QUIC_STATUS_ABORTED)
		{
			return "QUIC_STATUS_ABORTED";
		}
		if (status == MsQuic.QUIC_STATUS_ADDRESS_IN_USE)
		{
			return "QUIC_STATUS_ADDRESS_IN_USE";
		}
		if (status == MsQuic.QUIC_STATUS_INVALID_ADDRESS)
		{
			return "QUIC_STATUS_INVALID_ADDRESS";
		}
		if (status == MsQuic.QUIC_STATUS_CONNECTION_TIMEOUT)
		{
			return "QUIC_STATUS_CONNECTION_TIMEOUT";
		}
		if (status == MsQuic.QUIC_STATUS_CONNECTION_IDLE)
		{
			return "QUIC_STATUS_CONNECTION_IDLE";
		}
		if (status == MsQuic.QUIC_STATUS_UNREACHABLE)
		{
			return "QUIC_STATUS_UNREACHABLE";
		}
		if (status == MsQuic.QUIC_STATUS_INTERNAL_ERROR)
		{
			return "QUIC_STATUS_INTERNAL_ERROR";
		}
		if (status == MsQuic.QUIC_STATUS_CONNECTION_REFUSED)
		{
			return "QUIC_STATUS_CONNECTION_REFUSED";
		}
		if (status == MsQuic.QUIC_STATUS_PROTOCOL_ERROR)
		{
			return "QUIC_STATUS_PROTOCOL_ERROR";
		}
		if (status == MsQuic.QUIC_STATUS_VER_NEG_ERROR)
		{
			return "QUIC_STATUS_VER_NEG_ERROR";
		}
		if (status == MsQuic.QUIC_STATUS_TLS_ERROR)
		{
			return "QUIC_STATUS_TLS_ERROR";
		}
		if (status == MsQuic.QUIC_STATUS_USER_CANCELED)
		{
			return "QUIC_STATUS_USER_CANCELED";
		}
		if (status == MsQuic.QUIC_STATUS_ALPN_NEG_FAILURE)
		{
			return "QUIC_STATUS_ALPN_NEG_FAILURE";
		}
		if (status == MsQuic.QUIC_STATUS_STREAM_LIMIT_REACHED)
		{
			return "QUIC_STATUS_STREAM_LIMIT_REACHED";
		}
		if (status == MsQuic.QUIC_STATUS_ALPN_IN_USE)
		{
			return "QUIC_STATUS_ALPN_IN_USE";
		}
		if (status == MsQuic.QUIC_STATUS_CLOSE_NOTIFY)
		{
			return "QUIC_STATUS_CLOSE_NOTIFY";
		}
		if (status == MsQuic.QUIC_STATUS_BAD_CERTIFICATE)
		{
			return "QUIC_STATUS_BAD_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_UNSUPPORTED_CERTIFICATE)
		{
			return "QUIC_STATUS_UNSUPPORTED_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_REVOKED_CERTIFICATE)
		{
			return "QUIC_STATUS_REVOKED_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_EXPIRED_CERTIFICATE)
		{
			return "QUIC_STATUS_EXPIRED_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_UNKNOWN_CERTIFICATE)
		{
			return "QUIC_STATUS_UNKNOWN_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_REQUIRED_CERTIFICATE)
		{
			return "QUIC_STATUS_REQUIRED_CERTIFICATE";
		}
		if (status == MsQuic.QUIC_STATUS_CERT_EXPIRED)
		{
			return "QUIC_STATUS_CERT_EXPIRED";
		}
		if (status == MsQuic.QUIC_STATUS_CERT_UNTRUSTED_ROOT)
		{
			return "QUIC_STATUS_CERT_UNTRUSTED_ROOT";
		}
		if (status == MsQuic.QUIC_STATUS_CERT_NO_CERT)
		{
			return "QUIC_STATUS_CERT_NO_CERT";
		}
		return $"Unknown (0x{status:x})";
	}
}
