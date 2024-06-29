using System.IO;

namespace System.Security.Cryptography;

internal sealed class DSAWrapper : DSA
{
	private readonly DSA _wrapped;

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

	public override string SignatureAlgorithm => _wrapped.SignatureAlgorithm;

	public override string KeyExchangeAlgorithm => _wrapped.KeyExchangeAlgorithm;

	internal DSAWrapper(DSA wrapped)
	{
		_wrapped = wrapped;
	}

	public override DSAParameters ExportParameters(bool includePrivateParameters)
	{
		return _wrapped.ExportParameters(includePrivateParameters);
	}

	public override void ImportParameters(DSAParameters parameters)
	{
		_wrapped.ImportParameters(parameters);
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		return _wrapped.CreateSignature(rgbHash);
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		return _wrapped.VerifySignature(rgbHash, rgbSignature);
	}

	public override byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		return _wrapped.SignData(data, offset, count, hashAlgorithm);
	}

	public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		return _wrapped.SignData(data, hashAlgorithm);
	}

	public override bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		return _wrapped.VerifyData(data, offset, count, signature, hashAlgorithm);
	}

	public override bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		return _wrapped.VerifyData(data, signature, hashAlgorithm);
	}

	public override bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryCreateSignature(hash, destination, out bytesWritten);
	}

	public override bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		return _wrapped.TrySignData(data, destination, hashAlgorithm, out bytesWritten);
	}

	public override bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
	{
		return _wrapped.VerifyData(data, signature, hashAlgorithm);
	}

	public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		return _wrapped.VerifySignature(hash, signature);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		return _wrapped.TryExportPkcs8PrivateKey(destination, out bytesWritten);
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

	public override void FromXmlString(string xmlString)
	{
		_wrapped.FromXmlString(xmlString);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		return _wrapped.ToXmlString(includePrivateParameters);
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

	public override byte[] ExportPkcs8PrivateKey()
	{
		return _wrapped.ExportPkcs8PrivateKey();
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

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		return HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		return HashOneShotHelpers.HashData(hashAlgorithm, data);
	}
}
