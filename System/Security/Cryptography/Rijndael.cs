using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[Obsolete("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId = "SYSLIB0022", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Rijndael : SymmetricAlgorithm
{
	private static readonly KeySizes[] s_legalBlockSizes = new KeySizes[1]
	{
		new KeySizes(128, 256, 64)
	};

	private static readonly KeySizes[] s_legalKeySizes = new KeySizes[1]
	{
		new KeySizes(128, 256, 64)
	};

	[UnsupportedOSPlatform("browser")]
	public new static Rijndael Create()
	{
		return new RijndaelImplementation();
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static Rijndael? Create(string algName)
	{
		return (Rijndael)CryptoConfig.CreateFromName(algName);
	}

	protected Rijndael()
	{
		LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
		LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
		KeySizeValue = 256;
		BlockSizeValue = 128;
		FeedbackSizeValue = BlockSizeValue;
	}
}
