namespace System.Security.Cryptography;

internal sealed class UniversalCryptoDecryptor : UniversalCryptoTransform
{
	private byte[] _heldoverCipher;

	public UniversalCryptoDecryptor(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
		: base(paddingMode, basicSymmetricCipher)
	{
	}

	protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		int num = 0;
		if (SymmetricPadding.DepaddingRequired(base.PaddingMode))
		{
			if (_heldoverCipher != null)
			{
				int num2 = base.BasicSymmetricCipher.Transform(_heldoverCipher, outputBuffer);
				outputBuffer = outputBuffer.Slice(num2);
				num += num2;
			}
			else
			{
				_heldoverCipher = new byte[base.InputBlockSize];
			}
			inputBuffer.Slice(inputBuffer.Length - _heldoverCipher.Length).CopyTo(_heldoverCipher);
			inputBuffer = inputBuffer.Slice(0, inputBuffer.Length - _heldoverCipher.Length);
		}
		if (inputBuffer.Length > 0)
		{
			num += base.BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
		}
		return num;
	}

	protected unsafe override int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		if (inputBuffer.Length % base.PaddingSizeBytes != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_PartialBlock);
		}
		byte[] array = null;
		int num = 0;
		try
		{
			Span<byte> span;
			ReadOnlySpan<byte> input;
			if (_heldoverCipher == null)
			{
				num = inputBuffer.Length;
				array = System.Security.Cryptography.CryptoPool.Rent(inputBuffer.Length);
				span = array.AsSpan(0, inputBuffer.Length);
				input = inputBuffer;
			}
			else
			{
				num = _heldoverCipher.Length + inputBuffer.Length;
				array = System.Security.Cryptography.CryptoPool.Rent(num);
				span = array.AsSpan(0, num);
				_heldoverCipher.AsSpan().CopyTo(span);
				inputBuffer.CopyTo(span.Slice(_heldoverCipher.Length));
				input = span;
			}
			int num2 = 0;
			fixed (byte* ptr = span)
			{
				Span<byte> span2 = span[..base.BasicSymmetricCipher.TransformFinal(input, span)];
				if (span2.Length > 0)
				{
					num2 = SymmetricPadding.GetPaddingLength(span2, base.PaddingMode, base.InputBlockSize);
					span2.Slice(0, num2).CopyTo(outputBuffer);
				}
			}
			Reset();
			return num2;
		}
		finally
		{
			if (array != null)
			{
				System.Security.Cryptography.CryptoPool.Return(array, num);
			}
		}
	}

	protected unsafe override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		if (SymmetricPadding.DepaddingRequired(base.PaddingMode))
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(inputCount + base.InputBlockSize);
			int num = 0;
			fixed (byte* ptr = array)
			{
				try
				{
					num = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array);
					return array.AsSpan(0, num).ToArray();
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, num);
				}
			}
		}
		byte[] array2 = GC.AllocateUninitializedArray<byte>(inputCount);
		int num2 = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array2);
		return array2;
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing)
		{
			byte[] heldoverCipher = _heldoverCipher;
			_heldoverCipher = null;
			if (heldoverCipher != null)
			{
				Array.Clear(heldoverCipher);
			}
		}
		base.Dispose(disposing);
	}

	private void Reset()
	{
		if (_heldoverCipher != null)
		{
			Array.Clear(_heldoverCipher);
			_heldoverCipher = null;
		}
	}
}
