using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

public sealed class CertificateRevocationListBuilder
{
	private struct RevokedCertificate
	{
		internal byte[] Serial;

		internal DateTimeOffset RevocationTime;

		internal byte[] Extensions;

		internal RevokedCertificate(ref AsnValueReader reader, int version)
		{
			AsnValueReader reader2 = reader.ReadSequence();
			Serial = reader2.ReadIntegerBytes().ToArray();
			RevocationTime = ReadX509Time(ref reader2);
			Extensions = null;
			if (version > 0 && reader2.HasData)
			{
				if (!reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
				{
					throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
				}
				Extensions = reader2.ReadEncodedValue().ToArray();
			}
			reader2.ThrowIfNotEmpty();
		}
	}

	private readonly List<RevokedCertificate> _revoked;

	private AsnWriter _writer;

	public CertificateRevocationListBuilder()
	{
		_revoked = new List<RevokedCertificate>();
	}

	private CertificateRevocationListBuilder(List<RevokedCertificate> revoked)
	{
		_revoked = revoked;
	}

	public void AddEntry(X509Certificate2 certificate, DateTimeOffset? revocationTime = null, X509RevocationReason? reason = null)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		AddEntry(certificate.SerialNumberBytes.Span, revocationTime, reason);
	}

	public void AddEntry(byte[] serialNumber, DateTimeOffset? revocationTime = null, X509RevocationReason? reason = null)
	{
		ArgumentNullException.ThrowIfNull(serialNumber, "serialNumber");
		AddEntry(new ReadOnlySpan<byte>(serialNumber), revocationTime, reason);
	}

	public void AddEntry(ReadOnlySpan<byte> serialNumber, DateTimeOffset? revocationTime = null, X509RevocationReason? reason = null)
	{
		if (serialNumber.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "serialNumber");
		}
		if (serialNumber.Length > 1 && ((serialNumber[0] == 0 && serialNumber[1] < 128) || (serialNumber[0] == byte.MaxValue && serialNumber[1] > 127)))
		{
			throw new ArgumentException(System.SR.Argument_InvalidSerialNumberBytes, "serialNumber");
		}
		byte[] extensions = null;
		if (reason.HasValue)
		{
			X509RevocationReason valueOrDefault = reason.GetValueOrDefault();
			switch (valueOrDefault)
			{
			default:
				throw new ArgumentOutOfRangeException("reason", valueOrDefault, System.SR.Cryptography_CRLBuilder_ReasonNotSupported);
			case X509RevocationReason.Unspecified:
			case X509RevocationReason.KeyCompromise:
			case X509RevocationReason.CACompromise:
			case X509RevocationReason.AffiliationChanged:
			case X509RevocationReason.Superseded:
			case X509RevocationReason.CessationOfOperation:
			case X509RevocationReason.CertificateHold:
			case X509RevocationReason.PrivilegeWithdrawn:
			case X509RevocationReason.WeakAlgorithmOrKey:
				break;
			}
			AsnWriter asnWriter = _writer ?? (_writer = new AsnWriter(AsnEncodingRules.DER));
			asnWriter.Reset();
			using (asnWriter.PushSequence())
			{
				using (asnWriter.PushSequence())
				{
					asnWriter.WriteObjectIdentifier("2.5.29.21");
					using (asnWriter.PushOctetString())
					{
						asnWriter.WriteEnumeratedValue(valueOrDefault);
					}
				}
			}
			extensions = asnWriter.Encode();
		}
		_revoked.Add(new RevokedCertificate
		{
			Serial = serialNumber.ToArray(),
			RevocationTime = (revocationTime ?? DateTimeOffset.UtcNow).ToUniversalTime(),
			Extensions = extensions
		});
	}

	public bool RemoveEntry(byte[] serialNumber)
	{
		ArgumentNullException.ThrowIfNull(serialNumber, "serialNumber");
		return RemoveEntry(new ReadOnlySpan<byte>(serialNumber));
	}

	public bool RemoveEntry(ReadOnlySpan<byte> serialNumber)
	{
		for (int num = _revoked.Count - 1; num >= 0; num--)
		{
			if (serialNumber.SequenceEqual(_revoked[num].Serial))
			{
				_revoked.RemoveAt(num);
				return true;
			}
		}
		return false;
	}

	private static DateTimeOffset ReadX509Time(ref AsnValueReader reader)
	{
		if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime))
		{
			return reader.ReadUtcTime();
		}
		return reader.ReadGeneralizedTime();
	}

	private static DateTimeOffset? ReadX509TimeOpt(ref AsnValueReader reader)
	{
		if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime))
		{
			return reader.ReadUtcTime();
		}
		if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.GeneralizedTime))
		{
			return reader.ReadGeneralizedTime();
		}
		return null;
	}

	private static void WriteX509Time(AsnWriter writer, DateTimeOffset time)
	{
		DateTimeOffset value = time.ToUniversalTime();
		int year = value.Year;
		if (year >= 1950 && year < 2050)
		{
			writer.WriteUtcTime(value);
		}
		else
		{
			writer.WriteGeneralizedTime(time, omitFractionalSeconds: true);
		}
	}

	public byte[] Build(X509Certificate2 issuerCertificate, BigInteger crlNumber, DateTimeOffset nextUpdate, HashAlgorithmName hashAlgorithm, RSASignaturePadding? rsaSignaturePadding = null, DateTimeOffset? thisUpdate = null)
	{
		return Build(issuerCertificate, crlNumber, nextUpdate, thisUpdate.GetValueOrDefault(DateTimeOffset.UtcNow), hashAlgorithm, rsaSignaturePadding);
	}

	private byte[] Build(X509Certificate2 issuerCertificate, BigInteger crlNumber, DateTimeOffset nextUpdate, DateTimeOffset thisUpdate, HashAlgorithmName hashAlgorithm, RSASignaturePadding rsaSignaturePadding)
	{
		ArgumentNullException.ThrowIfNull(issuerCertificate, "issuerCertificate");
		if (!issuerCertificate.HasPrivateKey)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_IssuerRequiresPrivateKey, "issuerCertificate");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(crlNumber, "crlNumber");
		if (nextUpdate <= thisUpdate)
		{
			throw new ArgumentException(System.SR.Cryptography_CRLBuilder_DatesReversed);
		}
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		X509BasicConstraintsExtension x509BasicConstraintsExtension = (X509BasicConstraintsExtension)issuerCertificate.Extensions["2.5.29.19"];
		X509KeyUsageExtension x509KeyUsageExtension = (X509KeyUsageExtension)issuerCertificate.Extensions["2.5.29.15"];
		X509SubjectKeyIdentifierExtension x509SubjectKeyIdentifierExtension = (X509SubjectKeyIdentifierExtension)issuerCertificate.Extensions["2.5.29.14"];
		if (x509BasicConstraintsExtension == null)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_BasicConstraintsRequired, "issuerCertificate");
		}
		if (!x509BasicConstraintsExtension.CertificateAuthority)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_IssuerBasicConstraintsInvalid, "issuerCertificate");
		}
		if (x509KeyUsageExtension != null && (x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.CrlSign) == 0)
		{
			throw new ArgumentException(System.SR.Cryptography_CRLBuilder_IssuerKeyUsageInvalid, "issuerCertificate");
		}
		AsymmetricAlgorithm asymmetricAlgorithm = null;
		string keyAlgorithm = issuerCertificate.GetKeyAlgorithm();
		try
		{
			X509SignatureGenerator generator;
			if (!(keyAlgorithm == "1.2.840.113549.1.1.1"))
			{
				if (!(keyAlgorithm == "1.2.840.10045.2.1"))
				{
					throw new ArgumentException(System.SR.Format(System.SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm), "issuerCertificate");
				}
				ECDsa eCDsaPrivateKey = issuerCertificate.GetECDsaPrivateKey();
				asymmetricAlgorithm = eCDsaPrivateKey;
				generator = X509SignatureGenerator.CreateForECDsa(eCDsaPrivateKey);
			}
			else
			{
				if ((object)rsaSignaturePadding == null)
				{
					throw new ArgumentException(System.SR.Cryptography_CertReq_RSAPaddingRequired);
				}
				RSA rSAPrivateKey = issuerCertificate.GetRSAPrivateKey();
				asymmetricAlgorithm = rSAPrivateKey;
				generator = X509SignatureGenerator.CreateForRSA(rSAPrivateKey, rsaSignaturePadding);
			}
			return Build(authorityKeyIdentifier: (x509SubjectKeyIdentifierExtension == null) ? X509AuthorityKeyIdentifierExtension.CreateFromIssuerNameAndSerialNumber(issuerCertificate.IssuerName, issuerCertificate.SerialNumberBytes.Span) : X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(x509SubjectKeyIdentifierExtension), issuerName: issuerCertificate.SubjectName, generator: generator, crlNumber: crlNumber, nextUpdate: nextUpdate, thisUpdate: thisUpdate, hashAlgorithm: hashAlgorithm);
		}
		finally
		{
			asymmetricAlgorithm?.Dispose();
		}
	}

	public byte[] Build(X500DistinguishedName issuerName, X509SignatureGenerator generator, BigInteger crlNumber, DateTimeOffset nextUpdate, HashAlgorithmName hashAlgorithm, X509AuthorityKeyIdentifierExtension authorityKeyIdentifier, DateTimeOffset? thisUpdate = null)
	{
		return Build(issuerName, generator, crlNumber, nextUpdate, thisUpdate.GetValueOrDefault(DateTimeOffset.UtcNow), hashAlgorithm, authorityKeyIdentifier);
	}

	private byte[] Build(X500DistinguishedName issuerName, X509SignatureGenerator generator, BigInteger crlNumber, DateTimeOffset nextUpdate, DateTimeOffset thisUpdate, HashAlgorithmName hashAlgorithm, X509AuthorityKeyIdentifierExtension authorityKeyIdentifier)
	{
		ArgumentNullException.ThrowIfNull(issuerName, "issuerName");
		ArgumentNullException.ThrowIfNull(generator, "generator");
		ArgumentOutOfRangeException.ThrowIfNegative(crlNumber, "crlNumber");
		if (nextUpdate <= thisUpdate)
		{
			throw new ArgumentException(System.SR.Cryptography_CRLBuilder_DatesReversed);
		}
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		ArgumentNullException.ThrowIfNull(authorityKeyIdentifier, "authorityKeyIdentifier");
		byte[] signatureAlgorithmIdentifier = generator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
		AlgorithmIdentifierAsn algorithmIdentifierAsn = AlgorithmIdentifierAsn.Decode(signatureAlgorithmIdentifier, AsnEncodingRules.DER);
		if (algorithmIdentifierAsn.Parameters.HasValue)
		{
			Helpers.ValidateDer(algorithmIdentifierAsn.Parameters.GetValueOrDefault().Span);
		}
		AsnWriter asnWriter = _writer ?? (_writer = new AsnWriter(AsnEncodingRules.DER));
		asnWriter.Reset();
		using (asnWriter.PushSequence())
		{
			asnWriter.WriteInteger(1L);
			asnWriter.WriteEncodedValue(signatureAlgorithmIdentifier);
			asnWriter.WriteEncodedValue(issuerName.RawData);
			WriteX509Time(asnWriter, thisUpdate);
			WriteX509Time(asnWriter, nextUpdate);
			if (_revoked.Count > 0)
			{
				using (asnWriter.PushSequence())
				{
					foreach (RevokedCertificate item in _revoked)
					{
						using (asnWriter.PushSequence())
						{
							asnWriter.WriteInteger(item.Serial);
							WriteX509Time(asnWriter, item.RevocationTime);
							if (item.Extensions != null)
							{
								asnWriter.WriteEncodedValue(item.Extensions);
							}
						}
					}
				}
			}
			using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
			{
				using (asnWriter.PushSequence())
				{
					using (asnWriter.PushSequence())
					{
						asnWriter.WriteObjectIdentifier(authorityKeyIdentifier.Oid.Value);
						if (authorityKeyIdentifier.Critical)
						{
							asnWriter.WriteBoolean(value: true);
						}
						byte[] rawData = authorityKeyIdentifier.RawData;
						Helpers.ValidateDer(rawData);
						asnWriter.WriteOctetString(rawData);
					}
					using (asnWriter.PushSequence())
					{
						asnWriter.WriteObjectIdentifier("2.5.29.20");
						using (asnWriter.PushOctetString())
						{
							asnWriter.WriteInteger(crlNumber);
						}
					}
				}
			}
		}
		byte[] array = asnWriter.Encode();
		asnWriter.Reset();
		byte[] array2 = generator.SignData(array, hashAlgorithm);
		using (asnWriter.PushSequence())
		{
			asnWriter.WriteEncodedValue(array);
			asnWriter.WriteEncodedValue(signatureAlgorithmIdentifier);
			asnWriter.WriteBitString(array2);
		}
		return asnWriter.Encode();
	}

	public static X509Extension BuildCrlDistributionPointExtension(IEnumerable<string> uris, bool critical = false)
	{
		ArgumentNullException.ThrowIfNull(uris, "uris");
		AsnWriter asnWriter = null;
		foreach (string uri in uris)
		{
			if (uri == null)
			{
				throw new ArgumentException(System.SR.Cryptography_X509_CDP_NullValue, "uris");
			}
			if (asnWriter == null)
			{
				asnWriter = new AsnWriter(AsnEncodingRules.DER);
				asnWriter.PushSequence();
			}
			using (asnWriter.PushSequence())
			{
				using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
				{
					using (asnWriter.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
					{
						try
						{
							asnWriter.WriteCharacterString(UniversalTagNumber.IA5String, uri, new Asn1Tag(TagClass.ContextSpecific, 6));
						}
						catch (EncoderFallbackException inner)
						{
							throw new CryptographicException(System.SR.Cryptography_Invalid_IA5String, inner);
						}
					}
				}
			}
		}
		if (asnWriter == null)
		{
			throw new ArgumentException(System.SR.Cryptography_X509_CDP_MustNotBuildEmpty, "uris");
		}
		asnWriter.PopSequence();
		byte[] rawData = asnWriter.Encode();
		return new X509Extension(Oids.CrlDistributionPointsOid, rawData, critical);
	}

	public static CertificateRevocationListBuilder Load(byte[] currentCrl, out BigInteger currentCrlNumber)
	{
		ArgumentNullException.ThrowIfNull(currentCrl, "currentCrl");
		BigInteger currentCrlNumber2;
		int bytesConsumed;
		CertificateRevocationListBuilder result = Load(new ReadOnlySpan<byte>(currentCrl), out currentCrlNumber2, out bytesConsumed);
		if (bytesConsumed != currentCrl.Length)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		currentCrlNumber = currentCrlNumber2;
		return result;
	}

	public static CertificateRevocationListBuilder Load(ReadOnlySpan<byte> currentCrl, out BigInteger currentCrlNumber, out int bytesConsumed)
	{
		List<RevokedCertificate> list = new List<RevokedCertificate>();
		BigInteger bigInteger = 0;
		int length;
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(currentCrl, AsnEncodingRules.DER);
			ReadOnlySpan<byte> value = asnValueReader.PeekEncodedValue();
			length = value.Length;
			AsnValueReader reader = asnValueReader.ReadSequence();
			AsnValueReader reader2 = reader.ReadSequence();
			AlgorithmIdentifierAsn.Decode(ref reader, ReadOnlyMemory<byte>.Empty, out var decoded);
			if (!reader.TryReadPrimitiveBitString(out var _, out value))
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			reader.ThrowIfNotEmpty();
			int value2 = 0;
			if (reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Integer) && (!reader2.TryReadInt32(out value2) || value2 != 1))
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			AlgorithmIdentifierAsn.Decode(ref reader2, ReadOnlyMemory<byte>.Empty, out decoded);
			reader2.ReadSequence();
			ReadX509Time(ref reader2);
			ReadX509TimeOpt(ref reader2);
			AsnValueReader reader3 = default(AsnValueReader);
			if (reader2.HasData && reader2.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
			{
				reader3 = reader2.ReadSequence();
			}
			if (value2 > 0 && reader2.HasData)
			{
				AsnValueReader asnValueReader2 = reader2.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
				AsnValueReader asnValueReader3 = asnValueReader2.ReadSequence();
				asnValueReader2.ThrowIfNotEmpty();
				while (asnValueReader3.HasData)
				{
					AsnValueReader asnValueReader4 = asnValueReader3.ReadSequence();
					Oid sharedOrNullOid = Oids.GetSharedOrNullOid(ref asnValueReader4);
					if (sharedOrNullOid == null)
					{
						asnValueReader4.ReadObjectIdentifier();
					}
					if (asnValueReader4.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
					{
						asnValueReader4.ReadBoolean();
					}
					if (!asnValueReader4.TryReadPrimitiveOctetString(out var value3))
					{
						throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
					}
					if (sharedOrNullOid == Oids.CrlNumberOid)
					{
						AsnValueReader asnValueReader5 = new AsnValueReader(value3, AsnEncodingRules.DER);
						bigInteger = asnValueReader5.ReadInteger();
						asnValueReader5.ThrowIfNotEmpty();
					}
				}
			}
			reader2.ThrowIfNotEmpty();
			while (reader3.HasData)
			{
				RevokedCertificate item = new RevokedCertificate(ref reader3, value2);
				list.Add(item);
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		bytesConsumed = length;
		currentCrlNumber = bigInteger;
		return new CertificateRevocationListBuilder(list);
	}

	public static CertificateRevocationListBuilder LoadPem(string currentCrl, out BigInteger currentCrlNumber)
	{
		ArgumentNullException.ThrowIfNull(currentCrl, "currentCrl");
		return LoadPem(currentCrl.AsSpan(), out currentCrlNumber);
	}

	public static CertificateRevocationListBuilder LoadPem(ReadOnlySpan<char> currentCrl, out BigInteger currentCrlNumber)
	{
		PemEnumerator.Enumerator enumerator = new PemEnumerator(currentCrl).GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.Deconstruct(out var contents, out var pemFields);
			ReadOnlySpan<char> readOnlySpan = contents;
			PemFields pemFields2 = pemFields;
			Range label = pemFields2.Label;
			if (readOnlySpan[label.Start..label.End].SequenceEqual("X509 CRL"))
			{
				byte[] array = ArrayPool<byte>.Shared.Rent(pemFields2.DecodedDataLength);
				label = pemFields2.Base64Data;
				if (!Convert.TryFromBase64Chars(readOnlySpan[label.Start..label.End], array, out var bytesWritten))
				{
					throw new UnreachableException();
				}
				int bytesConsumed;
				CertificateRevocationListBuilder result = Load(array.AsSpan(0, bytesWritten), out currentCrlNumber, out bytesConsumed);
				ArrayPool<byte>.Shared.Return(array);
				return result;
			}
		}
		throw new CryptographicException(System.SR.Cryptography_NoPemOfLabel, "X509 CRL");
	}
}
