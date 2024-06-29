using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1.Pkcs7;

internal struct EncryptedContentInfoAsn
{
	internal string ContentType;

	internal AlgorithmIdentifierAsn ContentEncryptionAlgorithm;

	internal ReadOnlyMemory<byte>? EncryptedContent;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
	{
		decoded = default(EncryptedContentInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.ContentType = reader2.ReadObjectIdentifier();
		AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.ContentEncryptionAlgorithm);
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			if (reader2.TryReadPrimitiveOctetString(out var value, new Asn1Tag(TagClass.ContextSpecific, 0)))
			{
				decoded.EncryptedContent = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.EncryptedContent = reader2.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
			}
		}
		reader2.ThrowIfNotEmpty();
	}
}
