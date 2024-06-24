using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[NonVersionable]
internal readonly ref struct ByReference
{
	public readonly ref byte Value;

	public ByReference(ref byte value)
	{
		Value = ref value;
	}

	public static ByReference Create<T>(ref T p)
	{
		return new ByReference(ref Unsafe.As<T, byte>(ref p));
	}
}
