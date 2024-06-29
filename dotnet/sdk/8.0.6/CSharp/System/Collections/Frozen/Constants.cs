using System.Text;

namespace System.Collections.Frozen;

internal static class Constants
{
	public const int MaxItemsInSmallFrozenCollection = 4;

	public const int MaxItemsInSmallValueTypeFrozenCollection = 10;

	public static bool IsKnownComparable<T>()
	{
		if (!(typeof(T) == typeof(bool)) && !(typeof(T) == typeof(sbyte)) && !(typeof(T) == typeof(byte)) && !(typeof(T) == typeof(char)) && !(typeof(T) == typeof(short)) && !(typeof(T) == typeof(ushort)) && !(typeof(T) == typeof(int)) && !(typeof(T) == typeof(uint)) && !(typeof(T) == typeof(long)) && !(typeof(T) == typeof(ulong)) && !(typeof(T) == typeof(decimal)) && !(typeof(T) == typeof(float)) && !(typeof(T) == typeof(double)) && !(typeof(T) == typeof(decimal)) && !(typeof(T) == typeof(TimeSpan)) && !(typeof(T) == typeof(DateTime)) && !(typeof(T) == typeof(DateTimeOffset)) && !(typeof(T) == typeof(Guid)) && !(typeof(T) == typeof(Rune)) && !(typeof(T) == typeof(Half)) && !(typeof(T) == typeof(nint)) && !(typeof(T) == typeof(nuint)) && !(typeof(T) == typeof(DateOnly)) && !(typeof(T) == typeof(TimeOnly)) && !(typeof(T) == typeof(Int128)) && !(typeof(T) == typeof(UInt128)))
		{
			return typeof(T).IsEnum;
		}
		return true;
	}

	internal static bool KeysAreHashCodes<T>()
	{
		if (!(typeof(T) == typeof(int)) && !(typeof(T) == typeof(uint)) && !(typeof(T) == typeof(short)) && !(typeof(T) == typeof(ushort)) && !(typeof(T) == typeof(byte)) && !(typeof(T) == typeof(sbyte)))
		{
			if (typeof(T) == typeof(nint) || typeof(T) == typeof(nuint))
			{
				return IntPtr.Size == 4;
			}
			return false;
		}
		return true;
	}
}
