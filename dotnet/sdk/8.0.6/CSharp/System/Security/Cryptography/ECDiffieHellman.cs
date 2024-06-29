using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public abstract class ECDiffieHellman : ECAlgorithm
{
	public override string KeyExchangeAlgorithm => "ECDiffieHellman";

	public override string? SignatureAlgorithm => null;

	public abstract ECDiffieHellmanPublicKey PublicKey { get; }

	[UnsupportedOSPlatform("browser")]
	public new static ECDiffieHellman Create()
	{
		return new ECDiffieHellmanWrapper(new ECDiffieHellmanCng());
	}

	[UnsupportedOSPlatform("browser")]
	public static ECDiffieHellman Create(ECCurve curve)
	{
		return new ECDiffieHellmanWrapper(new ECDiffieHellmanCng(curve));
	}

	[UnsupportedOSPlatform("browser")]
	public static ECDiffieHellman Create(ECParameters parameters)
	{
		ECDiffieHellmanCng eCDiffieHellmanCng = new ECDiffieHellmanCng();
		try
		{
			eCDiffieHellmanCng.ImportParameters(parameters);
			return new ECDiffieHellmanWrapper(eCDiffieHellmanCng);
		}
		catch
		{
			eCDiffieHellmanCng.Dispose();
			throw;
		}
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static ECDiffieHellman? Create(string algorithm)
	{
		ArgumentNullException.ThrowIfNull(algorithm, "algorithm");
		return CryptoConfig.CreateFromName(algorithm) as ECDiffieHellman;
	}

	public virtual byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		throw DerivedClassMustOverride();
	}

	public byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm)
	{
		return DeriveKeyFromHash(otherPartyPublicKey, hashAlgorithm, null, null);
	}

	public virtual byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend)
	{
		throw DerivedClassMustOverride();
	}

	public byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey)
	{
		return DeriveKeyFromHmac(otherPartyPublicKey, hashAlgorithm, hmacKey, null, null);
	}

	public virtual byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend)
	{
		throw DerivedClassMustOverride();
	}

	public virtual byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
	{
		throw DerivedClassMustOverride();
	}

	public virtual byte[] DeriveRawSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		throw DerivedClassMustOverride();
	}

	private static NotImplementedException DerivedClassMustOverride()
	{
		return new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public override void FromXmlString(string xmlString)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}
}
