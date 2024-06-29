namespace System.Security.Cryptography;

internal static class SymmetricPadding
{
	public static int GetCiphertextLength(int plaintextLength, int paddingSizeInBytes, PaddingMode paddingMode)
	{
		int result;
		int num = Math.DivRem(plaintextLength, paddingSizeInBytes, out result) * paddingSizeInBytes;
		switch (paddingMode)
		{
		case PaddingMode.None:
			if (result != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_PartialBlock);
			}
			goto IL_003d;
		case PaddingMode.Zeros:
			if (result == 0)
			{
				goto IL_003d;
			}
			goto case PaddingMode.PKCS7;
		case PaddingMode.PKCS7:
		case PaddingMode.ANSIX923:
		case PaddingMode.ISO10126:
			return checked(num + paddingSizeInBytes);
		default:
			{
				throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
			}
			IL_003d:
			return plaintextLength;
		}
	}

	public static int PadBlock(ReadOnlySpan<byte> block, Span<byte> destination, int paddingSizeInBytes, PaddingMode paddingMode)
	{
		int length = block.Length;
		int num = length % paddingSizeInBytes;
		int num2 = paddingSizeInBytes - num;
		switch (paddingMode)
		{
		case PaddingMode.None:
			if (num != 0)
			{
				throw new CryptographicException(System.SR.Cryptography_PartialBlock);
			}
			if (destination.Length < length)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			return length;
		case PaddingMode.ANSIX923:
		{
			int num4 = length + num2;
			if (destination.Length < num4)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2 - 1).Clear();
			destination[length + num2 - 1] = (byte)num2;
			return num4;
		}
		case PaddingMode.ISO10126:
		{
			int num6 = length + num2;
			if (destination.Length < num6)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			RandomNumberGenerator.Fill(destination.Slice(length, num2 - 1));
			destination[length + num2 - 1] = (byte)num2;
			return num6;
		}
		case PaddingMode.PKCS7:
		{
			int num5 = length + num2;
			if (destination.Length < num5)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2).Fill((byte)num2);
			return num5;
		}
		case PaddingMode.Zeros:
		{
			if (num2 == paddingSizeInBytes)
			{
				num2 = 0;
			}
			int num3 = length + num2;
			if (destination.Length < num3)
			{
				throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
			}
			block.CopyTo(destination);
			destination.Slice(length, num2).Clear();
			return num3;
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
		}
	}

	public static bool DepaddingRequired(PaddingMode padding)
	{
		switch (padding)
		{
		case PaddingMode.PKCS7:
		case PaddingMode.ANSIX923:
		case PaddingMode.ISO10126:
			return true;
		case PaddingMode.None:
		case PaddingMode.Zeros:
			return false;
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
		}
	}

	public static int GetPaddingLength(ReadOnlySpan<byte> block, PaddingMode paddingMode, int blockSize)
	{
		int num;
		switch (paddingMode)
		{
		case PaddingMode.ANSIX923:
			num = block[block.Length - 1];
			if (num <= 0 || num > blockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			if (block.Slice(block.Length - num, num - 1).ContainsAnyExcept<byte>(0))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			break;
		case PaddingMode.ISO10126:
			num = block[block.Length - 1];
			if (num <= 0 || num > blockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			break;
		case PaddingMode.PKCS7:
		{
			num = block[block.Length - 1];
			if (num <= 0 || num > blockSize)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
			}
			for (int i = block.Length - num; i < block.Length - 1; i++)
			{
				if (block[i] != num)
				{
					throw new CryptographicException(System.SR.Cryptography_InvalidPadding);
				}
			}
			break;
		}
		case PaddingMode.None:
		case PaddingMode.Zeros:
			num = 0;
			break;
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownPaddingMode);
		}
		return block.Length - num;
	}
}
