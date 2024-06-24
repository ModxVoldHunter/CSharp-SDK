using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class RSABCrypt : RSA
{
	private static readonly SafeBCryptAlgorithmHandle s_algHandle = global::Interop.BCrypt.BCryptOpenAlgorithmProvider("RSA");

	private static readonly KeySizes s_keySizes = new KeySizes(512, 16384, 64);

	private SafeBCryptKeyHandle _key;

	private int _lastKeySize;

	public override KeySizes[] LegalKeySizes => new KeySizes[1] { s_keySizes };

	internal RSABCrypt()
	{
		KeySizeValue = 2048;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_key?.Dispose();
		}
		_lastKeySize = -1;
	}

	private SafeBCryptKeyHandle GetKey()
	{
		int keySize = KeySize;
		if (_lastKeySize == keySize)
		{
			return _key;
		}
		ThrowIfDisposed();
		SafeBCryptKeyHandle safeBCryptKeyHandle = global::Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keySize);
		global::Interop.BCrypt.BCryptFinalizeKeyPair(safeBCryptKeyHandle);
		SetKey(safeBCryptKeyHandle);
		return safeBCryptKeyHandle;
	}

	private void SetKey(SafeBCryptKeyHandle newKey)
	{
		int newKeySize = global::Interop.BCrypt.BCryptGetDWordProperty(newKey, "KeyStrength");
		SafeBCryptKeyHandle safeBCryptKeyHandle = Interlocked.Exchange(ref _key, newKey);
		ForceSetKeySize(newKeySize);
		safeBCryptKeyHandle?.Dispose();
	}

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		SafeBCryptKeyHandle key = GetKey();
		ArraySegment<byte> arraySegment = global::Interop.BCrypt.BCryptExportKey(key, includePrivateParameters ? "RSAFULLPRIVATEBLOB" : "RSAPUBLICBLOB");
		RSAParameters rsaParams = default(RSAParameters);
		rsaParams.FromBCryptBlob(arraySegment, includePrivateParameters);
		System.Security.Cryptography.CryptoPool.Return(arraySegment);
		return rsaParams;
	}

	public override void ImportParameters(RSAParameters parameters)
	{
		ThrowIfDisposed();
		ArraySegment<byte> arraySegment = parameters.ToBCryptBlob();
		SafeBCryptKeyHandle key;
		try
		{
			key = global::Interop.BCrypt.BCryptImportKeyPair(s_algHandle, (parameters.D != null) ? "RSAPRIVATEBLOB" : "RSAPUBLICBLOB", arraySegment);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(arraySegment);
		}
		SetKey(key);
	}

	public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		byte[] array = new byte[GetMaxOutputSize()];
		int written = Encrypt(new ReadOnlySpan<byte>(data), array.AsSpan(), padding);
		VerifyWritten(array, written);
		return array;
	}

	public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		return Decrypt(new ReadOnlySpan<byte>(data), padding);
	}

	public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		byte[] array = new byte[GetMaxOutputSize()];
		int written = SignHash(new ReadOnlySpan<byte>(hash), array.AsSpan(), hashAlgorithm, padding);
		VerifyWritten(array, written);
		return array;
	}

	public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		return VerifyHash(new ReadOnlySpan<byte>(hash), new ReadOnlySpan<byte>(signature), hashAlgorithm, padding);
	}

	public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(padding, "padding");
		SafeBCryptKeyHandle key = GetKey();
		int num = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (data.Length != num)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		return padding.Mode switch
		{
			RSAEncryptionPaddingMode.Pkcs1 => global::Interop.BCrypt.BCryptDecryptPkcs1(key, data, destination, out bytesWritten), 
			RSAEncryptionPaddingMode.Oaep => global::Interop.BCrypt.BCryptDecryptOaep(key, data, destination, padding.OaepHashAlgorithm.Name, out bytesWritten), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode), 
		};
	}

	public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(padding, "padding");
		SafeBCryptKeyHandle key = GetKey();
		int num = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		switch (padding.Mode)
		{
		case RSAEncryptionPaddingMode.Pkcs1:
			if (num - 11 < data.Length)
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num - 11));
			}
			bytesWritten = global::Interop.BCrypt.BCryptEncryptPkcs1(key, data, destination);
			return true;
		case RSAEncryptionPaddingMode.Oaep:
			bytesWritten = global::Interop.BCrypt.BCryptEncryptOaep(key, data, destination, padding.OaepHashAlgorithm.Name);
			return true;
		default:
			throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
		}
	}

	public override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
	{
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		SafeBCryptKeyHandle key = GetKey();
		if (hash.Length != RSACng.GetHashSizeInBytes(hashAlgorithm))
		{
			throw new CryptographicException(System.SR.Cryptography_SignHash_WrongSize);
		}
		int bytesWritten2;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = padding.Mode switch
		{
			RSASignaturePaddingMode.Pkcs1 => global::Interop.BCrypt.BCryptSignHashPkcs1(key, hash, destination, name, out bytesWritten2), 
			RSASignaturePaddingMode.Pss => global::Interop.BCrypt.BCryptSignHashPss(key, hash, destination, name, out bytesWritten2), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode), 
		};
		switch (nTSTATUS)
		{
		case global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS:
			bytesWritten = bytesWritten2;
			return true;
		case global::Interop.BCrypt.NTSTATUS.STATUS_BUFFER_TOO_SMALL:
			bytesWritten = 0;
			return false;
		default:
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
	}

	public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		SafeBCryptKeyHandle key = GetKey();
		if (hash.Length != RSACng.GetHashSizeInBytes(hashAlgorithm))
		{
			return false;
		}
		return padding.Mode switch
		{
			RSASignaturePaddingMode.Pkcs1 => global::Interop.BCrypt.BCryptVerifySignaturePkcs1(key, hash, signature, name), 
			RSASignaturePaddingMode.Pss => global::Interop.BCrypt.BCryptVerifySignaturePss(key, hash, signature, name), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode), 
		};
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
	}

	public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportPkcs8PrivateKey(source, out bytesRead);
	}

	public override void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportRSAPrivateKey(source, out bytesRead);
	}

	public override void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportRSAPublicKey(source, out bytesRead);
	}

	public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportSubjectPublicKeyInfo(source, out bytesRead);
	}

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
		_lastKeySize = newKeySize;
	}

	private static void VerifyWritten(byte[] array, int written)
	{
		if (array.Length != written)
		{
			throw new CryptographicException();
		}
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_lastKeySize < 0, this);
	}
}
