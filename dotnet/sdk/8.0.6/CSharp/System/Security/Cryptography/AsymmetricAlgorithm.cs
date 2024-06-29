using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public abstract class AsymmetricAlgorithm : IDisposable
{
	private delegate bool TryExportPbe<T>(ReadOnlySpan<T> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten);

	private delegate bool TryExport(Span<byte> destination, out int bytesWritten);

	protected int KeySizeValue;

	[MaybeNull]
	protected KeySizes[] LegalKeySizesValue;

	public virtual int KeySize
	{
		get
		{
			return KeySizeValue;
		}
		set
		{
			if (!value.IsLegalSize(LegalKeySizes))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
			}
			KeySizeValue = value;
		}
	}

	public virtual KeySizes[] LegalKeySizes => (KeySizes[])LegalKeySizesValue.Clone();

	public virtual string? SignatureAlgorithm
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual string? KeyExchangeAlgorithm
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static AsymmetricAlgorithm Create()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static AsymmetricAlgorithm? Create(string algName)
	{
		return CryptoConfigForwarder.CreateFromName<AsymmetricAlgorithm>(algName);
	}

	public virtual void FromXmlString(string xmlString)
	{
		throw new NotImplementedException();
	}

	public virtual string ToXmlString(bool includePrivateParameters)
	{
		throw new NotImplementedException();
	}

	public void Clear()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		Clear();
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public virtual void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		return ExportArray(passwordBytes, pbeParameters, TryExportEncryptedPkcs8PrivateKey);
	}

	public virtual byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		return ExportArray(password, pbeParameters, TryExportEncryptedPkcs8PrivateKey);
	}

	public virtual byte[] ExportPkcs8PrivateKey()
	{
		return ExportArray(TryExportPkcs8PrivateKey);
	}

	public virtual byte[] ExportSubjectPublicKeyInfo()
	{
		return ExportArray(TryExportSubjectPublicKeyInfo);
	}

	public virtual bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
	{
		PemKeyHelpers.ImportEncryptedPem(input, password, ImportEncryptedPkcs8PrivateKey);
	}

	public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
	{
		PemKeyHelpers.ImportEncryptedPem(input, passwordBytes, ImportEncryptedPkcs8PrivateKey);
	}

	public virtual void ImportFromPem(ReadOnlySpan<char> input)
	{
		PemKeyHelpers.ImportPem(input, delegate(ReadOnlySpan<char> label)
		{
			if (label.SequenceEqual("PRIVATE KEY".AsSpan()))
			{
				return ImportPkcs8PrivateKey;
			}
			return label.SequenceEqual("PUBLIC KEY".AsSpan()) ? new PemKeyHelpers.ImportKeyAction(ImportSubjectPublicKeyInfo) : null;
		});
	}

	public unsafe string ExportPkcs8PrivateKeyPem()
	{
		byte[] array = ExportPkcs8PrivateKey();
		fixed (byte* ptr = array)
		{
			try
			{
				return PemEncoding.WriteString("PRIVATE KEY", array);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(array);
			}
		}
	}

	public unsafe string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		byte[] array = ExportEncryptedPkcs8PrivateKey(password, pbeParameters);
		fixed (byte* ptr = array)
		{
			try
			{
				return PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", array);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(array);
			}
		}
	}

	public unsafe string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		byte[] array = ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);
		fixed (byte* ptr = array)
		{
			try
			{
				return PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", array);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(array);
			}
		}
	}

	public string ExportSubjectPublicKeyInfoPem()
	{
		byte[] array = ExportSubjectPublicKeyInfo();
		return PemEncoding.WriteString("PUBLIC KEY", array);
	}

	public bool TryExportSubjectPublicKeyInfoPem(Span<char> destination, out int charsWritten)
	{
		return PemKeyHelpers.TryExportToPem(this, "PUBLIC KEY", Export, destination, out charsWritten);
		static bool Export(AsymmetricAlgorithm alg, Span<byte> destination, out int bytesWritten)
		{
			return alg.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
		}
	}

	public bool TryExportPkcs8PrivateKeyPem(Span<char> destination, out int charsWritten)
	{
		return PemKeyHelpers.TryExportToPem(this, "PRIVATE KEY", Export, destination, out charsWritten);
		static bool Export(AsymmetricAlgorithm alg, Span<byte> destination, out int bytesWritten)
		{
			return alg.TryExportPkcs8PrivateKey(destination, out bytesWritten);
		}
	}

	public bool TryExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<char> destination, out int charsWritten)
	{
		return PemKeyHelpers.TryExportToEncryptedPem(this, password, pbeParameters, Export, destination, out charsWritten);
		static bool Export(AsymmetricAlgorithm alg, ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
		{
			return alg.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);
		}
	}

	public bool TryExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<char> destination, out int charsWritten)
	{
		return PemKeyHelpers.TryExportToEncryptedPem(this, passwordBytes, pbeParameters, Export, destination, out charsWritten);
		static bool Export(AsymmetricAlgorithm alg, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
		{
			return alg.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);
		}
	}

	private unsafe static byte[] ExportArray<T>(ReadOnlySpan<T> password, PbeParameters pbeParameters, TryExportPbe<T> exporter)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(password, pbeParameters, array, out bytesWritten))
					{
						return new Span<byte>(array, 0, bytesWritten).ToArray();
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}

	private unsafe static byte[] ExportArray(TryExport exporter)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(array, out bytesWritten))
					{
						return new Span<byte>(array, 0, bytesWritten).ToArray();
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}
}
