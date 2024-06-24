using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen;

internal static class Hashing
{
	public unsafe static int GetHashCodeOrdinal(ReadOnlySpan<char> s)
	{
		int num = s.Length;
		fixed (char* ptr = &MemoryMarshal.GetReference(s))
		{
			switch (num)
			{
			case 0:
				return 757602046;
			case 1:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *ptr;
				return (int)(352654597 + num3 * 1566083941);
			}
			case 2:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *ptr;
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ ptr[1];
				return (int)(352654597 + num3 * 1566083941);
			}
			case 3:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *ptr;
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ ptr[1];
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ ptr[2];
				return (int)(352654597 + num3 * 1566083941);
			}
			case 4:
			{
				uint num2 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *(uint*)ptr;
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ *(uint*)(ptr + 2);
				return (int)(num2 + num3 * 1566083941);
			}
			default:
			{
				uint num2 = 352654597u;
				uint num3 = num2;
				uint* ptr2 = (uint*)ptr;
				while (num >= 4)
				{
					num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ *ptr2;
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ ptr2[1];
					ptr2 += 2;
					num -= 4;
				}
				char* ptr3 = (char*)ptr2;
				while (num-- > 0)
				{
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ *(ptr3++);
				}
				return (int)(num2 + num3 * 1566083941);
			}
			}
		}
	}

	public unsafe static int GetHashCodeOrdinalIgnoreCaseAscii(ReadOnlySpan<char> s)
	{
		int num = s.Length;
		fixed (char* ptr = &MemoryMarshal.GetReference(s))
		{
			switch (num)
			{
			case 0:
				return 757602046;
			case 1:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*ptr | 0x20u);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 2:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*ptr | 0x20u);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (ptr[1] | 0x20u);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 3:
			{
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*ptr | 0x20u);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (ptr[1] | 0x20u);
				num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (ptr[2] | 0x20u);
				return (int)(352654597 + num3 * 1566083941);
			}
			case 4:
			{
				uint num2 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*(uint*)ptr | 0x200020u);
				uint num3 = (BitOperations.RotateLeft(352654597u, 5) + 352654597) ^ (*(uint*)(ptr + 2) | 0x200020u);
				return (int)(num2 + num3 * 1566083941);
			}
			default:
			{
				uint num2 = 352654597u;
				uint num3 = num2;
				uint* ptr2 = (uint*)ptr;
				while (num >= 4)
				{
					num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ (*ptr2 | 0x200020u);
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (ptr2[1] | 0x200020u);
					ptr2 += 2;
					num -= 4;
				}
				char* ptr3 = (char*)ptr2;
				while (num-- > 0)
				{
					num3 = (BitOperations.RotateLeft(num3, 5) + num3) ^ (*ptr3 | 0x200020u);
					ptr3++;
				}
				return (int)(num2 + num3 * 1566083941);
			}
			}
		}
	}

	public static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
	{
		int length = s.Length;
		char[] array = null;
		Span<char> span = ((length > 256) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(length))) : stackalloc char[256]);
		Span<char> destination = span;
		int hashCodeOrdinal = GetHashCodeOrdinal(destination[..s.ToUpperInvariant(destination)]);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return hashCodeOrdinal;
	}
}
