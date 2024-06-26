using System.Collections.Generic;
using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct CertificationRequestInfoAsn
{
	internal BigInteger Version;

	internal ReadOnlyMemory<byte> Subject;

	internal SubjectPublicKeyInfoAsn SubjectPublicKeyInfo;

	internal AttributeAsn[] Attributes;

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		writer.WriteInteger(Version);
		if (!Asn1Tag.TryDecode(Subject.Span, out var tag2, out var _) || !tag2.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		try
		{
			writer.WriteEncodedValue(Subject.Span);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		SubjectPublicKeyInfo.Encode(writer);
		writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
		for (int i = 0; i < Attributes.Length; i++)
		{
			Attributes[i].Encode(writer);
		}
		writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
		writer.PopSequence(tag);
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestInfoAsn decoded)
	{
		decoded = default(CertificationRequestInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.Version = reader2.ReadInteger();
		if (!reader2.PeekTag().HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.Sequence)))
		{
			throw new CryptographicException();
		}
		ReadOnlySpan<byte> other = reader2.ReadEncodedValue();
		decoded.Subject = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
		SubjectPublicKeyInfoAsn.Decode(ref reader2, rebind, out decoded.SubjectPublicKeyInfo);
		AsnValueReader reader3 = reader2.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
		List<AttributeAsn> list = new List<AttributeAsn>();
		while (reader3.HasData)
		{
			AttributeAsn.Decode(ref reader3, rebind, out var decoded2);
			list.Add(decoded2);
		}
		decoded.Attributes = list.ToArray();
		reader2.ThrowIfNotEmpty();
	}
}
