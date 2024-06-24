using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509AuthorityKeyIdentifierExtension : X509Extension
{
	private bool _decoded;

	private X500DistinguishedName _simpleIssuer;

	private ReadOnlyMemory<byte>? _keyIdentifier;

	private ReadOnlyMemory<byte>? _rawIssuer;

	private ReadOnlyMemory<byte>? _serialNumber;

	public ReadOnlyMemory<byte>? KeyIdentifier
	{
		get
		{
			if (!_decoded)
			{
				Decode(base.RawData);
			}
			return _keyIdentifier;
		}
	}

	public X500DistinguishedName? NamedIssuer
	{
		get
		{
			if (!_decoded)
			{
				Decode(base.RawData);
			}
			return _simpleIssuer;
		}
	}

	public ReadOnlyMemory<byte>? RawIssuer
	{
		get
		{
			if (!_decoded)
			{
				Decode(base.RawData);
			}
			return _rawIssuer;
		}
	}

	public ReadOnlyMemory<byte>? SerialNumber
	{
		get
		{
			if (!_decoded)
			{
				Decode(base.RawData);
			}
			return _serialNumber;
		}
	}

	public X509AuthorityKeyIdentifierExtension()
		: base(Oids.AuthorityKeyIdentifierOid)
	{
		_decoded = true;
	}

	public X509AuthorityKeyIdentifierExtension(byte[] rawData, bool critical = false)
		: base(Oids.AuthorityKeyIdentifierOid, rawData, critical)
	{
		Decode(base.RawData);
	}

	public X509AuthorityKeyIdentifierExtension(ReadOnlySpan<byte> rawData, bool critical = false)
		: base(Oids.AuthorityKeyIdentifierOid, rawData, critical)
	{
		Decode(base.RawData);
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = false;
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(X509SubjectKeyIdentifierExtension subjectKeyIdentifier)
	{
		ArgumentNullException.ThrowIfNull(subjectKeyIdentifier, "subjectKeyIdentifier");
		return CreateFromSubjectKeyIdentifier(subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span);
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(byte[] subjectKeyIdentifier)
	{
		ArgumentNullException.ThrowIfNull(subjectKeyIdentifier, "subjectKeyIdentifier");
		return CreateFromSubjectKeyIdentifier(new ReadOnlySpan<byte>(subjectKeyIdentifier));
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(ReadOnlySpan<byte> subjectKeyIdentifier)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		using (asnWriter.PushSequence())
		{
			asnWriter.WriteOctetString(subjectKeyIdentifier, new Asn1Tag(TagClass.ContextSpecific, 0));
		}
		Span<byte> destination = stackalloc byte[64];
		int bytesWritten;
		ReadOnlySpan<byte> rawData = ((!asnWriter.TryEncode(destination, out bytesWritten)) ? ((ReadOnlySpan<byte>)asnWriter.Encode()) : ((ReadOnlySpan<byte>)destination.Slice(0, bytesWritten)));
		return new X509AuthorityKeyIdentifierExtension(rawData);
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(X500DistinguishedName issuerName, byte[] serialNumber)
	{
		ArgumentNullException.ThrowIfNull(issuerName, "issuerName");
		ArgumentNullException.ThrowIfNull(serialNumber, "serialNumber");
		return CreateFromIssuerNameAndSerialNumber(issuerName, new ReadOnlySpan<byte>(serialNumber));
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(X500DistinguishedName issuerName, ReadOnlySpan<byte> serialNumber)
	{
		ArgumentNullException.ThrowIfNull(issuerName, "issuerName");
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		using (asnWriter.PushSequence())
		{
			using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
			{
				using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
				{
					asnWriter.WriteEncodedValue(issuerName.RawData);
				}
			}
			try
			{
				asnWriter.WriteInteger(serialNumber, new Asn1Tag(TagClass.ContextSpecific, 2));
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidSerialNumberBytes, "serialNumber");
			}
		}
		return new X509AuthorityKeyIdentifierExtension(asnWriter.Encode());
	}

	public static X509AuthorityKeyIdentifierExtension Create(byte[] keyIdentifier, X500DistinguishedName issuerName, byte[] serialNumber)
	{
		ArgumentNullException.ThrowIfNull(keyIdentifier, "keyIdentifier");
		ArgumentNullException.ThrowIfNull(issuerName, "issuerName");
		ArgumentNullException.ThrowIfNull(serialNumber, "serialNumber");
		return Create(new ReadOnlySpan<byte>(keyIdentifier), issuerName, new ReadOnlySpan<byte>(serialNumber));
	}

	public static X509AuthorityKeyIdentifierExtension Create(ReadOnlySpan<byte> keyIdentifier, X500DistinguishedName issuerName, ReadOnlySpan<byte> serialNumber)
	{
		ArgumentNullException.ThrowIfNull(issuerName, "issuerName");
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		using (asnWriter.PushSequence())
		{
			asnWriter.WriteOctetString(keyIdentifier, new Asn1Tag(TagClass.ContextSpecific, 0));
			using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
			{
				using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
				{
					asnWriter.WriteEncodedValue(issuerName.RawData);
				}
			}
			try
			{
				asnWriter.WriteInteger(serialNumber, new Asn1Tag(TagClass.ContextSpecific, 2));
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidSerialNumberBytes, "serialNumber");
			}
		}
		return new X509AuthorityKeyIdentifierExtension(asnWriter.Encode());
	}

	public static X509AuthorityKeyIdentifierExtension CreateFromCertificate(X509Certificate2 certificate, bool includeKeyIdentifier, bool includeIssuerAndSerial)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		ReadOnlyMemory<byte> readOnlyMemory;
		if (includeKeyIdentifier)
		{
			X509SubjectKeyIdentifierExtension x509SubjectKeyIdentifierExtension = (X509SubjectKeyIdentifierExtension)certificate.Extensions["2.5.29.14"];
			if (x509SubjectKeyIdentifierExtension == null)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_AKID_NoSKID);
			}
			readOnlyMemory = x509SubjectKeyIdentifierExtension.SubjectKeyIdentifierBytes;
			ReadOnlySpan<byte> span = readOnlyMemory.Span;
			if (includeIssuerAndSerial)
			{
				X500DistinguishedName issuerName = certificate.IssuerName;
				readOnlyMemory = certificate.SerialNumberBytes;
				return Create(span, issuerName, readOnlyMemory.Span);
			}
			return CreateFromSubjectKeyIdentifier(span);
		}
		if (includeIssuerAndSerial)
		{
			X500DistinguishedName issuerName2 = certificate.IssuerName;
			readOnlyMemory = certificate.SerialNumberBytes;
			return CreateFromIssuerNameAndSerialNumber(issuerName2, readOnlyMemory.Span);
		}
		ReadOnlySpan<byte> rawData = new byte[2] { 48, 0 };
		return new X509AuthorityKeyIdentifierExtension(rawData);
	}

	private void Decode(ReadOnlySpan<byte> rawData)
	{
		_keyIdentifier = null;
		_simpleIssuer = null;
		_rawIssuer = null;
		_serialNumber = null;
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(rawData, AsnEncodingRules.DER);
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
			Asn1Tag value = default(Asn1Tag);
			if (asnValueReader2.HasData)
			{
				value = asnValueReader2.PeekTag();
			}
			if (value.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
			{
				_keyIdentifier = asnValueReader2.ReadOctetString(value);
				if (asnValueReader2.HasData)
				{
					value = asnValueReader2.PeekTag();
				}
			}
			if (value.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
			{
				byte[] array = asnValueReader2.PeekEncodedValue().ToArray();
				_rawIssuer = array;
				AsnValueReader reader = asnValueReader2.ReadSequence(value);
				bool flag = false;
				while (reader.HasData)
				{
					GeneralNameAsn.Decode(ref reader, array, out var decoded);
					if (decoded.DirectoryName.HasValue)
					{
						if (!flag)
						{
							flag = true;
							_simpleIssuer = new X500DistinguishedName(decoded.DirectoryName.GetValueOrDefault().Span);
						}
						else
						{
							_simpleIssuer = null;
						}
					}
				}
				if (asnValueReader2.HasData)
				{
					value = asnValueReader2.PeekTag();
				}
			}
			if (value.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
			{
				_serialNumber = asnValueReader2.ReadIntegerBytes(value).ToArray();
			}
			asnValueReader2.ThrowIfNotEmpty();
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		_decoded = true;
	}
}
