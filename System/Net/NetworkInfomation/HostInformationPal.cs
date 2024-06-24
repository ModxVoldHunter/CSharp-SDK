using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation;

internal static class HostInformationPal
{
	private static string s_hostName;

	private static string s_domainName;

	private static uint s_nodeType;

	private static string s_scopeId;

	private static bool s_enableRouting;

	private static bool s_enableProxy;

	private static bool s_enableDns;

	private static volatile bool s_initialized;

	private static readonly object s_syncObject = new object();

	public static string GetDomainName()
	{
		EnsureInitialized();
		return s_domainName;
	}

	private static void EnsureInitialized()
	{
		if (!s_initialized)
		{
			Initialize();
		}
	}

	private unsafe static void Initialize()
	{
		lock (s_syncObject)
		{
			if (s_initialized)
			{
				return;
			}
			uint cb = 0u;
			uint networkParams = global::Interop.IpHlpApi.GetNetworkParams(IntPtr.Zero, &cb);
			while (true)
			{
				switch (networkParams)
				{
				case 111u:
				{
					nint num = Marshal.AllocHGlobal((int)cb);
					try
					{
						networkParams = global::Interop.IpHlpApi.GetNetworkParams(num, &cb);
						if (networkParams == 0)
						{
							global::Interop.IpHlpApi.FIXED_INFO* ptr = (global::Interop.IpHlpApi.FIXED_INFO*)num;
							s_hostName = ptr->HostName;
							s_domainName = ptr->DomainName;
							s_hostName = ptr->HostName;
							s_domainName = ptr->DomainName;
							s_nodeType = ptr->nodeType;
							s_scopeId = ptr->ScopeId;
							s_enableRouting = ptr->enableRouting != 0;
							s_enableProxy = ptr->enableProxy != 0;
							s_enableDns = ptr->enableDns != 0;
							s_initialized = true;
						}
					}
					finally
					{
						Marshal.FreeHGlobal(num);
					}
					break;
				}
				default:
					throw new Win32Exception((int)networkParams);
				case 0u:
					return;
				}
			}
		}
	}
}
