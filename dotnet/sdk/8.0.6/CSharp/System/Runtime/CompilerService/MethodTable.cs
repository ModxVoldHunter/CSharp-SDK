using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Explicit)]
internal struct MethodTable
{
	[FieldOffset(0)]
	public ushort ComponentSize;

	[FieldOffset(0)]
	private uint Flags;

	[FieldOffset(4)]
	public uint BaseSize;

	[FieldOffset(14)]
	public ushort InterfaceCount;

	[FieldOffset(16)]
	public unsafe MethodTable* ParentMethodTable;

	[FieldOffset(48)]
	public unsafe void* ElementType;

	[FieldOffset(56)]
	public unsafe MethodTable** InterfaceMap;

	public bool HasComponentSize => (Flags & 0x80000000u) != 0;

	public bool ContainsGCPointers => (Flags & 0x1000000) != 0;

	public bool NonTrivialInterfaceCast => (Flags & 0x406C0000) != 0;

	public bool HasTypeEquivalence => (Flags & 0x2000000) != 0;

	public bool HasDefaultConstructor => (Flags & 0x80000200u) == 512;

	public bool IsMultiDimensionalArray
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return BaseSize > 3 * 8;
		}
	}

	public int MultiDimensionalArrayRank
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (int)((BaseSize - 3 * 8) / 8);
		}
	}

	public bool IsValueType => (Flags & 0xC0000) == 262144;

	public bool IsNullable => (Flags & 0xF0000) == 327680;

	public bool HasInstantiation
	{
		get
		{
			if ((Flags & 0x80000000u) == 0)
			{
				return (Flags & 0x30) != 0;
			}
			return false;
		}
	}

	public bool IsGenericTypeDefinition => (Flags & 0x80000030u) == 48;

	public bool IsConstructedGenericType
	{
		get
		{
			uint num = Flags & 0x80000030u;
			if (num != 16)
			{
				return num == 32;
			}
			return true;
		}
	}

	internal unsafe static bool AreSameType(MethodTable* mt1, MethodTable* mt2)
	{
		return mt1 == mt2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe TypeHandle GetArrayElementTypeHandle()
	{
		return new TypeHandle(ElementType);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern uint GetNumInstanceFieldBytes();
}
