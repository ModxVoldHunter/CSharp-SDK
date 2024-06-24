using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal abstract class FileStreamStrategy : Stream
{
	internal abstract bool IsAsync { get; }

	internal bool IsDerived { get; init; }

	internal abstract string Name { get; }

	internal abstract SafeFileHandle SafeFileHandle { get; }

	internal nint Handle => SafeFileHandle.DangerousGetHandle();

	internal abstract bool IsClosed { get; }

	internal abstract void Lock(long position, long length);

	internal abstract void Unlock(long position, long length);

	internal abstract void Flush(bool flushToDisk);

	internal void DisposeInternal(bool disposing)
	{
		Dispose(disposing);
	}
}
