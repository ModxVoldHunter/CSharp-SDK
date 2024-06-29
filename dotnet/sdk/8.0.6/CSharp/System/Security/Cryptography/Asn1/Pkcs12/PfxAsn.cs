using System.Formats.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Asn1.Pkcs12;

internal struct PfxAsn
{
	internal int Version;

	internal ContentInfoAsn AuthSafe;

	internal MacData? MacData;

	private static ReadOnlySpan<char> EmptyPassword => "";

	private static ReadOnlySpan<char> NullPassword => default(ReadOnlySpan<char>);

	internal ulong CountTotalIterations()
	{
		ulong num = 0uL;
		if (AuthSafe.ContentType != "1.2.840.113549.1.7.1")
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		ReadOnlyMemory<byte> rebind = Helpers.DecodeOctetStringAsMemory(AuthSafe.Content);
		AsnValueReader asnValueReader = new AsnValueReader(rebind.Span, AsnEncodingRules.BER);
		AsnValueReader reader = asnValueReader.ReadSequence();
		asnValueReader.ThrowIfNotEmpty();
		bool flag = false;
		checked
		{
			while (reader.HasData)
			{
				ContentInfoAsn.Decode(ref reader, rebind, out var decoded);
				ArraySegment<byte>? arraySegment = null;
				try
				{
					ReadOnlyMemory<byte> rebind2;
					if (decoded.ContentType != "1.2.840.113549.1.7.1")
					{
						if (!(decoded.ContentType == "1.2.840.113549.1.7.6"))
						{
							throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
						}
						if (flag)
						{
							throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
						}
						uint iterations;
						ArraySegment<byte> arraySegment2 = DecryptContentInfo(decoded, out iterations);
						rebind2 = arraySegment2;
						arraySegment = arraySegment2;
						flag = true;
						num += iterations;
					}
					else
					{
						rebind2 = Helpers.DecodeOctetStringAsMemory(decoded.Content);
					}
					AsnValueReader asnValueReader2 = new AsnValueReader(rebind2.Span, AsnEncodingRules.BER);
					AsnValueReader reader2 = asnValueReader2.ReadSequence();
					asnValueReader2.ThrowIfNotEmpty();
					while (reader2.HasData)
					{
						SafeBagAsn.Decode(ref reader2, rebind2, out var decoded2);
						if (decoded2.BagId == "1.2.840.113549.1.12.10.1.2")
						{
							AsnValueReader reader3 = new AsnValueReader(decoded2.BagValue.Span, AsnEncodingRules.BER);
							EncryptedPrivateKeyInfoAsn.Decode(ref reader3, decoded2.BagValue, out var decoded3);
							num += IterationsFromParameters(in decoded3.EncryptionAlgorithm);
						}
					}
				}
				finally
				{
					if (arraySegment.HasValue)
					{
						System.Security.Cryptography.CryptoPool.Return(arraySegment.Value);
					}
				}
			}
			if (MacData.HasValue)
			{
				if (MacData.Value.IterationCount < 0)
				{
					throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
				}
				num += (uint)MacData.Value.IterationCount;
			}
			return num;
		}
	}

	private static ArraySegment<byte> DecryptContentInfo(ContentInfoAsn contentInfo, out uint iterations)
	{
		EncryptedDataAsn encryptedDataAsn = EncryptedDataAsn.Decode(contentInfo.Content, AsnEncodingRules.BER);
		if (encryptedDataAsn.Version != 0 && encryptedDataAsn.Version != 2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (encryptedDataAsn.EncryptedContentInfo.ContentType != "1.2.840.113549.1.7.1")
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (!encryptedDataAsn.EncryptedContentInfo.EncryptedContent.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		iterations = IterationsFromParameters(in encryptedDataAsn.EncryptedContentInfo.ContentEncryptionAlgorithm);
		if (iterations > 300000)
		{
			throw new X509IterationCountExceededException();
		}
		int length = encryptedDataAsn.EncryptedContentInfo.EncryptedContent.Value.Length;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(length);
		int num = 0;
		try
		{
			num = PasswordBasedEncryption.Decrypt(in encryptedDataAsn.EncryptedContentInfo.ContentEncryptionAlgorithm, EmptyPassword, default(ReadOnlySpan<byte>), encryptedDataAsn.EncryptedContentInfo.EncryptedContent.Value.Span, array);
			AsnValueReader asnValueReader = new AsnValueReader(array.AsSpan(0, num), AsnEncodingRules.BER);
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
		}
		catch
		{
			num = PasswordBasedEncryption.Decrypt(in encryptedDataAsn.EncryptedContentInfo.ContentEncryptionAlgorithm, NullPassword, default(ReadOnlySpan<byte>), encryptedDataAsn.EncryptedContentInfo.EncryptedContent.Value.Span, array);
			AsnValueReader asnValueReader3 = new AsnValueReader(array.AsSpan(0, num), AsnEncodingRules.BER);
			AsnValueReader asnValueReader4 = asnValueReader3.ReadSequence();
			asnValueReader3.ThrowIfNotEmpty();
		}
		finally
		{
			if (num == 0)
			{
				CryptographicOperations.ZeroMemory(array);
			}
		}
		return new ArraySegment<byte>(array, 0, num);
	}

	private static uint IterationsFromParameters(in AlgorithmIdentifierAsn algorithmIdentifier)
	{
		switch (algorithmIdentifier.Algorithm)
		{
		case "1.2.840.113549.1.5.13":
		{
			if (!algorithmIdentifier.Parameters.HasValue)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			PBES2Params pBES2Params = PBES2Params.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);
			if (pBES2Params.KeyDerivationFunc.Algorithm != "1.2.840.113549.1.5.12")
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			if (!pBES2Params.KeyDerivationFunc.Parameters.HasValue)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(pBES2Params.KeyDerivationFunc.Parameters.Value, AsnEncodingRules.BER);
			if (pbkdf2Params.IterationCount < 0)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			return (uint)pbkdf2Params.IterationCount;
		}
		case "1.2.840.113549.1.5.10":
		case "1.2.840.113549.1.5.11":
		case "1.2.840.113549.1.5.3":
		case "1.2.840.113549.1.5.6":
		case "1.2.840.113549.1.12.1.3":
		case "1.2.840.113549.1.12.1.4":
		case "1.2.840.113549.1.12.1.5":
		case "1.2.840.113549.1.12.1.6":
		{
			if (!algorithmIdentifier.Parameters.HasValue)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			PBEParameter pBEParameter = PBEParameter.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);
			if (pBEParameter.IterationCount < 0)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			return (uint)pBEParameter.IterationCount;
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out PfxAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PfxAsn decoded)
	{
		try
		{
			DecodeCore(ref reader, expectedTag, rebind, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PfxAsn decoded)
	{
		decoded = default(PfxAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		if (!reader2.TryReadInt32(out decoded.Version))
		{
			reader2.ThrowIfNotEmpty();
		}
		ContentInfoAsn.Decode(ref reader2, rebind, out decoded.AuthSafe);
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
		{
			System.Security.Cryptography.Asn1.Pkcs12.MacData.Decode(ref reader2, rebind, out var decoded2);
			decoded.MacData = decoded2;
		}
		reader2.ThrowIfNotEmpty();
	}
}
