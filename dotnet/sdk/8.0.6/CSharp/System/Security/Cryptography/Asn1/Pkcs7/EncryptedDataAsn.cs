using System.Collections.Generic;
using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1.Pkcs7;

internal struct EncryptedDataAsn
{
	internal int Version;

	internal EncryptedContentInfoAsn EncryptedContentInfo;

	internal AttributeAsn[] UnprotectedAttributes;

	internal static EncryptedDataAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static EncryptedDataAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
	{
		decoded = default(EncryptedDataAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		if (!reader2.TryReadInt32(out decoded.Version))
		{
			reader2.ThrowIfNotEmpty();
		}
		EncryptedContentInfoAsn.Decode(ref reader2, rebind, out decoded.EncryptedContentInfo);
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
		{
			AsnValueReader reader3 = reader2.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
			List<AttributeAsn> list = new List<AttributeAsn>();
			while (reader3.HasData)
			{
				AttributeAsn.Decode(ref reader3, rebind, out var decoded2);
				list.Add(decoded2);
			}
			decoded.UnprotectedAttributes = list.ToArray();
		}
		reader2.ThrowIfNotEmpty();
	}
}
