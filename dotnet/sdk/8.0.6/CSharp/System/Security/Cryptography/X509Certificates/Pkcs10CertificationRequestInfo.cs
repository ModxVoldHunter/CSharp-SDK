using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class Pkcs10CertificationRequestInfo
{
	internal X500DistinguishedName Subject { get; set; }

	internal PublicKey PublicKey { get; set; }

	internal Collection<X501Attribute> Attributes { get; } = new Collection<X501Attribute>();


	internal Pkcs10CertificationRequestInfo(X500DistinguishedName subject, PublicKey publicKey, IEnumerable<X501Attribute> attributes)
	{
		ArgumentNullException.ThrowIfNull(subject, "subject");
		ArgumentNullException.ThrowIfNull(publicKey, "publicKey");
		Subject = subject;
		PublicKey = publicKey;
		if (attributes != null)
		{
			Attributes.AddRange(attributes);
		}
	}

	internal byte[] ToPkcs10Request(X509SignatureGenerator signatureGenerator, HashAlgorithmName hashAlgorithm)
	{
		byte[] signatureAlgorithmIdentifier = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
		AlgorithmIdentifierAsn signatureAlgorithm = AlgorithmIdentifierAsn.Decode(signatureAlgorithmIdentifier, AsnEncodingRules.DER);
		if (signatureAlgorithm.Parameters.HasValue)
		{
			Helpers.ValidateDer(signatureAlgorithm.Parameters.Value.Span);
		}
		SubjectPublicKeyInfoAsn subjectPublicKeyInfo = default(SubjectPublicKeyInfoAsn);
		subjectPublicKeyInfo.Algorithm = new AlgorithmIdentifierAsn
		{
			Algorithm = PublicKey.Oid.Value,
			Parameters = PublicKey.EncodedParameters.RawData
		};
		subjectPublicKeyInfo.SubjectPublicKey = PublicKey.EncodedKeyValue.RawData;
		AttributeAsn[] array = new AttributeAsn[Attributes.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new AttributeAsn(Attributes[i]);
		}
		CertificationRequestInfoAsn certificationRequestInfoAsn = default(CertificationRequestInfoAsn);
		certificationRequestInfoAsn.Version = 0;
		certificationRequestInfoAsn.Subject = Subject.RawData;
		certificationRequestInfoAsn.SubjectPublicKeyInfo = subjectPublicKeyInfo;
		certificationRequestInfoAsn.Attributes = array;
		CertificationRequestInfoAsn certificationRequestInfo = certificationRequestInfoAsn;
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		certificationRequestInfo.Encode(asnWriter);
		byte[] data = asnWriter.Encode();
		asnWriter.Reset();
		CertificationRequestAsn certificationRequestAsn = default(CertificationRequestAsn);
		certificationRequestAsn.CertificationRequestInfo = certificationRequestInfo;
		certificationRequestAsn.SignatureAlgorithm = signatureAlgorithm;
		certificationRequestAsn.SignatureValue = signatureGenerator.SignData(data, hashAlgorithm);
		CertificationRequestAsn certificationRequestAsn2 = certificationRequestAsn;
		certificationRequestAsn2.Encode(asnWriter);
		return asnWriter.Encode();
	}
}
