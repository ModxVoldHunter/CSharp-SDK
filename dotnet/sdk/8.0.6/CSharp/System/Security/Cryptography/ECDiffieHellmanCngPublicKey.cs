using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey
{
	private readonly CngKeyBlobFormat _format;

	private readonly string _curveName;

	private bool _disposed;

	public CngKeyBlobFormat BlobFormat => _format;

	internal ECDiffieHellmanCngPublicKey(byte[] keyBlob, string curveName, CngKeyBlobFormat format)
		: base(keyBlob)
	{
		_format = format;
		_curveName = curveName;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disposed = true;
		}
		base.Dispose(disposing);
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override string ToXmlString()
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static ECDiffieHellmanCngPublicKey FromXmlString(string xml)
	{
		throw new PlatformNotSupportedException();
	}

	[SupportedOSPlatform("windows")]
	public static ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, CngKeyBlobFormat format)
	{
		ArgumentNullException.ThrowIfNull(publicKeyBlob, "publicKeyBlob");
		ArgumentNullException.ThrowIfNull(format, "format");
		using CngKey cngKey = CngKey.Import(publicKeyBlob, format);
		if (cngKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey);
		}
		return new ECDiffieHellmanCngPublicKey(publicKeyBlob, null, format);
	}

	internal static ECDiffieHellmanCngPublicKey FromKey(CngKey key)
	{
		CngKeyBlobFormat format;
		string curveName;
		byte[] keyBlob = ECCng.ExportKeyBlob(key, includePrivateParameters: false, out format, out curveName);
		return new ECDiffieHellmanCngPublicKey(keyBlob, curveName, format);
	}

	public CngKey Import()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return CngKey.Import(ToByteArray(), _curveName, BlobFormat);
	}

	public override ECParameters ExportExplicitParameters()
	{
		using CngKey key = Import();
		ECParameters ecParams = default(ECParameters);
		byte[] ecBlob = ECCng.ExportFullKeyBlob(key, includePrivateParameters: false);
		ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters: false);
		return ecParams;
	}

	public override ECParameters ExportParameters()
	{
		using CngKey cngKey = Import();
		ECParameters ecParams = default(ECParameters);
		string oidValue;
		string curveName = cngKey.GetCurveName(out oidValue);
		if (string.IsNullOrEmpty(curveName))
		{
			byte[] ecBlob = ECCng.ExportFullKeyBlob(cngKey, includePrivateParameters: false);
			ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters: false);
		}
		else
		{
			byte[] ecBlob2 = ECCng.ExportKeyBlob(cngKey, includePrivateParameters: false);
			ECCng.ExportNamedCurveParameters(ref ecParams, ecBlob2, includePrivateParameters: false);
			ecParams.Curve = ECCurve.CreateFromFriendlyName(curveName);
		}
		return ecParams;
	}
}
