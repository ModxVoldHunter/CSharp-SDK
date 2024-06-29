using System.Buffers.Binary;
using System.Security;

namespace System.Reflection;

internal static class AssemblyNameHelpers
{
	private static ReadOnlySpan<byte> EcmaKey => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 4, 0,
		0, 0, 0, 0, 0, 0
	};

	public static byte[] ComputePublicKeyToken(byte[] publicKey)
	{
		if (publicKey == null)
		{
			return null;
		}
		if (publicKey.Length == 0)
		{
			return Array.Empty<byte>();
		}
		if (!IsValidPublicKey(publicKey))
		{
			throw new SecurityException(SR.Security_InvalidAssemblyPublicKey);
		}
		Span<byte> output = stackalloc byte[20];
		Sha1ForNonSecretPurposes sha1ForNonSecretPurposes = default(Sha1ForNonSecretPurposes);
		sha1ForNonSecretPurposes.Start();
		sha1ForNonSecretPurposes.Append(publicKey);
		sha1ForNonSecretPurposes.Finish(output);
		byte[] array = new byte[8];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = output[output.Length - 1 - i];
		}
		return array;
	}

	private static bool IsValidPublicKey(byte[] publicKey)
	{
		uint num = (uint)publicKey.Length;
		if (num < 16)
		{
			return false;
		}
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(publicKey);
		uint num2 = BinaryPrimitives.ReadUInt32LittleEndian(readOnlySpan);
		uint num3 = BinaryPrimitives.ReadUInt32LittleEndian(readOnlySpan.Slice(4));
		uint num4 = BinaryPrimitives.ReadUInt32LittleEndian(readOnlySpan.Slice(8));
		if (num4 != num - 12)
		{
			return false;
		}
		if (EcmaKey.SequenceEqual(readOnlySpan))
		{
			return true;
		}
		bool flag = GetAlgClass(num3) == 32768 && GetAlgSid(num3) >= 4;
		if (num3 != 0 && !flag)
		{
			return false;
		}
		bool flag2 = GetAlgClass(num2) == 8192;
		if (num2 != 0 && !flag2)
		{
			return false;
		}
		if (publicKey[12] != 6)
		{
			return false;
		}
		return true;
	}

	private static uint GetAlgClass(uint x)
	{
		return x & 0xE000u;
	}

	private static uint GetAlgSid(uint x)
	{
		return x & 0x1FFu;
	}
}
