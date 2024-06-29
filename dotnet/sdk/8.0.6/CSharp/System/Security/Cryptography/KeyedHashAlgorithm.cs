using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public abstract class KeyedHashAlgorithm : HashAlgorithm
{
	protected byte[] KeyValue;

	public virtual byte[] Key
	{
		get
		{
			return KeyValue.CloneByteArray();
		}
		set
		{
			KeyValue = value.CloneByteArray();
		}
	}

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public new static KeyedHashAlgorithm Create()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static KeyedHashAlgorithm? Create(string algName)
	{
		return CryptoConfigForwarder.CreateFromName<KeyedHashAlgorithm>(algName);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (KeyValue != null)
			{
				Array.Clear(KeyValue);
			}
			KeyValue = null;
		}
		base.Dispose(disposing);
	}
}
