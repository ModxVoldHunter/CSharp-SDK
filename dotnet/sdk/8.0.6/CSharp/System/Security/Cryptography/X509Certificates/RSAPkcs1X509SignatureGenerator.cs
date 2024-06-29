using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class RSAPkcs1X509SignatureGenerator : X509SignatureGenerator
{
	private readonly RSA _key;

	internal RSAPkcs1X509SignatureGenerator(RSA key)
	{
		_key = key;
	}

	public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
	{
		return _key.SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
	}

	protected override PublicKey BuildPublicKey()
	{
		return BuildPublicKey(_key);
	}

	internal static PublicKey BuildPublicKey(RSA rsa)
	{
		Oid rsaOid = Oids.RsaOid;
		ReadOnlySpan<byte> rawData = new byte[2] { 5, 0 };
		return new PublicKey(rsaOid, new AsnEncodedData(rsaOid, rawData), new AsnEncodedData(rsaOid, rsa.ExportRSAPublicKey(), skipCopy: true));
	}

	public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
	{
		string oidValue;
		if (hashAlgorithm == HashAlgorithmName.SHA256)
		{
			oidValue = "1.2.840.113549.1.1.11";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA384)
		{
			oidValue = "1.2.840.113549.1.1.12";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA512)
		{
			oidValue = "1.2.840.113549.1.1.13";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA3_256)
		{
			oidValue = "2.16.840.1.101.3.4.3.14";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA3_384)
		{
			oidValue = "2.16.840.1.101.3.4.3.15";
		}
		else
		{
			if (!(hashAlgorithm == HashAlgorithmName.SHA3_512))
			{
				throw new ArgumentOutOfRangeException("hashAlgorithm", hashAlgorithm, System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
			}
			oidValue = "2.16.840.1.101.3.4.3.16";
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteObjectIdentifier(oidValue);
		asnWriter.WriteNull();
		asnWriter.PopSequence();
		return asnWriter.Encode();
	}
}
