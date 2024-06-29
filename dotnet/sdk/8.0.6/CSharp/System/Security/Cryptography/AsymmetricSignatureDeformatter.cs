namespace System.Security.Cryptography;

public abstract class AsymmetricSignatureDeformatter
{
	public abstract void SetKey(AsymmetricAlgorithm key);

	public abstract void SetHashAlgorithm(string strName);

	public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		SetHashAlgorithm(hash.ToAlgorithmName());
		return VerifySignature(hash.Hash, rgbSignature);
	}

	public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
}
