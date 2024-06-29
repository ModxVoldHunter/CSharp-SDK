using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

public class FromBase64Transform : ICryptoTransform, IDisposable
{
	private static readonly SearchValues<byte> s_whiteSpace = SearchValues.Create(new byte[6] { 32, 9, 10, 11, 12, 13 });

	private readonly FromBase64TransformMode _whitespaces;

	private byte[] _inputBuffer = new byte[4];

	private int _inputIndex;

	public int InputBlockSize => 4;

	public int OutputBlockSize => 3;

	public bool CanTransformMultipleBlocks => true;

	public virtual bool CanReuseTransform => true;

	public FromBase64Transform()
		: this(FromBase64TransformMode.IgnoreWhiteSpaces)
	{
	}

	public FromBase64Transform(FromBase64TransformMode whitespaces)
	{
		_whitespaces = whitespaces;
	}

	public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		ObjectDisposedException.ThrowIf(_inputBuffer == null, typeof(FromBase64Transform));
		if (outputBuffer == null)
		{
			ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);
		}
		ReadOnlySpan<byte> inputBuffer2 = inputBuffer.AsSpan(inputOffset, inputCount);
		int num = _inputIndex + inputBuffer2.Length;
		byte[] array = null;
		Span<byte> transformBuffer = stackalloc byte[32];
		if (num > 32)
		{
			transformBuffer = (array = System.Security.Cryptography.CryptoPool.Rent(inputCount));
		}
		transformBuffer = AppendInputBuffers(inputBuffer2, transformBuffer);
		num = transformBuffer.Length;
		if (num < InputBlockSize)
		{
			transformBuffer.CopyTo(_inputBuffer);
			_inputIndex = num;
			ReturnToCryptoPool(array, transformBuffer.Length);
			return 0;
		}
		ConvertFromBase64(transformBuffer, outputBuffer.AsSpan(outputOffset), out var _, out var written);
		ReturnToCryptoPool(array, transformBuffer.Length);
		return written;
	}

	public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		ObjectDisposedException.ThrowIf(_inputBuffer == null, typeof(FromBase64Transform));
		if (inputCount == 0)
		{
			return Array.Empty<byte>();
		}
		ReadOnlySpan<byte> inputBuffer2 = inputBuffer.AsSpan(inputOffset, inputCount);
		int num = _inputIndex + inputBuffer2.Length;
		byte[] array = null;
		Span<byte> transformBuffer = stackalloc byte[32];
		if (num > 32)
		{
			transformBuffer = (array = System.Security.Cryptography.CryptoPool.Rent(inputCount));
		}
		transformBuffer = AppendInputBuffers(inputBuffer2, transformBuffer);
		num = transformBuffer.Length;
		if (num < InputBlockSize)
		{
			Reset();
			ReturnToCryptoPool(array, transformBuffer.Length);
			return Array.Empty<byte>();
		}
		int outputSize = GetOutputSize(num, transformBuffer);
		byte[] array2 = new byte[outputSize];
		ConvertFromBase64(transformBuffer, array2, out var _, out var _);
		ReturnToCryptoPool(array, transformBuffer.Length);
		Reset();
		return array2;
	}

	private Span<byte> AppendInputBuffers(ReadOnlySpan<byte> inputBuffer, Span<byte> transformBuffer)
	{
		int num = _inputIndex;
		_inputBuffer.AsSpan(0, num).CopyTo(transformBuffer);
		if (_whitespaces == FromBase64TransformMode.DoNotIgnoreWhiteSpaces)
		{
			if (inputBuffer.ContainsAny(s_whiteSpace))
			{
				ThrowHelper.ThrowBase64FormatException();
			}
		}
		else
		{
			int num2;
			while ((num2 = inputBuffer.IndexOfAny(s_whiteSpace)) >= 0)
			{
				inputBuffer.Slice(0, num2).CopyTo(transformBuffer.Slice(num));
				num += num2;
				inputBuffer = inputBuffer.Slice(num2);
				do
				{
					inputBuffer = inputBuffer.Slice(1);
				}
				while (!inputBuffer.IsEmpty && s_whiteSpace.Contains(inputBuffer[0]));
			}
		}
		inputBuffer.CopyTo(transformBuffer.Slice(num));
		return transformBuffer.Slice(0, num + inputBuffer.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetOutputSize(int bytesToTransform, Span<byte> tmpBuffer)
	{
		int num = Base64.GetMaxDecodedFromUtf8Length(bytesToTransform);
		int length = tmpBuffer.Length;
		if (tmpBuffer[length - 2] == 61)
		{
			num--;
		}
		if (tmpBuffer[length - 1] == 61)
		{
			num--;
		}
		return num;
	}

	private void ConvertFromBase64(Span<byte> transformBuffer, Span<byte> outputBuffer, out int consumed, out int written)
	{
		int length = transformBuffer.Length;
		_inputIndex = length & 3;
		length -= _inputIndex;
		transformBuffer.Slice(transformBuffer.Length - _inputIndex).CopyTo(_inputBuffer);
		transformBuffer = transformBuffer.Slice(0, length);
		if (Base64.DecodeFromUtf8(transformBuffer, outputBuffer, out consumed, out written) != 0)
		{
			ThrowHelper.ThrowBase64FormatException();
		}
	}

	private static void ReturnToCryptoPool(byte[] array, int clearSize)
	{
		if (array != null)
		{
			System.Security.Cryptography.CryptoPool.Return(array, clearSize);
		}
	}

	public void Clear()
	{
		Dispose();
	}

	private void Reset()
	{
		_inputIndex = 0;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_inputBuffer != null)
			{
				CryptographicOperations.ZeroMemory(_inputBuffer);
				_inputBuffer = null;
			}
			Reset();
		}
	}

	~FromBase64Transform()
	{
		Dispose(disposing: false);
	}
}
