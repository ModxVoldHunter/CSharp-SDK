using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
public sealed class AesGcm : IDisposable
{
	private SafeKeyHandle _keyHandle;

	public static KeySizes NonceByteSizes { get; } = new KeySizes(12, 12, 1);


	public int? TagSizeInBytes { get; }

	public static bool IsSupported => true;

	public static KeySizes TagByteSizes { get; } = new KeySizes(12, 16, 1);


	[Obsolete("AesGcm should indicate the required tag size for encryption and decryption. Use a constructor that accepts the tag size.", DiagnosticId = "SYSLIB0053", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AesGcm(ReadOnlySpan<byte> key)
	{
		ThrowIfNotSupported();
		AesAEAD.CheckKeySize(key.Length);
		ImportKey(key);
	}

	[Obsolete("AesGcm should indicate the required tag size for encryption and decryption. Use a constructor that accepts the tag size.", DiagnosticId = "SYSLIB0053", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AesGcm(byte[] key)
		: this(new ReadOnlySpan<byte>(key ?? throw new ArgumentNullException("key")))
	{
	}

	public AesGcm(ReadOnlySpan<byte> key, int tagSizeInBytes)
	{
		ThrowIfNotSupported();
		AesAEAD.CheckKeySize(key.Length);
		if (!tagSizeInBytes.IsLegalSize(TagByteSizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidTagLength, "tagSizeInBytes");
		}
		TagSizeInBytes = tagSizeInBytes;
		ImportKey(key);
	}

	public AesGcm(byte[] key, int tagSizeInBytes)
		: this(new ReadOnlySpan<byte>(key ?? throw new ArgumentNullException("key")), tagSizeInBytes)
	{
	}

	public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
	{
		ArgumentNullException.ThrowIfNull(nonce, "nonce");
		ArgumentNullException.ThrowIfNull(plaintext, "plaintext");
		ArgumentNullException.ThrowIfNull(ciphertext, "ciphertext");
		ArgumentNullException.ThrowIfNull(tag, "tag");
		Encrypt((ReadOnlySpan<byte>)nonce, (ReadOnlySpan<byte>)plaintext, (Span<byte>)ciphertext, (Span<byte>)tag, (ReadOnlySpan<byte>)associatedData);
	}

	public void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		CheckParameters(plaintext, ciphertext, nonce, tag);
		EncryptCore(nonce, plaintext, ciphertext, tag, associatedData);
	}

	public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null)
	{
		ArgumentNullException.ThrowIfNull(nonce, "nonce");
		ArgumentNullException.ThrowIfNull(ciphertext, "ciphertext");
		ArgumentNullException.ThrowIfNull(tag, "tag");
		ArgumentNullException.ThrowIfNull(plaintext, "plaintext");
		Decrypt((ReadOnlySpan<byte>)nonce, (ReadOnlySpan<byte>)ciphertext, (ReadOnlySpan<byte>)tag, (Span<byte>)plaintext, (ReadOnlySpan<byte>)associatedData);
	}

	public void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		CheckParameters(plaintext, ciphertext, nonce, tag);
		DecryptCore(nonce, ciphertext, tag, plaintext, associatedData);
	}

	private void CheckParameters(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag)
	{
		if (plaintext.Length != ciphertext.Length)
		{
			throw new ArgumentException(System.SR.Cryptography_PlaintextCiphertextLengthMismatch);
		}
		if (!nonce.Length.IsLegalSize(NonceByteSizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidNonceLength, "nonce");
		}
		int? tagSizeInBytes = TagSizeInBytes;
		if (tagSizeInBytes.HasValue)
		{
			int valueOrDefault = tagSizeInBytes.GetValueOrDefault();
			if (tag.Length != valueOrDefault)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Cryptography_IncorrectTagLength, valueOrDefault), "tag");
			}
		}
		else if (!tag.Length.IsLegalSize(TagByteSizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidTagLength, "tag");
		}
	}

	private static void ThrowIfNotSupported()
	{
		if (true)
		{
		}
	}

	[MemberNotNull("_keyHandle")]
	private void ImportKey(ReadOnlySpan<byte> key)
	{
		_keyHandle = global::Interop.BCrypt.BCryptImportKey(BCryptAeadHandleCache.AesGcm, key);
	}

	private void EncryptCore(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		AeadCommon.Encrypt(_keyHandle, nonce, associatedData, plaintext, ciphertext, tag);
	}

	private void DecryptCore(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		AeadCommon.Decrypt(_keyHandle, nonce, associatedData, ciphertext, tag, plaintext, clearPlaintextOnFailure: true);
	}

	public void Dispose()
	{
		_keyHandle.Dispose();
	}
}
