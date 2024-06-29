namespace System.Threading;

internal interface IThreadPoolTypedWorkItemQueueCallback<T>
{
	static abstract void Invoke(T item);
}
