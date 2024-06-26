using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Strategies;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public static class RandomAccess
{
	private sealed class CallbackResetEvent : EventWaitHandle
	{
		private readonly ThreadPoolBoundHandle _threadPoolBoundHandle;

		private int _freeWhenZero = 2;

		internal CallbackResetEvent(ThreadPoolBoundHandle threadPoolBoundHandle)
			: base(initialState: false, EventResetMode.ManualReset)
		{
			_threadPoolBoundHandle = threadPoolBoundHandle;
		}

		internal unsafe void ReleaseRefCount(NativeOverlapped* pOverlapped)
		{
			if (Interlocked.Decrement(ref _freeWhenZero) == 0)
			{
				_threadPoolBoundHandle.FreeNativeOverlapped(pOverlapped);
			}
		}
	}

	private interface IMemoryHandler<T>
	{
		static abstract int GetLength(in T memory);

		static abstract MemoryHandle Pin(in T memory);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct MemoryHandler : IMemoryHandler<Memory<byte>>
	{
		public static int GetLength(in Memory<byte> memory)
		{
			return memory.Length;
		}

		public static MemoryHandle Pin(in Memory<byte> memory)
		{
			return memory.Pin();
		}

		static int IMemoryHandler<Memory<byte>>.GetLength(in Memory<byte> memory)
		{
			return GetLength(in memory);
		}

		static MemoryHandle IMemoryHandler<Memory<byte>>.Pin(in Memory<byte> memory)
		{
			return Pin(in memory);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct ReadOnlyMemoryHandler : IMemoryHandler<ReadOnlyMemory<byte>>
	{
		public static int GetLength(in ReadOnlyMemory<byte> memory)
		{
			return memory.Length;
		}

		public static MemoryHandle Pin(in ReadOnlyMemory<byte> memory)
		{
			return memory.Pin();
		}

		static int IMemoryHandler<ReadOnlyMemory<byte>>.GetLength(in ReadOnlyMemory<byte> memory)
		{
			return GetLength(in memory);
		}

		static MemoryHandle IMemoryHandler<ReadOnlyMemory<byte>>.Pin(in ReadOnlyMemory<byte> memory)
		{
			return Pin(in memory);
		}
	}

	private static readonly IOCompletionCallback s_callback = AllocateCallback();

	public static long GetLength(SafeFileHandle handle)
	{
		ValidateInput(handle, 0L);
		return handle.GetFileLength();
	}

	public static void SetLength(SafeFileHandle handle, long length)
	{
		ValidateInput(handle, 0L);
		if (length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("length");
		}
		SetFileLength(handle, length);
	}

	public static int Read(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
	{
		ValidateInput(handle, fileOffset);
		return ReadAtOffset(handle, buffer, fileOffset);
	}

	public static long Read(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
	{
		ValidateInput(handle, fileOffset);
		ValidateBuffers(buffers);
		return ReadScatterAtOffset(handle, buffers, fileOffset);
	}

	public static ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateInput(handle, fileOffset);
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		return ReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
	}

	public static ValueTask<long> ReadAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateInput(handle, fileOffset);
		ValidateBuffers(buffers);
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<long>(cancellationToken);
		}
		return ReadScatterAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
	}

	public static void Write(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
	{
		ValidateInput(handle, fileOffset);
		WriteAtOffset(handle, buffer, fileOffset);
	}

	public static void Write(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
	{
		ValidateInput(handle, fileOffset);
		ValidateBuffers(buffers);
		WriteGatherAtOffset(handle, buffers, fileOffset);
	}

	public static ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateInput(handle, fileOffset);
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		return WriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
	}

	public static ValueTask WriteAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateInput(handle, fileOffset);
		ValidateBuffers(buffers);
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		return WriteGatherAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
	}

	public static void FlushToDisk(SafeFileHandle handle)
	{
		ValidateInput(handle, 0L, allowUnseekableHandles: true);
		FileStreamHelpers.FlushToDisk(handle);
	}

	private static void ValidateInput(SafeFileHandle handle, long fileOffset, bool allowUnseekableHandles = false)
	{
		if (handle == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handle);
		}
		else if (handle.IsInvalid)
		{
			ThrowHelper.ThrowArgumentException_InvalidHandle("handle");
		}
		else if (!handle.CanSeek)
		{
			if (handle.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			if (!allowUnseekableHandles)
			{
				ThrowHelper.ThrowNotSupportedException_UnseekableStream();
			}
		}
		else if (fileOffset < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("fileOffset");
		}
	}

	private static void ValidateBuffers<T>(IReadOnlyList<T> buffers)
	{
		if (buffers == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffers);
		}
	}

	internal unsafe static void SetFileLength(SafeFileHandle handle, long length)
	{
		Interop.Kernel32.FILE_END_OF_FILE_INFO fILE_END_OF_FILE_INFO = default(Interop.Kernel32.FILE_END_OF_FILE_INFO);
		fILE_END_OF_FILE_INFO.EndOfFile = length;
		Interop.Kernel32.FILE_END_OF_FILE_INFO fILE_END_OF_FILE_INFO2 = fILE_END_OF_FILE_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(handle, 6, &fILE_END_OF_FILE_INFO2, (uint)sizeof(Interop.Kernel32.FILE_END_OF_FILE_INFO)))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			throw (lastPInvokeError == 87) ? new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_FileLengthTooBig) : Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, handle.Path);
		}
	}

	internal unsafe static int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
	{
		if (handle.IsAsync)
		{
			return ReadSyncUsingAsyncHandle(handle, buffer, fileOffset);
		}
		NativeOverlapped nativeOverlappedForSyncHandle = GetNativeOverlappedForSyncHandle(handle, fileOffset);
		fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
		{
			if (Interop.Kernel32.ReadFile(handle, bytes, buffer.Length, out var numBytesRead, &nativeOverlappedForSyncHandle) != 0)
			{
				return numBytesRead;
			}
			int lastWin32ErrorAndDisposeHandleIfInvalid = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
			int num = lastWin32ErrorAndDisposeHandleIfInvalid;
			if (num == 38)
			{
				return numBytesRead;
			}
			if (IsEndOfFile(lastWin32ErrorAndDisposeHandleIfInvalid, handle, fileOffset))
			{
				return 0;
			}
			throw Win32Marshal.GetExceptionForWin32Error(lastWin32ErrorAndDisposeHandleIfInvalid, handle.Path);
		}
	}

	private unsafe static int ReadSyncUsingAsyncHandle(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
	{
		handle.EnsureThreadPoolBindingInitialized();
		CallbackResetEvent callbackResetEvent = new CallbackResetEvent(handle.ThreadPoolBinding);
		NativeOverlapped* ptr = null;
		try
		{
			ptr = GetNativeOverlappedForAsyncHandle(handle, fileOffset, callbackResetEvent);
			fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
			{
				Interop.Kernel32.ReadFile(handle, bytes, buffer.Length, IntPtr.Zero, ptr);
				int num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (num == 997)
				{
					callbackResetEvent.WaitOne();
					num = 0;
				}
				if (num == 0)
				{
					int lpNumberOfBytesTransferred = 0;
					if (Interop.Kernel32.GetOverlappedResult(handle, ptr, ref lpNumberOfBytesTransferred, bWait: false))
					{
						return lpNumberOfBytesTransferred;
					}
					num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				}
				else
				{
					callbackResetEvent.ReleaseRefCount(ptr);
				}
				if (IsEndOfFile(num, handle, fileOffset))
				{
					ptr->InternalLow = IntPtr.Zero;
					return 0;
				}
				throw Win32Marshal.GetExceptionForWin32Error(num, handle.Path);
			}
		}
		finally
		{
			if (ptr != null)
			{
				callbackResetEvent.ReleaseRefCount(ptr);
			}
			callbackResetEvent.Dispose();
		}
	}

	internal unsafe static void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
	{
		if (buffer.IsEmpty)
		{
			return;
		}
		if (handle.IsAsync)
		{
			WriteSyncUsingAsyncHandle(handle, buffer, fileOffset);
			return;
		}
		NativeOverlapped nativeOverlappedForSyncHandle = GetNativeOverlappedForSyncHandle(handle, fileOffset);
		fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
		{
			if (Interop.Kernel32.WriteFile(handle, bytes, buffer.Length, out var _, &nativeOverlappedForSyncHandle) != 0)
			{
				return;
			}
			int lastWin32ErrorAndDisposeHandleIfInvalid = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
			throw Win32Marshal.GetExceptionForWin32Error(lastWin32ErrorAndDisposeHandleIfInvalid, handle.Path);
		}
	}

	private unsafe static void WriteSyncUsingAsyncHandle(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
	{
		if (buffer.IsEmpty)
		{
			return;
		}
		handle.EnsureThreadPoolBindingInitialized();
		CallbackResetEvent callbackResetEvent = new CallbackResetEvent(handle.ThreadPoolBinding);
		NativeOverlapped* ptr = null;
		try
		{
			ptr = GetNativeOverlappedForAsyncHandle(handle, fileOffset, callbackResetEvent);
			fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
			{
				Interop.Kernel32.WriteFile(handle, bytes, buffer.Length, IntPtr.Zero, ptr);
				int num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (num == 997)
				{
					callbackResetEvent.WaitOne();
					num = 0;
				}
				if (num == 0)
				{
					int lpNumberOfBytesTransferred = 0;
					if (Interop.Kernel32.GetOverlappedResult(handle, ptr, ref lpNumberOfBytesTransferred, bWait: false))
					{
						return;
					}
					num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				}
				else
				{
					callbackResetEvent.ReleaseRefCount(ptr);
				}
				if (num == 87)
				{
					throw new IOException(SR.IO_FileTooLong);
				}
				throw Win32Marshal.GetExceptionForWin32Error(num, handle.Path);
			}
		}
		finally
		{
			if (ptr != null)
			{
				callbackResetEvent.ReleaseRefCount(ptr);
			}
			callbackResetEvent.Dispose();
		}
	}

	internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy = null)
	{
		if (handle.IsAsync)
		{
			var (overlappedValueTaskSource, num) = QueueAsyncReadFile(handle, buffer, fileOffset, cancellationToken, strategy);
			if (overlappedValueTaskSource != null)
			{
				return new ValueTask<int>(overlappedValueTaskSource, overlappedValueTaskSource.Version);
			}
			if (num == 0)
			{
				return ValueTask.FromResult(0);
			}
			return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(num, handle.Path));
		}
		return AsyncOverSyncWithIoCancellation.InvokeAsync(delegate((SafeFileHandle handle, Memory<byte> buffer, long fileOffset, OSFileStreamStrategy strategy) state)
		{
			try
			{
				int num2 = ReadAtOffset(state.handle, state.buffer.Span, state.fileOffset);
				if (num2 != state.buffer.Length)
				{
					state.strategy?.OnIncompleteOperation(state.buffer.Length, num2);
				}
				return num2;
			}
			catch (Exception) when (state.strategy != null)
			{
				state.strategy.OnIncompleteOperation(state.buffer.Length, 0);
				throw;
			}
		}, (handle, buffer, fileOffset, strategy), cancellationToken);
	}

	private unsafe static (SafeFileHandle.OverlappedValueTaskSource vts, int errorCode) QueueAsyncReadFile(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy)
	{
		handle.EnsureThreadPoolBindingInitialized();
		SafeFileHandle.OverlappedValueTaskSource overlappedValueTaskSource = handle.GetOverlappedValueTaskSource();
		int num = 0;
		try
		{
			NativeOverlapped* ptr = overlappedValueTaskSource.PrepareForOperation(buffer, fileOffset, strategy);
			if (Interop.Kernel32.ReadFile(handle, (byte*)overlappedValueTaskSource._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, ptr) == 0)
			{
				num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (num != 997)
				{
					if (IsEndOfFile(num, handle, fileOffset))
					{
						ptr->InternalLow = IntPtr.Zero;
						overlappedValueTaskSource.Dispose();
						return (vts: null, errorCode: 0);
					}
					overlappedValueTaskSource.Dispose();
					return (vts: null, errorCode: num);
				}
				overlappedValueTaskSource.RegisterForCancellation(cancellationToken);
			}
		}
		catch
		{
			overlappedValueTaskSource.Dispose();
			throw;
		}
		finally
		{
			if (num != 997 && num != 0)
			{
				strategy?.OnIncompleteOperation(buffer.Length, 0);
			}
		}
		overlappedValueTaskSource.FinishedScheduling();
		return (vts: overlappedValueTaskSource, errorCode: -1);
	}

	internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy = null)
	{
		if (handle.IsAsync)
		{
			var (overlappedValueTaskSource, num) = QueueAsyncWriteFile(handle, buffer, fileOffset, cancellationToken, strategy);
			if (overlappedValueTaskSource != null)
			{
				return new ValueTask(overlappedValueTaskSource, overlappedValueTaskSource.Version);
			}
			if (num == 0)
			{
				return ValueTask.CompletedTask;
			}
			return ValueTask.FromException(Win32Marshal.GetExceptionForWin32Error(num, handle.Path));
		}
		return AsyncOverSyncWithIoCancellation.InvokeAsync(delegate((SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, OSFileStreamStrategy strategy) state)
		{
			try
			{
				WriteAtOffset(state.handle, state.buffer.Span, state.fileOffset);
			}
			catch (Exception) when (state.strategy != null)
			{
				state.strategy.OnIncompleteOperation(state.buffer.Length, 0);
				throw;
			}
		}, (handle, buffer, fileOffset, strategy), cancellationToken);
	}

	private unsafe static (SafeFileHandle.OverlappedValueTaskSource vts, int errorCode) QueueAsyncWriteFile(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy)
	{
		handle.EnsureThreadPoolBindingInitialized();
		SafeFileHandle.OverlappedValueTaskSource overlappedValueTaskSource = handle.GetOverlappedValueTaskSource();
		int num = 0;
		try
		{
			NativeOverlapped* lpOverlapped = overlappedValueTaskSource.PrepareForOperation(buffer, fileOffset, strategy);
			if (Interop.Kernel32.WriteFile(handle, (byte*)overlappedValueTaskSource._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, lpOverlapped) == 0)
			{
				num = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (num != 997)
				{
					overlappedValueTaskSource.Dispose();
					return (vts: null, errorCode: num);
				}
				overlappedValueTaskSource.RegisterForCancellation(cancellationToken);
			}
		}
		catch
		{
			overlappedValueTaskSource.Dispose();
			throw;
		}
		finally
		{
			if (num != 997 && num != 0)
			{
				strategy?.OnIncompleteOperation(buffer.Length, 0);
			}
		}
		overlappedValueTaskSource.FinishedScheduling();
		return (vts: overlappedValueTaskSource, errorCode: -1);
	}

	internal static long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
	{
		long num = 0L;
		int count = buffers.Count;
		for (int i = 0; i < count; i++)
		{
			Span<byte> span = buffers[i].Span;
			int num2 = ReadAtOffset(handle, span, fileOffset + num);
			num += num2;
			if (num2 != span.Length)
			{
				break;
			}
		}
		return num;
	}

	internal static void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
	{
		int num = 0;
		int count = buffers.Count;
		for (int i = 0; i < count; i++)
		{
			ReadOnlySpan<byte> span = buffers[i].Span;
			WriteAtOffset(handle, span, fileOffset + num);
			num += span.Length;
		}
	}

	private static bool CanUseScatterGatherWindowsAPIs(SafeFileHandle handle)
	{
		if (handle.IsAsync)
		{
			return (handle.GetFileOptions() & (FileOptions)536870912) != 0;
		}
		return false;
	}

	private unsafe static bool TryPrepareScatterGatherBuffers<T, THandler>(IReadOnlyList<T> buffers, [NotNullWhen(true)] out MemoryHandle[] handlesToDispose, out nint segmentsPtr, out int totalBytes) where T : struct where THandler : struct, IMemoryHandler<T>
	{
		int systemPageSize = Environment.SystemPageSize;
		long num = systemPageSize - 1;
		int count = buffers.Count;
		handlesToDispose = null;
		segmentsPtr = IntPtr.Zero;
		totalBytes = 0;
		long* ptr = null;
		bool flag = false;
		try
		{
			long num2 = 0L;
			for (int i = 0; i < count; i++)
			{
				T memory = buffers[i];
				int length = THandler.GetLength(in memory);
				num2 += length;
				if (length != systemPageSize || num2 > int.MaxValue)
				{
					return false;
				}
				MemoryHandle memoryHandle = THandler.Pin(in memory);
				long num3 = (long)memoryHandle.Pointer;
				if ((num3 & num) != 0L)
				{
					memoryHandle.Dispose();
					return false;
				}
				(handlesToDispose ?? (handlesToDispose = new MemoryHandle[count]))[i] = memoryHandle;
				if (ptr == null)
				{
					ptr = (long*)NativeMemory.Alloc((nuint)count + (nuint)1u, 8u);
					ptr[count] = 0L;
				}
				ptr[i] = num3;
			}
			segmentsPtr = (nint)ptr;
			totalBytes = (int)num2;
			flag = true;
			return handlesToDispose != null;
		}
		finally
		{
			if (!flag)
			{
				CleanupScatterGatherBuffers(handlesToDispose, (nint)ptr);
			}
		}
	}

	private unsafe static void CleanupScatterGatherBuffers(MemoryHandle[] handlesToDispose, nint segmentsPtr)
	{
		if (handlesToDispose != null)
		{
			foreach (MemoryHandle memoryHandle in handlesToDispose)
			{
				memoryHandle.Dispose();
			}
		}
		if (segmentsPtr != IntPtr.Zero)
		{
			NativeMemory.Free((void*)segmentsPtr);
		}
	}

	private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
	{
		if (!handle.IsAsync)
		{
			return AsyncOverSyncWithIoCancellation.InvokeAsync(((SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset) state) => ReadScatterAtOffset(state.handle, state.buffers, state.fileOffset), (handle, buffers, fileOffset), cancellationToken);
		}
		if (CanUseScatterGatherWindowsAPIs(handle) && TryPrepareScatterGatherBuffers<Memory<byte>, MemoryHandler>(buffers, out var handlesToDispose, out var segmentsPtr, out var totalBytes))
		{
			return ReadScatterAtOffsetSingleSyscallAsync(handle, handlesToDispose, segmentsPtr, fileOffset, totalBytes, cancellationToken);
		}
		return ReadScatterAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
	}

	private static async ValueTask<long> ReadScatterAtOffsetSingleSyscallAsync(SafeFileHandle handle, MemoryHandle[] handlesToDispose, nint segmentsPtr, long fileOffset, int totalBytes, CancellationToken cancellationToken)
	{
		try
		{
			return await ReadFileScatterAsync(handle, segmentsPtr, totalBytes, fileOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			CleanupScatterGatherBuffers(handlesToDispose, segmentsPtr);
		}
	}

	private unsafe static ValueTask<int> ReadFileScatterAsync(SafeFileHandle handle, nint segmentsPtr, int bytesToRead, long fileOffset, CancellationToken cancellationToken)
	{
		handle.EnsureThreadPoolBindingInitialized();
		SafeFileHandle.OverlappedValueTaskSource overlappedValueTaskSource = handle.GetOverlappedValueTaskSource();
		try
		{
			NativeOverlapped* ptr = overlappedValueTaskSource.PrepareForOperation(Memory<byte>.Empty, fileOffset);
			if (Interop.Kernel32.ReadFileScatter(handle, (long*)segmentsPtr, bytesToRead, IntPtr.Zero, ptr) == 0)
			{
				int lastWin32ErrorAndDisposeHandleIfInvalid = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (lastWin32ErrorAndDisposeHandleIfInvalid != 997)
				{
					if (IsEndOfFile(lastWin32ErrorAndDisposeHandleIfInvalid, handle, fileOffset))
					{
						ptr->InternalLow = IntPtr.Zero;
						overlappedValueTaskSource.Dispose();
						return ValueTask.FromResult(0);
					}
					overlappedValueTaskSource.Dispose();
					return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(lastWin32ErrorAndDisposeHandleIfInvalid, handle.Path));
				}
				overlappedValueTaskSource.RegisterForCancellation(cancellationToken);
			}
		}
		catch
		{
			overlappedValueTaskSource.Dispose();
			throw;
		}
		overlappedValueTaskSource.FinishedScheduling();
		return new ValueTask<int>(overlappedValueTaskSource, overlappedValueTaskSource.Version);
	}

	private static async ValueTask<long> ReadScatterAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
	{
		long total = 0L;
		int buffersCount = buffers.Count;
		for (int i = 0; i < buffersCount; i++)
		{
			Memory<byte> buffer = buffers[i];
			int num = await ReadAtOffsetAsync(handle, buffer, fileOffset + total, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			total += num;
			if (num != buffer.Length)
			{
				break;
			}
		}
		return total;
	}

	private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
	{
		if (!handle.IsAsync)
		{
			return AsyncOverSyncWithIoCancellation.InvokeAsync(delegate((SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset) state)
			{
				WriteGatherAtOffset(state.handle, state.buffers, state.fileOffset);
			}, (handle, buffers, fileOffset), cancellationToken);
		}
		if (CanUseScatterGatherWindowsAPIs(handle) && TryPrepareScatterGatherBuffers<ReadOnlyMemory<byte>, ReadOnlyMemoryHandler>(buffers, out var handlesToDispose, out var segmentsPtr, out var totalBytes))
		{
			return WriteGatherAtOffsetSingleSyscallAsync(handle, handlesToDispose, segmentsPtr, fileOffset, totalBytes, cancellationToken);
		}
		return WriteGatherAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
	}

	private static async ValueTask WriteGatherAtOffsetSingleSyscallAsync(SafeFileHandle handle, MemoryHandle[] handlesToDispose, nint segmentsPtr, long fileOffset, int totalBytes, CancellationToken cancellationToken)
	{
		try
		{
			await WriteFileGatherAsync(handle, segmentsPtr, totalBytes, fileOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			CleanupScatterGatherBuffers(handlesToDispose, segmentsPtr);
		}
	}

	private unsafe static ValueTask WriteFileGatherAsync(SafeFileHandle handle, nint segmentsPtr, int bytesToWrite, long fileOffset, CancellationToken cancellationToken)
	{
		handle.EnsureThreadPoolBindingInitialized();
		SafeFileHandle.OverlappedValueTaskSource overlappedValueTaskSource = handle.GetOverlappedValueTaskSource();
		try
		{
			NativeOverlapped* lpOverlapped = overlappedValueTaskSource.PrepareForOperation(ReadOnlyMemory<byte>.Empty, fileOffset);
			if (Interop.Kernel32.WriteFileGather(handle, (long*)segmentsPtr, bytesToWrite, IntPtr.Zero, lpOverlapped) == 0)
			{
				int lastWin32ErrorAndDisposeHandleIfInvalid = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
				if (lastWin32ErrorAndDisposeHandleIfInvalid != 997)
				{
					overlappedValueTaskSource.Dispose();
					return ValueTask.FromException(SafeFileHandle.OverlappedValueTaskSource.GetIOError(lastWin32ErrorAndDisposeHandleIfInvalid, null));
				}
				overlappedValueTaskSource.RegisterForCancellation(cancellationToken);
			}
		}
		catch
		{
			overlappedValueTaskSource.Dispose();
			throw;
		}
		overlappedValueTaskSource.FinishedScheduling();
		return new ValueTask(overlappedValueTaskSource, overlappedValueTaskSource.Version);
	}

	private static async ValueTask WriteGatherAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
	{
		int buffersCount = buffers.Count;
		for (int i = 0; i < buffersCount; i++)
		{
			ReadOnlyMemory<byte> rom = buffers[i];
			await WriteAtOffsetAsync(handle, rom, fileOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			fileOffset += rom.Length;
		}
	}

	private unsafe static NativeOverlapped* GetNativeOverlappedForAsyncHandle(SafeFileHandle handle, long fileOffset, CallbackResetEvent resetEvent)
	{
		NativeOverlapped* ptr = handle.ThreadPoolBinding.UnsafeAllocateNativeOverlapped(s_callback, resetEvent, null);
		if (handle.CanSeek)
		{
			ptr->OffsetLow = (int)fileOffset;
			ptr->OffsetHigh = (int)(fileOffset >> 32);
		}
		ptr->EventHandle = resetEvent.SafeWaitHandle.DangerousGetHandle();
		return ptr;
	}

	private static NativeOverlapped GetNativeOverlappedForSyncHandle(SafeFileHandle handle, long fileOffset)
	{
		NativeOverlapped result = default(NativeOverlapped);
		if (handle.CanSeek)
		{
			result.OffsetLow = (int)fileOffset;
			result.OffsetHigh = (int)(fileOffset >> 32);
		}
		return result;
	}

	private unsafe static IOCompletionCallback AllocateCallback()
	{
		return Callback;
		unsafe static void Callback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			CallbackResetEvent callbackResetEvent = (CallbackResetEvent)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
			callbackResetEvent.ReleaseRefCount(pOverlapped);
		}
	}

	internal static bool IsEndOfFile(int errorCode, SafeFileHandle handle, long fileOffset)
	{
		if (errorCode <= 87)
		{
			if (errorCode == 38 || (errorCode == 87 && IsEndOfFileForNoBuffering(handle, fileOffset)))
			{
				goto IL_002b;
			}
		}
		else if (errorCode == 109 || errorCode == 233)
		{
			goto IL_002b;
		}
		return false;
		IL_002b:
		return true;
	}

	private static bool IsEndOfFileForNoBuffering(SafeFileHandle fileHandle, long fileOffset)
	{
		if (fileHandle.IsNoBuffering && fileHandle.CanSeek)
		{
			return fileOffset >= fileHandle.GetFileLength();
		}
		return false;
	}
}
