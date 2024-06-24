using System.Runtime.InteropServices;

namespace System.Net.Sockets;

internal static class SocketExceptionFactory
{
	private static string CreateMessage(int nativeSocketError, EndPoint endPoint)
	{
		return Marshal.GetPInvokeErrorMessage(nativeSocketError) + " " + endPoint.ToString();
	}

	public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
	{
		return new SocketException(socketError, CreateMessage(socketError, endPoint));
	}
}
