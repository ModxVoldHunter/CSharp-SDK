using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class ValueType
{
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "Trimmed fields don't make a difference for equality")]
	public unsafe override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (GetType() != obj.GetType())
		{
			return false;
		}
		if (CanCompareBits(this))
		{
			return SpanHelpers.SequenceEqual(ref this.GetRawData(), ref obj.GetRawData(), RuntimeHelpers.GetMethodTable(this)->GetNumInstanceFieldBytes());
		}
		FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		for (int i = 0; i < fields.Length; i++)
		{
			object value = fields[i].GetValue(this);
			object value2 = fields[i].GetValue(obj);
			if (value == null)
			{
				if (value2 != null)
				{
					return false;
				}
			}
			else if (!value.Equals(value2))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool CanCompareBits(object obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public override extern int GetHashCode();

	public override string? ToString()
	{
		return GetType().ToString();
	}
}
