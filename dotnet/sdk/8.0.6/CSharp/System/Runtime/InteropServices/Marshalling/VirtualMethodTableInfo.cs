namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public readonly struct VirtualMethodTableInfo
{
	public unsafe void* ThisPointer { get; }

	public unsafe void** VirtualMethodTable { get; }

	public unsafe VirtualMethodTableInfo(void* thisPointer, void** virtualMethodTable)
	{
		ThisPointer = thisPointer;
		VirtualMethodTable = virtualMethodTable;
	}

	public unsafe void Deconstruct(out void* thisPointer, out void** virtualMethodTable)
	{
		thisPointer = ThisPointer;
		virtualMethodTable = VirtualMethodTable;
	}
}
