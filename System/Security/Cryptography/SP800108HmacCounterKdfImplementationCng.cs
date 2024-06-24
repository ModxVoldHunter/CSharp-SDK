using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class SP800108HmacCounterKdfImplementationCng : SP800108HmacCounterKdfImplementationBase
{
	private static readonly SafeBCryptAlgorithmHandle s_sp800108CtrHmacAlgorithmHandle = OpenAlgorithmHandle();

	private readonly SafeBCryptKeyHandle _keyHandle;

	private readonly HashAlgorithmName _hashAlgorithm;

	public override void Dispose()
	{
		_keyHandle.Dispose();
	}

	internal unsafe override void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		if (destination.Length == 0)
		{
			return;
		}
		fixed (byte* pvBuffer = label)
		{
			fixed (byte* pvBuffer2 = context)
			{
				fixed (byte* pbDerivedKey = destination)
				{
					fixed (char* pvBuffer3 = _hashAlgorithm.Name)
					{
						global::Interop.BCrypt.BCryptBuffer* ptr = stackalloc global::Interop.BCrypt.BCryptBuffer[3];
						ptr->BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_LABEL;
						ptr->pvBuffer = (nint)pvBuffer;
						ptr->cbBuffer = label.Length;
						ptr[1].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_CONTEXT;
						ptr[1].pvBuffer = (nint)pvBuffer2;
						ptr[1].cbBuffer = context.Length;
						ptr[2].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_HASH_ALGORITHM;
						ptr[2].pvBuffer = (nint)pvBuffer3;
						ptr[2].cbBuffer = (_hashAlgorithm.Name.Length + 1) * 2;
						Unsafe.SkipInit(out global::Interop.BCrypt.BCryptBufferDesc bCryptBufferDesc);
						bCryptBufferDesc.ulVersion = 0;
						bCryptBufferDesc.cBuffers = 3;
						bCryptBufferDesc.pBuffers = (nint)ptr;
						uint pcbResult;
						global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptKeyDerivation(_keyHandle, &bCryptBufferDesc, pbDerivedKey, destination.Length, out pcbResult, 0);
						if (nTSTATUS != 0)
						{
							throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
						}
						if (destination.Length != pcbResult)
						{
							throw new CryptographicException();
						}
					}
				}
			}
		}
	}

	internal override void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		Span<byte> stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding = new Utf8DataEncoding(label, stackBuffer);
		stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding2 = new Utf8DataEncoding(context, stackBuffer);
		DeriveBytes(utf8DataEncoding.Utf8Bytes, utf8DataEncoding2.Utf8Bytes, destination);
	}

	internal static void DeriveBytesOneShot(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		using SP800108HmacCounterKdfImplementationCng sP800108HmacCounterKdfImplementationCng = new SP800108HmacCounterKdfImplementationCng(key, hashAlgorithm);
		sP800108HmacCounterKdfImplementationCng.DeriveBytes(label, context, destination);
	}

	internal static void DeriveBytesOneShot(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		if (destination.Length == 0)
		{
			return;
		}
		Span<byte> stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding = new Utf8DataEncoding(label, stackBuffer);
		stackBuffer = stackalloc byte[256];
		using Utf8DataEncoding utf8DataEncoding2 = new Utf8DataEncoding(context, stackBuffer);
		DeriveBytesOneShot(key, hashAlgorithm, utf8DataEncoding.Utf8Bytes, utf8DataEncoding2.Utf8Bytes, destination);
	}

	private unsafe static SafeBCryptKeyHandle CreateSymmetricKey(byte* symmetricKey, int symmetricKeyLength)
	{
		SafeBCryptKeyHandle phKey;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = ((s_sp800108CtrHmacAlgorithmHandle == null) ? global::Interop.BCrypt.BCryptGenerateSymmetricKey(833u, out phKey, IntPtr.Zero, 0, symmetricKey, symmetricKeyLength, 0u) : global::Interop.BCrypt.BCryptGenerateSymmetricKey(s_sp800108CtrHmacAlgorithmHandle, out phKey, IntPtr.Zero, 0, symmetricKey, symmetricKeyLength, 0u));
		if (nTSTATUS != 0)
		{
			phKey.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		return phKey;
	}

	private static SafeBCryptAlgorithmHandle OpenAlgorithmHandle()
	{
		if (!global::Interop.BCrypt.PseudoHandlesSupported)
		{
			SafeBCryptAlgorithmHandle phAlgorithm;
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptOpenAlgorithmProvider(out phAlgorithm, "SP800_108_CTR_HMAC", null, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);
			if (nTSTATUS != 0)
			{
				phAlgorithm.Dispose();
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
			return phAlgorithm;
		}
		return null;
	}

	private static int GetHashBlockSize(string hashAlgorithmName)
	{
		switch (hashAlgorithmName)
		{
		case "SHA256":
		case "SHA1":
			return 64;
		case "SHA384":
		case "SHA512":
			return 128;
		case "SHA3-256":
			return 136;
		case "SHA3-384":
			return 104;
		case "SHA3-512":
			return 72;
		default:
			throw new CryptographicException();
		}
	}

	internal unsafe SP800108HmacCounterKdfImplementationCng(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
	{
		Span<byte> span = default(Span<byte>);
		int hashBlockSize = GetHashBlockSize(hashAlgorithm.Name);
		int num;
		ReadOnlySpan<byte> readOnlySpan;
		if (key.Length > hashBlockSize)
		{
			Span<byte> destination = stackalloc byte[64];
			num = HashOneShot(hashAlgorithm, key, destination);
			span = destination.Slice(0, num);
			readOnlySpan = span;
		}
		else if (!key.IsEmpty)
		{
			readOnlySpan = key;
			num = key.Length;
		}
		else
		{
			Span<byte> span2 = stackalloc byte[1] { 0 };
			readOnlySpan = span2;
			num = 0;
		}
		try
		{
			fixed (byte* symmetricKey = readOnlySpan)
			{
				_keyHandle = CreateSymmetricKey(symmetricKey, num);
			}
		}
		finally
		{
			CryptographicOperations.ZeroMemory(span);
		}
		_hashAlgorithm = hashAlgorithm;
	}

	private static int HashOneShot(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> data, Span<byte> destination)
	{
		switch (hashAlgorithm.Name)
		{
		case "SHA1":
			return SHA1.HashData(data, destination);
		case "SHA256":
			return SHA256.HashData(data, destination);
		case "SHA384":
			return SHA384.HashData(data, destination);
		case "SHA512":
			return SHA512.HashData(data, destination);
		case "SHA3-256":
			if (!SHA3_256.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			return SHA3_256.HashData(data, destination);
		case "SHA3-384":
			if (!SHA3_384.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			return SHA3_384.HashData(data, destination);
		case "SHA3-512":
			if (!SHA3_512.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			return SHA3_512.HashData(data, destination);
		default:
			throw new CryptographicException();
		}
	}
}
