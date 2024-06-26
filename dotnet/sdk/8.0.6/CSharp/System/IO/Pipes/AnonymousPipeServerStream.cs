using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public sealed class AnonymousPipeServerStream : PipeStream
{
	private SafePipeHandle _clientHandle;

	private bool _clientHandleExposed;

	private bool _clientHandleExposedAsString;

	private readonly HandleInheritability _inheritability;

	public SafePipeHandle ClientSafePipeHandle
	{
		get
		{
			_clientHandleExposed = true;
			return _clientHandle;
		}
	}

	public override PipeTransmissionMode TransmissionMode => PipeTransmissionMode.Byte;

	public override PipeTransmissionMode ReadMode
	{
		set
		{
			CheckPipePropertyOperations();
			switch (value)
			{
			default:
				throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
			case PipeTransmissionMode.Message:
				throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeMessagesNotSupported);
			case PipeTransmissionMode.Byte:
				break;
			}
		}
	}

	public AnonymousPipeServerStream()
		: this(PipeDirection.Out, HandleInheritability.None, 0)
	{
	}

	public AnonymousPipeServerStream(PipeDirection direction)
		: this(direction, HandleInheritability.None, 0)
	{
	}

	public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability)
		: this(direction, inheritability, 0)
	{
	}

	public AnonymousPipeServerStream(PipeDirection direction, SafePipeHandle serverSafePipeHandle, SafePipeHandle clientSafePipeHandle)
		: base(direction, 0)
	{
		if (direction == PipeDirection.InOut)
		{
			throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeUnidirectional);
		}
		ArgumentNullException.ThrowIfNull(serverSafePipeHandle, "serverSafePipeHandle");
		ArgumentNullException.ThrowIfNull(clientSafePipeHandle, "clientSafePipeHandle");
		if (serverSafePipeHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "serverSafePipeHandle");
		}
		if (clientSafePipeHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "clientSafePipeHandle");
		}
		PipeStream.ValidateHandleIsPipe(serverSafePipeHandle);
		PipeStream.ValidateHandleIsPipe(clientSafePipeHandle);
		InitializeHandle(serverSafePipeHandle, isExposed: true, isAsync: false);
		_clientHandle = clientSafePipeHandle;
		_clientHandleExposed = true;
		base.State = PipeState.Connected;
	}

	public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
		: base(direction, bufferSize)
	{
		if (direction == PipeDirection.InOut)
		{
			throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeUnidirectional);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability", System.SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
		}
		Create(direction, inheritability, bufferSize);
		_inheritability = inheritability;
	}

	~AnonymousPipeServerStream()
	{
		Dispose(disposing: false);
	}

	public string GetClientHandleAsString()
	{
		_clientHandleExposedAsString = (_clientHandleExposed = true);
		GC.SuppressFinalize(_clientHandle);
		return ((IntPtr)_clientHandle.DangerousGetHandle()).ToString();
	}

	public void DisposeLocalCopyOfClientHandle()
	{
		if (_clientHandle != null && !_clientHandle.IsClosed)
		{
			_clientHandle.Dispose();
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (!_clientHandleExposed || (_clientHandleExposedAsString && _inheritability == HandleInheritability.Inheritable))
			{
				DisposeLocalCopyOfClientHandle();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	internal AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability, int bufferSize, PipeSecurity pipeSecurity)
		: base(direction, bufferSize)
	{
		if (direction == PipeDirection.InOut)
		{
			throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeUnidirectional);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability", System.SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
		}
		Create(direction, inheritability, bufferSize, pipeSecurity);
	}

	private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
	{
		Create(direction, inheritability, bufferSize, null);
	}

	private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize, PipeSecurity pipeSecurity)
	{
		GCHandle pinningHandle = default(GCHandle);
		bool flag;
		SafePipeHandle hReadPipe;
		SafePipeHandle hWritePipe;
		try
		{
			global::Interop.Kernel32.SECURITY_ATTRIBUTES lpPipeAttributes = PipeStream.GetSecAttrs(inheritability, pipeSecurity, ref pinningHandle);
			flag = ((direction != PipeDirection.In) ? global::Interop.Kernel32.CreatePipe(out hReadPipe, out hWritePipe, ref lpPipeAttributes, bufferSize) : global::Interop.Kernel32.CreatePipe(out hWritePipe, out hReadPipe, ref lpPipeAttributes, bufferSize));
		}
		finally
		{
			if (pinningHandle.IsAllocated)
			{
				pinningHandle.Free();
			}
		}
		if (!flag)
		{
			Exception exceptionForLastWin32Error = System.IO.Win32Marshal.GetExceptionForLastWin32Error();
			hWritePipe.Dispose();
			hReadPipe.Dispose();
			throw exceptionForLastWin32Error;
		}
		if (!global::Interop.Kernel32.DuplicateHandle(global::Interop.Kernel32.GetCurrentProcess(), hWritePipe, global::Interop.Kernel32.GetCurrentProcess(), out var lpTargetHandle, 0u, bInheritHandle: false, 2u))
		{
			Exception exceptionForLastWin32Error2 = System.IO.Win32Marshal.GetExceptionForLastWin32Error();
			hWritePipe.Dispose();
			lpTargetHandle.Dispose();
			hReadPipe.Dispose();
			throw exceptionForLastWin32Error2;
		}
		hWritePipe.Dispose();
		_clientHandle = hReadPipe;
		InitializeHandle(lpTargetHandle, isExposed: false, isAsync: false);
		base.State = PipeState.Connected;
	}
}
