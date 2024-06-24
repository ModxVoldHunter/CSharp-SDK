using System.Runtime.InteropServices;

namespace System.Threading;

internal struct Win32ThreadPoolNativeOverlapped
{
	private sealed class ExecutionContextCallbackArgs
	{
		internal uint _errorCode;

		internal uint _bytesWritten;

		internal unsafe Win32ThreadPoolNativeOverlapped* _overlapped;

		internal OverlappedData _data;
	}

	internal sealed class OverlappedData
	{
		internal GCHandle[] _pinnedData;

		internal IOCompletionCallback _callback;

		internal object _state;

		internal ExecutionContext _executionContext;

		internal ThreadPoolBoundHandle _boundHandle;

		internal PreAllocatedOverlapped _preAllocated;

		internal bool _completed;

		internal void Reset()
		{
			if (_pinnedData != null)
			{
				for (int i = 0; i < _pinnedData.Length; i++)
				{
					if (_pinnedData[i].IsAllocated && _pinnedData[i].Target != null)
					{
						_pinnedData[i].Target = null;
					}
				}
			}
			_callback = null;
			_state = null;
			_executionContext = null;
			_completed = false;
			_preAllocated = null;
		}
	}

	[ThreadStatic]
	private static ExecutionContextCallbackArgs t_executionContextCallbackArgs;

	private static ContextCallback s_executionContextCallback;

	private static OverlappedData[] s_dataArray;

	private static int s_dataCount;

	private static nint s_freeList;

	private NativeOverlapped _overlapped;

	private nint _nextFree;

	private int _dataIndex;

	internal OverlappedData Data => s_dataArray[_dataIndex];

	internal unsafe static Win32ThreadPoolNativeOverlapped* Allocate(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionControl)
	{
		Win32ThreadPoolNativeOverlapped* ptr = AllocateNew();
		try
		{
			ptr->SetData(callback, state, pinData, preAllocated, flowExecutionControl);
			return ptr;
		}
		catch
		{
			Free(ptr);
			throw;
		}
	}

	private unsafe static Win32ThreadPoolNativeOverlapped* AllocateNew()
	{
		nint num;
		Win32ThreadPoolNativeOverlapped* ptr;
		while ((num = Volatile.Read(ref s_freeList)) != IntPtr.Zero)
		{
			ptr = (Win32ThreadPoolNativeOverlapped*)num;
			if (Interlocked.CompareExchange(ref s_freeList, ptr->_nextFree, num) == num)
			{
				ptr->_nextFree = IntPtr.Zero;
				return ptr;
			}
		}
		ptr = (Win32ThreadPoolNativeOverlapped*)NativeMemory.Alloc((nuint)sizeof(Win32ThreadPoolNativeOverlapped));
		*ptr = default(Win32ThreadPoolNativeOverlapped);
		OverlappedData value = new OverlappedData();
		int num2 = Interlocked.Increment(ref s_dataCount) - 1;
		if (num2 < 0)
		{
			Environment.FailFast("Too many outstanding Win32ThreadPoolNativeOverlapped instances");
		}
		while (true)
		{
			OverlappedData[] array = Volatile.Read(ref s_dataArray);
			int num3 = ((array != null) ? array.Length : 0);
			if (num3 <= num2)
			{
				int num4 = num3;
				if (num4 == 0)
				{
					num4 = 128;
				}
				while (num4 <= num2)
				{
					num4 = num4 * 3 / 2;
				}
				OverlappedData[] array2 = array;
				Array.Resize(ref array2, num4);
				if (Interlocked.CompareExchange(ref s_dataArray, array2, array) != array)
				{
					continue;
				}
				array = array2;
			}
			if (s_dataArray[num2] != null)
			{
				break;
			}
			Interlocked.Exchange(ref array[num2], value);
		}
		ptr->_dataIndex = num2;
		return ptr;
	}

	private void SetData(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionContext)
	{
		OverlappedData data = Data;
		data._callback = callback;
		data._state = state;
		data._executionContext = (flowExecutionContext ? ExecutionContext.Capture() : null);
		data._preAllocated = preAllocated;
		if (pinData == null)
		{
			return;
		}
		if (pinData is object[] array && array.GetType() == typeof(object[]))
		{
			if (data._pinnedData == null || data._pinnedData.Length < array.Length)
			{
				Array.Resize(ref data._pinnedData, array.Length);
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (!data._pinnedData[i].IsAllocated)
				{
					data._pinnedData[i] = GCHandle.Alloc(array[i], GCHandleType.Pinned);
				}
				else
				{
					data._pinnedData[i].Target = array[i];
				}
			}
		}
		else
		{
			OverlappedData overlappedData = data;
			if (overlappedData._pinnedData == null)
			{
				overlappedData._pinnedData = new GCHandle[1];
			}
			if (!data._pinnedData[0].IsAllocated)
			{
				data._pinnedData[0] = GCHandle.Alloc(pinData, GCHandleType.Pinned);
			}
			else
			{
				data._pinnedData[0].Target = pinData;
			}
		}
	}

	internal unsafe static void Free(Win32ThreadPoolNativeOverlapped* overlapped)
	{
		overlapped->Data.Reset();
		overlapped->_overlapped = default(NativeOverlapped);
		nint num;
		do
		{
			num = (overlapped->_nextFree = Volatile.Read(ref s_freeList));
		}
		while (Interlocked.CompareExchange(ref s_freeList, (nint)overlapped, num) != num);
	}

	internal unsafe static NativeOverlapped* ToNativeOverlapped(Win32ThreadPoolNativeOverlapped* overlapped)
	{
		return (NativeOverlapped*)overlapped;
	}

	internal unsafe static Win32ThreadPoolNativeOverlapped* FromNativeOverlapped(NativeOverlapped* overlapped)
	{
		return (Win32ThreadPoolNativeOverlapped*)overlapped;
	}

	internal unsafe static void CompleteWithCallback(uint errorCode, uint bytesWritten, Win32ThreadPoolNativeOverlapped* overlapped)
	{
		OverlappedData data = overlapped->Data;
		data._completed = true;
		if (data._executionContext == null)
		{
			data._callback(errorCode, bytesWritten, ToNativeOverlapped(overlapped));
			return;
		}
		ContextCallback callback = OnExecutionContextCallback;
		ExecutionContextCallbackArgs executionContextCallbackArgs = t_executionContextCallbackArgs;
		if (executionContextCallbackArgs == null)
		{
			executionContextCallbackArgs = new ExecutionContextCallbackArgs();
		}
		t_executionContextCallbackArgs = null;
		executionContextCallbackArgs._errorCode = errorCode;
		executionContextCallbackArgs._bytesWritten = bytesWritten;
		executionContextCallbackArgs._overlapped = overlapped;
		executionContextCallbackArgs._data = data;
		ExecutionContext.Run(data._executionContext, callback, executionContextCallbackArgs);
	}

	private unsafe static void OnExecutionContextCallback(object state)
	{
		ExecutionContextCallbackArgs executionContextCallbackArgs = (ExecutionContextCallbackArgs)state;
		uint errorCode = executionContextCallbackArgs._errorCode;
		uint bytesWritten = executionContextCallbackArgs._bytesWritten;
		Win32ThreadPoolNativeOverlapped* overlapped = executionContextCallbackArgs._overlapped;
		OverlappedData data = executionContextCallbackArgs._data;
		executionContextCallbackArgs._data = null;
		t_executionContextCallbackArgs = executionContextCallbackArgs;
		data._callback(errorCode, bytesWritten, ToNativeOverlapped(overlapped));
	}
}
