using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

internal sealed class ServiceNameStore
{
	private readonly List<string> _serviceNames;

	private ServiceNameCollection _serviceNameCollection;

	public ServiceNameCollection ServiceNames => _serviceNameCollection ?? (_serviceNameCollection = new ServiceNameCollection(_serviceNames));

	public ServiceNameStore()
	{
		_serviceNames = new List<string>();
		_serviceNameCollection = null;
	}

	private static string NormalizeServiceName(string inputServiceName)
	{
		if (string.IsNullOrWhiteSpace(inputServiceName))
		{
			return inputServiceName;
		}
		int num = inputServiceName.IndexOf('/');
		if (num < 0)
		{
			return inputServiceName;
		}
		ReadOnlySpan<char> readOnlySpan = inputServiceName.AsSpan(0, num + 1);
		string text = inputServiceName.Substring(num + 1);
		if (string.IsNullOrWhiteSpace(text))
		{
			return inputServiceName;
		}
		string text2 = text;
		ReadOnlySpan<char> readOnlySpan2 = default(ReadOnlySpan<char>);
		ReadOnlySpan<char> readOnlySpan3 = default(ReadOnlySpan<char>);
		UriHostNameType uriHostNameType = Uri.CheckHostName(text);
		if (uriHostNameType == UriHostNameType.Unknown)
		{
			string text3 = text;
			int num2 = text.IndexOf('/');
			if (num2 >= 0)
			{
				text3 = text.Substring(0, num2);
				readOnlySpan3 = text.AsSpan(num2);
				text2 = text3;
			}
			int num3 = text3.LastIndexOf(':');
			if (num3 >= 0)
			{
				text2 = text3.Substring(0, num3);
				readOnlySpan2 = text3.AsSpan(num3 + 1);
				if (!ushort.TryParse(readOnlySpan2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
				{
					return inputServiceName;
				}
				readOnlySpan2 = text3.AsSpan(num3);
			}
			uriHostNameType = Uri.CheckHostName(text2);
		}
		if (uriHostNameType != UriHostNameType.Dns)
		{
			return inputServiceName;
		}
		if (!Uri.TryCreate(Uri.UriSchemeHttp + Uri.SchemeDelimiter + text2, UriKind.Absolute, out Uri result2))
		{
			return inputServiceName;
		}
		string components = result2.GetComponents(UriComponents.NormalizedHost, UriFormat.SafeUnescaped);
		string text4 = string.Concat(readOnlySpan, components, readOnlySpan2, readOnlySpan3);
		if (inputServiceName.Equals(text4, StringComparison.OrdinalIgnoreCase))
		{
			return inputServiceName;
		}
		return text4;
	}

	private bool AddSingleServiceName(string spn)
	{
		spn = NormalizeServiceName(spn);
		if (Contains(spn))
		{
			return false;
		}
		_serviceNames.Add(spn);
		return true;
	}

	public bool Add(string uriPrefix)
	{
		string[] array = BuildServiceNames(uriPrefix);
		bool flag = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (AddSingleServiceName(text))
			{
				flag = true;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_add, text, uriPrefix), "Add");
				}
			}
		}
		if (flag)
		{
			_serviceNameCollection = null;
		}
		else if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_not_add, uriPrefix), "Add");
		}
		return flag;
	}

	public bool Remove(string uriPrefix)
	{
		string inputServiceName = BuildSimpleServiceName(uriPrefix);
		inputServiceName = NormalizeServiceName(inputServiceName);
		bool flag = Contains(inputServiceName);
		if (flag)
		{
			_serviceNames.Remove(inputServiceName);
			_serviceNameCollection = null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			if (flag)
			{
				System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_remove, inputServiceName, uriPrefix), "Remove");
			}
			else
			{
				System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_not_remove, uriPrefix), "Remove");
			}
		}
		return flag;
	}

	private bool Contains(string newServiceName)
	{
		if (newServiceName == null)
		{
			return false;
		}
		foreach (string serviceName in _serviceNames)
		{
			if (serviceName.Equals(newServiceName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public void Clear()
	{
		_serviceNames.Clear();
		_serviceNameCollection = null;
	}

	private static string ExtractHostname(string uriPrefix, bool allowInvalidUriStrings)
	{
		if (Uri.IsWellFormedUriString(uriPrefix, UriKind.Absolute))
		{
			Uri uri = new Uri(uriPrefix);
			return uri.Host;
		}
		if (allowInvalidUriStrings)
		{
			int num = uriPrefix.IndexOf("://", StringComparison.Ordinal) + 3;
			int i = num;
			for (bool flag = false; i < uriPrefix.Length && uriPrefix[i] != '/' && (uriPrefix[i] != ':' || flag); i++)
			{
				if (uriPrefix[i] == '[')
				{
					if (flag)
					{
						i = num;
						break;
					}
					flag = true;
				}
				if (flag && uriPrefix[i] == ']')
				{
					flag = false;
				}
			}
			return uriPrefix.Substring(num, i - num);
		}
		return null;
	}

	public static string BuildSimpleServiceName(string uriPrefix)
	{
		string text = ExtractHostname(uriPrefix, allowInvalidUriStrings: false);
		if (text != null)
		{
			return "HTTP/" + text;
		}
		return null;
	}

	public static string[] BuildServiceNames(string uriPrefix)
	{
		string text = ExtractHostname(uriPrefix, allowInvalidUriStrings: true);
		if (text == "*" || text == "+" || IPAddress.TryParse(text, out IPAddress _))
		{
			try
			{
				string hostName = Dns.GetHostEntry(string.Empty).HostName;
				return new string[1] { "HTTP/" + hostName };
			}
			catch (SocketException)
			{
				return Array.Empty<string>();
			}
			catch (SecurityException)
			{
				return Array.Empty<string>();
			}
		}
		if (!text.Contains('.'))
		{
			try
			{
				string hostName2 = Dns.GetHostEntry(text).HostName;
				return new string[2]
				{
					"HTTP/" + text,
					"HTTP/" + hostName2
				};
			}
			catch (SocketException)
			{
				return new string[1] { "HTTP/" + text };
			}
			catch (SecurityException)
			{
				return new string[1] { "HTTP/" + text };
			}
		}
		return new string[1] { "HTTP/" + text };
	}
}
