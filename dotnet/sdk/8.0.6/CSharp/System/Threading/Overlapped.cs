using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace System.Threading;

public class Overlapped
{
	private IAsyncResult _asyncResult;

	internal object _callback;

	private unsafe NativeOverlapped* _pNativeOverlapped;

	private nint _eventHandle;

	private int _offsetLow;

	private int _offsetHigh;

	public IAsyncResult AsyncResult
	{
		get
		{
			return _asyncResult;
		}
		set
		{
			_asyncResult = value;
		}
	}

	public unsafe int OffsetLow
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return _offsetLow;
			}
			return _pNativeOverlapped->OffsetLow;
		}
		set
		{
			//IL_001d->IL001d: Incompatible stack types: I vs Ref
			_pNativeOverlapped == null ? ref _offsetLow : ref _pNativeOverlapped->OffsetLow = value;
		}
	}

	public unsafe int OffsetHigh
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return _offsetHigh;
			}
			return _pNativeOverlapped->OffsetHigh;
		}
		set
		{
			//IL_001d->IL001d: Incompatible stack types: I vs Ref
			_pNativeOverlapped == null ? ref _offsetHigh : ref _pNativeOverlapped->OffsetHigh = value;
		}
	}

	[Obsolete("Overlapped.EventHandle is not 64-bit compatible and has been deprecated. Use EventHandleIntPtr instead.")]
	public int EventHandle
	{
		get
		{
			return ((IntPtr)EventHandleIntPtr).ToInt32();
		}
		set
		{
			EventHandleIntPtr = new IntPtr(value);
		}
	}

	public unsafe nint EventHandleIntPtr
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return _eventHandle;
			}
			return _pNativeOverlapped->EventHandle;
		}
		set
		{
			//IL_001d->IL001d: Incompatible stack types: I vs Ref
			_pNativeOverlapped == null ? ref _eventHandle : ref *(IntPtr*)(&_pNativeOverlapped->EventHandle) = value;
		}
	}

	public Overlapped()
	{
	}

	public Overlapped(int offsetLo, int offsetHi, nint hEvent, IAsyncResult? ar)
	{
		_offsetLow = offsetLo;
		_offsetHigh = offsetHi;
		_eventHandle = hEvent;
		_asyncResult = ar;
	}

	[Obsolete("This constructor is not 64-bit compatible and has been deprecated. Use the constructor that accepts an IntPtr for the event handle instead.")]
	public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult? ar)
		: this(offsetLo, offsetHi, new IntPtr(hEvent), ar)
	{
	}

	[Obsolete("This overload is not safe and has been deprecated. Use Pack(IOCompletionCallback?, object?) instead.")]
	[CLSCompliant(false)]
	public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb)
	{
		return Pack(iocb, null);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
	{
		if (_pNativeOverlapped != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
		}
		if (iocb != null)
		{
			ExecutionContext executionContext = ExecutionContext.Capture();
			_callback = ((executionContext != null && !executionContext.IsDefault) ? ((object)new IOCompletionCallbackHelper(iocb, executionContext)) : ((object)iocb));
		}
		else
		{
			_callback = null;
		}
		return AllocateNativeOverlapped(userData);
	}

	[Obsolete("This overload is not safe and has been deprecated. Use UnsafePack(IOCompletionCallback?, object?) instead.")]
	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb)
	{
		return UnsafePack(iocb, null);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
	{
		if (_pNativeOverlapped != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
		}
		_callback = iocb;
		return AllocateNativeOverlapped(userData);
	}

	[CLSCompliant(false)]
	public unsafe static Overlapped Unpack(NativeOverlapped* nativeOverlappedPtr)
	{
		ArgumentNullException.ThrowIfNull(nativeOverlappedPtr, "nativeOverlappedPtr");
		return GetOverlappedFromNative(nativeOverlappedPtr);
	}

	[CLSCompliant(false)]
	public unsafe static void Free(NativeOverlapped* nativeOverlappedPtr)
	{
		ArgumentNullException.ThrowIfNull(nativeOverlappedPtr, "nativeOverlappedPtr");
		GetOverlappedFromNative(nativeOverlappedPtr)._pNativeOverlapped = null;
		FreeNativeOverlapped(nativeOverlappedPtr);
	}

	private unsafe NativeOverlapped* AllocateNativeOverlapped(object userData)
	{
		NativeOverlapped* ptr = null;
		try
		{
			nuint num = 1u;
			if (userData != null)
			{
				num = ((!(userData.GetType() == typeof(object[]))) ? (num + 1) : (num + (nuint)((object[])userData).Length));
			}
			ptr = (NativeOverlapped*)NativeMemory.Alloc((nuint)(sizeof(NativeOverlapped) + 8 + (nint)num * (nint)sizeof(GCHandle)));
			GCHandleCountRef(ptr) = 0u;
			ptr->InternalLow = 0;
			ptr->InternalHigh = 0;
			ptr->OffsetLow = _offsetLow;
			ptr->OffsetHigh = _offsetHigh;
			ptr->EventHandle = _eventHandle;
			GCHandleRef(ptr, 0u) = GCHandle.Alloc(this);
			GCHandleCountRef(ptr)++;
			if (userData != null)
			{
				if (userData.GetType() == typeof(object[]))
				{
					object[] array = (object[])userData;
					for (int i = 0; i < array.Length; i++)
					{
						GCHandleRef(ptr, (nuint)(i + 1)) = GCHandle.Alloc(array[i], GCHandleType.Pinned);
						GCHandleCountRef(ptr)++;
					}
				}
				else
				{
					GCHandleRef(ptr, 1u) = GCHandle.Alloc(userData, GCHandleType.Pinned);
					GCHandleCountRef(ptr)++;
				}
			}
			_pNativeOverlapped = ptr;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolIOPack(ptr);
			}
			NativeOverlapped* result = ptr;
			ptr = null;
			return result;
		}
		finally
		{
			if (ptr != null)
			{
				_pNativeOverlapped = null;
				FreeNativeOverlapped(ptr);
			}
		}
	}

	internal unsafe static void FreeNativeOverlapped(NativeOverlapped* pNativeOverlapped)
	{
		nuint num = GCHandleCountRef(pNativeOverlapped);
		for (nuint num2 = 0u; num2 < num; num2++)
		{
			GCHandleRef(pNativeOverlapped, num2).Free();
		}
		NativeMemory.Free(pNativeOverlapped);
	}

	private unsafe static ref nuint GCHandleCountRef(NativeOverlapped* pNativeOverlapped)
	{
		return ref *(nuint*)(pNativeOverlapped + 1);
	}

	private unsafe static ref GCHandle GCHandleRef(NativeOverlapped* pNativeOverlapped, nuint index)
	{
		return ref *(GCHandle*)((byte*)(pNativeOverlapped + 1) + sizeof(nuint) + (long)index * (long)sizeof(GCHandle));
	}

	internal unsafe static Overlapped GetOverlappedFromNative(NativeOverlapped* pNativeOverlapped)
	{
		object target = GCHandleRef(pNativeOverlapped, 0u).Target;
		return (Overlapped)target;
	}
}
