using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Net.Http;

internal sealed class HttpEnvironmentProxy : IWebProxy
{
	private readonly Uri _httpProxyUri;

	private readonly Uri _httpsProxyUri;

	private readonly string[] _bypass;

	private ICredentials _credentials;

	public ICredentials Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			_credentials = value;
		}
	}

	private HttpEnvironmentProxy(Uri httpProxy, Uri httpsProxy, string bypassList)
	{
		_httpProxyUri = httpProxy;
		_httpsProxyUri = httpsProxy;
		_credentials = HttpEnvironmentProxyCredentials.TryCreate(httpProxy, httpsProxy);
		_bypass = bypassList?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static Uri GetUriFromString(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		int num = 0;
		string scheme = "http";
		ushort result = 80;
		if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			num = 7;
		}
		else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			num = 8;
			scheme = "https";
			result = 443;
		}
		else if (value.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase))
		{
			num = 9;
			scheme = "socks4";
		}
		else if (value.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
		{
			num = 9;
			scheme = "socks5";
		}
		else if (value.StartsWith("socks4a://", StringComparison.OrdinalIgnoreCase))
		{
			num = 10;
			scheme = "socks4a";
		}
		if (num > 0)
		{
			value = value.Substring(num);
		}
		string text = null;
		string text2 = null;
		int num2 = value.LastIndexOf('@');
		if (num2 != -1)
		{
			string text3 = value.Substring(0, num2);
			try
			{
				text3 = Uri.UnescapeDataString(text3);
			}
			catch
			{
			}
			value = value.Substring(num2 + 1);
			num2 = text3.IndexOf(':');
			if (num2 == -1)
			{
				text = text3;
			}
			else
			{
				text = text3.Substring(0, num2);
				text2 = text3.Substring(num2 + 1);
			}
		}
		int num3 = value.IndexOf(']');
		num2 = value.LastIndexOf(':');
		string host;
		if (num2 == -1 || (num3 != -1 && num2 < num3))
		{
			host = value;
		}
		else
		{
			host = value.Substring(0, num2);
			int i;
			for (i = num2 + 1; i < value.Length && char.IsDigit(value[i]); i++)
			{
			}
			if (!ushort.TryParse(value.AsSpan(num2 + 1, i - num2 - 1), out result))
			{
				return null;
			}
		}
		try
		{
			UriBuilder uriBuilder = new UriBuilder(scheme, host, result);
			if (text != null)
			{
				uriBuilder.UserName = Uri.EscapeDataString(text);
			}
			if (text2 != null)
			{
				uriBuilder.Password = Uri.EscapeDataString(text2);
			}
			Uri uri = uriBuilder.Uri;
			if (text == "" && text2 == "")
			{
				Span<Range> destination = stackalloc Range[3];
				ReadOnlySpan<char> source = uri.ToString();
				if (source.Split(destination, '/') == 3)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 2);
					Range range = destination[0];
					defaultInterpolatedStringHandler.AppendFormatted(source[range.Start..range.End]);
					defaultInterpolatedStringHandler.AppendLiteral("//:@");
					range = destination[2];
					defaultInterpolatedStringHandler.AppendFormatted(source[range.Start..range.End]);
					uri = new Uri(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			return uri;
		}
		catch
		{
		}
		return null;
	}

	private bool IsMatchInBypassList(Uri input)
	{
		if (_bypass != null)
		{
			string[] bypass = _bypass;
			foreach (string text in bypass)
			{
				if (text[0] == '.')
				{
					if (text.Length - 1 == input.Host.Length && string.Compare(text, 1, input.Host, 0, input.Host.Length, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return true;
					}
					if (input.Host.EndsWith(text, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				else if (string.Equals(text, input.Host, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	public Uri GetProxy(Uri uri)
	{
		if (!HttpUtilities.IsSupportedNonSecureScheme(uri.Scheme))
		{
			return _httpsProxyUri;
		}
		return _httpProxyUri;
	}

	public bool IsBypassed(Uri uri)
	{
		if (!(GetProxy(uri) == null))
		{
			return IsMatchInBypassList(uri);
		}
		return true;
	}

	public static bool TryCreate([NotNullWhen(true)] out IWebProxy proxy)
	{
		Uri uri = null;
		if (Environment.GetEnvironmentVariable("GATEWAY_INTERFACE") == null)
		{
			uri = GetUriFromString(Environment.GetEnvironmentVariable("HTTP_PROXY"));
		}
		Uri uri2 = GetUriFromString(Environment.GetEnvironmentVariable("HTTPS_PROXY"));
		if (uri == null || uri2 == null)
		{
			Uri uriFromString = GetUriFromString(Environment.GetEnvironmentVariable("ALL_PROXY"));
			if ((object)uri == null)
			{
				uri = uriFromString;
			}
			if ((object)uri2 == null)
			{
				uri2 = uriFromString;
			}
		}
		if (uri == null && uri2 == null)
		{
			proxy = null;
			return false;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("NO_PROXY");
		proxy = new HttpEnvironmentProxy(uri, uri2, environmentVariable);
		return true;
	}
}
