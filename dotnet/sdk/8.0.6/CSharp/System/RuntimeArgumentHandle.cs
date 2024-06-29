namespace System;

public ref struct RuntimeArgumentHandle
{
	private nint m_ptr;

	internal nint Value => m_ptr;
}
