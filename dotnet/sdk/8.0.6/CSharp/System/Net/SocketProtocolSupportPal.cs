using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net;

internal static class SocketProtocolSupportPal
{
	public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6) && !IsIPv6Disabled();


	public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);


	public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);


	private static bool IsIPv6Disabled()
	{
		if (AppContext.TryGetSwitch("System.Net.DisableIPv6", out var isEnabled))
		{
			return isEnabled;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6");
		if (environmentVariable != null)
		{
			if (!(environmentVariable == "1"))
			{
				return environmentVariable.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static bool IsSupported(AddressFamily af)
	{
		global::Interop.Winsock.EnsureInitialized();
		nint num = -1;
		nint num2 = num;
		try
		{
			num2 = global::Interop.Winsock.WSASocketW(af, 1, 0, IntPtr.Zero, 0, 128);
			return num2 != num || Marshal.GetLastPInvokeError() != 10047;
		}
		finally
		{
			if (num2 != num)
			{
				global::Interop.Winsock.closesocket(num2);
			}
		}
	}
}
