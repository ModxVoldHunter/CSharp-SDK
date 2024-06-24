using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class X86Serialize : X86Base
{
	[Intrinsic]
	public new abstract class X64 : X86Base.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static void Serialize()
	{
		Serialize();
	}
}
