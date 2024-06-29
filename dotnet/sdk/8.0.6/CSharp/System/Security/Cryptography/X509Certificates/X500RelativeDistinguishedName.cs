using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X500RelativeDistinguishedName
{
	private readonly Oid _singleElementType;

	private readonly ReadOnlyMemory<byte> _singleElementValue;

	public ReadOnlyMemory<byte> RawData { get; }

	public bool HasMultipleElements => _singleElementType == null;

	internal X500RelativeDistinguishedName(ReadOnlyMemory<byte> rawData)
	{
		RawData = rawData;
		ReadOnlySpan<byte> span = rawData.Span;
		AsnValueReader asnValueReader = new AsnValueReader(span, AsnEncodingRules.DER).ReadSetOf(null, skipSortOrderValidation: true);
		AsnValueReader asnValueReader2 = asnValueReader.ReadSequence();
		Oid sharedOrNewOid = Oids.GetSharedOrNewOid(ref asnValueReader2);
		ReadOnlySpan<byte> other = asnValueReader2.ReadEncodedValue();
		asnValueReader2.ThrowIfNotEmpty();
		if (asnValueReader.HasData)
		{
			do
			{
				asnValueReader2 = asnValueReader.ReadSequence();
				if (Oids.GetSharedOrNullOid(ref asnValueReader2) == null)
				{
					asnValueReader2.ReadObjectIdentifier();
				}
				asnValueReader2.ReadEncodedValue();
				asnValueReader2.ThrowIfNotEmpty();
			}
			while (asnValueReader.HasData);
		}
		else
		{
			_singleElementType = sharedOrNewOid;
			int elementOffset;
			bool flag = span.Overlaps(other, out elementOffset);
			_singleElementValue = rawData.Slice(elementOffset, other.Length);
		}
	}

	public Oid GetSingleElementType()
	{
		if (_singleElementType == null)
		{
			throw new InvalidOperationException(System.SR.Cryptography_X500_MultiValued);
		}
		return _singleElementType;
	}

	public string? GetSingleElementValue()
	{
		if (_singleElementValue.IsEmpty)
		{
			throw new InvalidOperationException(System.SR.Cryptography_X500_MultiValued);
		}
		try
		{
			AsnValueReader tavReader = new AsnValueReader(_singleElementValue.Span, AsnEncodingRules.DER);
			Asn1Tag asn1Tag = tavReader.PeekTag();
			if (asn1Tag.TagClass == TagClass.Universal)
			{
				switch ((UniversalTagNumber)asn1Tag.TagValue)
				{
				case UniversalTagNumber.UTF8String:
				case UniversalTagNumber.NumericString:
				case UniversalTagNumber.PrintableString:
				case UniversalTagNumber.TeletexString:
				case UniversalTagNumber.IA5String:
				case UniversalTagNumber.BMPString:
					return tavReader.ReadAnyAsnString();
				}
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		return null;
	}
}
