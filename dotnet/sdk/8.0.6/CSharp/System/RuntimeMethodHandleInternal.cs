namespace System;

internal struct RuntimeMethodHandleInternal
{
	internal nint m_handle;

	internal static RuntimeMethodHandleInternal EmptyHandle => default(RuntimeMethodHandleInternal);

	internal nint Value => m_handle;

	internal bool IsNullHandle()
	{
		return m_handle == IntPtr.Zero;
	}

	internal RuntimeMethodHandleInternal(nint value)
	{
		m_handle = value;
	}
}
