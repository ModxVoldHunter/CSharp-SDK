namespace System.Net.Sockets;

internal static class SocketAddressExtensions
{
	public static IPAddress GetIPAddress(this SocketAddress socketAddress)
	{
		return System.Net.Sockets.IPEndPointExtensions.GetIPAddress(socketAddress.Buffer.Span);
	}

	public static int GetPort(this SocketAddress socketAddress)
	{
		return System.Net.SocketAddressPal.GetPort(socketAddress.Buffer.Span);
	}

	public static bool Equals(this SocketAddress socketAddress, EndPoint endPoint)
	{
		if (socketAddress.Family == endPoint?.AddressFamily && endPoint is IPEndPoint endPoint2)
		{
			return System.Net.Sockets.IPEndPointExtensions.Equals(endPoint2, socketAddress.Buffer.Span);
		}
		return false;
	}
}
