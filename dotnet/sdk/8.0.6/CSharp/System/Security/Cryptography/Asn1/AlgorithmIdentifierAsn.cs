using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct AlgorithmIdentifierAsn
{
	internal string Algorithm;

	internal ReadOnlyMemory<byte>? Parameters;

	internal static readonly ReadOnlyMemory<byte> ExplicitDerNull = new byte[2] { 5, 0 };

	internal void Encode(AsnWriter writer)
	{
		Encode(writer, Asn1Tag.Sequence);
	}

	internal void Encode(AsnWriter writer, Asn1Tag tag)
	{
		writer.PushSequence(tag);
		try
		{
			writer.WriteObjectIdentifier(Algorithm);
		}
		catch (ArgumentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		if (Parameters.HasValue)
		{
			try
			{
				writer.WriteEncodedValue(Parameters.Value.Span);
			}
			catch (ArgumentException inner2)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
			}
		}
		writer.PopSequence(tag);
	}

	internal static AlgorithmIdentifierAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		return Decode(Asn1Tag.Sequence, encoded, ruleSet);
	}

	internal static AlgorithmIdentifierAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
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

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
	{
		decoded = default(AlgorithmIdentifierAsn);
		AsnValueReader asnValueReader = reader.ReadSequence(expectedTag);
		ReadOnlySpan<byte> span = rebind.Span;
		decoded.Algorithm = asnValueReader.ReadObjectIdentifier();
		if (asnValueReader.HasData)
		{
			ReadOnlySpan<byte> other = asnValueReader.ReadEncodedValue();
			decoded.Parameters = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
		}
		asnValueReader.ThrowIfNotEmpty();
	}

	internal readonly bool HasNullEquivalentParameters()
	{
		return RepresentsNull(Parameters);
	}

	internal static bool RepresentsNull(ReadOnlyMemory<byte>? parameters)
	{
		if (!parameters.HasValue)
		{
			return true;
		}
		ReadOnlySpan<byte> span = parameters.Value.Span;
		if (span.Length != 2)
		{
			return false;
		}
		if (span[0] != 5)
		{
			return false;
		}
		return span[1] == 0;
	}
}
