using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates;

internal static class X500DirectoryStringHelper
{
	internal static string ReadAnyAsnString(this ref AsnValueReader tavReader)
	{
		Asn1Tag asn1Tag = tavReader.PeekTag();
		if (asn1Tag.TagClass != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		switch ((UniversalTagNumber)asn1Tag.TagValue)
		{
		case UniversalTagNumber.UTF8String:
		case UniversalTagNumber.NumericString:
		case UniversalTagNumber.PrintableString:
		case UniversalTagNumber.TeletexString:
		case UniversalTagNumber.IA5String:
		case UniversalTagNumber.BMPString:
			return tavReader.ReadCharacterString((UniversalTagNumber)asn1Tag.TagValue).TrimEnd('\0');
		default:
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
	}
}
