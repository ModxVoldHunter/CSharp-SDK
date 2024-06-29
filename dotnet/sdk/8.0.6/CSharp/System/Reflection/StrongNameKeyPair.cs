using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace System.Reflection;

[Obsolete("Strong name signing is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0017", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public class StrongNameKeyPair : IDeserializationCallback, ISerializable
{
	public byte[] PublicKey
	{
		get
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
	}

	public StrongNameKeyPair(FileStream keyPairFile)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	public StrongNameKeyPair(byte[] keyPairArray)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected StrongNameKeyPair(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public StrongNameKeyPair(string keyPairContainer)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}
}
