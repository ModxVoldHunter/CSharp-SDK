using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X500DistinguishedNameBuilder
{
	private readonly List<byte[]> _encodedComponents = new List<byte[]>();

	private readonly AsnWriter _writer = new AsnWriter(AsnEncodingRules.DER);

	public void Add(string oidValue, string value, UniversalTagNumber? stringEncodingType = null)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentException.ThrowIfNullOrEmpty(oidValue, "oidValue");
		UniversalTagNumber andValidateTagNumber = GetAndValidateTagNumber(stringEncodingType);
		EncodeComponent(oidValue, value, andValidateTagNumber, "value");
	}

	public void Add(Oid oid, string value, UniversalTagNumber? stringEncodingType = null)
	{
		ArgumentNullException.ThrowIfNull(oid, "oid");
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentException.ThrowIfNullOrEmpty(oid.Value, "oid.Value");
		UniversalTagNumber andValidateTagNumber = GetAndValidateTagNumber(stringEncodingType);
		EncodeComponent(oid.Value, value, andValidateTagNumber, "value");
	}

	public void AddEmailAddress(string emailAddress)
	{
		ArgumentException.ThrowIfNullOrEmpty(emailAddress, "emailAddress");
		if (emailAddress.Length > 255)
		{
			throw new ArgumentException(System.SR.Argument_X500_EmailTooLong, "emailAddress");
		}
		EncodeComponent("1.2.840.113549.1.9.1", emailAddress, UniversalTagNumber.IA5String, "emailAddress");
	}

	public void AddCommonName(string commonName)
	{
		ArgumentException.ThrowIfNullOrEmpty(commonName, "commonName");
		EncodeComponent("2.5.4.3", commonName, UniversalTagNumber.UTF8String, "commonName");
	}

	public void AddLocalityName(string localityName)
	{
		ArgumentException.ThrowIfNullOrEmpty(localityName, "localityName");
		EncodeComponent("2.5.4.7", localityName, UniversalTagNumber.UTF8String, "localityName");
	}

	public void AddCountryOrRegion(string twoLetterCode)
	{
		ArgumentException.ThrowIfNullOrEmpty(twoLetterCode, "twoLetterCode");
		ReadOnlySpan<char> source = twoLetterCode;
		if (twoLetterCode.Length != 2 || !char.IsAsciiLetter(twoLetterCode[0]) || !char.IsAsciiLetter(twoLetterCode[1]))
		{
			throw new ArgumentException(System.SR.Argument_X500_InvalidCountryOrRegion, "twoLetterCode");
		}
		Span<char> span = stackalloc char[2];
		int num = source.ToUpperInvariant(span);
		EncodeComponent("2.5.4.6", span, UniversalTagNumber.PrintableString, "twoLetterCode");
	}

	public void AddOrganizationName(string organizationName)
	{
		ArgumentException.ThrowIfNullOrEmpty(organizationName, "organizationName");
		EncodeComponent("2.5.4.10", organizationName, UniversalTagNumber.UTF8String, "organizationName");
	}

	public void AddOrganizationalUnitName(string organizationalUnitName)
	{
		ArgumentException.ThrowIfNullOrEmpty(organizationalUnitName, "organizationalUnitName");
		EncodeComponent("2.5.4.11", organizationalUnitName, UniversalTagNumber.UTF8String, "organizationalUnitName");
	}

	public void AddStateOrProvinceName(string stateOrProvinceName)
	{
		ArgumentException.ThrowIfNullOrEmpty(stateOrProvinceName, "stateOrProvinceName");
		EncodeComponent("2.5.4.8", stateOrProvinceName, UniversalTagNumber.UTF8String, "stateOrProvinceName");
	}

	public void AddDomainComponent(string domainComponent)
	{
		ArgumentException.ThrowIfNullOrEmpty(domainComponent, "domainComponent");
		EncodeComponent("0.9.2342.19200300.100.1.25", domainComponent, UniversalTagNumber.IA5String, "domainComponent");
	}

	public X500DistinguishedName Build()
	{
		_writer.Reset();
		using (_writer.PushSequence())
		{
			for (int num = _encodedComponents.Count - 1; num >= 0; num--)
			{
				_writer.WriteEncodedValue(_encodedComponents[num]);
			}
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(_writer.GetEncodedLength());
		int length = _writer.Encode(array);
		X500DistinguishedName result = new X500DistinguishedName(array.AsSpan(0, length));
		System.Security.Cryptography.CryptoPool.Return(array, 0);
		return result;
	}

	private void EncodeComponent(string oid, ReadOnlySpan<char> value, UniversalTagNumber stringEncodingType, [CallerArgumentExpression("value")] string paramName = null)
	{
		_writer.Reset();
		using (_writer.PushSetOf())
		{
			using (_writer.PushSequence())
			{
				_writer.WriteObjectIdentifier(oid);
				try
				{
					_writer.WriteCharacterString(stringEncodingType, value);
				}
				catch (EncoderFallbackException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.Argument_Asn1_InvalidStringContents, stringEncodingType), paramName);
				}
			}
		}
		_encodedComponents.Add(_writer.Encode());
	}

	private static UniversalTagNumber GetAndValidateTagNumber(UniversalTagNumber? stringEncodingType)
	{
		switch (stringEncodingType)
		{
		case null:
			return UniversalTagNumber.UTF8String;
		case UniversalTagNumber.UTF8String:
		case UniversalTagNumber.NumericString:
		case UniversalTagNumber.PrintableString:
		case UniversalTagNumber.TeletexString:
		case UniversalTagNumber.IA5String:
		case UniversalTagNumber.VisibleString:
		case UniversalTagNumber.BMPString:
			return stringEncodingType.GetValueOrDefault();
		default:
			throw new ArgumentException(System.SR.Argument_Asn1_InvalidCharacterString, "stringEncodingType");
		}
	}
}
