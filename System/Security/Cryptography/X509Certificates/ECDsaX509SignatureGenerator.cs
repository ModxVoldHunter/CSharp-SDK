using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class ECDsaX509SignatureGenerator : X509SignatureGenerator
{
	private readonly ECDsa _key;

	internal ECDsaX509SignatureGenerator(ECDsa key)
	{
		_key = key;
	}

	public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
	{
		string oidValue;
		if (hashAlgorithm == HashAlgorithmName.SHA256)
		{
			oidValue = "1.2.840.10045.4.3.2";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA384)
		{
			oidValue = "1.2.840.10045.4.3.3";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA512)
		{
			oidValue = "1.2.840.10045.4.3.4";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA3_256)
		{
			oidValue = "2.16.840.1.101.3.4.3.10";
		}
		else if (hashAlgorithm == HashAlgorithmName.SHA3_384)
		{
			oidValue = "2.16.840.1.101.3.4.3.11";
		}
		else
		{
			if (!(hashAlgorithm == HashAlgorithmName.SHA3_512))
			{
				throw new ArgumentOutOfRangeException("hashAlgorithm", hashAlgorithm, System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
			}
			oidValue = "2.16.840.1.101.3.4.3.12";
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteObjectIdentifier(oidValue);
		asnWriter.PopSequence();
		return asnWriter.Encode();
	}

	public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
	{
		return _key.SignData(data, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
	}

	protected override PublicKey BuildPublicKey()
	{
		ECParameters eCParameters = _key.ExportParameters(includePrivateParameters: false);
		if (!eCParameters.Curve.IsNamed)
		{
			throw new InvalidOperationException(System.SR.Cryptography_ECC_NamedCurvesOnly);
		}
		string text = eCParameters.Curve.Oid.Value;
		if (string.IsNullOrEmpty(text))
		{
			string friendlyName = eCParameters.Curve.Oid.FriendlyName;
			text = friendlyName switch
			{
				"nistP256" => "1.2.840.10045.3.1.7", 
				"nistP384" => "1.3.132.0.34", 
				"nistP521" => "1.3.132.0.35", 
				_ => new Oid(friendlyName).Value, 
			};
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.WriteObjectIdentifier(text);
		byte[] rawData = asnWriter.Encode();
		byte[] array = new byte[1 + eCParameters.Q.X.Length + eCParameters.Q.Y.Length];
		array[0] = 4;
		Buffer.BlockCopy(eCParameters.Q.X, 0, array, 1, eCParameters.Q.X.Length);
		Buffer.BlockCopy(eCParameters.Q.Y, 0, array, 1 + eCParameters.Q.X.Length, eCParameters.Q.Y.Length);
		Oid ecPublicKeyOid = Oids.EcPublicKeyOid;
		return new PublicKey(ecPublicKeyOid, new AsnEncodedData(ecPublicKeyOid, rawData, skipCopy: true), new AsnEncodedData(ecPublicKeyOid, array, skipCopy: true));
	}
}
