namespace System.Runtime.CompilerServices;

internal struct TypeHandle
{
	private unsafe readonly void* m_asTAddr;

	public unsafe bool IsNull => m_asTAddr == null;

	public unsafe bool IsTypeDesc
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ((nuint)m_asTAddr & (nuint)2u) != 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe TypeHandle(void* tAddr)
	{
		m_asTAddr = tAddr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe MethodTable* AsMethodTable()
	{
		return (MethodTable*)m_asTAddr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static TypeHandle TypeHandleOf<T>()
	{
		return new TypeHandle((void*)RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle));
	}
}
