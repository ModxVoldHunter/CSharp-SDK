using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class NullableEqualityComparer<T> : EqualityComparer<T?>, ISerializable where T : struct
{
	internal override int IndexOf(T?[] array, T? value, int startIndex, int count)
	{
		int num = startIndex + count;
		if (!value.HasValue)
		{
			for (int i = startIndex; i < num; i++)
			{
				if (!array[i].HasValue)
				{
					return i;
				}
			}
		}
		else
		{
			for (int j = startIndex; j < num; j++)
			{
				if (array[j].HasValue && EqualityComparer<T>.Default.Equals(array[j].value, value.value))
				{
					return j;
				}
			}
		}
		return -1;
	}

	internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count)
	{
		int num = startIndex - count + 1;
		if (!value.HasValue)
		{
			for (int num2 = startIndex; num2 >= num; num2--)
			{
				if (!array[num2].HasValue)
				{
					return num2;
				}
			}
		}
		else
		{
			for (int num3 = startIndex; num3 >= num; num3--)
			{
				if (array[num3].HasValue && EqualityComparer<T>.Default.Equals(array[num3].value, value.value))
				{
					return num3;
				}
			}
		}
		return -1;
	}

	public NullableEqualityComparer()
	{
	}

	private NullableEqualityComparer(SerializationInfo info, StreamingContext context)
	{
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (!typeof(T).IsAssignableTo(typeof(IEquatable<T>)))
		{
			info.SetType(typeof(ObjectEqualityComparer<T?>));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(T? x, T? y)
	{
		if (x.HasValue)
		{
			if (y.HasValue)
			{
				return EqualityComparer<T>.Default.Equals(x.value, y.value);
			}
			return false;
		}
		if (y.HasValue)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode(T? obj)
	{
		return obj.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj != null)
		{
			return GetType() == obj.GetType();
		}
		return false;
	}

	public override int GetHashCode()
	{
		return GetType().GetHashCode();
	}
}
