using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1.Pkcs12;

internal struct MacData
{
	internal DigestInfoAsn Mac;

	internal ReadOnlyMemory<byte> MacSalt;

	internal int IterationCount;

	private static ReadOnlySpan<byte> DefaultIterationCount => new byte[3] { 2, 1, 1 };

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out MacData decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
	{
		decoded = default(MacData);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		DigestInfoAsn.Decode(ref reader2, rebind, out decoded.Mac);
		if (reader2.TryReadPrimitiveOctetString(out var value))
		{
			decoded.MacSalt = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.MacSalt = reader2.ReadOctetString();
		}
		if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
		{
			if (!reader2.TryReadInt32(out decoded.IterationCount))
			{
				reader2.ThrowIfNotEmpty();
			}
		}
		else
		{
			AsnValueReader asnValueReader = new AsnValueReader(DefaultIterationCount, AsnEncodingRules.DER);
			if (!asnValueReader.TryReadInt32(out decoded.IterationCount))
			{
				asnValueReader.ThrowIfNotEmpty();
			}
		}
		reader2.ThrowIfNotEmpty();
	}
}
