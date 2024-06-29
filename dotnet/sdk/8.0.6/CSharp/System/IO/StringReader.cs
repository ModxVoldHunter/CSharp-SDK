using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StringReader : TextReader
{
	private string _s;

	private int _pos;

	public StringReader(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		_s = s;
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		_s = null;
		_pos = 0;
		base.Dispose(disposing);
	}

	public override int Peek()
	{
		string s = _s;
		if (s == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int pos = _pos;
		if ((uint)pos < (uint)s.Length)
		{
			return s[pos];
		}
		return -1;
	}

	public override int Read()
	{
		string s = _s;
		if (s == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int pos = _pos;
		if ((uint)pos < (uint)s.Length)
		{
			_pos++;
			return s[pos];
		}
		return -1;
	}

	public override int Read(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (_s == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int num = _s.Length - _pos;
		if (num > 0)
		{
			if (num > count)
			{
				num = count;
			}
			_s.CopyTo(_pos, buffer, index, num);
			_pos += num;
		}
		return num;
	}

	public override int Read(Span<char> buffer)
	{
		if (GetType() != typeof(StringReader))
		{
			return base.Read(buffer);
		}
		string s = _s;
		if (s == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int num = s.Length - _pos;
		if (num > 0)
		{
			if (num > buffer.Length)
			{
				num = buffer.Length;
			}
			s.AsSpan(_pos, num).CopyTo(buffer);
			_pos += num;
		}
		return num;
	}

	public override int ReadBlock(Span<char> buffer)
	{
		return Read(buffer);
	}

	public override string ReadToEnd()
	{
		string text = _s;
		if (text == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int pos = _pos;
		_pos = text.Length;
		if (pos != 0)
		{
			text = text.Substring(pos);
		}
		return text;
	}

	public override string? ReadLine()
	{
		string s = _s;
		if (s == null)
		{
			ThrowObjectDisposedException_ReaderClosed();
		}
		int pos = _pos;
		if ((uint)pos >= (uint)s.Length)
		{
			return null;
		}
		ReadOnlySpan<char> span = s.AsSpan(pos);
		int num = span.IndexOfAny('\r', '\n');
		if (num >= 0)
		{
			string result = s.Substring(pos, num);
			char c = span[num];
			pos += num + 1;
			if (c == '\r' && (uint)pos < (uint)s.Length && s[pos] == '\n')
			{
				pos++;
			}
			_pos = pos;
			return result;
		}
		string result2 = s.Substring(pos);
		_pos = s.Length;
		return result2;
	}

	public override Task<string?> ReadLineAsync()
	{
		return Task.FromResult(ReadLine());
	}

	public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
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
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(ReadBlock(buffer, index, count));
	}

	public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(ReadBlock(buffer.Span));
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}

	public override Task<int> ReadAsync(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return Task.FromResult(Read(buffer, index, count));
	}

	public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(Read(buffer.Span));
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}

	[DoesNotReturn]
	private static void ThrowObjectDisposedException_ReaderClosed()
	{
		throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
	}
}
