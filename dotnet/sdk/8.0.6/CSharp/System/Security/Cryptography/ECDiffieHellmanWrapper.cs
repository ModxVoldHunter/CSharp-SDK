namespace System.Security.Cryptography;

internal sealed class ECDiffieHellmanWrapper : ECDiffieHellman
{
	private sealed class ECDiffieHellmanPublicKeyWrapper : ECDiffieHellmanPublicKey
	{
		private readonly ECDiffieHellmanPublicKey _wrapped;

		internal ECDiffieHellmanPublicKey WrappedKey => _wrapped;

		internal ECDiffieHellmanPublicKeyWrapper(ECDiffieHellmanPublicKey wrapped)
		{
			_wrapped = wrapped;
		}

		public override ECParameters ExportParameters()
		{
			return _wrapped.ExportParameters();
		}

		public override ECParameters ExportExplicitParameters()
		{
			return _wrapped.ExportExplicitParameters();
		}

		public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
		{
			return _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
		}

		public override byte[] ExportSubjectPublicKeyInfo()
		{
			return _wrapped.ExportSubjectPublicKeyInfo();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_wrapped.Dispose();
			}
		}

		public override byte[] ToByteArray()
		{
			return _wrapped.ToByteArray();
		}

		public override string ToXmlString()
		{
			return _wrapped.ToXmlString();
		}

		public override bool Equals(object obj)
		{
			return _wrapped.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _wrapped.GetHashCode();
		}

		public override string ToString()
		{
			return _wrapped.ToString();
		}
	}

	private readonly ECDiffieHellman _wrapped;

	public override string KeyExchangeAlgorithm => _wrapped.KeyExchangeAlgorithm;

	public override string SignatureAlgorithm => _wrapped.SignatureAlgorithm;

	public override ECDiffieHellmanPublicKey PublicKey => new ECDiffieHellmanPublicKeyWrapper(_wrapped.PublicKey);

	public override int KeySize
	{
		get
		{
			return _wrapped.KeySize;
		}
		set
		{
			_wrapped.KeySize = value;
		}
	}

	public override KeySizes[] LegalKeySizes => _wrapped.LegalKeySizes;

	internal ECDiffieHellmanWrapper(ECDiffieHellman wrapped)
	{
		_wrapped = wrapped;
	}

	public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		return _wrapped.DeriveKeyMaterial(Unwrap(otherPartyPublicKey));
	}

	public override byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] secretPrepend, byte[] secretAppend)
	{
		return _wrapped.DeriveKeyFromHash(Unwrap(otherPartyPublicKey), hashAlgorithm, secretPrepend, secretAppend);
	}

	public override byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] hmacKey, byte[] secretPrepend, byte[] secretAppend)
	{
		return _wrapped.DeriveKeyFromHmac(Unwrap(otherPartyPublicKey), hashAlgorithm, hmacKey, secretPrepend, secretAppend);
	}

	public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
	{
		return _wrapped.DeriveKeyTls(Unwrap(otherPartyPublicKey), prfLabel, prfSeed);
	}

	public override byte[] DeriveRawSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		return _wrapped.DeriveRawSecretAgreement(Unwrap(otherPartyPublicKey));
	}

	public override void FromXmlString(string xmlString)
	{
		_wrapped.FromXmlString(xmlString);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		return _wrapped.ToXmlString(includePrivateParameters);
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		return _wrapped.ExportParameters(includePrivateParameters);
	}

	public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		return _wrapped.ExportExplicitParameters(includePrivateParameters);
	}

	public override void ImportParameters(ECParameters parameters)
	{
		_wrapped.ImportParameters(parameters);
	}

	public override void GenerateKey(ECCurve curve)
	{
		_wrapped.GenerateKey(curve);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		_wrapped.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		_wrapped.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
	}

	public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		_wrapped.ImportPkcs8PrivateKey(source, out bytesRead);
	}

	public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		_wrapped.ImportSubjectPublicKeyInfo(source, out bytesRead);
	}

	public override void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		_wrapped.ImportECPrivateKey(source, out bytesRead);
	}

	public override byte[] ExportECPrivateKey()
	{
		return _wrapped.ExportECPrivateKey();
	}

	public override bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportECPrivateKey(destination, out bytesWritten);
	}

	public override void ImportFromPem(ReadOnlySpan<char> input)
	{
		_wrapped.ImportFromPem(input);
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
	{
		_wrapped.ImportFromEncryptedPem(input, password);
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
	{
		_wrapped.ImportFromEncryptedPem(input, passwordBytes);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_wrapped.Dispose();
		}
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		return _wrapped.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		return _wrapped.ExportEncryptedPkcs8PrivateKey(password, pbeParameters);
	}

	public override byte[] ExportSubjectPublicKeyInfo()
	{
		return _wrapped.ExportSubjectPublicKeyInfo();
	}

	public override bool Equals(object obj)
	{
		return _wrapped.Equals(obj);
	}

	public override int GetHashCode()
	{
		return _wrapped.GetHashCode();
	}

	public override string ToString()
	{
		return _wrapped.ToString();
	}

	private static ECDiffieHellmanPublicKey Unwrap(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		if (otherPartyPublicKey is ECDiffieHellmanPublicKeyWrapper eCDiffieHellmanPublicKeyWrapper)
		{
			return eCDiffieHellmanPublicKeyWrapper.WrappedKey;
		}
		return otherPartyPublicKey;
	}
}
