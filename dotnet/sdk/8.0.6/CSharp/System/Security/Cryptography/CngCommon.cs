using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class CngCommon
{
	public unsafe static byte[] SignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo, int estimatedSize)
	{
		byte[] array = new byte[estimatedSize];
		int pcbResult;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, array, out pcbResult, paddingMode);
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, array, out pcbResult, paddingMode);
		}
		if (errorCode.IsBufferTooSmall())
		{
			array = new byte[pcbResult];
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, array, out pcbResult, paddingMode);
		}
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, array, out pcbResult, paddingMode);
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		Array.Resize(ref array, pcbResult);
		return array;
	}

	public unsafe static bool TrySignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, Span<byte> signature, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo, out int bytesWritten)
	{
		for (int i = 0; i <= 1; i++)
		{
			int pcbResult;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, signature, out pcbResult, paddingMode);
			global::Interop.NCrypt.ErrorCode errorCode2 = errorCode;
			if (errorCode2 == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS)
			{
				bytesWritten = pcbResult;
				return true;
			}
			if (!errorCode2.IsBufferTooSmall())
			{
				if (errorCode2 != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
				{
					throw errorCode.ToCryptographicException();
				}
				continue;
			}
			bytesWritten = 0;
			return false;
		}
		throw global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
	}

	public unsafe static bool VerifyHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
		}
		return errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
	}
}
