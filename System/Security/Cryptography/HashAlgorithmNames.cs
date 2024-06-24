namespace System.Security.Cryptography;

internal static class HashAlgorithmNames
{
	public static string ToAlgorithmName(this HashAlgorithm hashAlgorithm)
	{
		if (hashAlgorithm is SHA1)
		{
			return "SHA1";
		}
		if (hashAlgorithm is SHA256)
		{
			return "SHA256";
		}
		if (hashAlgorithm is SHA384)
		{
			return "SHA384";
		}
		if (hashAlgorithm is SHA512)
		{
			return "SHA512";
		}
		if (hashAlgorithm is MD5)
		{
			return "MD5";
		}
		return hashAlgorithm.ToString();
	}

	public static string ToUpper(string hashAlgorithmName)
	{
		if (hashAlgorithmName.Equals("SHA256", StringComparison.OrdinalIgnoreCase))
		{
			return "SHA256";
		}
		if (hashAlgorithmName.Equals("SHA384", StringComparison.OrdinalIgnoreCase))
		{
			return "SHA384";
		}
		if (hashAlgorithmName.Equals("SHA512", StringComparison.OrdinalIgnoreCase))
		{
			return "SHA512";
		}
		if (hashAlgorithmName.Equals("SHA1", StringComparison.OrdinalIgnoreCase))
		{
			return "SHA1";
		}
		if (hashAlgorithmName.Equals("MD5", StringComparison.OrdinalIgnoreCase))
		{
			return "MD5";
		}
		return hashAlgorithmName;
	}
}
