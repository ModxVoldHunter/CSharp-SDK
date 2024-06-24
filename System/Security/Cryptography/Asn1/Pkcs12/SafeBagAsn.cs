using System.Collections.Generic;
using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1.Pkcs12;

internal struct SafeBagAsn
{
	internal string BagId;

	internal ReadOnlyMemory<byte> BagValue;

	internal AttributeAsn[] BagAttributes;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
	{
		decoded = default(SafeBagAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.BagId = asnValueReader.ReadObjectIdentifier();
		AsnValueReader asnValueReader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		ReadOnlySpan<byte> other = asnValueReader2.ReadEncodedValue();
		decoded.BagValue = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
		asnValueReader2.ThrowIfNotEmpty();
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.SetOf))
		{
			AsnValueReader reader2 = asnValueReader.ReadSetOf();
			List<AttributeAsn> list = new List<AttributeAsn>();
			while (reader2.HasData)
			{
				AttributeAsn.Decode(ref reader2, rebind, out var decoded2);
				list.Add(decoded2);
			}
			decoded.BagAttributes = list.ToArray();
		}
		asnValueReader.ThrowIfNotEmpty();
	}
}
