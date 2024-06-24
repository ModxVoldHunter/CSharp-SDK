using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct DigestInfoAsn
{
	internal AlgorithmIdentifierAsn DigestAlgorithm;

	internal ReadOnlyMemory<byte> Digest;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
	{
		decoded = default(DigestInfoAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		AlgorithmIdentifierAsn.Decode(ref reader2, rebind, out decoded.DigestAlgorithm);
		if (reader2.TryReadPrimitiveOctetString(out var value))
		{
			decoded.Digest = (span.Overlaps(value, out var elementOffset) ? rebind.Slice(elementOffset, value.Length) : ((ReadOnlyMemory<byte>)value.ToArray()));
		}
		else
		{
			decoded.Digest = reader2.ReadOctetString();
		}
		reader2.ThrowIfNotEmpty();
	}
}
