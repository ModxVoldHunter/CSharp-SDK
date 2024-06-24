using System.Runtime.Versioning;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

public sealed class TripleDESCng : TripleDES, ICngSymmetricAlgorithm
{
	private CngSymmetricAlgorithmCore _core;

	public override byte[] Key
	{
		get
		{
			return _core.GetKeyIfExportable();
		}
		set
		{
			_core.SetKey(value);
		}
	}

	public override int KeySize
	{
		get
		{
			return base.KeySize;
		}
		set
		{
			_core.SetKeySize(value, this);
		}
	}

	byte[] ICngSymmetricAlgorithm.BaseKey
	{
		get
		{
			return base.Key;
		}
		set
		{
			base.Key = value;
		}
	}

	int ICngSymmetricAlgorithm.BaseKeySize
	{
		get
		{
			return base.KeySize;
		}
		set
		{
			base.KeySize = value;
		}
	}

	[SupportedOSPlatform("windows")]
	public TripleDESCng()
	{
		_core = new CngSymmetricAlgorithmCore(this);
	}

	[SupportedOSPlatform("windows")]
	public TripleDESCng(string keyName)
		: this(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider)
	{
	}

	[SupportedOSPlatform("windows")]
	public TripleDESCng(string keyName, CngProvider provider)
		: this(keyName, provider, CngKeyOpenOptions.None)
	{
	}

	[SupportedOSPlatform("windows")]
	public TripleDESCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
	{
		_core = new CngSymmetricAlgorithmCore(this, keyName, provider, openOptions);
	}

	public override ICryptoTransform CreateDecryptor()
	{
		return _core.CreateDecryptor();
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return _core.CreateDecryptor(rgbKey, rgbIV);
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return _core.CreateEncryptor();
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return _core.CreateEncryptor(rgbKey, rgbIV);
	}

	public override void GenerateKey()
	{
		_core.GenerateKey();
	}

	public override void GenerateIV()
	{
		_core.GenerateIV();
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(default(ReadOnlySpan<byte>), encrypting: false, CipherMode.ECB, 0);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(default(ReadOnlySpan<byte>), encrypting: true, CipherMode.ECB, 0);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(iv, encrypting: true, CipherMode.CBC, 0);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(iv, encrypting: false, CipherMode.CBC, 0);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(iv, encrypting: false, CipherMode.CFB, feedbackSizeInBits);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = _core.CreateLiteSymmetricCipher(iv, encrypting: true, CipherMode.CFB, feedbackSizeInBits);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	bool ICngSymmetricAlgorithm.IsWeakKey(byte[] key)
	{
		return TripleDES.IsWeakKey(key);
	}

	int ICngSymmetricAlgorithm.GetPaddingSize(CipherMode mode, int feedbackSizeBits)
	{
		return this.GetPaddingSize(mode, feedbackSizeBits);
	}

	SafeAlgorithmHandle ICngSymmetricAlgorithm.GetEphemeralModeHandle(CipherMode mode, int feedbackSizeInBits)
	{
		return TripleDesBCryptModes.GetSharedHandle(mode, feedbackSizeInBits / 8);
	}

	string ICngSymmetricAlgorithm.GetNCryptAlgorithmIdentifier()
	{
		return "3DES";
	}

	byte[] ICngSymmetricAlgorithm.PreprocessKey(byte[] key)
	{
		if (key.Length == 16)
		{
			byte[] array = new byte[24];
			Array.Copy(key, array, 16);
			Array.Copy(key, 0, array, 16, 8);
			return array;
		}
		return key;
	}

	bool ICngSymmetricAlgorithm.IsValidEphemeralFeedbackSize(int feedbackSizeInBits)
	{
		if (feedbackSizeInBits != 8)
		{
			return feedbackSizeInBits == 64;
		}
		return true;
	}
}
