using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct AccessDescriptionAsn
{
	internal string AccessMethod;

	internal GeneralNameAsn AccessLocation;

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out AccessDescriptionAsn decoded)
	{
		Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
	}

	internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AccessDescriptionAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AccessDescriptionAsn decoded)
	{
		decoded = default(AccessDescriptionAsn);
		AsnValueReader reader2 = reader.ReadSequence(expectedTag);
		decoded.AccessMethod = reader2.ReadObjectIdentifier();
		GeneralNameAsn.Decode(ref reader2, rebind, out decoded.AccessLocation);
		reader2.ThrowIfNotEmpty();
	}
}
