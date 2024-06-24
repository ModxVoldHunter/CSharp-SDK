namespace System.Runtime.InteropServices;

public readonly struct HandleRef
{
	private readonly object _wrapper;

	private readonly nint _handle;

	public object? Wrapper => _wrapper;

	public nint Handle => _handle;

	public HandleRef(object? wrapper, nint handle)
	{
		_wrapper = wrapper;
		_handle = handle;
	}

	public static explicit operator nint(HandleRef value)
	{
		return value._handle;
	}

	public static nint ToIntPtr(HandleRef value)
	{
		return value._handle;
	}
}
