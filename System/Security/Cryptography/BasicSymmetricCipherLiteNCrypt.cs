using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class BasicSymmetricCipherLiteNCrypt : ILiteSymmetricCipher, IDisposable
{
	private static readonly CngProperty s_ECBMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeECB\0"), CngPropertyOptions.None);

	private static readonly CngProperty s_CBCMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeCBC\0"), CngPropertyOptions.None);

	private static readonly CngProperty s_CFBMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeCFB\0"), CngPropertyOptions.None);

	private readonly bool _encrypting;

	private CngKey _key;

	public int BlockSizeInBytes { get; }

	public int PaddingSizeInBytes { get; }

	public BasicSymmetricCipherLiteNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, ReadOnlySpan<byte> iv, bool encrypting, int paddingSizeInBytes)
	{
		BlockSizeInBytes = blockSizeInBytes;
		PaddingSizeInBytes = paddingSizeInBytes;
		_encrypting = encrypting;
		_key = cngKeyFactory();
		CngProperty property = cipherMode switch
		{
			CipherMode.ECB => s_ECBMode, 
			CipherMode.CBC => s_CBCMode, 
			CipherMode.CFB => s_CFBMode, 
			_ => throw new CryptographicException(System.SR.Cryptography_InvalidCipherMode), 
		};
		_key.SetProperty(property);
		Reset(iv);
	}

	public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int num = 0;
		if (input.Overlaps(output, out var elementOffset) && elementOffset != 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(output.Length);
			try
			{
				num = NCryptTransform(input, array);
				array.AsSpan(0, num).CopyTo(output);
			}
			finally
			{
				System.Security.Cryptography.CryptoPool.Return(array, num);
			}
		}
		else
		{
			num = NCryptTransform(input, output);
		}
		if (num != input.Length)
		{
			throw new CryptographicException(System.SR.Cryptography_UnexpectedTransformTruncation);
		}
		return num;
		unsafe int NCryptTransform(ReadOnlySpan<byte> input, Span<byte> output)
		{
			using SafeNCryptKeyHandle hKey = _key.Handle;
			int pcbResult;
			global::Interop.NCrypt.ErrorCode errorCode = (_encrypting ? global::Interop.NCrypt.NCryptEncrypt(hKey, input, input.Length, null, output, output.Length, out pcbResult, global::Interop.NCrypt.AsymmetricPaddingMode.None) : global::Interop.NCrypt.NCryptDecrypt(hKey, input, input.Length, null, output, output.Length, out pcbResult, global::Interop.NCrypt.AsymmetricPaddingMode.None));
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			return pcbResult;
		}
	}

	public unsafe void Reset(ReadOnlySpan<byte> iv)
	{
		if (iv.IsEmpty)
		{
			return;
		}
		fixed (byte* pbInput = &MemoryMarshal.GetReference(iv))
		{
			using SafeNCryptKeyHandle hObject = _key.Handle;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(hObject, "IV", pbInput, iv.Length, CngPropertyOptions.None);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
	}

	public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = 0;
		if (input.Length != 0)
		{
			result = Transform(input, output);
		}
		return result;
	}

	public void Dispose()
	{
		_key?.Dispose();
		_key = null;
	}
}
