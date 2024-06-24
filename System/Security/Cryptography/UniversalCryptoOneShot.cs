namespace System.Security.Cryptography;

internal static class UniversalCryptoOneShot
{
	public unsafe static bool OneShotDecrypt(ILiteSymmetricCipher cipher, PaddingMode paddingMode, ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
	{
		if (input.Length % cipher.PaddingSizeInBytes != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_PartialBlock);
		}
		if (output.Length >= input.Length)
		{
			Span<byte> span = output[..cipher.TransformFinal(input, output)];
			try
			{
				bytesWritten = SymmetricPadding.GetPaddingLength(span, paddingMode, cipher.BlockSizeInBytes);
				CryptographicOperations.ZeroMemory(span.Slice(bytesWritten));
				return true;
			}
			catch (CryptographicException)
			{
				CryptographicOperations.ZeroMemory(span);
				throw;
			}
		}
		if (!SymmetricPadding.DepaddingRequired(paddingMode) || input.Length - cipher.BlockSizeInBytes > output.Length)
		{
			bytesWritten = 0;
			return false;
		}
		Span<byte> output2 = stackalloc byte[128];
		if (input.Length <= 128)
		{
			int paddingLength = SymmetricPadding.GetPaddingLength(output2[..cipher.TransformFinal(input, output2)], paddingMode, cipher.BlockSizeInBytes);
			Span<byte> buffer = output2.Slice(0, paddingLength);
			if (output.Length < paddingLength)
			{
				CryptographicOperations.ZeroMemory(buffer);
				bytesWritten = 0;
				return false;
			}
			buffer.CopyTo(output);
			CryptographicOperations.ZeroMemory(buffer);
			bytesWritten = paddingLength;
			return true;
		}
		if (!input.Overlaps(output, out var elementOffset) || elementOffset == 0)
		{
			int num = 0;
			int length = 0;
			int blockSizeInBytes = cipher.BlockSizeInBytes;
			ReadOnlySpan<byte> input2 = input.Slice(0, input.Length - blockSizeInBytes);
			blockSizeInBytes = cipher.BlockSizeInBytes;
			int length2 = input.Length;
			int num2 = length2 - blockSizeInBytes;
			ReadOnlySpan<byte> input3 = input.Slice(num2, length2 - num2);
			try
			{
				num = cipher.Transform(input2, output);
				length = cipher.TransformFinal(input3, output2);
				int paddingLength2 = SymmetricPadding.GetPaddingLength(output2.Slice(0, length), paddingMode, cipher.BlockSizeInBytes);
				Span<byte> buffer2 = output2.Slice(0, paddingLength2);
				if (output.Length - num < paddingLength2)
				{
					CryptographicOperations.ZeroMemory(buffer2);
					CryptographicOperations.ZeroMemory(output.Slice(0, num));
					bytesWritten = 0;
					return false;
				}
				buffer2.CopyTo(output.Slice(num));
				CryptographicOperations.ZeroMemory(buffer2);
				bytesWritten = num + paddingLength2;
				return true;
			}
			catch (CryptographicException)
			{
				CryptographicOperations.ZeroMemory(output.Slice(0, num));
				CryptographicOperations.ZeroMemory(output2.Slice(0, length));
				throw;
			}
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(input.Length);
		Span<byte> output3 = array.AsSpan(0, input.Length);
		Span<byte> span2 = default(Span<byte>);
		fixed (byte* ptr = output3)
		{
			try
			{
				span2 = output3[..cipher.TransformFinal(input, output3)];
				int paddingLength3 = SymmetricPadding.GetPaddingLength(span2, paddingMode, cipher.BlockSizeInBytes);
				if (paddingLength3 > output.Length)
				{
					bytesWritten = 0;
					return false;
				}
				span2.Slice(0, paddingLength3).CopyTo(output);
				bytesWritten = paddingLength3;
				return true;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span2);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	public static bool OneShotEncrypt(ILiteSymmetricCipher cipher, PaddingMode paddingMode, ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
	{
		int ciphertextLength = SymmetricPadding.GetCiphertextLength(input.Length, cipher.PaddingSizeInBytes, paddingMode);
		if (output.Length < ciphertextLength)
		{
			bytesWritten = 0;
			return false;
		}
		Span<byte> span = output[..SymmetricPadding.PadBlock(input, output, cipher.PaddingSizeInBytes, paddingMode)];
		bytesWritten = cipher.TransformFinal(span, span);
		return true;
	}
}
