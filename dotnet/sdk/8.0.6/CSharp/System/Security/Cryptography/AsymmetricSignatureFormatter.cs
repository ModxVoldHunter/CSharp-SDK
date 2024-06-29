namespace System.Security.Cryptography;

public abstract class AsymmetricSignatureFormatter
{
	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract void SetHashAlgorithm(string strName);

	public virtual byte[] CreateSignature(HashAlgorithm hash)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		SetHashAlgorithm(hash.ToAlgorithmName());
		return CreateSignature(hash.Hash);
	}

	public abstract byte[] CreateSignature(byte[] rgbHash);
}
