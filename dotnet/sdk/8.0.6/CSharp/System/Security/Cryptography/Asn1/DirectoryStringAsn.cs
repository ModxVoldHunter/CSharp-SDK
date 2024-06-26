using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct DirectoryStringAsn
{
	internal string TeletexString;

	internal string PrintableString;

	internal ReadOnlyMemory<byte>? UniversalString;

	internal string Utf8String;

	internal string BmpString;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (TeletexString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.TeletexString, TeletexString);
			flag = true;
		}
		if (PrintableString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.PrintableString, PrintableString);
			flag = true;
		}
		if (UniversalString.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			if (!Asn1Tag.TryDecode(UniversalString.Value.Span, out var tag, out var _) || !tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.UniversalString)))
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteEncodedValue(UniversalString.Value.Span);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			flag = true;
		}
		if (Utf8String != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.UTF8String, Utf8String);
			flag = true;
		}
		if (BmpString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.BMPString, BmpString);
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DirectoryStringAsn decoded)
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

	private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DirectoryStringAsn decoded)
	{
		decoded = default(DirectoryStringAsn);
		Asn1Tag asn1Tag = reader.PeekTag();
		ReadOnlySpan<byte> span = rebind.Span;
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.TeletexString)))
		{
			decoded.TeletexString = reader.ReadCharacterString(UniversalTagNumber.TeletexString);
			return;
		}
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.PrintableString)))
		{
			decoded.PrintableString = reader.ReadCharacterString(UniversalTagNumber.PrintableString);
			return;
		}
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.UniversalString)))
		{
			ReadOnlySpan<byte> other = reader.ReadEncodedValue();
			decoded.UniversalString = (span.Overlaps(other, out var elementOffset) ? rebind.Slice(elementOffset, other.Length) : ((ReadOnlyMemory<byte>)other.ToArray()));
			return;
		}
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.UTF8String)))
		{
			decoded.Utf8String = reader.ReadCharacterString(UniversalTagNumber.UTF8String);
			return;
		}
		if (asn1Tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.BMPString)))
		{
			decoded.BmpString = reader.ReadCharacterString(UniversalTagNumber.BMPString);
			return;
		}
		throw new CryptographicException();
	}
}
