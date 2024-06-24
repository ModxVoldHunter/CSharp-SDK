using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

internal interface IReadWriteAdapter
{
	static abstract ValueTask<int> ReadAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);

	static abstract ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken);

	static abstract ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

	static abstract Task FlushAsync(Stream stream, CancellationToken cancellationToken);

	static abstract Task WaitAsync(TaskCompletionSource<bool> waiter);
}
