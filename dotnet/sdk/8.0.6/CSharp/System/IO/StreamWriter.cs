using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StreamWriter : TextWriter
{
	public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, UTF8NoBOM, 128, leaveOpen: true);

	private readonly Stream _stream;

	private readonly Encoding _encoding;

	private readonly Encoder _encoder;

	private byte[] _byteBuffer;

	private readonly char[] _charBuffer;

	private int _charPos;

	private int _charLen;

	private bool _autoFlush;

	private bool _haveWrittenPreamble;

	private readonly bool _closable;

	private bool _disposed;

	private Task _asyncWriteTask = Task.CompletedTask;

	private static Encoding UTF8NoBOM => EncodingCache.UTF8NoBOM;

	public virtual bool AutoFlush
	{
		get
		{
			return _autoFlush;
		}
		set
		{
			CheckAsyncTaskInProgress();
			_autoFlush = value;
			if (value)
			{
				Flush(flushStream: true, flushEncoder: false);
			}
		}
	}

	public virtual Stream BaseStream => _stream;

	public override Encoding Encoding => _encoding;

	private void CheckAsyncTaskInProgress()
	{
		if (!_asyncWriteTask.IsCompleted)
		{
			ThrowAsyncIOInProgress();
		}
	}

	[DoesNotReturn]
	private static void ThrowAsyncIOInProgress()
	{
		throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);
	}

	public StreamWriter(Stream stream)
		: this(stream, UTF8NoBOM, 1024, leaveOpen: false)
	{
	}

	public StreamWriter(Stream stream, Encoding encoding)
		: this(stream, encoding, 1024, leaveOpen: false)
	{
	}

	public StreamWriter(Stream stream, Encoding encoding, int bufferSize)
		: this(stream, encoding, bufferSize, leaveOpen: false)
	{
	}

	public StreamWriter(Stream stream, Encoding? encoding = null, int bufferSize = -1, bool leaveOpen = false)
		: base(null)
	{
		if (stream == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream);
		}
		if (!stream.CanWrite)
		{
			throw new ArgumentException(SR.Argument_StreamNotWritable);
		}
		if (bufferSize == -1)
		{
			bufferSize = 1024;
		}
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize, "bufferSize");
		_stream = stream;
		_encoding = encoding ?? UTF8NoBOM;
		_encoder = _encoding.GetEncoder();
		if (bufferSize < 128)
		{
			bufferSize = 128;
		}
		_charBuffer = new char[bufferSize];
		_charLen = bufferSize;
		if (_stream.CanSeek && _stream.Position > 0)
		{
			_haveWrittenPreamble = true;
		}
		_closable = !leaveOpen;
	}

	public StreamWriter(string path)
		: this(path, append: false, UTF8NoBOM, 1024)
	{
	}

	public StreamWriter(string path, bool append)
		: this(path, append, UTF8NoBOM, 1024)
	{
	}

	public StreamWriter(string path, bool append, Encoding encoding)
		: this(path, append, encoding, 1024)
	{
	}

	public StreamWriter(string path, bool append, Encoding encoding, int bufferSize)
		: this(ValidateArgsAndOpenPath(path, append, encoding, bufferSize), encoding, bufferSize, leaveOpen: false)
	{
	}

	public StreamWriter(string path, FileStreamOptions options)
		: this(path, UTF8NoBOM, options)
	{
	}

	public StreamWriter(string path, Encoding encoding, FileStreamOptions options)
		: this(ValidateArgsAndOpenPath(path, encoding, options), encoding, 4096)
	{
	}

	private static FileStream ValidateArgsAndOpenPath(string path, Encoding encoding, FileStreamOptions options)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
		ArgumentNullException.ThrowIfNull(options, "options");
		if ((options.Access & FileAccess.Write) == 0)
		{
			throw new ArgumentException(SR.Argument_StreamNotWritable, "options");
		}
		return new FileStream(path, options);
	}

	private static FileStream ValidateArgsAndOpenPath(string path, bool append, Encoding encoding, int bufferSize)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize, "bufferSize");
		return new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 4096);
	}

	public override void Close()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (!_disposed && disposing)
			{
				CheckAsyncTaskInProgress();
				Flush(flushStream: true, flushEncoder: true);
			}
		}
		finally
		{
			CloseStreamFromDispose(disposing);
		}
	}

	private void CloseStreamFromDispose(bool disposing)
	{
		if (!_closable || _disposed)
		{
			return;
		}
		try
		{
			if (disposing)
			{
				_stream.Close();
			}
		}
		finally
		{
			_disposed = true;
			_charLen = 0;
			base.Dispose(disposing);
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (!(GetType() != typeof(StreamWriter)))
		{
			return DisposeAsyncCore();
		}
		return base.DisposeAsync();
	}

	private async ValueTask DisposeAsyncCore()
	{
		try
		{
			if (!_disposed)
			{
				await FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			CloseStreamFromDispose(disposing: true);
		}
		GC.SuppressFinalize(this);
	}

	public override void Flush()
	{
		CheckAsyncTaskInProgress();
		Flush(flushStream: true, flushEncoder: true);
	}

	private void Flush(bool flushStream, bool flushEncoder)
	{
		ThrowIfDisposed();
		if (_charPos == 0 && !flushStream && !flushEncoder)
		{
			return;
		}
		if (!_haveWrittenPreamble)
		{
			_haveWrittenPreamble = true;
			ReadOnlySpan<byte> preamble = _encoding.Preamble;
			if (preamble.Length > 0)
			{
				_stream.Write(preamble);
			}
		}
		Span<byte> bytes;
		if (_byteBuffer != null)
		{
			bytes = _byteBuffer;
		}
		else
		{
			int maxByteCount = _encoding.GetMaxByteCount(_charPos);
			Span<byte> span = ((maxByteCount > 1024) ? ((Span<byte>)(_byteBuffer = new byte[_encoding.GetMaxByteCount(_charBuffer.Length)])) : stackalloc byte[1024]);
			bytes = span;
		}
		int bytes2 = _encoder.GetBytes(new ReadOnlySpan<char>(_charBuffer, 0, _charPos), bytes, flushEncoder);
		_charPos = 0;
		if (bytes2 > 0)
		{
			_stream.Write(bytes.Slice(0, bytes2));
		}
		if (flushStream)
		{
			_stream.Flush();
		}
	}

	public override void Write(char value)
	{
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen)
		{
			Flush(flushStream: false, flushEncoder: false);
		}
		_charBuffer[_charPos] = value;
		_charPos++;
		if (_autoFlush)
		{
			Flush(flushStream: true, flushEncoder: false);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Write(char[]? buffer)
	{
		WriteSpan(buffer, appendNewLine: false);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Write(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		WriteSpan(buffer.AsSpan(index, count), appendNewLine: false);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Write(ReadOnlySpan<char> buffer)
	{
		if (GetType() == typeof(StreamWriter))
		{
			WriteSpan(buffer, appendNewLine: false);
		}
		else
		{
			base.Write(buffer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void WriteSpan(ReadOnlySpan<char> buffer, bool appendNewLine)
	{
		CheckAsyncTaskInProgress();
		if (buffer.Length <= 4 && buffer.Length <= _charLen - _charPos)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				_charBuffer[_charPos++] = buffer[i];
			}
		}
		else
		{
			ThrowIfDisposed();
			char[] charBuffer = _charBuffer;
			fixed (char* ptr = &MemoryMarshal.GetReference(buffer))
			{
				fixed (char* ptr3 = &charBuffer[0])
				{
					char* ptr2 = ptr;
					int num = buffer.Length;
					int num2 = _charPos;
					while (num > 0)
					{
						if (num2 == charBuffer.Length)
						{
							Flush(flushStream: false, flushEncoder: false);
							num2 = 0;
						}
						int num3 = Math.Min(charBuffer.Length - num2, num);
						int num4 = num3 * 2;
						Buffer.MemoryCopy(ptr2, ptr3 + num2, num4, num4);
						_charPos += num3;
						num2 += num3;
						ptr2 += num3;
						num -= num3;
					}
				}
			}
		}
		if (appendNewLine)
		{
			char[] coreNewLine = CoreNewLine;
			for (int j = 0; j < coreNewLine.Length; j++)
			{
				if (_charPos == _charLen)
				{
					Flush(flushStream: false, flushEncoder: false);
				}
				_charBuffer[_charPos] = coreNewLine[j];
				_charPos++;
			}
		}
		if (_autoFlush)
		{
			Flush(flushStream: true, flushEncoder: false);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Write(string? value)
	{
		WriteSpan(value, appendNewLine: false);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void WriteLine(string? value)
	{
		CheckAsyncTaskInProgress();
		WriteSpan(value, appendNewLine: true);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void WriteLine(ReadOnlySpan<char> buffer)
	{
		if (GetType() == typeof(StreamWriter))
		{
			CheckAsyncTaskInProgress();
			WriteSpan(buffer, appendNewLine: true);
		}
		else
		{
			base.WriteLine(buffer);
		}
	}

	private void WriteFormatHelper(string format, ReadOnlySpan<object> args, bool appendNewLine)
	{
		int num = (format?.Length ?? 0) + args.Length * 8;
		ValueStringBuilder valueStringBuilder;
		if (num <= 256)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new ValueStringBuilder(num);
		}
		ValueStringBuilder valueStringBuilder2 = valueStringBuilder;
		valueStringBuilder2.AppendFormatHelper(null, format, args);
		WriteSpan(valueStringBuilder2.AsSpan(), appendNewLine);
		valueStringBuilder2.Dispose();
	}

	public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0)
	{
		if (GetType() == typeof(StreamWriter))
		{
			WriteFormatHelper(format, new ReadOnlySpan<object>(ref arg0), appendNewLine: false);
		}
		else
		{
			base.Write(format, arg0);
		}
	}

	public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
	{
		if (GetType() == typeof(StreamWriter))
		{
			TwoObjects twoObjects = new TwoObjects(arg0, arg1);
			WriteFormatHelper(format, MemoryMarshal.CreateReadOnlySpan(ref twoObjects.Arg0, 2), appendNewLine: false);
		}
		else
		{
			base.Write(format, arg0, arg1);
		}
	}

	public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
	{
		if (GetType() == typeof(StreamWriter))
		{
			ThreeObjects threeObjects = new ThreeObjects(arg0, arg1, arg2);
			WriteFormatHelper(format, MemoryMarshal.CreateReadOnlySpan(ref threeObjects.Arg0, 3), appendNewLine: false);
		}
		else
		{
			base.Write(format, arg0, arg1, arg2);
		}
	}

	public override void Write([StringSyntax("CompositeFormat")] string format, params object?[] arg)
	{
		if (GetType() == typeof(StreamWriter))
		{
			if (arg == null)
			{
				ArgumentNullException.Throw((format == null) ? "format" : "arg");
			}
			WriteFormatHelper(format, arg, appendNewLine: false);
		}
		else
		{
			base.Write(format, arg);
		}
	}

	public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0)
	{
		if (GetType() == typeof(StreamWriter))
		{
			WriteFormatHelper(format, new ReadOnlySpan<object>(ref arg0), appendNewLine: true);
		}
		else
		{
			base.WriteLine(format, arg0);
		}
	}

	public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
	{
		if (GetType() == typeof(StreamWriter))
		{
			TwoObjects twoObjects = new TwoObjects(arg0, arg1);
			WriteFormatHelper(format, MemoryMarshal.CreateReadOnlySpan(ref twoObjects.Arg0, 2), appendNewLine: true);
		}
		else
		{
			base.WriteLine(format, arg0, arg1);
		}
	}

	public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
	{
		if (GetType() == typeof(StreamWriter))
		{
			ThreeObjects threeObjects = new ThreeObjects(arg0, arg1, arg2);
			WriteFormatHelper(format, MemoryMarshal.CreateReadOnlySpan(ref threeObjects.Arg0, 3), appendNewLine: true);
		}
		else
		{
			base.WriteLine(format, arg0, arg1, arg2);
		}
	}

	public override void WriteLine([StringSyntax("CompositeFormat")] string format, params object?[] arg)
	{
		if (GetType() == typeof(StreamWriter))
		{
			ArgumentNullException.ThrowIfNull(arg, "arg");
			WriteFormatHelper(format, arg, appendNewLine: true);
		}
		else
		{
			base.WriteLine(format, arg);
		}
	}

	public override Task WriteAsync(char value)
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteAsync(value);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(value, appendNewLine: false);
	}

	private async Task WriteAsyncInternal(char value, bool appendNewLine)
	{
		if (_charPos == _charLen)
		{
			await FlushAsyncInternal(flushStream: false, flushEncoder: false).ConfigureAwait(continueOnCapturedContext: false);
		}
		_charBuffer[_charPos++] = value;
		if (appendNewLine)
		{
			for (int i = 0; i < CoreNewLine.Length; i++)
			{
				if (_charPos == _charLen)
				{
					await FlushAsyncInternal(flushStream: false, flushEncoder: false).ConfigureAwait(continueOnCapturedContext: false);
				}
				_charBuffer[_charPos++] = CoreNewLine[i];
			}
		}
		if (_autoFlush)
		{
			await FlushAsyncInternal(flushStream: true, flushEncoder: false).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override Task WriteAsync(string? value)
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteAsync(value);
		}
		if (value != null)
		{
			ThrowIfDisposed();
			CheckAsyncTaskInProgress();
			return _asyncWriteTask = WriteAsyncInternal(value.AsMemory(), appendNewLine: false, default(CancellationToken));
		}
		return Task.CompletedTask;
	}

	public override Task WriteAsync(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(new ReadOnlyMemory<char>(buffer, index, count), appendNewLine: false, default(CancellationToken));
	}

	public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return _asyncWriteTask = WriteAsyncInternal(buffer, appendNewLine: false, cancellationToken);
	}

	private async Task WriteAsyncInternal(ReadOnlyMemory<char> source, bool appendNewLine, CancellationToken cancellationToken)
	{
		int num;
		for (int copied = 0; copied < source.Length; copied += num)
		{
			if (_charPos == _charLen)
			{
				await FlushAsyncInternal(flushStream: false, flushEncoder: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			num = Math.Min(_charLen - _charPos, source.Length - copied);
			ReadOnlySpan<char> readOnlySpan = source.Span;
			readOnlySpan = readOnlySpan.Slice(copied, num);
			readOnlySpan.CopyTo(new Span<char>(_charBuffer, _charPos, num));
			_charPos += num;
		}
		if (appendNewLine)
		{
			for (int i = 0; i < CoreNewLine.Length; i++)
			{
				if (_charPos == _charLen)
				{
					await FlushAsyncInternal(flushStream: false, flushEncoder: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				_charBuffer[_charPos++] = CoreNewLine[i];
			}
		}
		if (_autoFlush)
		{
			await FlushAsyncInternal(flushStream: true, flushEncoder: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override Task WriteLineAsync()
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteLineAsync();
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(ReadOnlyMemory<char>.Empty, appendNewLine: true, default(CancellationToken));
	}

	public override Task WriteLineAsync(char value)
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteLineAsync(value);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(value, appendNewLine: true);
	}

	public override Task WriteLineAsync(string? value)
	{
		if (value == null)
		{
			return WriteLineAsync();
		}
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteLineAsync(value);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(value.AsMemory(), appendNewLine: true, default(CancellationToken));
	}

	public override Task WriteLineAsync(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteLineAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = WriteAsyncInternal(new ReadOnlyMemory<char>(buffer, index, count), appendNewLine: true, default(CancellationToken));
	}

	public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.WriteLineAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return _asyncWriteTask = WriteAsyncInternal(buffer, appendNewLine: true, cancellationToken);
	}

	public override Task FlushAsync()
	{
		if (GetType() != typeof(StreamWriter))
		{
			return base.FlushAsync();
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = FlushAsyncInternal(flushStream: true, flushEncoder: true, CancellationToken.None);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (GetType() != typeof(StreamWriter))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(cancellationToken);
			}
			return FlushAsync();
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return _asyncWriteTask = FlushAsyncInternal(flushStream: true, flushEncoder: true, cancellationToken);
	}

	private Task FlushAsyncInternal(bool flushStream, bool flushEncoder, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (_charPos == 0 && !flushStream && !flushEncoder)
		{
			return Task.CompletedTask;
		}
		return Core(flushStream, flushEncoder, cancellationToken);
		async Task Core(bool flushStream, bool flushEncoder, CancellationToken cancellationToken)
		{
			if (!_haveWrittenPreamble)
			{
				_haveWrittenPreamble = true;
				byte[] preamble = _encoding.GetPreamble();
				if (preamble.Length != 0)
				{
					await _stream.WriteAsync(new ReadOnlyMemory<byte>(preamble), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			byte[] array = _byteBuffer ?? (_byteBuffer = new byte[_encoding.GetMaxByteCount(_charBuffer.Length)]);
			int bytes = _encoder.GetBytes(new ReadOnlySpan<char>(_charBuffer, 0, _charPos), array, flushEncoder);
			_charPos = 0;
			if (bytes > 0)
			{
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(array, 0, bytes), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (flushStream)
			{
				await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowObjectDisposedException();
		}
		void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_WriterClosed);
		}
	}
}
