using System.Formats.Asn1;

namespace System.Security.Cryptography;

public abstract class ECAlgorithm : AsymmetricAlgorithm
{
	private static readonly string[] s_validOids = new string[1] { "1.2.840.10045.2.1" };

	private protected static readonly KeySizes[] s_defaultKeySizes = new KeySizes[2]
	{
		new KeySizes(256, 384, 128),
		new KeySizes(521, 521, 0)
	};

	public virtual ECParameters ExportParameters(bool includePrivateParameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportParameters(ECParameters parameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void GenerateKey(ECCurve curve)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public unsafe override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter pkcs8Writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8Writer, pbeParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		ArgumentNullException.ThrowIfNull(pbeParameters, "pbeParameters");
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter pkcs8Writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8Writer, pbeParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: false);
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, passwordBytes, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, password, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public unsafe override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadPkcs8(s_validOids, source, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadSubjectPublicKeyInfo(s_validOids, source, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPublicKey, out int bytesRead2, out ECParameters ret);
		ImportParameters(ret);
		bytesRead = bytesRead2;
	}

	public unsafe virtual void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		int bytesRead2;
		ECParameters parameters = EccKeyFormatHelper.FromECPrivateKey(source, out bytesRead2);
		fixed (byte* ptr = parameters.D)
		{
			try
			{
				ImportParameters(parameters);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(parameters.D);
			}
		}
	}

	public unsafe virtual byte[] ExportECPrivateKey()
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WriteECPrivateKey(in ecParameters);
				return asnWriter.Encode();
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe virtual bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WriteECPrivateKey(in ecParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public override void ImportFromPem(ReadOnlySpan<char> input)
	{
		PemKeyHelpers.ImportPem(input, (ReadOnlySpan<char> label) => label switch
		{
			"PRIVATE KEY" => ImportPkcs8PrivateKey, 
			"PUBLIC KEY" => ImportSubjectPublicKeyInfo, 
			"EC PRIVATE KEY" => ImportECPrivateKey, 
			_ => null, 
		});
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
	{
		base.ImportFromEncryptedPem(input, password);
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
	{
		base.ImportFromEncryptedPem(input, passwordBytes);
	}

	public unsafe string ExportECPrivateKeyPem()
	{
		byte[] array = ExportECPrivateKey();
		fixed (byte* ptr = array)
		{
			try
			{
				return PemEncoding.WriteString("EC PRIVATE KEY", array);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(array);
			}
		}
	}

	public bool TryExportECPrivateKeyPem(Span<char> destination, out int charsWritten)
	{
		return PemKeyHelpers.TryExportToPem(this, "EC PRIVATE KEY", Export, destination, out charsWritten);
		static bool Export(ECAlgorithm alg, Span<byte> destination, out int bytesWritten)
		{
			return alg.TryExportECPrivateKey(destination, out bytesWritten);
		}
	}
}
