using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class RSACng : RSA, IRuntimeAlgorithm
{
	private static readonly ConcurrentDictionary<HashAlgorithmName, int> s_hashSizes = new ConcurrentDictionary<HashAlgorithmName, int>(new KeyValuePair<HashAlgorithmName, int>[3]
	{
		KeyValuePair.Create(HashAlgorithmName.SHA256, 32),
		KeyValuePair.Create(HashAlgorithmName.SHA384, 48),
		KeyValuePair.Create(HashAlgorithmName.SHA512, 64)
	});

	private CngAlgorithmCore _core = new CngAlgorithmCore(typeof(RSACng));

	private static readonly CngKeyBlobFormat s_rsaFullPrivateBlob = new CngKeyBlobFormat("RSAFULLPRIVATEBLOB");

	private static readonly CngKeyBlobFormat s_rsaPrivateBlob = new CngKeyBlobFormat("RSAPRIVATEBLOB");

	private static readonly CngKeyBlobFormat s_rsaPublicBlob = new CngKeyBlobFormat("RSAPUBLICBLOB");

	public override KeySizes[] LegalKeySizes => new KeySizes[1]
	{
		new KeySizes(512, 16384, 64)
	};

	public CngKey Key
	{
		get
		{
			return _core.GetOrGenerateKey(KeySize, CngAlgorithm.Rsa);
		}
		private set
		{
			if (value.AlgorithmGroup != CngAlgorithmGroup.Rsa)
			{
				throw new ArgumentException(System.SR.Cryptography_ArgRSARequiresRSAKey, "value");
			}
			_core.SetKey(value);
			ForceSetKeySize(value.KeySize);
		}
	}

	[SupportedOSPlatform("windows")]
	public RSACng()
		: this(2048)
	{
	}

	[SupportedOSPlatform("windows")]
	public RSACng(int keySize)
	{
		KeySize = keySize;
	}

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
	}

	public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		return EncryptOrDecrypt(data, padding, encrypt: true);
	}

	public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		return EncryptOrDecrypt(data, padding, encrypt: false);
	}

	public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		return TryEncryptOrDecrypt(data, destination, padding, encrypt: true, out bytesWritten);
	}

	public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		return TryEncryptOrDecrypt(data, destination, padding, encrypt: false, out bytesWritten);
	}

	private unsafe byte[] EncryptOrDecrypt(byte[] data, RSAEncryptionPadding padding, bool encrypt)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		int num = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (!encrypt && data.Length != num)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		if (encrypt && padding.Mode == RSAEncryptionPaddingMode.Pkcs1 && data.Length > num - 11)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num - 11));
		}
		using SafeNCryptKeyHandle key = GetDuplicatedKeyHandle();
		if (encrypt && data.Length == 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			Span<byte> span = new Span<byte>(array, 0, num);
			try
			{
				if (padding == RSAEncryptionPadding.Pkcs1)
				{
					RsaPaddingProcessor.PadPkcs1Encryption(data, span);
				}
				else
				{
					if (padding.Mode != RSAEncryptionPaddingMode.Oaep)
					{
						throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
					}
					RsaPaddingProcessor.PadOaep(padding.OaepHashAlgorithm, data, span);
				}
				return EncryptOrDecrypt(key, span, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
		switch (padding.Mode)
		{
		case RSAEncryptionPaddingMode.Pkcs1:
			return EncryptOrDecrypt(key, data, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt);
		case RSAEncryptionPaddingMode.Oaep:
		{
			nint num2 = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
			try
			{
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO);
				bCRYPT_OAEP_PADDING_INFO.pszAlgId = num2;
				bCRYPT_OAEP_PADDING_INFO.pbLabel = IntPtr.Zero;
				bCRYPT_OAEP_PADDING_INFO.cbLabel = 0;
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO2 = bCRYPT_OAEP_PADDING_INFO;
				return EncryptOrDecrypt(key, data, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &bCRYPT_OAEP_PADDING_INFO2, encrypt);
			}
			finally
			{
				Marshal.FreeHGlobal(num2);
			}
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
		}
	}

	private unsafe bool TryEncryptOrDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, bool encrypt, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(padding, "padding");
		int num = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (!encrypt && data.Length != num)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		if (encrypt && padding.Mode == RSAEncryptionPaddingMode.Pkcs1 && data.Length > num - 11)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num - 11));
		}
		using SafeNCryptKeyHandle key = GetDuplicatedKeyHandle();
		if (encrypt && data.Length == 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			Span<byte> span = new Span<byte>(array, 0, num);
			try
			{
				if (padding == RSAEncryptionPadding.Pkcs1)
				{
					RsaPaddingProcessor.PadPkcs1Encryption(data, span);
				}
				else
				{
					if (padding.Mode != RSAEncryptionPaddingMode.Oaep)
					{
						throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
					}
					RsaPaddingProcessor.PadOaep(padding.OaepHashAlgorithm, data, span);
				}
				return TryEncryptOrDecrypt(key, span, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
		switch (padding.Mode)
		{
		case RSAEncryptionPaddingMode.Pkcs1:
			return TryEncryptOrDecrypt(key, data, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt, out bytesWritten);
		case RSAEncryptionPaddingMode.Oaep:
		{
			nint num2 = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
			try
			{
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO);
				bCRYPT_OAEP_PADDING_INFO.pszAlgId = num2;
				bCRYPT_OAEP_PADDING_INFO.pbLabel = IntPtr.Zero;
				bCRYPT_OAEP_PADDING_INFO.cbLabel = 0;
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO2 = bCRYPT_OAEP_PADDING_INFO;
				return TryEncryptOrDecrypt(key, data, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &bCRYPT_OAEP_PADDING_INFO2, encrypt, out bytesWritten);
			}
			finally
			{
				Marshal.FreeHGlobal(num2);
			}
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
		}
	}

	private unsafe byte[] EncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt)
	{
		int maxOutputSize = GetMaxOutputSize();
		byte[] array = new byte[maxOutputSize];
		int bytesNeeded = 0;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
		for (int i = 0; i <= 1; i++)
		{
			errorCode = EncryptOrDecrypt(key, input, array, paddingMode, paddingInfo, encrypt, out bytesNeeded);
			if (errorCode != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
			{
				break;
			}
		}
		if (errorCode.IsBufferTooSmall())
		{
			CryptographicOperations.ZeroMemory(array);
			array = new byte[bytesNeeded];
			for (int j = 0; j <= 1; j++)
			{
				errorCode = EncryptOrDecrypt(key, input, array, paddingMode, paddingInfo, encrypt, out bytesNeeded);
				if (errorCode != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
				{
					break;
				}
			}
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (bytesNeeded != array.Length)
		{
			byte[] array2 = array.AsSpan(0, bytesNeeded).ToArray();
			CryptographicOperations.ZeroMemory(array);
			array = array2;
		}
		return array;
	}

	private unsafe static bool TryEncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, Span<byte> output, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt, out int bytesWritten)
	{
		for (int i = 0; i <= 1; i++)
		{
			int bytesNeeded;
			global::Interop.NCrypt.ErrorCode errorCode = EncryptOrDecrypt(key, input, output, paddingMode, paddingInfo, encrypt, out bytesNeeded);
			global::Interop.NCrypt.ErrorCode errorCode2 = errorCode;
			if (errorCode2 == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS)
			{
				bytesWritten = bytesNeeded;
				return true;
			}
			if (!errorCode2.IsBufferTooSmall())
			{
				if (errorCode2 != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
				{
					throw errorCode.ToCryptographicException();
				}
				continue;
			}
			bytesWritten = 0;
			return false;
		}
		throw global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
	}

	private unsafe static global::Interop.NCrypt.ErrorCode EncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, Span<byte> output, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt, out int bytesNeeded)
	{
		global::Interop.NCrypt.ErrorCode errorCode = (encrypt ? global::Interop.NCrypt.NCryptEncrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode) : global::Interop.NCrypt.NCryptDecrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode));
		if (errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS && bytesNeeded > output.Length)
		{
			errorCode = global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL;
		}
		return errorCode;
	}

	public override void ImportParameters(RSAParameters parameters)
	{
		ArraySegment<byte> arraySegment = parameters.ToBCryptBlob();
		try
		{
			ImportKeyBlob(arraySegment, parameters.D != null);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(arraySegment);
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
		if (response.GetAlgorithmGroup() != "RSA")
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

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		byte[] array = ExportKeyBlob(includePrivateParameters);
		RSAParameters rsaParams = default(RSAParameters);
		rsaParams.FromBCryptBlob(array, includePrivateParameters);
		return rsaParams;
	}

	internal static int GetHashSizeInBytes(HashAlgorithmName hashAlgorithm)
	{
		return s_hashSizes.GetOrAdd(hashAlgorithm, delegate(HashAlgorithmName hashAlgorithm)
		{
			using HashProviderCng hashProviderCng = new HashProviderCng(hashAlgorithm.Name, null);
			return hashProviderCng.HashSizeInBytes;
		});
	}

	public unsafe override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			throw new CryptographicException(System.SR.Cryptography_SignHash_WrongSize);
		}
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		nint num = Marshal.StringToHGlobalUni(name);
		try
		{
			int maxOutputSize = GetMaxOutputSize();
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = num;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2, maxOutputSize);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = num;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2, maxOutputSize);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	public unsafe override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
	{
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			throw new CryptographicException(System.SR.Cryptography_SignHash_WrongSize);
		}
		nint num = Marshal.StringToHGlobalUni(name);
		try
		{
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = num;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2, out bytesWritten);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = num;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2, out bytesWritten);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
	}

	public unsafe override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(padding, "padding");
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			return false;
		}
		nint num = Marshal.StringToHGlobalUni(name);
		try
		{
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = num;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = num;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	[SupportedOSPlatform("windows")]
	public RSACng(CngKey key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgRSARequiresRSAKey, "key");
		}
		Key = CngAlgorithmCore.Duplicate(key);
	}

	[SupportedOSPlatform("windows")]
	internal RSACng(CngKey key, bool transferOwnership)
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

	private void ImportKeyBlob(ReadOnlySpan<byte> rsaBlob, bool includePrivate)
	{
		CngKeyBlobFormat format = (includePrivate ? s_rsaPrivateBlob : s_rsaPublicBlob);
		CngKey cngKey = CngKey.Import(rsaBlob, format);
		try
		{
			cngKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;
			Key = cngKey;
		}
		catch
		{
			cngKey.Dispose();
			throw;
		}
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

	private byte[] ExportKeyBlob(bool includePrivateParameters)
	{
		return Key.Export(includePrivateParameters ? s_rsaFullPrivateBlob : s_rsaPublicBlob);
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

	private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
	{
		return Key.Handle;
	}
}
