using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public abstract class Aes : SymmetricAlgorithm
{
	private static readonly KeySizes[] s_legalBlockSizes = new KeySizes[1]
	{
		new KeySizes(128, 128, 0)
	};

	private static readonly KeySizes[] s_legalKeySizes = new KeySizes[1]
	{
		new KeySizes(128, 256, 64)
	};

	protected Aes()
	{
		LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
		LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
		BlockSizeValue = 128;
		FeedbackSizeValue = 8;
		KeySizeValue = 256;
		ModeValue = CipherMode.CBC;
	}

	[UnsupportedOSPlatform("browser")]
	public new static Aes Create()
	{
		return new AesImplementation();
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static Aes? Create(string algorithmName)
	{
		return (Aes)CryptoConfig.CreateFromName(algorithmName);
	}
}
