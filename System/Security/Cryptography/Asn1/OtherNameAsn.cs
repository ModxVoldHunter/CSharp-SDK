using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct OtherNameAsn
{
	internal string TypeId;

	internal ReadOnlyMemory<byte> Value;

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(TypeId);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		try
		{
			writer.WriteEncodedValue(Value.Span);
		}
		catch (ArgumentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
		writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		writer.PopSequence(tag);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OtherNameAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OtherNameAsn decoded)
	{
		decoded = default(OtherNameAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.TypeId = asnValueReader.ReadObjectIdentifier();
		AsnValueReader asnValueReader2 = asnValueReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
		ReadOnlySpan<byte> other = asnValueReader2.ReadEncodedValue();
		decoded.Value = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
		asnValueReader2.ThrowIfNotEmpty();
		asnValueReader.ThrowIfNotEmpty();
	}
}
