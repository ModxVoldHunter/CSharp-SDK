using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class Base64Stream : DelegatedStream, IEncodableStream
{
	private sealed class ReadStateInfo
	{
		internal byte Val { get; set; }

		internal byte Pos { get; set; }
	}

	private readonly Base64WriteStateInfo _writeState;

	private ReadStateInfo _readState;

	private readonly Base64Encoder _encoder;

	private static ReadOnlySpan<byte> Base64DecodeMap => new byte[256]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 62, 255, 255, 255, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, 255, 255,
		255, 255, 255, 255, 255, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, 255, 255, 255, 255, 255, 255, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255
	};

	private ReadStateInfo ReadState => _readState ?? (_readState = new ReadStateInfo());

	internal WriteStateInfoBase WriteState => _writeState;

	internal Base64Stream(Stream stream, Base64WriteStateInfo writeStateInfo)
		: base(stream)
	{
		_writeState = new Base64WriteStateInfo();
		_encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
	}

	internal Base64Stream(Base64WriteStateInfo writeStateInfo)
		: base(new MemoryStream())
	{
		_writeState = writeStateInfo;
		_encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override void Close()
	{
		if (_writeState != null && WriteState.Length > 0)
		{
			_encoder.AppendPadding();
			FlushInternal();
		}
		base.Close();
	}

	public unsafe int DecodeBytes(byte[] buffer, int offset, int count)
	{
		fixed (byte* ptr = buffer)
		{
			byte* ptr2 = ptr + offset;
			byte* ptr3 = ptr2;
			byte* ptr4 = ptr2;
			byte* ptr5 = ptr2 + count;
			while (ptr3 < ptr5)
			{
				if (*ptr3 == 13 || *ptr3 == 10 || *ptr3 == 61 || *ptr3 == 32 || *ptr3 == 9)
				{
					ptr3++;
					continue;
				}
				byte b = Base64DecodeMap[*ptr3];
				if (b == byte.MaxValue)
				{
					throw new FormatException(System.SR.MailBase64InvalidCharacter);
				}
				switch (ReadState.Pos)
				{
				case 0:
					ReadState.Val = (byte)(b << 2);
					ReadState.Pos++;
					break;
				case 1:
					*(ptr4++) = (byte)(ReadState.Val + (b >> 4));
					ReadState.Val = (byte)(b << 4);
					ReadState.Pos++;
					break;
				case 2:
					*(ptr4++) = (byte)(ReadState.Val + (b >> 2));
					ReadState.Val = (byte)(b << 6);
					ReadState.Pos++;
					break;
				case 3:
					*(ptr4++) = (byte)(ReadState.Val + b);
					ReadState.Pos = 0;
					break;
				}
				ptr3++;
			}
			return (int)(ptr4 - ptr2);
		}
	}

	public int EncodeBytes(byte[] buffer, int offset, int count)
	{
		return EncodeBytes(buffer, offset, count, dontDeferFinalBytes: true, shouldAppendSpaceToCRLF: true);
	}

	internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
	{
		return _encoder.EncodeBytes(buffer, offset, count, dontDeferFinalBytes, shouldAppendSpaceToCRLF);
	}

	public int EncodeString(string value, Encoding encoding)
	{
		return _encoder.EncodeString(value, encoding);
	}

	public string GetEncodedString()
	{
		return _encoder.GetEncodedString();
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public override void Flush()
	{
		if (_writeState != null && WriteState.Length > 0)
		{
			FlushInternal();
		}
		base.Flush();
	}

	public override async Task FlushAsync(CancellationToken cancellationToken)
	{
		if (_writeState != null && WriteState.Length > 0)
		{
			await WriteAsync(WriteState.Buffer.AsMemory(0, WriteState.Length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			WriteState.Reset();
		}
		await base.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private void FlushInternal()
	{
		base.Write(WriteState.Buffer, 0, WriteState.Length);
		WriteState.Reset();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		int num;
		do
		{
			num = base.Read(buffer, offset, count);
			if (num == 0)
			{
				return 0;
			}
			num = DecodeBytes(buffer, offset, num);
		}
		while (num <= 0);
		return num;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsyncCore(buffer, offset, count, cancellationToken);
		async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int num;
			do
			{
				num = await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					return 0;
				}
				num = DecodeBytes(buffer, offset, num);
			}
			while (num <= 0);
			return num;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		int num = 0;
		while (true)
		{
			num += EncodeBytes(buffer, offset + num, count - num, dontDeferFinalBytes: false, shouldAppendSpaceToCRLF: false);
			if (num < count)
			{
				FlushInternal();
				continue;
			}
			break;
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsyncCore(buffer, offset, count, cancellationToken);
		async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int written = 0;
			while (true)
			{
				written += EncodeBytes(buffer, offset + written, count - written, dontDeferFinalBytes: false, shouldAppendSpaceToCRLF: false);
				if (written >= count)
				{
					break;
				}
				await FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
