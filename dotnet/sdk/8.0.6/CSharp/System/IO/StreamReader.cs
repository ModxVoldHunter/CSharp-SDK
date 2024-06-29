using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StreamReader : TextReader
{
	internal sealed class NullStreamReader : StreamReader
	{
		public override Encoding CurrentEncoding => Encoding.Unicode;

		protected override void Dispose(bool disposing)
		{
		}

		public override int Peek()
		{
			return -1;
		}

		public override int Read()
		{
			return -1;
		}

		public override int Read(char[] buffer, int index, int count)
		{
			return 0;
		}

		public override int Read(Span<char> buffer)
		{
			return 0;
		}

		public override Task<int> ReadAsync(char[] buffer, int index, int count)
		{
			return Task.FromResult(0);
		}

		public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask<int>);
			}
			return ValueTask.FromCanceled<int>(cancellationToken);
		}

		public override int ReadBlock(char[] buffer, int index, int count)
		{
			return 0;
		}

		public override int ReadBlock(Span<char> buffer)
		{
			return 0;
		}

		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
		{
			return Task.FromResult(0);
		}

		public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask<int>);
			}
			return ValueTask.FromCanceled<int>(cancellationToken);
		}

		public override string ReadLine()
		{
			return null;
		}

		public override Task<string> ReadLineAsync()
		{
			return Task.FromResult<string>(null);
		}

		public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask<string>);
			}
			return ValueTask.FromCanceled<string>(cancellationToken);
		}

		public override string ReadToEnd()
		{
			return "";
		}

		public override Task<string> ReadToEndAsync()
		{
			return Task.FromResult("");
		}

		public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.FromResult("");
			}
			return Task.FromCanceled<string>(cancellationToken);
		}

		internal override ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask<int>);
			}
			return ValueTask.FromCanceled<int>(cancellationToken);
		}

		internal override int ReadBuffer()
		{
			return 0;
		}
	}

	public new static readonly StreamReader Null = new NullStreamReader();

	private readonly Stream _stream;

	private Encoding _encoding;

	private Decoder _decoder;

	private readonly byte[] _byteBuffer;

	private char[] _charBuffer;

	private int _charPos;

	private int _charLen;

	private int _byteLen;

	private int _bytePos;

	private int _maxCharsPerBuffer;

	private bool _disposed;

	private bool _detectEncoding;

	private bool _checkPreamble;

	private bool _isBlocked;

	private readonly bool _closable;

	private Task _asyncReadTask = Task.CompletedTask;

	public virtual Encoding CurrentEncoding => _encoding;

	public virtual Stream BaseStream => _stream;

	public bool EndOfStream
	{
		get
		{
			ThrowIfDisposed();
			CheckAsyncTaskInProgress();
			if (_charPos < _charLen)
			{
				return false;
			}
			int num = ReadBuffer();
			return num == 0;
		}
	}

	private void CheckAsyncTaskInProgress()
	{
		if (!_asyncReadTask.IsCompleted)
		{
			ThrowAsyncIOInProgress();
		}
	}

	[DoesNotReturn]
	private static void ThrowAsyncIOInProgress()
	{
		throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);
	}

	private StreamReader()
	{
		_stream = Stream.Null;
		_closable = true;
	}

	public StreamReader(Stream stream)
		: this(stream, detectEncodingFromByteOrderMarks: true)
	{
	}

	public StreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
		: this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding)
		: this(stream, encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
		: this(stream, encoding, detectEncodingFromByteOrderMarks, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
		: this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
	{
		if (stream == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream);
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException(SR.Argument_StreamNotReadable);
		}
		if (bufferSize == -1)
		{
			bufferSize = 1024;
		}
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize, "bufferSize");
		_stream = stream;
		_encoding = encoding ?? (encoding = Encoding.UTF8);
		_decoder = encoding.GetDecoder();
		if (bufferSize < 128)
		{
			bufferSize = 128;
		}
		_byteBuffer = new byte[bufferSize];
		_maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
		_charBuffer = new char[_maxCharsPerBuffer];
		_detectEncoding = detectEncodingFromByteOrderMarks;
		int length = encoding.Preamble.Length;
		_checkPreamble = length > 0 && length <= bufferSize;
		_closable = !leaveOpen;
	}

	public StreamReader(string path)
		: this(path, detectEncodingFromByteOrderMarks: true)
	{
	}

	public StreamReader(string path, bool detectEncodingFromByteOrderMarks)
		: this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding)
		: this(path, encoding, detectEncodingFromByteOrderMarks: true, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
		: this(path, encoding, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
		: this(ValidateArgsAndOpenPath(path, encoding, bufferSize), encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false)
	{
	}

	public StreamReader(string path, FileStreamOptions options)
		: this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, options)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
		: this(ValidateArgsAndOpenPath(path, encoding, options), encoding, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	private static FileStream ValidateArgsAndOpenPath(string path, Encoding encoding, FileStreamOptions options)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
		ArgumentNullException.ThrowIfNull(options, "options");
		if ((options.Access & FileAccess.Read) == 0)
		{
			throw new ArgumentException(SR.Argument_StreamNotReadable, "options");
		}
		return new FileStream(path, options);
	}

	private static FileStream ValidateArgsAndOpenPath(string path, Encoding encoding, int bufferSize)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize, "bufferSize");
		return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (!_closable)
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
			_charPos = 0;
			_charLen = 0;
			base.Dispose(disposing);
		}
	}

	public void DiscardBufferedData()
	{
		CheckAsyncTaskInProgress();
		_byteLen = 0;
		_charLen = 0;
		_charPos = 0;
		if (_encoding != null)
		{
			_decoder = _encoding.GetDecoder();
		}
		_isBlocked = false;
	}

	public override int Peek()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && ReadBuffer() == 0)
		{
			return -1;
		}
		return _charBuffer[_charPos];
	}

	public override int Read()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && ReadBuffer() == 0)
		{
			return -1;
		}
		int result = _charBuffer[_charPos];
		_charPos++;
		return result;
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
		return ReadSpan(new Span<char>(buffer, index, count));
	}

	public override int Read(Span<char> buffer)
	{
		if (!(GetType() == typeof(StreamReader)))
		{
			return base.Read(buffer);
		}
		return ReadSpan(buffer);
	}

	private int ReadSpan(Span<char> buffer)
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		int num = 0;
		bool readToUserBuffer = false;
		int num2 = buffer.Length;
		while (num2 > 0)
		{
			int num3 = _charLen - _charPos;
			if (num3 == 0)
			{
				num3 = ReadBuffer(buffer.Slice(num), out readToUserBuffer);
			}
			if (num3 == 0)
			{
				break;
			}
			if (num3 > num2)
			{
				num3 = num2;
			}
			if (!readToUserBuffer)
			{
				new Span<char>(_charBuffer, _charPos, num3).CopyTo(buffer.Slice(num));
				_charPos += num3;
			}
			num += num3;
			num2 -= num3;
			if (_isBlocked)
			{
				break;
			}
		}
		return num;
	}

	public override string ReadToEnd()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		StringBuilder stringBuilder = new StringBuilder(_charLen - _charPos);
		do
		{
			stringBuilder.Append(_charBuffer, _charPos, _charLen - _charPos);
			_charPos = _charLen;
			ReadBuffer();
		}
		while (_charLen > 0);
		return stringBuilder.ToString();
	}

	public override int ReadBlock(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return base.ReadBlock(buffer, index, count);
	}

	public override int ReadBlock(Span<char> buffer)
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlock(buffer);
		}
		int num = 0;
		int num2;
		do
		{
			num2 = ReadSpan(buffer.Slice(num));
			num += num2;
		}
		while (num2 > 0 && num < buffer.Length);
		return num;
	}

	private void CompressBuffer(int n)
	{
		byte[] byteBuffer = _byteBuffer;
		_ = byteBuffer.Length;
		new ReadOnlySpan<byte>(byteBuffer, n, _byteLen - n).CopyTo(byteBuffer);
		_byteLen -= n;
	}

	private void DetectEncoding()
	{
		byte[] byteBuffer = _byteBuffer;
		_detectEncoding = false;
		bool flag = false;
		ushort num = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer);
		switch (num)
		{
		case 65534:
			_encoding = Encoding.BigEndianUnicode;
			CompressBuffer(2);
			flag = true;
			break;
		case 65279:
			if (_byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
			{
				_encoding = Encoding.Unicode;
				CompressBuffer(2);
				flag = true;
			}
			else
			{
				_encoding = Encoding.UTF32;
				CompressBuffer(4);
				flag = true;
			}
			break;
		default:
			if (_byteLen >= 3 && num == 48111 && byteBuffer[2] == 191)
			{
				_encoding = Encoding.UTF8;
				CompressBuffer(3);
				flag = true;
			}
			else if (_byteLen >= 4 && num == 0 && byteBuffer[2] == 254 && byteBuffer[3] == byte.MaxValue)
			{
				_encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
				CompressBuffer(4);
				flag = true;
			}
			else if (_byteLen == 2)
			{
				_detectEncoding = true;
			}
			break;
		}
		if (flag)
		{
			_decoder = _encoding.GetDecoder();
			int maxCharCount = _encoding.GetMaxCharCount(byteBuffer.Length);
			if (maxCharCount > _maxCharsPerBuffer)
			{
				_charBuffer = new char[maxCharCount];
			}
			_maxCharsPerBuffer = maxCharCount;
		}
	}

	private bool IsPreamble()
	{
		if (!_checkPreamble)
		{
			return false;
		}
		return IsPreambleWorker();
		bool IsPreambleWorker()
		{
			ReadOnlySpan<byte> preamble = _encoding.Preamble;
			int num = Math.Min(_byteLen, preamble.Length);
			for (int i = _bytePos; i < num; i++)
			{
				if (_byteBuffer[i] != preamble[i])
				{
					_bytePos = 0;
					_checkPreamble = false;
					return false;
				}
			}
			_bytePos = num;
			if (_bytePos == preamble.Length)
			{
				CompressBuffer(preamble.Length);
				_bytePos = 0;
				_checkPreamble = false;
				_detectEncoding = false;
			}
			return _checkPreamble;
		}
	}

	internal virtual int ReadBuffer()
	{
		_charLen = 0;
		_charPos = 0;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		bool flag = false;
		do
		{
			if (_checkPreamble)
			{
				int num = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
				if (num == 0)
				{
					flag = true;
					break;
				}
				_byteLen += num;
			}
			else
			{
				_byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
				if (_byteLen == 0)
				{
					flag = true;
					break;
				}
			}
			_isBlocked = _byteLen < _byteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
				}
				_charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
			}
		}
		while (_charLen == 0);
		if (flag)
		{
			_charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
			_bytePos = 0;
			_byteLen = 0;
		}
		return _charLen;
	}

	private int ReadBuffer(Span<char> userBuffer, out bool readToUserBuffer)
	{
		_charLen = 0;
		_charPos = 0;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		bool flag = false;
		int num = 0;
		readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;
		do
		{
			if (_checkPreamble)
			{
				int num2 = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
				if (num2 == 0)
				{
					flag = true;
					break;
				}
				_byteLen += num2;
			}
			else
			{
				_byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
				if (_byteLen == 0)
				{
					flag = true;
					break;
				}
			}
			_isBlocked = _byteLen < _byteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
					readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;
				}
				num = (readToUserBuffer ? _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer, flush: false) : (_charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: false)));
			}
		}
		while (num == 0);
		if (flag)
		{
			num = (readToUserBuffer ? _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer, flush: true) : (_charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true)));
			_bytePos = 0;
			_byteLen = 0;
		}
		_isBlocked &= num < userBuffer.Length;
		return num;
	}

	public override string? ReadLine()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && ReadBuffer() == 0)
		{
			return null;
		}
		Span<char> initialBuffer = stackalloc char[256];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		do
		{
			ReadOnlySpan<char> readOnlySpan = _charBuffer.AsSpan(_charPos, _charLen - _charPos);
			int num = readOnlySpan.IndexOfAny('\r', '\n');
			if (num >= 0)
			{
				string result;
				if (valueStringBuilder.Length == 0)
				{
					result = new string(readOnlySpan.Slice(0, num));
				}
				else
				{
					result = string.Concat(valueStringBuilder.AsSpan(), readOnlySpan.Slice(0, num));
					valueStringBuilder.Dispose();
				}
				char c = readOnlySpan[num];
				_charPos += num + 1;
				if (c == '\r' && (_charPos < _charLen || ReadBuffer() > 0) && _charBuffer[_charPos] == '\n')
				{
					_charPos++;
				}
				return result;
			}
			valueStringBuilder.Append(readOnlySpan);
		}
		while (ReadBuffer() > 0);
		return valueStringBuilder.ToString();
	}

	public override Task<string?> ReadLineAsync()
	{
		return ReadLineAsync(default(CancellationToken)).AsTask();
	}

	public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadLineAsync(cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return new ValueTask<string>((Task<string>)(_asyncReadTask = ReadLineAsyncInternal(cancellationToken)));
	}

	private async Task<string> ReadLineAsyncInternal(CancellationToken cancellationToken)
	{
		bool flag = _charPos == _charLen;
		bool flag2 = flag;
		if (flag2)
		{
			flag2 = await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) == 0;
		}
		if (flag2)
		{
			return null;
		}
		char[] arrayPoolBuffer = null;
		int arrayPoolBufferPos = 0;
		string retVal;
		do
		{
			char[] charBuffer = _charBuffer;
			int charLen = _charLen;
			int charPos = _charPos;
			int num = charBuffer.AsSpan(charPos, charLen - charPos).IndexOfAny('\r', '\n');
			if (num >= 0)
			{
				if (arrayPoolBuffer == null)
				{
					retVal = new string(charBuffer, charPos, num);
				}
				else
				{
					retVal = string.Concat(arrayPoolBuffer.AsSpan(0, arrayPoolBufferPos), charBuffer.AsSpan(charPos, num));
					ArrayPool<char>.Shared.Return(arrayPoolBuffer);
				}
				charPos += num;
				char c = charBuffer[charPos++];
				_charPos = charPos;
				if (c == '\r')
				{
					bool flag3 = charPos < charLen;
					bool flag4 = flag3;
					if (!flag4)
					{
						flag4 = await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) > 0;
					}
					if (flag4 && _charBuffer[_charPos] == '\n')
					{
						_charPos++;
					}
				}
				return retVal;
			}
			if (arrayPoolBuffer == null)
			{
				arrayPoolBuffer = ArrayPool<char>.Shared.Rent(charLen - charPos + 80);
			}
			else if (arrayPoolBuffer.Length - arrayPoolBufferPos < charLen - charPos)
			{
				char[] array = ArrayPool<char>.Shared.Rent(checked(arrayPoolBufferPos + charLen - charPos));
				arrayPoolBuffer.AsSpan(0, arrayPoolBufferPos).CopyTo(array);
				ArrayPool<char>.Shared.Return(arrayPoolBuffer);
				arrayPoolBuffer = array;
			}
			charBuffer.AsSpan(charPos, charLen - charPos).CopyTo(arrayPoolBuffer.AsSpan(arrayPoolBufferPos));
			arrayPoolBufferPos += charLen - charPos;
		}
		while (await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) > 0);
		if (arrayPoolBuffer != null)
		{
			retVal = new string(arrayPoolBuffer, 0, arrayPoolBufferPos);
			ArrayPool<char>.Shared.Return(arrayPoolBuffer);
		}
		else
		{
			retVal = string.Empty;
		}
		return retVal;
	}

	public override Task<string> ReadToEndAsync()
	{
		return ReadToEndAsync(default(CancellationToken));
	}

	public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadToEndAsync(cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<string>)(_asyncReadTask = ReadToEndAsyncInternal(cancellationToken));
	}

	private async Task<string> ReadToEndAsyncInternal(CancellationToken cancellationToken)
	{
		StringBuilder sb = new StringBuilder(_charLen - _charPos);
		do
		{
			int charPos = _charPos;
			sb.Append(_charBuffer, charPos, _charLen - charPos);
			_charPos = _charLen;
			await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (_charLen > 0);
		return sb.ToString();
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
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<int>)(_asyncReadTask = ReadAsyncInternal(new Memory<char>(buffer, index, count), CancellationToken.None).AsTask());
	}

	public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		return ReadAsyncInternal(buffer, cancellationToken);
	}

	internal override async ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken)
	{
		bool flag = _charPos == _charLen;
		bool flag2 = flag;
		if (flag2)
		{
			flag2 = await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) == 0;
		}
		if (flag2)
		{
			return 0;
		}
		int charsRead = 0;
		bool readToUserBuffer = false;
		byte[] tmpByteBuffer = _byteBuffer;
		Stream tmpStream = _stream;
		int count = buffer.Length;
		while (count > 0)
		{
			int n = _charLen - _charPos;
			if (n == 0)
			{
				_charLen = 0;
				_charPos = 0;
				if (!_checkPreamble)
				{
					_byteLen = 0;
				}
				readToUserBuffer = count >= _maxCharsPerBuffer;
				do
				{
					if (_checkPreamble)
					{
						int bytePos = _bytePos;
						int num = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer, bytePos, tmpByteBuffer.Length - bytePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							if (_byteLen > 0)
							{
								if (readToUserBuffer)
								{
									n = _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
									_charLen = 0;
								}
								else
								{
									n = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
									_charLen += n;
								}
							}
							_isBlocked = true;
							break;
						}
						_byteLen += num;
					}
					else
					{
						_byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (_byteLen == 0)
						{
							_isBlocked = true;
							break;
						}
					}
					_isBlocked = _byteLen < tmpByteBuffer.Length;
					if (!IsPreamble())
					{
						if (_detectEncoding && _byteLen >= 2)
						{
							DetectEncoding();
							readToUserBuffer = count >= _maxCharsPerBuffer;
						}
						_charPos = 0;
						if (readToUserBuffer)
						{
							n = _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
							_charLen = 0;
						}
						else
						{
							n = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
							_charLen += n;
						}
					}
				}
				while (n == 0);
				if (n == 0)
				{
					break;
				}
			}
			if (n > count)
			{
				n = count;
			}
			if (!readToUserBuffer)
			{
				new Span<char>(_charBuffer, _charPos, n).CopyTo(buffer.Span.Slice(charsRead));
				_charPos += n;
			}
			charsRead += n;
			count -= n;
			if (_isBlocked)
			{
				break;
			}
		}
		return charsRead;
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
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlockAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<int>)(_asyncReadTask = base.ReadBlockAsync(buffer, index, count));
	}

	public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlockAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		ValueTask<int> result = ReadBlockAsyncInternal(buffer, cancellationToken);
		if (result.IsCompletedSuccessfully)
		{
			return result;
		}
		return new ValueTask<int>((Task<int>)(_asyncReadTask = result.AsTask()));
	}

	private async ValueTask<int> ReadBufferAsync(CancellationToken cancellationToken)
	{
		_charLen = 0;
		_charPos = 0;
		byte[] tmpByteBuffer = _byteBuffer;
		Stream tmpStream = _stream;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		bool eofReached = false;
		do
		{
			if (_checkPreamble)
			{
				int bytePos = _bytePos;
				int num = await tmpStream.ReadAsync(tmpByteBuffer.AsMemory(bytePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					eofReached = true;
					break;
				}
				_byteLen += num;
			}
			else
			{
				_byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (_byteLen == 0)
				{
					eofReached = true;
					break;
				}
			}
			_isBlocked = _byteLen < tmpByteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
				}
				_charLen = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
			}
		}
		while (_charLen == 0);
		if (eofReached)
		{
			_charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
			_bytePos = 0;
			_byteLen = 0;
		}
		return _charLen;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowObjectDisposedException();
		}
		void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_ReaderClosed);
		}
	}
}
