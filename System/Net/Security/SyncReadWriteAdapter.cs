using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

[StructLayout(LayoutKind.Sequential, Size = 1)]
internal readonly struct SyncReadWriteAdapter : IReadWriteAdapter
{
	public static ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		return new ValueTask<int>(stream.Read(buffer.Span));
	}

	public static ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken)
	{
		return new ValueTask<int>(stream.ReadAtLeast(buffer.Span, minimumBytes, throwOnEndOfStream));
	}

	public static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		stream.Write(buffer.Span);
		return default(ValueTask);
	}

	public static Task FlushAsync(Stream stream, CancellationToken cancellationToken)
	{
		stream.Flush();
		return Task.CompletedTask;
	}

	public static Task WaitAsync(TaskCompletionSource<bool> waiter)
	{
		waiter.Task.GetAwaiter().GetResult();
		return Task.CompletedTask;
	}
}
