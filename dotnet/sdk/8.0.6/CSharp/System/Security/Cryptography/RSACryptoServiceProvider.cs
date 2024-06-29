using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public sealed class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm, IRuntimeAlgorithm
{
	private int _keySize;

	private readonly CspParameters _parameters;

	private readonly bool _randomKeyContainer;

	private SafeCapiKeyHandle _safeKeyHandle;

	private SafeProvHandle _safeProvHandle;

	private static volatile CspProviderFlags s_useMachineKeyStore;

	private bool _disposed;

	private SafeProvHandle SafeProvHandle
	{
		get
		{
			if (_safeProvHandle == null)
			{
				lock (_parameters)
				{
					if (_safeProvHandle == null)
					{
						SafeProvHandle safeProvHandle = CapiHelper.CreateProvHandle(_parameters, _randomKeyContainer);
						_safeProvHandle = safeProvHandle;
					}
				}
				return _safeProvHandle;
			}
			return _safeProvHandle;
		}
		set
		{
			lock (_parameters)
			{
				SafeProvHandle safeProvHandle = _safeProvHandle;
				if (value != safeProvHandle)
				{
					if (safeProvHandle != null)
					{
						SafeCapiKeyHandle safeKeyHandle = _safeKeyHandle;
						_safeKeyHandle = null;
						safeKeyHandle?.Dispose();
						safeProvHandle.Dispose();
					}
					_safeProvHandle = value;
				}
			}
		}
	}

	private SafeCapiKeyHandle SafeKeyHandle
	{
		get
		{
			if (_safeKeyHandle == null)
			{
				lock (_parameters)
				{
					if (_safeKeyHandle == null)
					{
						SafeCapiKeyHandle keyPairHelper = CapiHelper.GetKeyPairHelper(CapiHelper.CspAlgorithmType.Rsa, _parameters, _keySize, SafeProvHandle);
						_safeKeyHandle = keyPairHelper;
					}
				}
			}
			return _safeKeyHandle;
		}
		set
		{
			lock (_parameters)
			{
				SafeCapiKeyHandle safeKeyHandle = _safeKeyHandle;
				if (value != safeKeyHandle)
				{
					_safeKeyHandle = value;
					safeKeyHandle?.Dispose();
				}
			}
		}
	}

	[SupportedOSPlatform("windows")]
	public CspKeyContainerInfo CspKeyContainerInfo
	{
		get
		{
			SafeCapiKeyHandle safeKeyHandle = SafeKeyHandle;
			return new CspKeyContainerInfo(_parameters, _randomKeyContainer);
		}
	}

	public override int KeySize
	{
		get
		{
			byte[] keyParameter = CapiHelper.GetKeyParameter(SafeKeyHandle, CapiHelper.ClrPropertyId.CLR_KEYLEN);
			_keySize = BinaryPrimitives.ReadInt32LittleEndian(keyParameter);
			return _keySize;
		}
	}

	public override KeySizes[] LegalKeySizes => new KeySizes[1]
	{
		new KeySizes(384, 16384, 8)
	};

	public bool PersistKeyInCsp
	{
		get
		{
			return CapiHelper.GetPersistKeyInCsp(SafeProvHandle);
		}
		set
		{
			bool persistKeyInCsp = PersistKeyInCsp;
			if (value != persistKeyInCsp)
			{
				CapiHelper.SetPersistKeyInCsp(SafeProvHandle, value);
			}
		}
	}

	public bool PublicOnly
	{
		get
		{
			byte[] keyParameter = CapiHelper.GetKeyParameter(SafeKeyHandle, CapiHelper.ClrPropertyId.CLR_PUBLICKEYONLY);
			return keyParameter[0] == 1;
		}
	}

	public static bool UseMachineKeyStore
	{
		get
		{
			return s_useMachineKeyStore == CspProviderFlags.UseMachineKeyStore;
		}
		set
		{
			s_useMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags);
		}
	}

	public override string? KeyExchangeAlgorithm
	{
		get
		{
			if (_parameters.KeyNumber == 1)
			{
				return "RSA-PKCS1-KeyEx";
			}
			return null;
		}
	}

	public override string SignatureAlgorithm => "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

	[UnsupportedOSPlatform("browser")]
	public RSACryptoServiceProvider()
		: this(0, new CspParameters(24, null, null, s_useMachineKeyStore), useDefaultKeySize: true)
	{
	}

	[UnsupportedOSPlatform("browser")]
	public RSACryptoServiceProvider(int dwKeySize)
		: this(dwKeySize, new CspParameters(24, null, null, s_useMachineKeyStore), useDefaultKeySize: false)
	{
	}

	[SupportedOSPlatform("windows")]
	public RSACryptoServiceProvider(int dwKeySize, CspParameters? parameters)
		: this(dwKeySize, parameters, useDefaultKeySize: false)
	{
	}

	[SupportedOSPlatform("windows")]
	public RSACryptoServiceProvider(CspParameters? parameters)
		: this(0, parameters, useDefaultKeySize: true)
	{
	}

	private RSACryptoServiceProvider(int keySize, CspParameters parameters, bool useDefaultKeySize)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(keySize, "keySize");
		_parameters = CapiHelper.SaveCspParameters(CapiHelper.CspAlgorithmType.Rsa, parameters, s_useMachineKeyStore, out _randomKeyContainer);
		_keySize = (useDefaultKeySize ? 1024 : keySize);
		if (!_randomKeyContainer)
		{
			SafeCapiKeyHandle safeKeyHandle = SafeKeyHandle;
		}
	}

	public byte[] Decrypt(byte[] rgb, bool fOAEP)
	{
		ArgumentNullException.ThrowIfNull(rgb, "rgb");
		int keySize = KeySize;
		if (rgb.Length != keySize / 8)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		CapiHelper.DecryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, out var decryptedData);
		return decryptedData;
	}

	[Obsolete("RSA.EncryptValue and DecryptValue are not supported and throw NotSupportedException. Use RSA.Encrypt and RSA.Decrypt instead.", DiagnosticId = "SYSLIB0048", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override byte[] DecryptValue(byte[] rgb)
	{
		return base.DecryptValue(rgb);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
			{
				_safeKeyHandle.Dispose();
			}
			if (_safeProvHandle != null && !_safeProvHandle.IsClosed)
			{
				_safeProvHandle.Dispose();
			}
			_disposed = true;
		}
	}

	public byte[] Encrypt(byte[] rgb, bool fOAEP)
	{
		ArgumentNullException.ThrowIfNull(rgb, "rgb");
		if (fOAEP)
		{
			int maxOutputSize = GetMaxOutputSize();
			if (maxOutputSize - 42 < rgb.Length)
			{
				throw (-2146893820).ToCryptographicException();
			}
		}
		byte[] pbEncryptedKey = null;
		CapiHelper.EncryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, ref pbEncryptedKey);
		return pbEncryptedKey;
	}

	[Obsolete("RSA.EncryptValue and DecryptValue are not supported and throw NotSupportedException. Use RSA.Encrypt and RSA.Decrypt instead.", DiagnosticId = "SYSLIB0048", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override byte[] EncryptValue(byte[] rgb)
	{
		return base.EncryptValue(rgb);
	}

	public byte[] ExportCspBlob(bool includePrivateParameters)
	{
		return CapiHelper.ExportKeyBlob(includePrivateParameters, SafeKeyHandle);
	}

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		byte[] cspBlob = ExportCspBlob(includePrivateParameters);
		return cspBlob.ToRSAParameters(includePrivateParameters);
	}

	private static SafeProvHandle AcquireSafeProviderHandle()
	{
		CapiHelper.AcquireCsp(new CspParameters(24), out var safeProvHandle);
		return safeProvHandle;
	}

	public void ImportCspBlob(byte[] keyBlob)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		SafeCapiKeyHandle safeKeyHandle;
		if (IsPublic(keyBlob))
		{
			SafeProvHandle safeProvHandle = AcquireSafeProviderHandle();
			CapiHelper.ImportKeyBlob(safeProvHandle, CspProviderFlags.NoFlags, addNoSaltFlag: false, keyBlob, out safeKeyHandle);
			SafeProvHandle = safeProvHandle;
		}
		else
		{
			CapiHelper.ImportKeyBlob(SafeProvHandle, _parameters.Flags, addNoSaltFlag: false, keyBlob, out safeKeyHandle);
		}
		SafeKeyHandle = safeKeyHandle;
	}

	public override void ImportParameters(RSAParameters parameters)
	{
		byte[] keyBlob = parameters.ToKeyBlob();
		ImportCspBlob(keyBlob);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
	}

	public byte[] SignData(byte[] buffer, int offset, int count, object halg)
	{
		int num = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithmName hashAlgorithm = CapiHelper.AlgIdToHashAlgorithmName(num);
		byte[] rgbHash = HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(buffer, offset, count));
		return SignHash(rgbHash, num);
	}

	public byte[] SignData(byte[] buffer, object halg)
	{
		int num = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithmName hashAlgorithm = CapiHelper.AlgIdToHashAlgorithmName(num);
		byte[] rgbHash = HashOneShotHelpers.HashData(hashAlgorithm, buffer);
		return SignHash(rgbHash, num);
	}

	public byte[] SignData(Stream inputStream, object halg)
	{
		int num = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithmName hashAlgorithm = CapiHelper.AlgIdToHashAlgorithmName(num);
		byte[] rgbHash = HashOneShotHelpers.HashData(hashAlgorithm, inputStream);
		return SignHash(rgbHash, num);
	}

	public byte[] SignHash(byte[] rgbHash, string? str)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		if (PublicOnly)
		{
			throw new CryptographicException(System.SR.Cryptography_CSP_NoPrivateKey);
		}
		int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);
		return SignHash(rgbHash, calgHash);
	}

	private byte[] SignHash(byte[] rgbHash, int calgHash)
	{
		SafeCapiKeyHandle safeKeyHandle = SafeKeyHandle;
		return CapiHelper.SignValue(SafeProvHandle, _parameters.KeyNumber, 9216, calgHash, rgbHash);
	}

	public bool VerifyData(byte[] buffer, object halg, byte[] signature)
	{
		int num = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithmName hashAlgorithm = CapiHelper.AlgIdToHashAlgorithmName(num);
		byte[] rgbHash = HashOneShotHelpers.HashData(hashAlgorithm, buffer);
		return VerifyHash(rgbHash, num, signature);
	}

	public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		ArgumentNullException.ThrowIfNull(rgbSignature, "rgbSignature");
		int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);
		return VerifyHash(rgbHash, calgHash, rgbSignature);
	}

	private bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature)
	{
		return CapiHelper.VerifySign(SafeProvHandle, SafeKeyHandle, 9216, calgHash, rgbHash, rgbSignature);
	}

	private static bool IsPublic(byte[] keyBlob)
	{
		ArgumentNullException.ThrowIfNull(keyBlob, "keyBlob");
		if (keyBlob[0] != 6)
		{
			return false;
		}
		if (keyBlob[11] != 49 || keyBlob[10] != 65 || keyBlob[9] != 83 || keyBlob[8] != 82)
		{
			return false;
		}
		return true;
	}

	private static int GetAlgorithmId(HashAlgorithmName hashAlgorithm)
	{
		return hashAlgorithm.Name switch
		{
			"MD5" => 32771, 
			"SHA1" => 32772, 
			"SHA256" => 32780, 
			"SHA384" => 32781, 
			"SHA512" => 32782, 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name), 
		};
	}

	public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		if (padding == RSAEncryptionPadding.Pkcs1)
		{
			return Encrypt(data, fOAEP: false);
		}
		if (padding == RSAEncryptionPadding.OaepSHA1)
		{
			return Encrypt(data, fOAEP: true);
		}
		throw PaddingModeNotSupported();
	}

	public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		if (padding == RSAEncryptionPadding.Pkcs1)
		{
			return Decrypt(data, fOAEP: false);
		}
		if (padding == RSAEncryptionPadding.OaepSHA1)
		{
			return Decrypt(data, fOAEP: true);
		}
		throw PaddingModeNotSupported();
	}

	public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		if (padding != RSASignaturePadding.Pkcs1)
		{
			throw PaddingModeNotSupported();
		}
		return SignHash(hash, GetAlgorithmId(hashAlgorithm));
	}

	public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		if (padding != RSASignaturePadding.Pkcs1)
		{
			throw PaddingModeNotSupported();
		}
		return VerifyHash(hash, GetAlgorithmId(hashAlgorithm), signature);
	}

	private static CryptographicException PaddingModeNotSupported()
	{
		return new CryptographicException(System.SR.Cryptography_InvalidPaddingMode);
	}
}
