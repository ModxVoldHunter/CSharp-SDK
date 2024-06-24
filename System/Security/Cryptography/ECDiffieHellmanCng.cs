using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class ECDiffieHellmanCng : ECDiffieHellman
{
	private CngAlgorithmCore _core = new CngAlgorithmCore(typeof(ECDiffieHellmanCng))
	{
		DefaultKeyType = CngAlgorithm.ECDiffieHellman
	};

	private CngAlgorithm _hashAlgorithm = CngAlgorithm.Sha256;

	private ECDiffieHellmanKeyDerivationFunction _kdf;

	private byte[] _hmacKey;

	private byte[] _label;

	private byte[] _secretAppend;

	private byte[] _secretPrepend;

	private byte[] _seed;

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

	public ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction
	{
		get
		{
			return _kdf;
		}
		set
		{
			if (value < ECDiffieHellmanKeyDerivationFunction.Hash || value > ECDiffieHellmanKeyDerivationFunction.Tls)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_kdf = value;
		}
	}

	public byte[]? HmacKey
	{
		get
		{
			return _hmacKey;
		}
		set
		{
			_hmacKey = value;
		}
	}

	public byte[]? Label
	{
		get
		{
			return _label;
		}
		set
		{
			_label = value;
		}
	}

	public byte[]? SecretAppend
	{
		get
		{
			return _secretAppend;
		}
		set
		{
			_secretAppend = value;
		}
	}

	public byte[]? SecretPrepend
	{
		get
		{
			return _secretPrepend;
		}
		set
		{
			_secretPrepend = value;
		}
	}

	public byte[]? Seed
	{
		get
		{
			return _seed;
		}
		set
		{
			_seed = value;
		}
	}

	public bool UseSecretAgreementAsHmacKey => HmacKey == null;

	public override ECDiffieHellmanPublicKey PublicKey => ECDiffieHellmanCngPublicKey.FromKey(Key);

	public CngKey Key
	{
		get
		{
			return GetKey();
		}
		private set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			if (value.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
			{
				throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey, "value");
			}
			_core.SetKey(value);
			ForceSetKeySize(value.KeySize);
		}
	}

	[SupportedOSPlatform("windows")]
	public ECDiffieHellmanCng()
		: this(521)
	{
	}

	[SupportedOSPlatform("windows")]
	public ECDiffieHellmanCng(int keySize)
	{
		KeySize = keySize;
	}

	[SupportedOSPlatform("windows")]
	public ECDiffieHellmanCng(ECCurve curve)
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

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
	}

	public override byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
		return global::Interop.NCrypt.DeriveKeyMaterialHash(secretAgreement, hashAlgorithm.Name, secretPrepend, secretAppend, global::Interop.NCrypt.SecretAgreementFlags.None);
	}

	public override byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
		global::Interop.NCrypt.SecretAgreementFlags flags = ((hmacKey == null) ? global::Interop.NCrypt.SecretAgreementFlags.UseSecretAsHmacKey : global::Interop.NCrypt.SecretAgreementFlags.None);
		return global::Interop.NCrypt.DeriveKeyMaterialHmac(secretAgreement, hashAlgorithm.Name, hmacKey, secretPrepend, secretAppend, flags);
	}

	public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		ArgumentNullException.ThrowIfNull(prfLabel, "prfLabel");
		ArgumentNullException.ThrowIfNull(prfSeed, "prfSeed");
		using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
		return global::Interop.NCrypt.DeriveKeyMaterialTls(secretAgreement, prfLabel, prfSeed, global::Interop.NCrypt.SecretAgreementFlags.None);
	}

	public override byte[] DeriveRawSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
		return global::Interop.NCrypt.DeriveKeyMaterialTruncate(secretAgreement, global::Interop.NCrypt.SecretAgreementFlags.None);
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
				byte[] primeCurveBlob = ECCng.GetPrimeCurveBlob(ref parameters2, ecdh: true);
				ImportFullKeyBlob(primeCurveBlob, includePrivateParameters: true);
			}
			else
			{
				byte[] primeCurveBlob2 = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: true);
				ImportFullKeyBlob(primeCurveBlob2, flag);
			}
			return;
		}
		if (curve.IsNamed)
		{
			if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
			}
			if (!flag2 && flag)
			{
				byte[] array2 = new byte[parameters.D.Length];
				ECParameters parameters3 = parameters;
				parameters3.Q.X = array2;
				parameters3.Q.Y = array2;
				byte[] namedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters3, ecdh: true);
				ImportKeyBlob(namedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
			}
			else
			{
				byte[] namedCurveBlob2 = ECCng.GetNamedCurveBlob(ref parameters, ecdh: true);
				ImportKeyBlob(namedCurveBlob2, curve.Oid.FriendlyName, flag);
			}
			return;
		}
		throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
	}

	public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		byte[] array = ExportFullKeyBlob(includePrivateParameters);
		try
		{
			ECParameters ecParams = default(ECParameters);
			ECCng.ExportPrimeCurveParameters(ref ecParams, array, includePrivateParameters);
			return ecParams;
		}
		finally
		{
			Array.Clear(array);
		}
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		ECParameters ecParams = default(ECParameters);
		string oidValue;
		string curveName = GetCurveName(out oidValue);
		byte[] array = null;
		try
		{
			if (string.IsNullOrEmpty(curveName))
			{
				array = ExportFullKeyBlob(includePrivateParameters);
				ECCng.ExportPrimeCurveParameters(ref ecParams, array, includePrivateParameters);
			}
			else
			{
				array = ExportKeyBlob(includePrivateParameters);
				ECCng.ExportNamedCurveParameters(ref ecParams, array, includePrivateParameters);
				ecParams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
			}
			return ecParams;
		}
		finally
		{
			if (array != null)
			{
				Array.Clear(array);
			}
		}
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
		if (response.GetAlgorithmGroup() != "ECDH")
		{
			response.FreeKey();
			throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
		}
		AcceptImport(response);
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

	[SupportedOSPlatform("windows")]
	public ECDiffieHellmanCng(CngKey key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		if (key.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey, "key");
		}
		Key = CngAlgorithmCore.Duplicate(key);
	}

	[SupportedOSPlatform("windows")]
	internal ECDiffieHellmanCng(CngKey key, bool transferOwnership)
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

	public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		if (otherPartyPublicKey is ECDiffieHellmanCngPublicKey eCDiffieHellmanCngPublicKey)
		{
			using CngKey otherPartyPublicKey2 = eCDiffieHellmanCngPublicKey.Import();
			return DeriveKeyMaterial(otherPartyPublicKey2);
		}
		ECParameters parameters = otherPartyPublicKey.ExportParameters();
		using ECDiffieHellmanCng eCDiffieHellmanCng = new ECDiffieHellmanCng();
		eCDiffieHellmanCng.ImportParameters(parameters);
		ECDiffieHellmanCngPublicKey eCDiffieHellmanCngPublicKey2;
		using (eCDiffieHellmanCngPublicKey2 = (ECDiffieHellmanCngPublicKey)eCDiffieHellmanCng.PublicKey)
		{
			using CngKey otherPartyPublicKey3 = eCDiffieHellmanCngPublicKey2.Import();
			return DeriveKeyMaterial(otherPartyPublicKey3);
		}
	}

	public byte[] DeriveKeyMaterial(CngKey otherPartyPublicKey)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		if (otherPartyPublicKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey, "otherPartyPublicKey");
		}
		if (otherPartyPublicKey.KeySize != KeySize)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHKeySizeMismatch, "otherPartyPublicKey");
		}
		global::Interop.NCrypt.SecretAgreementFlags flags = (UseSecretAgreementAsHmacKey ? global::Interop.NCrypt.SecretAgreementFlags.UseSecretAsHmacKey : global::Interop.NCrypt.SecretAgreementFlags.None);
		using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
		switch (KeyDerivationFunction)
		{
		case ECDiffieHellmanKeyDerivationFunction.Hash:
			return global::Interop.NCrypt.DeriveKeyMaterialHash(secretAgreement, HashAlgorithm.Algorithm, _secretPrepend, _secretAppend, flags);
		case ECDiffieHellmanKeyDerivationFunction.Hmac:
			return global::Interop.NCrypt.DeriveKeyMaterialHmac(secretAgreement, HashAlgorithm.Algorithm, _hmacKey, _secretPrepend, _secretAppend, flags);
		default:
			if (_label == null || _seed == null)
			{
				throw new InvalidOperationException(System.SR.Cryptography_TlsRequiresLabelAndSeed);
			}
			return global::Interop.NCrypt.DeriveKeyMaterialTls(secretAgreement, _label, _seed, flags);
		}
	}

	public SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		if (otherPartyPublicKey is ECDiffieHellmanCngPublicKey eCDiffieHellmanCngPublicKey)
		{
			using CngKey otherPartyPublicKey2 = eCDiffieHellmanCngPublicKey.Import();
			return DeriveSecretAgreementHandle(otherPartyPublicKey2);
		}
		ECParameters parameters = otherPartyPublicKey.ExportParameters();
		using ECDiffieHellmanCng eCDiffieHellmanCng = new ECDiffieHellmanCng();
		eCDiffieHellmanCng.ImportParameters(parameters);
		ECDiffieHellmanCngPublicKey eCDiffieHellmanCngPublicKey2;
		using (eCDiffieHellmanCngPublicKey2 = (ECDiffieHellmanCngPublicKey)eCDiffieHellmanCng.PublicKey)
		{
			using CngKey otherPartyPublicKey3 = eCDiffieHellmanCngPublicKey2.Import();
			return DeriveSecretAgreementHandle(otherPartyPublicKey3);
		}
	}

	public SafeNCryptSecretHandle DeriveSecretAgreementHandle(CngKey otherPartyPublicKey)
	{
		ArgumentNullException.ThrowIfNull(otherPartyPublicKey, "otherPartyPublicKey");
		if (otherPartyPublicKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey, "otherPartyPublicKey");
		}
		if (otherPartyPublicKey.KeySize != KeySize)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHKeySizeMismatch, "otherPartyPublicKey");
		}
		using SafeNCryptKeyHandle privateKey = Key.Handle;
		using SafeNCryptKeyHandle otherPartyPublicKey2 = otherPartyPublicKey.Handle;
		return global::Interop.NCrypt.DeriveSecretAgreement(privateKey, otherPartyPublicKey2);
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
			CngAlgorithm cngAlgorithm = CngKey.EcdhCurveNameToAlgorithm(curve.Oid.FriendlyName);
			if (CngKey.IsECNamedCurve(cngAlgorithm.Algorithm))
			{
				CngKey orGenerateKey = _core.GetOrGenerateKey(curve);
				ForceSetKeySize(orGenerateKey.KeySize);
				return;
			}
			int num;
			if (cngAlgorithm == CngAlgorithm.ECDiffieHellmanP256)
			{
				num = 256;
			}
			else if (cngAlgorithm == CngAlgorithm.ECDiffieHellmanP384)
			{
				num = 384;
			}
			else
			{
				if (!(cngAlgorithm == CngAlgorithm.ECDiffieHellmanP521))
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
			256 => CngAlgorithm.ECDiffieHellmanP256, 
			384 => CngAlgorithm.ECDiffieHellmanP384, 
			521 => CngAlgorithm.ECDiffieHellmanP521, 
			_ => throw new ArgumentException(System.SR.Cryptography_InvalidKeySize), 
		};
		return _core.GetOrGenerateKey(keySize, algorithm);
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public void FromXmlString(string xml, ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId = "SYSLIB0042", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public string ToXmlString(ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}
}
