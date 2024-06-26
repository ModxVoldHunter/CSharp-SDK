namespace System.Threading;

internal struct ThreadPoolCallbackWrapper
{
	private Thread _currentThread;

	public static ThreadPoolCallbackWrapper Enter()
	{
		Thread currentThread = Thread.CurrentThread;
		if (!currentThread.IsThreadPoolThread)
		{
			currentThread.IsThreadPoolThread = true;
			ThreadPool.InitializeForThreadPoolThread();
		}
		ThreadPoolCallbackWrapper result = default(ThreadPoolCallbackWrapper);
		result._currentThread = currentThread;
		return result;
	}

	public void Exit(bool resetThread = true)
	{
		if (resetThread)
		{
			_currentThread.ResetThreadPoolThread();
		}
	}
}
