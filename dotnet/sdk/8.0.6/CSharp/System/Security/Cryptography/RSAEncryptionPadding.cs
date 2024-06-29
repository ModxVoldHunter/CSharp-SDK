using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public sealed class RSAEncryptionPadding : IEquatable<RSAEncryptionPadding>
{
	private readonly RSAEncryptionPaddingMode _mode;

	private readonly HashAlgorithmName _oaepHashAlgorithm;

	public static RSAEncryptionPadding Pkcs1 { get; } = new RSAEncryptionPadding(RSAEncryptionPaddingMode.Pkcs1, default(HashAlgorithmName));


	public static RSAEncryptionPadding OaepSHA1 { get; } = CreateOaep(HashAlgorithmName.SHA1);


	public static RSAEncryptionPadding OaepSHA256 { get; } = CreateOaep(HashAlgorithmName.SHA256);


	public static RSAEncryptionPadding OaepSHA384 { get; } = CreateOaep(HashAlgorithmName.SHA384);


	public static RSAEncryptionPadding OaepSHA512 { get; } = CreateOaep(HashAlgorithmName.SHA512);


	public static RSAEncryptionPadding OaepSHA3_256 { get; } = CreateOaep(HashAlgorithmName.SHA3_256);


	public static RSAEncryptionPadding OaepSHA3_384 { get; } = CreateOaep(HashAlgorithmName.SHA3_384);


	public static RSAEncryptionPadding OaepSHA3_512 { get; } = CreateOaep(HashAlgorithmName.SHA3_512);


	public RSAEncryptionPaddingMode Mode => _mode;

	public HashAlgorithmName OaepHashAlgorithm => _oaepHashAlgorithm;

	private RSAEncryptionPadding(RSAEncryptionPaddingMode mode, HashAlgorithmName oaepHashAlgorithm)
	{
		_mode = mode;
		_oaepHashAlgorithm = oaepHashAlgorithm;
	}

	public static RSAEncryptionPadding CreateOaep(HashAlgorithmName hashAlgorithm)
	{
		ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, "hashAlgorithm");
		return new RSAEncryptionPadding(RSAEncryptionPaddingMode.Oaep, hashAlgorithm);
	}

	public override int GetHashCode()
	{
		return CombineHashCodes(_mode.GetHashCode(), _oaepHashAlgorithm.GetHashCode());
	}

	private static int CombineHashCodes(int h1, int h2)
	{
		return ((h1 << 5) + h1) ^ h2;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as RSAEncryptionPadding);
	}

	public bool Equals([NotNullWhen(true)] RSAEncryptionPadding? other)
	{
		if ((object)other != null && _mode == other._mode)
		{
			return _oaepHashAlgorithm == other._oaepHashAlgorithm;
		}
		return false;
	}

	public static bool operator ==(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return _mode.ToString() + _oaepHashAlgorithm.Name;
	}
}
