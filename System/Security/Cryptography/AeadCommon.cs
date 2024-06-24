using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal static class AeadCommon
{
	public unsafe static void Encrypt(SafeKeyHandle keyHandle, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag)
	{
		fixed (byte* pbInput = &Helpers.GetNonNullPinnableReference(plaintext))
		{
			fixed (byte* pbNonce = &Helpers.GetNonNullPinnableReference(nonce))
			{
				fixed (byte* pbOutput = &Helpers.GetNonNullPinnableReference(ciphertext))
				{
					fixed (byte* pbTag = &Helpers.GetNonNullPinnableReference(tag))
					{
						fixed (byte* pbAuthData = &Helpers.GetNonNullPinnableReference(associatedData))
						{
							global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO = global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbNonce = pbNonce;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbNonce = nonce.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbTag = pbTag;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbTag = tag.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbAuthData = pbAuthData;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbAuthData = associatedData.Length;
							int cbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptEncrypt(keyHandle, pbInput, plaintext.Length, new IntPtr(&bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO), null, 0, pbOutput, ciphertext.Length, out cbResult, 0);
							if (nTSTATUS != 0)
							{
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
							}
						}
					}
				}
			}
		}
	}

	public unsafe static void Decrypt(SafeKeyHandle keyHandle, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, bool clearPlaintextOnFailure)
	{
		fixed (byte* pbOutput = &Helpers.GetNonNullPinnableReference(plaintext))
		{
			fixed (byte* pbNonce = &Helpers.GetNonNullPinnableReference(nonce))
			{
				fixed (byte* pbInput = &Helpers.GetNonNullPinnableReference(ciphertext))
				{
					fixed (byte* pbTag = &Helpers.GetNonNullPinnableReference(tag))
					{
						fixed (byte* pbAuthData = &Helpers.GetNonNullPinnableReference(associatedData))
						{
							global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO = global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbNonce = pbNonce;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbNonce = nonce.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbTag = pbTag;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbTag = tag.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbAuthData = pbAuthData;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbAuthData = associatedData.Length;
							int cbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDecrypt(keyHandle, pbInput, ciphertext.Length, new IntPtr(&bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO), null, 0, pbOutput, plaintext.Length, out cbResult, 0);
							switch (nTSTATUS)
							{
							case global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS:
								break;
							case global::Interop.BCrypt.NTSTATUS.STATUS_AUTH_TAG_MISMATCH:
								if (clearPlaintextOnFailure)
								{
									CryptographicOperations.ZeroMemory(plaintext);
								}
								throw new AuthenticationTagMismatchException();
							default:
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
							}
						}
					}
				}
			}
		}
	}
}
