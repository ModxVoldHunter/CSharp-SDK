using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

[StructLayout(LayoutKind.Sequential, Size = 1)]
internal readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
{
	public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		return stream.ReadAsync(buffer, cancellationToken);
	}

	public static ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken)
	{
		return stream.ReadAtLeastAsync(buffer, minimumBytes, throwOnEndOfStream, cancellationToken);
	}

	public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		return stream.WriteAsync(buffer, cancellationToken);
	}

	public static Task FlushAsync(Stream stream, CancellationToken cancellationToken)
	{
		return stream.FlushAsync(cancellationToken);
	}

	public static Task WaitAsync(TaskCompletionSource<bool> waiter)
	{
		return waiter.Task;
	}
}
