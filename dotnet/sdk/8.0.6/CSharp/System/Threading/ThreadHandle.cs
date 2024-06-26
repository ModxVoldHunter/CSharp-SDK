namespace System.Threading;

internal readonly struct ThreadHandle
{
	private readonly nint _ptr;

	internal ThreadHandle(nint pThread)
	{
		_ptr = pThread;
	}
}
