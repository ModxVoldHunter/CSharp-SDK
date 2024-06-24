using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace System.Xml;

public abstract class XmlResolver
{
	private sealed class XmlFileSystemResolver : XmlResolver
	{
		internal static readonly XmlFileSystemResolver s_singleton = new XmlFileSystemResolver();

		private XmlFileSystemResolver()
		{
		}

		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			if (((object)ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object)) && absoluteUri.Scheme == "file")
			{
				return new FileStream(absoluteUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
			}
			throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
		}

		public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			if ((ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object)) && absoluteUri.Scheme == "file")
			{
				return Task.FromResult((object)new FileStream(absoluteUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: true));
			}
			throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
		}
	}

	private sealed class XmlThrowingResolver : XmlResolver
	{
		internal static readonly XmlThrowingResolver s_singleton = new XmlThrowingResolver();

		public override ICredentials Credentials
		{
			set
			{
			}
		}

		private XmlThrowingResolver()
		{
		}

		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			throw new XmlException(System.SR.Format(System.SR.Xml_NullResolver, absoluteUri));
		}

		public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			throw new XmlException(System.SR.Format(System.SR.Xml_NullResolver, absoluteUri));
		}
	}

	public virtual ICredentials Credentials
	{
		set
		{
		}
	}

	public static XmlResolver FileSystemResolver => XmlFileSystemResolver.s_singleton;

	public static XmlResolver ThrowingResolver => XmlThrowingResolver.s_singleton;

	public abstract object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn);

	public virtual Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		throw new NotImplementedException();
	}

	public virtual Uri ResolveUri(Uri? baseUri, string? relativeUri)
	{
		if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
		{
			Uri uri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);
			if (!uri.IsAbsoluteUri && uri.OriginalString.Length > 0)
			{
				uri = new Uri(Path.GetFullPath(relativeUri));
			}
			return uri;
		}
		if (string.IsNullOrEmpty(relativeUri))
		{
			return baseUri;
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new NotSupportedException(System.SR.Xml_RelativeUriNotSupported);
		}
		return new Uri(baseUri, relativeUri);
	}

	public virtual bool SupportsType(Uri absoluteUri, Type? type)
	{
		ArgumentNullException.ThrowIfNull(absoluteUri, "absoluteUri");
		if (type == null || type == typeof(Stream))
		{
			return true;
		}
		return false;
	}
}
