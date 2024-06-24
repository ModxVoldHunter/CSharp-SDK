namespace System;

internal struct RuntimeFieldHandleInternal
{
	internal nint m_handle;

	internal nint Value => m_handle;

	internal RuntimeFieldHandleInternal(nint value)
	{
		m_handle = value;
	}
}
