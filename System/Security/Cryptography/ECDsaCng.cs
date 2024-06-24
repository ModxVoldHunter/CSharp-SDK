using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class ECDsaCng : ECDsa, IRuntimeAlgorithm
{
	private CngAlgorithmCore _core = new CngAlgorithmCore(typeof(ECDsaCng));

	private CngAlgorithm _hashAlgorithm = CngAlgorithm.Sha256;

	public override int KeySize
	{
		get
		{
			return base.KeySize;
		}
		set
		{
			if (KeySize != value)
			{
				base.KeySize = value;
				DisposeKey();
			}
		}
	}

	public override KeySizes[] LegalKeySizes => ECAlgorithm.s_defaultKeySizes.CloneKeySizesArray();

	public CngAlgorithm HashAlgorithm
	{
		get
		{
			return _hashAlgorithm;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_hashAlgorithm = value;
		}
	}

	public CngKey Key
	{
		get
		{
			return GetKey();
		}
		private set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			if (!IsEccAlgorithmGroup(value.AlgorithmGroup))
			{
				throw new ArgumentException(System.SR.Cryptography_ArgECDsaRequiresECDsaKey, "value");
			}
			_core.SetKey(value);
			ForceSetKeySize(value.KeySize);
		}
	}

	[SupportedOSPlatform("windows")]
	public ECDsaCng(ECCurve curve)
	{
		try
		{
			GenerateKey(curve);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	[SupportedOSPlatform("windows")]
	public ECDsaCng()
		: this(521)
	{
	}

	[SupportedOSPlatform("windows")]
	public ECDsaCng(int keySize)
	{
		KeySize = keySize;
	}

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
	}

	public override void ImportParameters(ECParameters parameters)
	{
		parameters.Validate();
		ThrowIfDisposed();
		ECCurve curve = parameters.Curve;
		bool flag = parameters.D != null;
		bool flag2 = parameters.Q.X != null && parameters.Q.Y != null;
		if (curve.IsPrime)
		{
			if (!flag2 && flag)
			{
				byte[] array = new byte[parameters.D.Length];
				ECParameters parameters2 = parameters;
				parameters2.Q.X = array;
				parameters2.Q.Y = array;
				byte[] primeCurveBlob = ECCng.GetPrimeCurveBlob(ref parameters2, ecdh: false);
				ImportFullKeyBlob(primeCurveBlob, includePrivateParameters: true);
			}
			else
			{
				byte[] primeCurveBlob2 = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: false);
				ImportFullKeyBlob(primeCurveBlob2, flag);
			}
			return;
		}
		if (curve.IsNamed)
		{
			if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value.ToString()));
			}
			if (!flag2 && flag)
			{
				byte[] array2 = new byte[parameters.D.Length];
				ECParameters parameters3 = parameters;
				parameters3.Q.X = array2;
				parameters3.Q.Y = array2;
				byte[] namedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters3, ecdh: false);
				ImportKeyBlob(namedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
			}
			else
			{
				byte[] namedCurveBlob2 = ECCng.GetNamedCurveBlob(ref parameters, ecdh: false);
				ImportKeyBlob(namedCurveBlob2, curve.Oid.FriendlyName, flag);
			}
			return;
		}
		throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
	}

	public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		byte[] ecBlob = ExportFullKeyBlob(includePrivateParameters);
		ECParameters ecParams = default(ECParameters);
		ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
		return ecParams;
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		ECParameters ecParams = default(ECParameters);
		string oidValue;
		string curveName = GetCurveName(out oidValue);
		if (string.IsNullOrEmpty(curveName))
		{
			byte[] ecBlob = ExportFullKeyBlob(includePrivateParameters);
			ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
		}
		else
		{
			byte[] ecBlob2 = ExportKeyBlob(includePrivateParameters);
			ECCng.ExportNamedCurveParameters(ref ecParams, ecBlob2, includePrivateParameters);
			ecParams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
		}
		return ecParams;
	}

	public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		CngPkcs8.Pkcs8Response response = CngPkcs8.ImportPkcs8PrivateKey(source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	private void ProcessPkcs8Response(CngPkcs8.Pkcs8Response response)
	{
		string algorithmGroup = response.GetAlgorithmGroup();
		if (algorithmGroup == "ECDSA" || algorithmGroup == "ECDH")
		{
			AcceptImport(response);
			return;
		}
		response.FreeKey();
		throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		return CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters);
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		if (CngPkcs8.IsPlatformScheme(pbeParameters))
		{
			return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
		}
		return CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, password, pbeParameters);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		if (CngPkcs8.IsPlatformScheme(pbeParameters))
		{
			return TryExportEncryptedPkcs8(password, pbeParameters.IterationCount, destination, out bytesWritten);
		}
		return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, password, pbeParameters, destination, out bytesWritten);
	}

	public unsafe override byte[] SignHash(byte[] hash)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		int maxSignatureSize = GetMaxSignatureSize(DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, maxSignatureSize);
	}

	public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		return TrySignHashCore(source, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
	}

	protected unsafe override bool TrySignHashCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
		{
			if (!keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, out bytesWritten))
			{
				bytesWritten = 0;
				return false;
			}
		}
		return signatureFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => true, 
			DSASignatureFormat.Rfc3279DerSequence => AsymmetricAlgorithmHelpers.TryConvertIeee1363ToDer(destination.Slice(0, bytesWritten), destination, out bytesWritten), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownSignatureFormat, signatureFormat.ToString()), 
		};
	}

	public override bool VerifyHash(byte[] hash, byte[] signature)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	protected unsafe override bool VerifyHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		if (signatureFormat != 0)
		{
			signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
		}
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.None, null);
	}

	[SupportedOSPlatform("windows")]
	public ECDsaCng(CngKey key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDsaRequiresECDsaKey, "key");
		}
		Key = CngAlgorithmCore.Duplicate(key);
	}

	[SupportedOSPlatform("windows")]
	internal ECDsaCng(CngKey key, bool transferOwnership)
	{
		Key = key;
	}

	protected override void Dispose(bool disposing)
	{
		_core.Dispose();
	}

	private void ThrowIfDisposed()
	{
		_core.ThrowIfDisposed();
	}

	private void DisposeKey()
	{
		_core.DisposeKey();
	}

	private static bool IsEccAlgorithmGroup(CngAlgorithmGroup algorithmGroup)
	{
		if (!(algorithmGroup == CngAlgorithmGroup.ECDsa))
		{
			return algorithmGroup == CngAlgorithmGroup.ECDiffieHellman;
		}
		return true;
	}

	internal string GetCurveName(out string oidValue)
	{
		return Key.GetCurveName(out oidValue);
	}

	private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
	{
		CngKey cngKey = ECCng.ImportFullKeyBlob(ecfullKeyBlob, includePrivateParameters);
		try
		{
			Key = cngKey;
		}
		catch
		{
			cngKey.Dispose();
			throw;
		}
	}

	private void ImportKeyBlob(byte[] ecfullKeyBlob, string curveName, bool includePrivateParameters)
	{
		CngKey cngKey = ECCng.ImportKeyBlob(ecfullKeyBlob, curveName, includePrivateParameters);
		try
		{
			Key = cngKey;
		}
		catch
		{
			cngKey.Dispose();
			throw;
		}
	}

	private byte[] ExportKeyBlob(bool includePrivateParameters)
	{
		return ECCng.ExportKeyBlob(Key, includePrivateParameters);
	}

	private byte[] ExportFullKeyBlob(bool includePrivateParameters)
	{
		return ECCng.ExportFullKeyBlob(Key, includePrivateParameters);
	}

	private void AcceptImport(CngPkcs8.Pkcs8Response response)
	{
		try
		{
			Key = response.Key;
		}
		catch
		{
			response.FreeKey();
			throw;
		}
	}

	public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		return Key.TryExportKeyBlob("PKCS8_PRIVATEKEY", destination, out bytesWritten);
	}

	private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
	{
		return Key.ExportPkcs8KeyBlob(pkcs8Password, kdfCount);
	}

	private bool TryExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount, Span<byte> destination, out int bytesWritten)
	{
		return Key.TryExportPkcs8KeyBlob(pkcs8Password, kdfCount, destination, out bytesWritten);
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public void FromXmlString(string xml, ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}

	public byte[] SignData(byte[] data)
	{
		return SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public byte[] SignData(byte[] data, int offset, int count)
	{
		return SignData(data, offset, count, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public byte[] SignData(Stream data)
	{
		return SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public string ToXmlString(ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}

	public bool VerifyData(byte[] data, byte[] signature)
	{
		return VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public bool VerifyData(byte[] data, int offset, int count, byte[] signature)
	{
		return VerifyData(data, offset, count, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public bool VerifyData(Stream data, byte[] signature)
	{
		return VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public override void GenerateKey(ECCurve curve)
	{
		curve.Validate();
		_core.DisposeKey();
		if (curve.IsNamed)
		{
			if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
			}
			CngAlgorithm cngAlgorithm = CngKey.EcdsaCurveNameToAlgorithm(curve.Oid.FriendlyName);
			if (CngKey.IsECNamedCurve(cngAlgorithm.Algorithm))
			{
				CngKey orGenerateKey = _core.GetOrGenerateKey(curve);
				ForceSetKeySize(orGenerateKey.KeySize);
				return;
			}
			int num;
			if (cngAlgorithm == CngAlgorithm.ECDsaP256)
			{
				num = 256;
			}
			else if (cngAlgorithm == CngAlgorithm.ECDsaP384)
			{
				num = 384;
			}
			else
			{
				if (!(cngAlgorithm == CngAlgorithm.ECDsaP521))
				{
					throw new ArgumentException(System.SR.Cryptography_InvalidKeySize);
				}
				num = 521;
			}
			_core.GetOrGenerateKey(num, cngAlgorithm);
			ForceSetKeySize(num);
		}
		else
		{
			if (!curve.IsExplicit)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
			}
			CngKey orGenerateKey2 = _core.GetOrGenerateKey(curve);
			ForceSetKeySize(orGenerateKey2.KeySize);
		}
	}

	private CngKey GetKey()
	{
		if (_core.IsKeyGeneratedNamedCurve())
		{
			return _core.GetOrGenerateKey(null);
		}
		int keySize = KeySize;
		CngAlgorithm algorithm = keySize switch
		{
			256 => CngAlgorithm.ECDsaP256, 
			384 => CngAlgorithm.ECDsaP384, 
			521 => CngAlgorithm.ECDsaP521, 
			_ => throw new ArgumentException(System.SR.Cryptography_InvalidKeySize), 
		};
		return _core.GetOrGenerateKey(keySize, algorithm);
	}

	private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
	{
		return Key.Handle;
	}
}
