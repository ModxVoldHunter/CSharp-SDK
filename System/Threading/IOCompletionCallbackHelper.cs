namespace System.Threading;

internal sealed class IOCompletionCallbackHelper
{
	private readonly IOCompletionCallback _ioCompletionCallback;

	private readonly ExecutionContext _executionContext;

	private uint _errorCode;

	private uint _numBytes;

	private unsafe NativeOverlapped* _pNativeOverlapped;

	private static readonly ContextCallback IOCompletionCallback_Context_Delegate = IOCompletionCallback_Context;

	public IOCompletionCallbackHelper(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
	{
		_ioCompletionCallback = ioCompletionCallback;
		_executionContext = executionContext;
	}

	private unsafe static void IOCompletionCallback_Context(object state)
	{
		IOCompletionCallbackHelper iOCompletionCallbackHelper = (IOCompletionCallbackHelper)state;
		iOCompletionCallbackHelper._ioCompletionCallback(iOCompletionCallbackHelper._errorCode, iOCompletionCallbackHelper._numBytes, iOCompletionCallbackHelper._pNativeOverlapped);
	}

	public unsafe static void PerformSingleIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pNativeOverlapped)
	{
		Overlapped overlappedFromNative = Overlapped.GetOverlappedFromNative(pNativeOverlapped);
		object callback = overlappedFromNative._callback;
		if (callback is IOCompletionCallback iOCompletionCallback)
		{
			iOCompletionCallback(errorCode, numBytes, pNativeOverlapped);
		}
		else if (callback != null)
		{
			IOCompletionCallbackHelper iOCompletionCallbackHelper = (IOCompletionCallbackHelper)callback;
			iOCompletionCallbackHelper._errorCode = errorCode;
			iOCompletionCallbackHelper._numBytes = numBytes;
			iOCompletionCallbackHelper._pNativeOverlapped = pNativeOverlapped;
			ExecutionContext.RunInternal(iOCompletionCallbackHelper._executionContext, IOCompletionCallback_Context_Delegate, iOCompletionCallbackHelper);
		}
	}
}
