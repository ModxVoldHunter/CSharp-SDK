using System.Formats.Asn1;

namespace System.Security.Cryptography;

public abstract class ECDiffieHellmanPublicKey : IDisposable
{
	private readonly byte[] _keyBlob;

	protected ECDiffieHellmanPublicKey()
	{
		_keyBlob = Array.Empty<byte>();
	}

	[Obsolete("ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead.", DiagnosticId = "SYSLIB0043", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected ECDiffieHellmanPublicKey(byte[] keyBlob)
	{
		ArgumentNullException.ThrowIfNull(keyBlob, "keyBlob");
		_keyBlob = (byte[])keyBlob.Clone();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	[Obsolete("ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead.", DiagnosticId = "SYSLIB0043", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual byte[] ToByteArray()
	{
		return (byte[])_keyBlob.Clone();
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual string ToXmlString()
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportParameters()
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportExplicitParameters()
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters();
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public virtual byte[] ExportSubjectPublicKeyInfo()
	{
		ECParameters ecParameters = ExportParameters();
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.Encode();
	}
}
