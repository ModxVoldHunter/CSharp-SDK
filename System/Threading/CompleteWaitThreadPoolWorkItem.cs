namespace System.Threading;

internal sealed class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
{
	private readonly RegisteredWaitHandle _registeredWaitHandle;

	private readonly bool _timedOut;

	void IThreadPoolWorkItem.Execute()
	{
		PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
	}

	public CompleteWaitThreadPoolWorkItem(RegisteredWaitHandle registeredWaitHandle, bool timedOut)
	{
		_registeredWaitHandle = registeredWaitHandle;
		_timedOut = timedOut;
	}
}
