using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

internal sealed class SyncTextReader : TextReader
{
	internal readonly TextReader _in;

	public static SyncTextReader GetSynchronizedTextReader(TextReader reader)
	{
		return (reader as SyncTextReader) ?? new SyncTextReader(reader);
	}

	internal SyncTextReader(TextReader t)
	{
		_in = t;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			lock (this)
			{
				_in.Dispose();
			}
		}
	}

	public override int Peek()
	{
		lock (this)
		{
			return _in.Peek();
		}
	}

	public override int Read()
	{
		lock (this)
		{
			return _in.Read();
		}
	}

	public override int Read(char[] buffer, int index, int count)
	{
		lock (this)
		{
			return _in.Read(buffer, index, count);
		}
	}

	public override int ReadBlock(char[] buffer, int index, int count)
	{
		lock (this)
		{
			return _in.ReadBlock(buffer, index, count);
		}
	}

	public override string ReadLine()
	{
		lock (this)
		{
			return _in.ReadLine();
		}
	}

	public override string ReadToEnd()
	{
		lock (this)
		{
			return _in.ReadToEnd();
		}
	}

	public override Task<string> ReadLineAsync()
	{
		return Task.FromResult(ReadLine());
	}

	public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<string>(ReadLine());
		}
		return ValueTask.FromCanceled<string>(cancellationToken);
	}

	public override Task<string> ReadToEndAsync()
	{
		return Task.FromResult(ReadToEnd());
	}

	public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return Task.FromResult(ReadToEnd());
		}
		return Task.FromCanceled<string>(cancellationToken);
	}

	public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(ReadBlock(buffer, index, count));
	}

	public override Task<int> ReadAsync(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(Read(buffer, index, count));
	}
}
