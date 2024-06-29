namespace System.Runtime.CompilerServices;

internal struct PortableTailCallFrame
{
	public nint TailCallAwareReturnAddress;

	public unsafe delegate*<nint, ref byte, PortableTailCallFrame*, void> NextCall;
}
