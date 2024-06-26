using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public abstract class ECDsa : ECAlgorithm
{
	public override string? KeyExchangeAlgorithm => null;

	public override string SignatureAlgorithm => "ECDsa";

	[UnsupportedOSPlatform("browser")]
	public new static ECDsa Create()
	{
		return new ECDsaWrapper(new ECDsaCng());
	}

	[UnsupportedOSPlatform("browser")]
	public static ECDsa Create(ECCurve curve)
	{
		return new ECDsaWrapper(new ECDsaCng(curve));
	}

	[UnsupportedOSPlatform("browser")]
	public static ECDsa Create(ECParameters parameters)
	{
		ECDsaCng eCDsaCng = new ECDsaCng();
		eCDsaCng.ImportParameters(parameters);
		return new ECDsaWrapper(eCDsaCng);
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static ECDsa? Create(string algorithm)
	{
		ArgumentNullException.ThrowIfNull(algorithm, "algorithm");
		return CryptoConfig.CreateFromName(algorithm) as ECDsa;
	}

	public virtual byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		return SignData(data, 0, data.Length, hashAlgorithm);
	}

	public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, data.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, data.Length - offset, "count");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return SignHash(hash);
	}

	public byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, data.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, data.Length - offset, "count");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
	}

	protected virtual byte[] SignDataCore(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[256];
		int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
		byte[] array = null;
		bool flag = false;
		int bytesWritten = 0;
		if (maxSignatureSize > destination.Length)
		{
			array = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
			destination = array;
		}
		try
		{
			if (!TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten))
			{
				throw new CryptographicException();
			}
			byte[] result = destination.Slice(0, bytesWritten).ToArray();
			flag = true;
			return result;
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(array.AsSpan(0, bytesWritten));
				if (flag)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
			}
		}
	}

	public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
	}

	public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
	}

	protected virtual byte[] SignDataCore(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		byte[] array = HashData(data, hashAlgorithm);
		return SignHashCore(array, signatureFormat);
	}

	public byte[] SignHash(byte[] hash, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignHashCore(hash, signatureFormat);
	}

	public byte[] SignHash(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignHashCore(hash, signatureFormat);
	}

	public byte[] SignHash(ReadOnlySpan<byte> hash)
	{
		return SignHashCore(hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	public int SignHash(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		if (TrySignHashCore(hash, destination, signatureFormat, out var bytesWritten))
		{
			return bytesWritten;
		}
		throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
	}

	public int SignHash(ReadOnlySpan<byte> hash, Span<byte> destination)
	{
		if (TrySignHashCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out var bytesWritten))
		{
			return bytesWritten;
		}
		throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
	}

	protected virtual byte[] SignHashCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[256];
		int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
		byte[] array = null;
		bool flag = false;
		int bytesWritten = 0;
		if (maxSignatureSize > destination.Length)
		{
			array = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
			destination = array;
		}
		try
		{
			if (!TrySignHashCore(hash, destination, signatureFormat, out bytesWritten))
			{
				throw new CryptographicException();
			}
			byte[] result = destination.Slice(0, bytesWritten).ToArray();
			flag = true;
			return result;
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(array.AsSpan(0, bytesWritten));
				if (flag)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
			}
		}
	}

	public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return TrySignHash(hash, destination, out bytesWritten);
	}

	public bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
	}

	protected virtual bool TrySignDataCore(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
	}

	public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		byte[] hash = HashData(data, hashAlgorithm);
		return SignHash(hash);
	}

	public byte[] SignData(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		return SignDataCore(data, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	public byte[] SignData(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
	}

	public int SignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		if (TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out var bytesWritten))
		{
			return bytesWritten;
		}
		throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
	}

	public int SignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (TrySignDataCore(data, destination, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out var bytesWritten))
		{
			return bytesWritten;
		}
		throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
	}

	public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
	}

	public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, data.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, data.Length - offset, "count");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, data.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, data.Length - offset, "count");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, signatureFormat);
	}

	public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	protected virtual bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> span = stackalloc byte[64];
		span = ((!TryHashData(data, span, hashAlgorithm, out var bytesWritten)) ? ((Span<byte>)HashData(data.ToArray(), 0, data.Length, hashAlgorithm)) : span.Slice(0, bytesWritten));
		return VerifyHashCore(span, signature, signatureFormat);
	}

	public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		byte[] hash = HashData(data, hashAlgorithm);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	protected virtual bool VerifyDataCore(Stream data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		byte[] array = HashData(data, hashAlgorithm);
		return VerifyHashCore(array, signature, signatureFormat);
	}

	public abstract byte[] SignHash(byte[] hash);

	public abstract bool VerifyHash(byte[] hash, byte[] signature);

	protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		return HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));
	}

	protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		return HashOneShotHelpers.HashData(hashAlgorithm, data);
	}

	protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		if (this is IRuntimeAlgorithm)
		{
			return HashOneShotHelpers.TryHashData(hashAlgorithm, data, destination, out bytesWritten);
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		bool flag = false;
		try
		{
			data.CopyTo(array);
			byte[] array2 = HashData(array, 0, data.Length, hashAlgorithm);
			flag = true;
			if (array2.Length <= destination.Length)
			{
				new ReadOnlySpan<byte>(array2).CopyTo(destination);
				bytesWritten = array2.Length;
				return true;
			}
			bytesWritten = 0;
			return false;
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			if (flag)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
	{
		return TrySignHashCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
	}

	public bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
	}

	protected virtual bool TrySignHashCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		byte[] signature = SignHash(hash.ToArray());
		byte[] array = AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(signature, signatureFormat);
		return Helpers.TryCopyToDestination(array, destination, out bytesWritten);
	}

	public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	public bool VerifyHash(byte[] hash, byte[] signature, DSASignatureFormat signatureFormat)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		ArgumentNullException.ThrowIfNull(signature, "signature");
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyHashCore(hash, signature, signatureFormat);
	}

	public bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyHashCore(hash, signature, signatureFormat);
	}

	protected virtual bool VerifyHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		byte[] array = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
		if (array == null)
		{
			return false;
		}
		return VerifyHash(hash.ToArray(), array);
	}

	private ReadOnlySpan<byte> HashSpanToTmp(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Span<byte> tmp)
	{
		if (TryHashData(data, tmp, hashAlgorithm, out var bytesWritten))
		{
			return tmp.Slice(0, bytesWritten);
		}
		return HashSpanToArray(data, hashAlgorithm);
	}

	private byte[] HashSpanToArray(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		bool flag = false;
		try
		{
			data.CopyTo(array);
			byte[] result = HashData(array, 0, data.Length, hashAlgorithm);
			flag = true;
			return result;
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			if (flag)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	public int GetMaxSignatureSize(DSASignatureFormat signatureFormat)
	{
		int keySize = KeySize;
		if (keySize == 0)
		{
			ExportParameters(includePrivateParameters: false);
			keySize = KeySize;
			if (keySize == 0)
			{
				throw new NotSupportedException(System.SR.Cryptography_InvalidKeySize);
			}
		}
		return signatureFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => AsymmetricAlgorithmHelpers.BitsToBytes(keySize) * 2, 
			DSASignatureFormat.Rfc3279DerSequence => AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(keySize), 
			_ => throw new ArgumentOutOfRangeException("signatureFormat"), 
		};
	}

	public override void FromXmlString(string xmlString)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}
}
