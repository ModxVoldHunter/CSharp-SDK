using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class CngHelpers
{
	private static readonly CngKeyBlobFormat s_cipherKeyBlobFormat = new CngKeyBlobFormat("CipherKeyBlob");

	internal static CryptographicException ToCryptographicException(this global::Interop.NCrypt.ErrorCode errorCode)
	{
		return ((int)errorCode).ToCryptographicException();
	}

	internal static SafeNCryptProviderHandle OpenStorageProvider(this CngProvider provider)
	{
		string provider2 = provider.Provider;
		SafeNCryptProviderHandle phProvider;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptOpenStorageProvider(out phProvider, provider2, 0);
		if (errorCode != 0)
		{
			phProvider.Dispose();
			throw errorCode.ToCryptographicException();
		}
		return phProvider;
	}

	public unsafe static void SetExportPolicy(this SafeNCryptKeyHandle keyHandle, CngExportPolicies exportPolicy)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Export Policy", &exportPolicy, 4, CngPropertyOptions.Persist);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
	}

	internal unsafe static byte[] GetProperty(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, null, 0, out var pcbResult, options);
		switch (errorCode)
		{
		case global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND:
			return null;
		default:
			throw errorCode.ToCryptographicException();
		case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
		{
			byte[] array = new byte[pcbResult];
			fixed (byte* pbOutput = array)
			{
				errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, pbOutput, array.Length, out pcbResult, options);
			}
			switch (errorCode)
			{
			case global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND:
				return null;
			default:
				throw errorCode.ToCryptographicException();
			case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
				Array.Resize(ref array, pcbResult);
				return array;
			}
		}
		}
	}

	internal unsafe static string GetPropertyAsString(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		byte[] property = ncryptHandle.GetProperty(propertyName, options);
		if (property == null)
		{
			return null;
		}
		if (property.Length == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = &property[0])
		{
			return Marshal.PtrToStringUni((nint)ptr);
		}
	}

	public static int GetPropertyAsDword(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		byte[] property = ncryptHandle.GetProperty(propertyName, options);
		if (property == null)
		{
			return 0;
		}
		return BitConverter.ToInt32(property, 0);
	}

	internal unsafe static nint GetPropertyAsIntPtr(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		Unsafe.SkipInit(out nint num);
		int pcbResult;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, &num, IntPtr.Size, out pcbResult, options);
		return errorCode switch
		{
			global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND => IntPtr.Zero, 
			global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS => num, 
			_ => throw errorCode.ToCryptographicException(), 
		};
	}

	internal static byte[] GetSymmetricKeyDataIfExportable(this CngKey cngKey, string algorithm)
	{
		byte[] buffer = cngKey.Export(s_cipherKeyBlobFormat);
		using MemoryStream input = new MemoryStream(buffer);
		using BinaryReader binaryReader = new BinaryReader(input, Encoding.Unicode);
		int num = binaryReader.ReadInt32();
		if (num != 16)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num2 = binaryReader.ReadInt32();
		if (num2 != 1380470851)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num3 = binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		string text = new string(binaryReader.ReadChars(num3 / 2 - 1));
		if (text != algorithm)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CngKeyWrongAlgorithm, text, algorithm));
		}
		if (binaryReader.ReadChar() != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num4 = binaryReader.ReadInt32();
		if ((long)num4 != 1296188491)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num5 = binaryReader.ReadInt32();
		if ((long)num5 != 1)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int count = binaryReader.ReadInt32();
		return binaryReader.ReadBytes(count);
	}

	internal unsafe static ArraySegment<byte> ToBCryptBlob(this in RSAParameters parameters)
	{
		if (parameters.Exponent == null || parameters.Modulus == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
		}
		bool flag;
		if (parameters.D == null)
		{
			flag = false;
			if (parameters.P != null || parameters.DP != null || parameters.Q != null || parameters.DQ != null || parameters.InverseQ != null)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
		}
		else
		{
			flag = true;
			if (parameters.P == null || parameters.DP == null || parameters.Q == null || parameters.DQ == null || parameters.InverseQ == null)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
			int num = (parameters.Modulus.Length + 1) / 2;
			if (parameters.D.Length != parameters.Modulus.Length || parameters.P.Length != num || parameters.Q.Length != num || parameters.DP.Length != num || parameters.DQ.Length != num || parameters.InverseQ.Length != num)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
		}
		int num2 = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB) + parameters.Exponent.Length + parameters.Modulus.Length;
		if (flag)
		{
			num2 += parameters.P.Length + parameters.Q.Length;
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num2);
		fixed (byte* ptr = &array[0])
		{
			global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB*)ptr;
			ptr2->Magic = (flag ? global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC : global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC);
			ptr2->BitLength = parameters.Modulus.Length * 8;
			ptr2->cbPublicExp = parameters.Exponent.Length;
			ptr2->cbModulus = parameters.Modulus.Length;
			if (flag)
			{
				ptr2->cbPrime1 = parameters.P.Length;
				ptr2->cbPrime2 = parameters.Q.Length;
			}
			else
			{
				ptr2->cbPrime1 = (ptr2->cbPrime2 = 0);
			}
			int offset = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB);
			global::Interop.BCrypt.Emit(array, ref offset, parameters.Exponent);
			global::Interop.BCrypt.Emit(array, ref offset, parameters.Modulus);
			if (flag)
			{
				global::Interop.BCrypt.Emit(array, ref offset, parameters.P);
				global::Interop.BCrypt.Emit(array, ref offset, parameters.Q);
			}
		}
		return new ArraySegment<byte>(array, 0, num2);
	}

	internal unsafe static void FromBCryptBlob(this ref RSAParameters rsaParams, ReadOnlySpan<byte> rsaBlob, bool includePrivateParameters)
	{
		if (rsaBlob.Length < sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB))
		{
			throw global::Interop.NCrypt.ErrorCode.E_FAIL.ToCryptographicException();
		}
		fixed (byte* ptr = &rsaBlob[0])
		{
			global::Interop.BCrypt.KeyBlobMagicNumber magic2 = (global::Interop.BCrypt.KeyBlobMagicNumber)Unsafe.ReadUnaligned<int>(ptr);
			CheckMagicValueOfKey(magic2, includePrivateParameters);
			global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB*)ptr;
			int offset = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB);
			rsaParams.Exponent = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPublicExp);
			rsaParams.Modulus = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbModulus);
			if (includePrivateParameters)
			{
				rsaParams.P = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.Q = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime2);
				rsaParams.DP = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.DQ = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime2);
				rsaParams.InverseQ = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.D = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbModulus);
			}
		}
		static void CheckMagicValueOfKey(global::Interop.BCrypt.KeyBlobMagicNumber magic, bool includePrivateParameters)
		{
			if (includePrivateParameters)
			{
				if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
				{
					throw new CryptographicException(System.SR.Cryptography_NotValidPrivateKey);
				}
			}
			else if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
			}
		}
	}
}
