using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct GeneralNameAsn
{
	internal OtherNameAsn? OtherName;

	internal string Rfc822Name;

	internal string DnsName;

	internal ReadOnlyMemory<byte>? X400Address;

	internal ReadOnlyMemory<byte>? DirectoryName;

	internal EdiPartyNameAsn? EdiPartyName;

	internal string Uri;

	internal ReadOnlyMemory<byte>? IPAddress;

	internal string RegisteredId;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (OtherName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			OtherName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 0));
			flag = true;
		}
		if (Rfc822Name != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, Rfc822Name, new Asn1Tag(TagClass.ContextSpecific, 1));
			flag = true;
		}
		if (DnsName != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, DnsName, new Asn1Tag(TagClass.ContextSpecific, 2));
			flag = true;
		}
		if (X400Address.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			if (!Asn1Tag.TryDecode(X400Address.Value.Span, out var tag, out var _) || !tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteEncodedValue(X400Address.Value.Span);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			flag = true;
		}
		if (DirectoryName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
			try
			{
				writer.WriteEncodedValue(DirectoryName.Value.Span);
			}
			catch (ArgumentException inner2)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
			}
			writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
			flag = true;
		}
		if (EdiPartyName.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			EdiPartyName.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 5));
			flag = true;
		}
		if (Uri != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.IA5String, Uri, new Asn1Tag(TagClass.ContextSpecific, 6));
			flag = true;
		}
		if (IPAddress.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteOctetString(IPAddress.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 7));
			flag = true;
		}
		if (RegisteredId != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteObjectIdentifier(RegisteredId, new Asn1Tag(TagClass.ContextSpecific, 8));
			}
			catch (ArgumentException inner3)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner3);
			}
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
	{
		try
		{
			DecodeCore(ref reader, rebind, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out GeneralNameAsn decoded)
	{
		decoded = default(GeneralNameAsn);
		Asn1Tag asn1Tag = reader.PeekTag();
		ReadOnlySpan<byte> span = rebind.Span;
		ReadOnlySpan<byte> value;
		int elementOffset;
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
		{
			OtherNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 0), rebind, out var decoded2);
			decoded.OtherName = decoded2;
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
		{
			decoded.Rfc822Name = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 1));
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
		{
			decoded.DnsName = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2));
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
		{
			value = reader.ReadEncodedValue();
			decoded.X400Address = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 4)))
		{
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 4));
			value = asnValueReader.ReadEncodedValue();
			decoded.DirectoryName = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			asnValueReader.ThrowIfNotEmpty();
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 5)))
		{
			EdiPartyNameAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 5), rebind, out var decoded3);
			decoded.EdiPartyName = decoded3;
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
		{
			decoded.Uri = reader.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 6));
		}
		else if (asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7)))
		{
			if (reader.TryReadPrimitiveOctetString(out value, new Asn1Tag(TagClass.ContextSpecific, 7)))
			{
				decoded.IPAddress = (span.Overlaps(value, out elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
			}
			else
			{
				decoded.IPAddress = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
			}
		}
		else
		{
			if (!asn1Tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 8)))
			{
				throw new CryptographicException();
			}
			decoded.RegisteredId = reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 8));
		}
	}
}
