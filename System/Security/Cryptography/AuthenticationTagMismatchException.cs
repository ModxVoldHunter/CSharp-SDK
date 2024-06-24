namespace System.Security.Cryptography;

public sealed class AuthenticationTagMismatchException : CryptographicException
{
	public AuthenticationTagMismatchException()
		: base(System.SR.Cryptography_AuthTagMismatch)
	{
	}

	public AuthenticationTagMismatchException(string? message)
		: base(message)
	{
	}

	public AuthenticationTagMismatchException(string? message, Exception? inner)
		: base(message, inner)
	{
	}
}
