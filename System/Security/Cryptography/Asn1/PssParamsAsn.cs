using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Asn1;

internal struct PssParamsAsn
{
	internal AlgorithmIdentifierAsn HashAlgorithm;

	internal AlgorithmIdentifierAsn MaskGenAlgorithm;

	internal int SaltLength;

	internal int TrailerField;

	private static ReadOnlySpan<byte> DefaultHashAlgorithm => new byte[11]
	{
		48, 9, 6, 5, 43, 14, 3, 2, 26, 5,
		0
	};

	private static ReadOnlySpan<byte> DefaultMaskGenAlgorithm => new byte[24]
	{
		48, 22, 6, 9, 42, 134, 72, 134, 247, 13,
		1, 1, 8, 48, 9, 6, 5, 43, 14, 3,
		2, 26, 5, 0
	};

	private static ReadOnlySpan<byte> DefaultSaltLength => new byte[3] { 2, 1, 20 };

	private static ReadOnlySpan<byte> DefaultTrailerField => new byte[3] { 2, 1, 1 };

	internal RSASignaturePadding GetSignaturePadding(int? digestValueLength = null)
	{
		if (TrailerField != 1)
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs_InvalidSignatureParameters);
		}
		if (MaskGenAlgorithm.Algorithm != "1.2.840.113549.1.1.8")
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs_PssParametersMgfNotSupported, MaskGenAlgorithm.Algorithm);
		}
		if (!MaskGenAlgorithm.Parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs_InvalidSignatureParameters);
		}
		AlgorithmIdentifierAsn algorithmIdentifierAsn = AlgorithmIdentifierAsn.Decode(MaskGenAlgorithm.Parameters.Value, AsnEncodingRules.DER);
		if (algorithmIdentifierAsn.Algorithm != HashAlgorithm.Algorithm)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Pkcs_PssParametersMgfHashMismatch, algorithmIdentifierAsn.Algorithm, HashAlgorithm.Algorithm));
		}
		int num = digestValueLength.GetValueOrDefault();
		if (!digestValueLength.HasValue)
		{
			num = Helpers.HashOidToByteLength(HashAlgorithm.Algorithm);
		}
		if (SaltLength != num)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Pkcs_PssParametersSaltMismatch, SaltLength, HashAlgorithm.Algorithm));
		}
		return RSASignaturePadding.Pss;
	}

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		HashAlgorithm.Encode(asnWriter);
		if (!asnWriter.EncodedValueEquals(DefaultHashAlgorithm))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			asnWriter.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		AsnWriter asnWriter2 = new AsnWriter(AsnEncodingRules.DER);
		MaskGenAlgorithm.Encode(asnWriter2);
		if (!asnWriter2.EncodedValueEquals(DefaultMaskGenAlgorithm))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
			asnWriter2.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		}
		AsnWriter asnWriter3 = new AsnWriter(AsnEncodingRules.DER, 6);
		asnWriter3.WriteInteger(SaltLength);
		if (!asnWriter3.EncodedValueEquals(DefaultSaltLength))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
			asnWriter3.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
		}
		AsnWriter asnWriter4 = new AsnWriter(AsnEncodingRules.DER, 6);
		asnWriter4.WriteInteger(TrailerField);
		if (!asnWriter4.EncodedValueEquals(DefaultTrailerField))
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
			asnWriter4.CopyTo(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
		}
		writer.PopSequence(tag);
	}

	internal static PssParamsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static PssParamsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);
			DecodeCore(ref reader, expectedTag, encoded, out var decoded);
			reader.ThrowIfNotEmpty();
			return decoded;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PssParamsAsn decoded)
	{
		decoded = default(PssParamsAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		AsnValueReader reader2;
		AsnValueReader reader3;
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.HashAlgorithm);
			reader2.ThrowIfNotEmpty();
		}
		else
		{
			reader3 = new AsnValueReader(DefaultHashAlgorithm, AsnEncodingRules.DER);
			AlgorithmIdentifierAsn.Decode(ref reader3, rebind, out decoded.HashAlgorithm);
		}
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
			AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.MaskGenAlgorithm);
			reader2.ThrowIfNotEmpty();
		}
		else
		{
			reader3 = new AsnValueReader(DefaultMaskGenAlgorithm, AsnEncodingRules.DER);
			AlgorithmIdentifierAsn.Decode(ref reader3, rebind, out decoded.MaskGenAlgorithm);
		}
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
			if (!reader2.TryReadInt32(out decoded.SaltLength))
			{
				reader2.ThrowIfNotEmpty();
			}
			reader2.ThrowIfNotEmpty();
		}
		else
		{
			reader3 = new AsnValueReader(DefaultSaltLength, AsnEncodingRules.DER);
			if (!reader3.TryReadInt32(out decoded.SaltLength))
			{
				reader3.ThrowIfNotEmpty();
			}
		}
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
			if (!reader2.TryReadInt32(out decoded.TrailerField))
			{
				reader2.ThrowIfNotEmpty();
			}
			reader2.ThrowIfNotEmpty();
		}
		else
		{
			reader3 = new AsnValueReader(DefaultTrailerField, AsnEncodingRules.DER);
			if (!reader3.TryReadInt32(out decoded.TrailerField))
			{
				reader3.ThrowIfNotEmpty();
			}
		}
		asnValueReader.ThrowIfNotEmpty();
	}
}
