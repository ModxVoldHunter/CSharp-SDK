using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Xml;

public class XmlUrlResolver : XmlResolver
{
	private ICredentials _credentials;

	private IWebProxy _proxy;

	[UnsupportedOSPlatform("browser")]
	public override ICredentials? Credentials
	{
		set
		{
			_credentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public IWebProxy? Proxy
	{
		set
		{
			_proxy = value;
		}
	}

	public RequestCachePolicy CachePolicy
	{
		set
		{
		}
	}

	public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if ((object)ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return XmlDownloadManager.GetStream(absoluteUri, _credentials, _proxy);
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
	}

	public override async Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return await XmlDownloadManager.GetStreamAsync(absoluteUri, _credentials, _proxy).ConfigureAwait(continueOnCapturedContext: false);
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
	}

	public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
	{
		return base.ResolveUri(baseUri, relativeUri);
	}
}
