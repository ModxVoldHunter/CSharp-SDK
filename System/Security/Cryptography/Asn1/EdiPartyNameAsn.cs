using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct EdiPartyNameAsn
{
	internal DirectoryStringAsn? NameAssigner;

	internal DirectoryStringAsn PartyName;

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		if (NameAssigner.HasValue)
		{
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			NameAssigner.Value.Encode(writer);
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		PartyName.Encode(writer);
		writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		writer.PopSequence(tag);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EdiPartyNameAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EdiPartyNameAsn decoded)
	{
		decoded = default(EdiPartyNameAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		AsnValueReader reader2;
		if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
			DirectoryStringAsn.Decode(ref reader2, rebind, out var decoded2);
			decoded.NameAssigner = decoded2;
			reader2.ThrowIfNotEmpty();
		}
		reader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
		DirectoryStringAsn.Decode(ref reader2, rebind, out decoded.PartyName);
		reader2.ThrowIfNotEmpty();
		asnValueReader.ThrowIfNotEmpty();
	}
}
