using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

internal sealed class TarHeader
{
	internal Stream _dataStream;

	internal long _endOfHeaderAndDataAndBlockAlignment;

	internal TarEntryFormat _format;

	internal string _name;

	internal int _mode;

	internal int _uid;

	internal int _gid;

	internal long _size;

	internal DateTimeOffset _mTime;

	internal int _checksum;

	internal TarEntryType _typeFlag;

	internal string _linkName;

	internal string _magic;

	internal string _version;

	internal string _gName;

	internal string _uName;

	internal int _devMajor;

	internal int _devMinor;

	internal string _prefix;

	private Dictionary<string, string> _ea;

	internal DateTimeOffset _aTime;

	internal DateTimeOffset _cTime;

	internal byte[] _gnuUnusedBytes;

	internal Dictionary<string, string> ExtendedAttributes => _ea ?? (_ea = new Dictionary<string, string>());

	private static ReadOnlySpan<byte> UstarMagicBytes => new byte[6] { 117, 115, 116, 97, 114, 0 };

	private static ReadOnlySpan<byte> UstarVersionBytes => "00"u8;

	private static ReadOnlySpan<byte> GnuMagicBytes => "ustar "u8;

	private static ReadOnlySpan<byte> GnuVersionBytes => new byte[2] { 32, 0 };

	internal TarHeader(TarEntryFormat format, string name = "", int mode = 0, DateTimeOffset mTime = default(DateTimeOffset), TarEntryType typeFlag = TarEntryType.RegularFile)
	{
		_format = format;
		_name = name;
		_mode = mode;
		_mTime = mTime;
		_typeFlag = typeFlag;
		_magic = GetMagicForFormat(format);
		_version = GetVersionForFormat(format);
	}

	internal TarHeader(TarEntryFormat format, TarEntryType typeFlag, TarHeader other)
		: this(format, other._name, other._mode, other._mTime, typeFlag)
	{
		_uid = other._uid;
		_gid = other._gid;
		_size = other._size;
		_checksum = other._checksum;
		_linkName = other._linkName;
		_dataStream = other._dataStream;
	}

	internal void InitializeExtendedAttributesWithExisting(IEnumerable<KeyValuePair<string, string>> existing)
	{
		foreach (KeyValuePair<string, string> item in existing)
		{
			int num = item.Key.AsSpan().IndexOfAny('=', '\n');
			if (num >= 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.TarExtAttrDisallowedKeyChar, item.Key, (item.Key[num] == '\n') ? "\\n" : ((object)item.Key[num])));
			}
			if (item.Value.Contains('\n'))
			{
				throw new ArgumentException(System.SR.Format(System.SR.TarExtAttrDisallowedValueChar, item.Key, "\\n"));
			}
			if (_ea == null)
			{
				_ea = new Dictionary<string, string>();
			}
			_ea.Add(item.Key, item.Value);
		}
	}

	private static string GetMagicForFormat(TarEntryFormat format)
	{
		switch (format)
		{
		case TarEntryFormat.Ustar:
		case TarEntryFormat.Pax:
			return "ustar\0";
		case TarEntryFormat.Gnu:
			return "ustar ";
		default:
			return string.Empty;
		}
	}

	private static string GetVersionForFormat(TarEntryFormat format)
	{
		switch (format)
		{
		case TarEntryFormat.Ustar:
		case TarEntryFormat.Pax:
			return "00";
		case TarEntryFormat.Gnu:
			return " \0";
		default:
			return string.Empty;
		}
	}

	internal static TarHeader TryGetNextHeader(Stream archiveStream, bool copyData, TarEntryFormat initialFormat, bool processDataBlock)
	{
		Span<byte> buffer = stackalloc byte[512];
		archiveStream.ReadExactly(buffer);
		TarHeader tarHeader = TryReadAttributes(initialFormat, buffer);
		if (tarHeader != null && processDataBlock)
		{
			tarHeader.ProcessDataBlock(archiveStream, copyData);
		}
		return tarHeader;
	}

	internal static async ValueTask<TarHeader> TryGetNextHeaderAsync(Stream archiveStream, bool copyData, TarEntryFormat initialFormat, bool processDataBlock, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		byte[] rented = ArrayPool<byte>.Shared.Rent(512);
		Memory<byte> buffer = rented.AsMemory(0, 512);
		await archiveStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		TarHeader header = TryReadAttributes(initialFormat, buffer.Span);
		if (header != null && processDataBlock)
		{
			await header.ProcessDataBlockAsync(archiveStream, copyData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		ArrayPool<byte>.Shared.Return(rented);
		return header;
	}

	private static TarHeader TryReadAttributes(TarEntryFormat initialFormat, Span<byte> buffer)
	{
		TarHeader tarHeader = TryReadCommonAttributes(buffer, initialFormat);
		if (tarHeader != null)
		{
			tarHeader.ReadMagicAttribute(buffer);
			if (tarHeader._format != TarEntryFormat.V7)
			{
				tarHeader.ReadVersionAttribute(buffer);
				tarHeader.ReadPosixAndGnuSharedAttributes(buffer);
				if (tarHeader._format == TarEntryFormat.Ustar)
				{
					tarHeader.ReadUstarAttributes(buffer);
				}
				else if (tarHeader._format == TarEntryFormat.Gnu)
				{
					tarHeader.ReadGnuAttributes(buffer);
				}
			}
		}
		return tarHeader;
	}

	internal void ReplaceNormalAttributesWithExtended(Dictionary<string, string> dictionaryFromExtendedAttributesHeader)
	{
		if (dictionaryFromExtendedAttributesHeader != null && dictionaryFromExtendedAttributesHeader.Count != 0)
		{
			InitializeExtendedAttributesWithExisting(dictionaryFromExtendedAttributesHeader);
			if (ExtendedAttributes.TryGetValue("path", out var value))
			{
				_name = value;
			}
			if (ExtendedAttributes.TryGetValue("linkpath", out var value2))
			{
				_linkName = value2;
			}
			if (TarHelpers.TryGetDateTimeOffsetFromTimestampString(ExtendedAttributes, "mtime", out var dateTimeOffset))
			{
				_mTime = dateTimeOffset;
			}
			if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, "mode", out var baseTenInteger))
			{
				_mode = baseTenInteger;
			}
			if (TarHelpers.TryGetStringAsBaseTenLong(ExtendedAttributes, "size", out var baseTenLong))
			{
				_size = baseTenLong;
			}
			if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, "uid", out var baseTenInteger2))
			{
				_uid = baseTenInteger2;
			}
			if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, "gid", out var baseTenInteger3))
			{
				_gid = baseTenInteger3;
			}
			if (ExtendedAttributes.TryGetValue("uname", out var value3))
			{
				_uName = value3;
			}
			if (ExtendedAttributes.TryGetValue("gname", out var value4))
			{
				_gName = value4;
			}
			if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, "devmajor", out var baseTenInteger4))
			{
				_devMajor = baseTenInteger4;
			}
			if (TarHelpers.TryGetStringAsBaseTenInteger(ExtendedAttributes, "devminor", out var baseTenInteger5))
			{
				_devMinor = baseTenInteger5;
			}
		}
	}

	internal void ProcessDataBlock(Stream archiveStream, bool copyData)
	{
		bool flag = true;
		switch (_typeFlag)
		{
		case TarEntryType.GlobalExtendedAttributes:
		case TarEntryType.ExtendedAttributes:
			ReadExtendedAttributesBlock(archiveStream);
			break;
		case TarEntryType.LongLink:
		case TarEntryType.LongPath:
			ReadGnuLongPathDataBlock(archiveStream);
			break;
		case TarEntryType.HardLink:
		case TarEntryType.SymbolicLink:
		case TarEntryType.CharacterDevice:
		case TarEntryType.BlockDevice:
		case TarEntryType.Directory:
		case TarEntryType.Fifo:
			if (_size > 0)
			{
				throw new InvalidDataException(System.SR.Format(System.SR.TarSizeFieldTooLargeForEntryType, _typeFlag));
			}
			break;
		default:
			_dataStream = GetDataStream(archiveStream, copyData);
			if (_dataStream is SeekableSubReadStream)
			{
				TarHelpers.AdvanceStream(archiveStream, _size);
			}
			else if (_dataStream is SubReadStream)
			{
				flag = false;
			}
			break;
		}
		if (flag)
		{
			if (_size > 0)
			{
				TarHelpers.SkipBlockAlignmentPadding(archiveStream, _size);
			}
			if (archiveStream.CanSeek)
			{
				_endOfHeaderAndDataAndBlockAlignment = archiveStream.Position;
			}
		}
	}

	private async Task ProcessDataBlockAsync(Stream archiveStream, bool copyData, CancellationToken cancellationToken)
	{
		bool skipBlockAlignmentPadding = true;
		switch (_typeFlag)
		{
		case TarEntryType.GlobalExtendedAttributes:
		case TarEntryType.ExtendedAttributes:
			await ReadExtendedAttributesBlockAsync(archiveStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case TarEntryType.LongLink:
		case TarEntryType.LongPath:
			await ReadGnuLongPathDataBlockAsync(archiveStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case TarEntryType.HardLink:
		case TarEntryType.SymbolicLink:
		case TarEntryType.CharacterDevice:
		case TarEntryType.BlockDevice:
		case TarEntryType.Directory:
		case TarEntryType.Fifo:
			if (_size > 0)
			{
				throw new InvalidDataException(System.SR.Format(System.SR.TarSizeFieldTooLargeForEntryType, _typeFlag));
			}
			break;
		default:
			_dataStream = await GetDataStreamAsync(archiveStream, copyData, _size, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (_dataStream is SeekableSubReadStream)
			{
				await TarHelpers.AdvanceStreamAsync(archiveStream, _size, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else if (_dataStream is SubReadStream)
			{
				skipBlockAlignmentPadding = false;
			}
			break;
		}
		if (skipBlockAlignmentPadding)
		{
			if (_size > 0)
			{
				await TarHelpers.SkipBlockAlignmentPaddingAsync(archiveStream, _size, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (archiveStream.CanSeek)
			{
				_endOfHeaderAndDataAndBlockAlignment = archiveStream.Position;
			}
		}
	}

	private Stream GetDataStream(Stream archiveStream, bool copyData)
	{
		if (_size == 0L)
		{
			return null;
		}
		if (copyData)
		{
			MemoryStream memoryStream = new MemoryStream();
			TarHelpers.CopyBytes(archiveStream, memoryStream, _size);
			memoryStream.Position = 0L;
			return memoryStream;
		}
		if (!archiveStream.CanSeek)
		{
			return new SubReadStream(archiveStream, 0L, _size);
		}
		return new SeekableSubReadStream(archiveStream, archiveStream.Position, _size);
	}

	private static async ValueTask<Stream> GetDataStreamAsync(Stream archiveStream, bool copyData, long size, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (size == 0L)
		{
			return null;
		}
		if (copyData)
		{
			MemoryStream copiedData = new MemoryStream();
			await TarHelpers.CopyBytesAsync(archiveStream, copiedData, size, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			copiedData.Position = 0L;
			return copiedData;
		}
		return archiveStream.CanSeek ? new SeekableSubReadStream(archiveStream, archiveStream.Position, size) : new SubReadStream(archiveStream, 0L, size);
	}

	private static TarHeader TryReadCommonAttributes(Span<byte> buffer, TarEntryFormat initialFormat)
	{
		Span<byte> span = buffer.Slice(148, 8);
		if (TarHelpers.IsAllNullBytes(span))
		{
			return null;
		}
		int num = (int)TarHelpers.ParseOctal<uint>(span);
		if (num == 0)
		{
			return null;
		}
		long num2 = (long)TarHelpers.ParseOctal<ulong>(buffer.Slice(124, 12));
		if (num2 < 0)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.TarSizeFieldNegative));
		}
		TarHeader tarHeader = new TarHeader(initialFormat, TarHelpers.GetTrimmedUtf8String(buffer.Slice(0, 100)), (int)TarHelpers.ParseOctal<uint>(buffer.Slice(100, 8)), TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch((long)TarHelpers.ParseOctal<ulong>(buffer.Slice(136, 12))), (TarEntryType)buffer[156])
		{
			_checksum = num,
			_size = num2,
			_uid = (int)TarHelpers.ParseOctal<uint>(buffer.Slice(108, 8)),
			_gid = (int)TarHelpers.ParseOctal<uint>(buffer.Slice(116, 8)),
			_linkName = TarHelpers.GetTrimmedUtf8String(buffer.Slice(157, 100))
		};
		if (tarHeader._format == TarEntryFormat.Unknown)
		{
			TarHeader tarHeader2 = tarHeader;
			TarEntryFormat format;
			switch (tarHeader._typeFlag)
			{
			case TarEntryType.GlobalExtendedAttributes:
			case TarEntryType.ExtendedAttributes:
				format = TarEntryFormat.Pax;
				break;
			case TarEntryType.DirectoryList:
			case TarEntryType.LongLink:
			case TarEntryType.LongPath:
			case TarEntryType.MultiVolume:
			case TarEntryType.RenamedOrSymlinked:
			case TarEntryType.TapeVolume:
				format = TarEntryFormat.Gnu;
				break;
			case TarEntryType.V7RegularFile:
				format = TarEntryFormat.V7;
				break;
			case TarEntryType.SparseFile:
				throw new NotSupportedException(System.SR.Format(System.SR.TarEntryTypeNotSupported, tarHeader._typeFlag));
			default:
				format = ((tarHeader._typeFlag != TarEntryType.RegularFile) ? TarEntryFormat.V7 : TarEntryFormat.Ustar);
				break;
			}
			tarHeader2._format = format;
		}
		return tarHeader;
	}

	private void ReadMagicAttribute(Span<byte> buffer)
	{
		Span<byte> span = buffer.Slice(257, 6);
		if (TarHelpers.IsAllNullBytes(span))
		{
			_format = TarEntryFormat.V7;
		}
		else if (span.SequenceEqual(GnuMagicBytes))
		{
			_magic = "ustar ";
			_format = TarEntryFormat.Gnu;
		}
		else if (span.SequenceEqual(UstarMagicBytes))
		{
			_magic = "ustar\0";
			if (_format == TarEntryFormat.V7)
			{
				_format = TarEntryFormat.Ustar;
			}
		}
		else
		{
			_magic = Encoding.ASCII.GetString(span);
		}
	}

	private void ReadVersionAttribute(Span<byte> buffer)
	{
		if (_format == TarEntryFormat.V7)
		{
			return;
		}
		Span<byte> span = buffer.Slice(263, 2);
		switch (_format)
		{
		case TarEntryFormat.Ustar:
		case TarEntryFormat.Pax:
			if (!span.SequenceEqual(UstarVersionBytes))
			{
				if (!span.SequenceEqual(GnuVersionBytes))
				{
					throw new InvalidDataException(System.SR.Format(System.SR.TarPosixFormatExpected, _name));
				}
				_version = " \0";
			}
			else
			{
				_version = "00";
			}
			break;
		case TarEntryFormat.Gnu:
			if (!span.SequenceEqual(GnuVersionBytes))
			{
				if (!span.SequenceEqual(UstarVersionBytes))
				{
					throw new InvalidDataException(System.SR.Format(System.SR.TarGnuFormatExpected, _name));
				}
				_version = "00";
			}
			else
			{
				_version = " \0";
			}
			break;
		default:
			_version = Encoding.ASCII.GetString(span);
			break;
		}
	}

	private void ReadPosixAndGnuSharedAttributes(Span<byte> buffer)
	{
		_uName = TarHelpers.GetTrimmedUtf8String(buffer.Slice(265, 32));
		_gName = TarHelpers.GetTrimmedUtf8String(buffer.Slice(297, 32));
		TarEntryType typeFlag = _typeFlag;
		if (typeFlag - 51 <= (TarEntryType)1)
		{
			_devMajor = (int)TarHelpers.ParseOctal<uint>(buffer.Slice(329, 8));
			_devMinor = (int)TarHelpers.ParseOctal<uint>(buffer.Slice(337, 8));
		}
	}

	private void ReadGnuAttributes(Span<byte> buffer)
	{
		long secondsSinceUnixEpoch = (long)TarHelpers.ParseOctal<ulong>(buffer.Slice(345, 12));
		_aTime = TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceUnixEpoch);
		long secondsSinceUnixEpoch2 = (long)TarHelpers.ParseOctal<ulong>(buffer.Slice(357, 12));
		_cTime = TarHelpers.GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceUnixEpoch2);
	}

	private void ReadUstarAttributes(Span<byte> buffer)
	{
		_prefix = TarHelpers.GetTrimmedUtf8String(buffer.Slice(345, 155));
		if (!string.IsNullOrEmpty(_prefix))
		{
			_name = _prefix + "/" + _name;
		}
	}

	private void ReadExtendedAttributesBlock(Stream archiveStream)
	{
		if (_size != 0L)
		{
			ValidateSize();
			byte[] array = null;
			Span<byte> span = ((_size > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent((int)_size))) : stackalloc byte[256]);
			Span<byte> span2 = span;
			span2 = span2.Slice(0, (int)_size);
			archiveStream.ReadExactly(span2);
			ReadExtendedAttributesFromBuffer(span2, _name);
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private async ValueTask ReadExtendedAttributesBlockAsync(Stream archiveStream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_size != 0L)
		{
			ValidateSize();
			byte[] buffer = ArrayPool<byte>.Shared.Rent((int)_size);
			Memory<byte> memory = buffer.AsMemory(0, (int)_size);
			await archiveStream.ReadExactlyAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			ReadExtendedAttributesFromBuffer(memory.Span, _name);
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private void ValidateSize()
	{
		if ((uint)_size > (uint)Array.MaxLength)
		{
			ThrowSizeFieldTooLarge();
		}
		[DoesNotReturn]
		void ThrowSizeFieldTooLarge()
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.TarSizeFieldTooLargeForEntryType, _typeFlag.ToString()));
		}
	}

	private void ReadExtendedAttributesFromBuffer(ReadOnlySpan<byte> buffer, string name)
	{
		buffer = TarHelpers.TrimEndingNullsAndSpaces(buffer);
		string key;
		string value;
		while (TryGetNextExtendedAttribute(ref buffer, out key, out value))
		{
			if (!ExtendedAttributes.TryAdd(key, value))
			{
				throw new InvalidDataException(System.SR.Format(System.SR.TarDuplicateExtendedAttribute, name));
			}
		}
	}

	private void ReadGnuLongPathDataBlock(Stream archiveStream)
	{
		if (_size != 0L)
		{
			ValidateSize();
			byte[] array = null;
			Span<byte> span = ((_size > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent((int)_size))) : stackalloc byte[256]);
			Span<byte> span2 = span;
			span2 = span2.Slice(0, (int)_size);
			archiveStream.ReadExactly(span2);
			ReadGnuLongPathDataFromBuffer(span2);
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private async ValueTask ReadGnuLongPathDataBlockAsync(Stream archiveStream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_size != 0L)
		{
			ValidateSize();
			byte[] buffer = ArrayPool<byte>.Shared.Rent((int)_size);
			Memory<byte> memory = buffer.AsMemory(0, (int)_size);
			await archiveStream.ReadExactlyAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			ReadGnuLongPathDataFromBuffer(memory.Span);
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private void ReadGnuLongPathDataFromBuffer(ReadOnlySpan<byte> buffer)
	{
		string trimmedUtf8String = TarHelpers.GetTrimmedUtf8String(buffer);
		if (_typeFlag == TarEntryType.LongLink)
		{
			_linkName = trimmedUtf8String;
		}
		else if (_typeFlag == TarEntryType.LongPath)
		{
			_name = trimmedUtf8String;
		}
	}

	private static bool TryGetNextExtendedAttribute(ref ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out string key, [NotNullWhen(true)] out string value)
	{
		key = null;
		value = null;
		int num = buffer.IndexOf<byte>(10);
		if (num < 0)
		{
			return false;
		}
		ReadOnlySpan<byte> span = buffer.Slice(0, num);
		buffer = buffer.Slice(num + 1);
		int num2 = span.IndexOf<byte>(32);
		if (num2 < 0)
		{
			return false;
		}
		span = span.Slice(num2 + 1);
		int num3 = span.IndexOf<byte>(61);
		if (num3 < 0)
		{
			return false;
		}
		ReadOnlySpan<byte> bytes = span.Slice(0, num3);
		ReadOnlySpan<byte> bytes2 = span.Slice(num3 + 1);
		key = Encoding.UTF8.GetString(bytes);
		value = Encoding.UTF8.GetString(bytes2);
		return true;
	}

	private void WriteWithSeekableDataStream(TarEntryFormat format, Stream archiveStream, Span<byte> buffer)
	{
		_size = GetTotalDataBytesToWrite();
		WriteFieldsToBuffer(format, buffer);
		archiveStream.Write(buffer);
		if (_dataStream != null)
		{
			WriteData(archiveStream, _dataStream);
		}
	}

	private async Task WriteWithSeekableDataStreamAsync(TarEntryFormat format, Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		_size = GetTotalDataBytesToWrite();
		WriteFieldsToBuffer(format, buffer.Span);
		await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (_dataStream != null)
		{
			await WriteDataAsync(archiveStream, _dataStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void WriteWithUnseekableDataStream(TarEntryFormat format, Stream destinationStream, Span<byte> buffer, bool shouldAdvanceToEnd)
	{
		long position = destinationStream.Position;
		ushort num;
		switch (format)
		{
		case TarEntryFormat.V7:
			num = 512;
			break;
		case TarEntryFormat.Ustar:
		case TarEntryFormat.Pax:
			num = 512;
			break;
		case TarEntryFormat.Gnu:
			num = 512;
			break;
		default:
			throw new ArgumentOutOfRangeException("format");
		}
		ushort num2 = num;
		long num3 = position + num2;
		destinationStream.Seek(num2, SeekOrigin.Current);
		_dataStream.CopyTo(destinationStream);
		long position2 = destinationStream.Position;
		_size = position2 - num3;
		WriteEmptyPadding(destinationStream);
		long position3 = destinationStream.Position;
		destinationStream.Position = position;
		WriteFieldsToBuffer(format, buffer);
		destinationStream.Write(buffer);
		if (shouldAdvanceToEnd)
		{
			destinationStream.Position = position3;
		}
	}

	private async Task WriteWithUnseekableDataStreamAsync(TarEntryFormat format, Stream destinationStream, Memory<byte> buffer, bool shouldAdvanceToEnd, CancellationToken cancellationToken)
	{
		long headerStartPosition = destinationStream.Position;
		ushort num;
		switch (format)
		{
		case TarEntryFormat.V7:
			num = 512;
			break;
		case TarEntryFormat.Ustar:
		case TarEntryFormat.Pax:
			num = 512;
			break;
		case TarEntryFormat.Gnu:
			num = 512;
			break;
		default:
			throw new ArgumentOutOfRangeException("format");
		}
		ushort num2 = num;
		long dataStartPosition = headerStartPosition + num2;
		destinationStream.Seek(num2, SeekOrigin.Current);
		await _dataStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		long position = destinationStream.Position;
		_size = position - dataStartPosition;
		await WriteEmptyPaddingAsync(destinationStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		long endOfHeaderPosition = destinationStream.Position;
		destinationStream.Position = headerStartPosition;
		WriteFieldsToBuffer(format, buffer.Span);
		await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (shouldAdvanceToEnd)
		{
			destinationStream.Position = endOfHeaderPosition;
		}
	}

	private void WriteV7FieldsToBuffer(Span<byte> buffer)
	{
		TarEntryType correctTypeFlagForFormat = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.V7, _typeFlag);
		int num = WriteName(buffer);
		num += WriteCommonFields(buffer, correctTypeFlagForFormat);
		_checksum = WriteChecksum(num, buffer);
	}

	private void WriteUstarFieldsToBuffer(Span<byte> buffer)
	{
		TarEntryType correctTypeFlagForFormat = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Ustar, _typeFlag);
		int num = WriteUstarName(buffer);
		num += WriteCommonFields(buffer, correctTypeFlagForFormat);
		num += WritePosixMagicAndVersion(buffer);
		num += WritePosixAndGnuSharedFields(buffer);
		_checksum = WriteChecksum(num, buffer);
	}

	internal void WriteAsPaxGlobalExtendedAttributes(Stream archiveStream, Span<byte> buffer, int globalExtendedAttributesEntryNumber)
	{
		VerifyGlobalExtendedAttributesDataIsValid(globalExtendedAttributesEntryNumber);
		WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: true, globalExtendedAttributesEntryNumber);
	}

	internal Task WriteAsPaxGlobalExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		VerifyGlobalExtendedAttributesDataIsValid(globalExtendedAttributesEntryNumber);
		return WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: true, globalExtendedAttributesEntryNumber, cancellationToken);
	}

	private void VerifyGlobalExtendedAttributesDataIsValid(int globalExtendedAttributesEntryNumber)
	{
	}

	internal void WriteAsV7(Stream archiveStream, Span<byte> buffer)
	{
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				WriteWithUnseekableDataStream(TarEntryFormat.V7, archiveStream, buffer, shouldAdvanceToEnd: true);
				return;
			}
		}
		WriteWithSeekableDataStream(TarEntryFormat.V7, archiveStream, buffer);
	}

	internal Task WriteAsV7Async(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				return WriteWithUnseekableDataStreamAsync(TarEntryFormat.V7, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken);
			}
		}
		return WriteWithSeekableDataStreamAsync(TarEntryFormat.V7, archiveStream, buffer, cancellationToken);
	}

	internal void WriteAsUstar(Stream archiveStream, Span<byte> buffer)
	{
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				WriteWithUnseekableDataStream(TarEntryFormat.Ustar, archiveStream, buffer, shouldAdvanceToEnd: true);
				return;
			}
		}
		WriteWithSeekableDataStream(TarEntryFormat.Ustar, archiveStream, buffer);
	}

	internal Task WriteAsUstarAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				return WriteWithUnseekableDataStreamAsync(TarEntryFormat.Ustar, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken);
			}
		}
		return WriteWithSeekableDataStreamAsync(TarEntryFormat.Ustar, archiveStream, buffer, cancellationToken);
	}

	internal void WriteAsPax(Stream archiveStream, Span<byte> buffer)
	{
		TarHeader tarHeader = new TarHeader(TarEntryFormat.Pax);
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					WriteWithUnseekableDataStream(TarEntryFormat.Pax, memoryStream, buffer, shouldAdvanceToEnd: false);
					memoryStream.Position = 0L;
					buffer.Clear();
					CollectExtendedAttributesFromStandardFieldsIfNeeded();
					tarHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: false, -1);
					buffer.Clear();
					memoryStream.CopyTo(archiveStream);
					return;
				}
			}
		}
		_size = GetTotalDataBytesToWrite();
		CollectExtendedAttributesFromStandardFieldsIfNeeded();
		tarHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: false, -1);
		buffer.Clear();
		WriteWithSeekableDataStream(TarEntryFormat.Pax, archiveStream, buffer);
	}

	internal async Task WriteAsPaxAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarHeader extendedAttributesHeader = new TarHeader(TarEntryFormat.Pax);
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				using (MemoryStream tempStream = new MemoryStream())
				{
					await WriteWithUnseekableDataStreamAsync(TarEntryFormat.Pax, tempStream, buffer, shouldAdvanceToEnd: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					tempStream.Position = 0L;
					buffer.Span.Clear();
					CollectExtendedAttributesFromStandardFieldsIfNeeded();
					await extendedAttributesHeader.WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: false, -1, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					buffer.Span.Clear();
					await tempStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				return;
			}
		}
		_size = GetTotalDataBytesToWrite();
		CollectExtendedAttributesFromStandardFieldsIfNeeded();
		await extendedAttributesHeader.WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: false, -1, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		buffer.Span.Clear();
		await WriteWithSeekableDataStreamAsync(TarEntryFormat.Pax, archiveStream, buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal void WriteAsGnu(Stream archiveStream, Span<byte> buffer)
	{
		if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > 100)
		{
			TarHeader gnuLongMetadataHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
			gnuLongMetadataHeader.WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
			buffer.Clear();
		}
		if (Encoding.UTF8.GetByteCount(_name) > 100)
		{
			TarHeader gnuLongMetadataHeader2 = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
			gnuLongMetadataHeader2.WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
			buffer.Clear();
		}
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				WriteWithUnseekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer, shouldAdvanceToEnd: true);
				return;
			}
		}
		WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
	}

	internal async Task WriteAsGnuAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > 100)
		{
			TarHeader gnuLongMetadataHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
			await gnuLongMetadataHeader.WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			buffer.Span.Clear();
		}
		if (Encoding.UTF8.GetByteCount(_name) > 100)
		{
			TarHeader gnuLongMetadataHeader2 = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
			await gnuLongMetadataHeader2.WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			buffer.Span.Clear();
		}
		if (archiveStream.CanSeek)
		{
			Stream dataStream = _dataStream;
			if (dataStream != null && !dataStream.CanSeek)
			{
				await WriteWithUnseekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
		}
		await WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static TarHeader GetGnuLongMetadataHeader(TarEntryType entryType, string longText)
	{
		return new TarHeader(TarEntryFormat.Gnu)
		{
			_name = "././@LongLink",
			_mode = TarHelpers.GetDefaultMode(entryType),
			_uid = 0,
			_gid = 0,
			_mTime = DateTimeOffset.MinValue,
			_typeFlag = entryType,
			_dataStream = new MemoryStream(Encoding.UTF8.GetBytes(longText))
		};
	}

	private void WriteGnuFieldsToBuffer(Span<byte> buffer)
	{
		int num = WriteName(buffer);
		num += WriteCommonFields(buffer, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Gnu, _typeFlag));
		num += WriteGnuMagicAndVersion(buffer);
		num += WritePosixAndGnuSharedFields(buffer);
		num += WriteGnuFields(buffer);
		_checksum = WriteChecksum(num, buffer);
	}

	private void WriteAsPaxExtendedAttributes(Stream archiveStream, Span<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber)
	{
		WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber, extendedAttributes);
		WriteWithSeekableDataStream(TarEntryFormat.Pax, archiveStream, buffer);
	}

	private Task WriteAsPaxExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber, extendedAttributes);
		return WriteWithSeekableDataStreamAsync(TarEntryFormat.Pax, archiveStream, buffer, cancellationToken);
	}

	private void WriteAsPaxExtendedAttributesShared(bool isGea, int globalExtendedAttributesEntryNumber, Dictionary<string, string> extendedAttributes)
	{
		_dataStream = GenerateExtendedAttributesDataStream(extendedAttributes);
		_name = (isGea ? GenerateGlobalExtendedAttributeName(globalExtendedAttributesEntryNumber) : GenerateExtendedAttributeName());
		_mode = TarHelpers.GetDefaultMode(_typeFlag);
		_typeFlag = (isGea ? TarEntryType.GlobalExtendedAttributes : TarEntryType.ExtendedAttributes);
	}

	private void WritePaxFieldsToBuffer(Span<byte> buffer)
	{
		int num = WriteName(buffer);
		num += WriteCommonFields(buffer, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Pax, _typeFlag));
		num += WritePosixMagicAndVersion(buffer);
		num += WritePosixAndGnuSharedFields(buffer);
		_checksum = WriteChecksum(num, buffer);
	}

	private void WriteFieldsToBuffer(TarEntryFormat format, Span<byte> buffer)
	{
		switch (format)
		{
		case TarEntryFormat.V7:
			WriteV7FieldsToBuffer(buffer);
			break;
		case TarEntryFormat.Ustar:
			WriteUstarFieldsToBuffer(buffer);
			break;
		case TarEntryFormat.Pax:
			WritePaxFieldsToBuffer(buffer);
			break;
		case TarEntryFormat.Gnu:
			WriteGnuFieldsToBuffer(buffer);
			break;
		}
	}

	private int WriteName(Span<byte> buffer)
	{
		ReadOnlySpan<char> text = _name;
		int utf8TextLength = GetUtf8TextLength(text);
		if (utf8TextLength > 100)
		{
			if (_format == TarEntryFormat.V7)
			{
				throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "Name"), "entry");
			}
			text = text[..GetUtf16TruncatedTextLength(text, 100)];
		}
		return WriteAsUtf8String(text, buffer.Slice(0, 100));
	}

	private int WriteUstarName(Span<byte> buffer)
	{
		if (GetUtf8TextLength(_name) > 256)
		{
			throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "Name"), "entry");
		}
		Span<byte> bytes = stackalloc byte[256];
		ReadOnlySpan<byte> readOnlySpan = bytes[..Encoding.UTF8.GetBytes(_name, bytes)];
		if (readOnlySpan.Length <= 100)
		{
			return WriteLeftAlignedBytesAndGetChecksum(readOnlySpan, buffer.Slice(0, 100));
		}
		int num = readOnlySpan.LastIndexOfAny(System.IO.PathInternal.Utf8DirectorySeparators);
		ReadOnlySpan<byte> bytesToWrite;
		ReadOnlySpan<byte> readOnlySpan2;
		if (num < 1)
		{
			bytesToWrite = readOnlySpan;
			readOnlySpan2 = default(ReadOnlySpan<byte>);
		}
		else
		{
			bytesToWrite = readOnlySpan.Slice(num + 1);
			readOnlySpan2 = readOnlySpan.Slice(0, num);
		}
		while (readOnlySpan2.Length - bytesToWrite.Length > 155)
		{
			num = readOnlySpan2.LastIndexOfAny(System.IO.PathInternal.Utf8DirectorySeparators);
			if (num < 1)
			{
				break;
			}
			bytesToWrite = readOnlySpan.Slice(num + 1);
			readOnlySpan2 = readOnlySpan.Slice(0, num);
		}
		if (readOnlySpan2.Length <= 155 && bytesToWrite.Length <= 100)
		{
			int num2 = WriteLeftAlignedBytesAndGetChecksum(readOnlySpan2, buffer.Slice(345, 155));
			return num2 + WriteLeftAlignedBytesAndGetChecksum(bytesToWrite, buffer.Slice(0, 100));
		}
		throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "Name"), "entry");
	}

	private int WriteCommonFields(Span<byte> buffer, TarEntryType actualEntryType)
	{
		int num = 0;
		if (_mode > 0)
		{
			num += FormatOctal(_mode, buffer.Slice(100, 8));
		}
		if (_uid > 0)
		{
			num += FormatOctal(_uid, buffer.Slice(108, 8));
		}
		if (_gid > 0)
		{
			num += FormatOctal(_gid, buffer.Slice(116, 8));
		}
		if (_size > 0)
		{
			if (_size <= 8589934591L)
			{
				num += FormatOctal(_size, buffer.Slice(124, 12));
			}
			else if (_format != TarEntryFormat.Pax)
			{
				throw new ArgumentException(System.SR.Format(System.SR.TarSizeFieldTooLargeForEntryFormat, _format));
			}
		}
		num += WriteAsTimestamp(_mTime, buffer.Slice(136, 12));
		char c = (char)actualEntryType;
		buffer[156] = (byte)c;
		num += c;
		if (!string.IsNullOrEmpty(_linkName))
		{
			ReadOnlySpan<char> text = _linkName;
			if (GetUtf8TextLength(text) > 100)
			{
				TarEntryFormat format = _format;
				if (format != TarEntryFormat.Pax && format != TarEntryFormat.Gnu)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "LinkName"), "entry");
				}
				text = text[..GetUtf16TruncatedTextLength(text, 100)];
			}
			num += WriteAsUtf8String(text, buffer.Slice(157, 100));
		}
		return num;
	}

	public long GetTotalDataBytesToWrite()
	{
		if (_dataStream == null)
		{
			return 0L;
		}
		long length = _dataStream.Length;
		long position = _dataStream.Position;
		if (position >= length)
		{
			return 0L;
		}
		return length - position;
	}

	private static int WritePosixMagicAndVersion(Span<byte> buffer)
	{
		int num = WriteLeftAlignedBytesAndGetChecksum(UstarMagicBytes, buffer.Slice(257, 6));
		return num + WriteLeftAlignedBytesAndGetChecksum(UstarVersionBytes, buffer.Slice(263, 2));
	}

	private static int WriteGnuMagicAndVersion(Span<byte> buffer)
	{
		int num = WriteLeftAlignedBytesAndGetChecksum(GnuMagicBytes, buffer.Slice(257, 6));
		return num + WriteLeftAlignedBytesAndGetChecksum(GnuVersionBytes, buffer.Slice(263, 2));
	}

	private int WritePosixAndGnuSharedFields(Span<byte> buffer)
	{
		int num = 0;
		if (!string.IsNullOrEmpty(_uName))
		{
			ReadOnlySpan<char> text = _uName;
			if (GetUtf8TextLength(text) > 32)
			{
				if (_format != TarEntryFormat.Pax)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "UserName"), "entry");
				}
				text = text[..GetUtf16TruncatedTextLength(text, 32)];
			}
			num += WriteAsUtf8String(text, buffer.Slice(265, 32));
		}
		if (!string.IsNullOrEmpty(_gName))
		{
			ReadOnlySpan<char> text2 = _gName;
			if (GetUtf8TextLength(text2) > 32)
			{
				if (_format != TarEntryFormat.Pax)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TarEntryFieldExceedsMaxLength, "GroupName"), "entry");
				}
				text2 = text2[..GetUtf16TruncatedTextLength(text2, 32)];
			}
			num += WriteAsUtf8String(text2, buffer.Slice(297, 32));
		}
		if (_devMajor > 0)
		{
			num += FormatOctal(_devMajor, buffer.Slice(329, 8));
		}
		if (_devMinor > 0)
		{
			num += FormatOctal(_devMinor, buffer.Slice(337, 8));
		}
		return num;
	}

	private int WriteGnuFields(Span<byte> buffer)
	{
		int num = WriteAsTimestamp(_aTime, buffer.Slice(345, 12));
		num += WriteAsTimestamp(_cTime, buffer.Slice(357, 12));
		if (_gnuUnusedBytes != null)
		{
			num += WriteLeftAlignedBytesAndGetChecksum(_gnuUnusedBytes, buffer.Slice(369, 126));
		}
		return num;
	}

	private void WriteData(Stream archiveStream, Stream dataStream)
	{
		dataStream.CopyTo(archiveStream);
		WriteEmptyPadding(archiveStream);
	}

	private void WriteEmptyPadding(Stream archiveStream)
	{
		int num = TarHelpers.CalculatePadding(_size);
		if (num != 0)
		{
			Span<byte> span = stackalloc byte[512];
			span = span.Slice(0, num);
			span.Clear();
			archiveStream.Write(span);
		}
	}

	private ValueTask WriteEmptyPaddingAsync(Stream archiveStream, CancellationToken cancellationToken)
	{
		int num = TarHelpers.CalculatePadding(_size);
		if (num != 0)
		{
			byte[] array = new byte[num];
			return archiveStream.WriteAsync(array, cancellationToken);
		}
		return ValueTask.CompletedTask;
	}

	private async Task WriteDataAsync(Stream archiveStream, Stream dataStream, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await dataStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		int num = TarHelpers.CalculatePadding(_size);
		if (num != 0)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(num);
			Array.Clear(buffer, 0, num);
			await archiveStream.WriteAsync(buffer.AsMemory(0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private static MemoryStream GenerateExtendedAttributesDataStream(Dictionary<string, string> extendedAttributes)
	{
		MemoryStream memoryStream = null;
		byte[] array = null;
		Span<byte> destination = stackalloc byte[512];
		if (extendedAttributes.Count > 0)
		{
			memoryStream = new MemoryStream();
			foreach (KeyValuePair<string, string> extendedAttribute in extendedAttributes)
			{
				extendedAttribute.Deconstruct(out var key, out var value2);
				string text = key;
				string text2 = value2;
				int num = 3 + Encoding.UTF8.GetByteCount(text) + Encoding.UTF8.GetByteCount(text2);
				int num2 = CountDigits(num);
				num += num2;
				int num3;
				while ((num3 = CountDigits(num)) != num2)
				{
					num += num3 - num2;
					num2 = num3;
				}
				if (destination.Length < num)
				{
					if (array != null)
					{
						ArrayPool<byte>.Shared.Return(array);
					}
					destination = (array = ArrayPool<byte>.Shared.Rent(num));
				}
				int bytesWritten;
				bool flag = Utf8Formatter.TryFormat(num, destination, out bytesWritten);
				destination[bytesWritten++] = 32;
				bytesWritten += Encoding.UTF8.GetBytes(text, destination.Slice(bytesWritten));
				destination[bytesWritten++] = 61;
				bytesWritten += Encoding.UTF8.GetBytes(text2, destination.Slice(bytesWritten));
				destination[bytesWritten++] = 10;
				memoryStream.Write(destination.Slice(0, bytesWritten));
			}
			memoryStream.Position = 0L;
		}
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return memoryStream;
		static int CountDigits(int value)
		{
			int num4 = 1;
			while (true)
			{
				value /= 10;
				if (value == 0)
				{
					break;
				}
				num4++;
			}
			return num4;
		}
	}

	private void CollectExtendedAttributesFromStandardFieldsIfNeeded()
	{
		ExtendedAttributes["path"] = _name;
		ExtendedAttributes["mtime"] = TarHelpers.GetTimestampStringFromDateTimeOffset(_mTime);
		TryAddStringField(ExtendedAttributes, "gname", _gName, 32);
		TryAddStringField(ExtendedAttributes, "uname", _uName, 32);
		if (!string.IsNullOrEmpty(_linkName))
		{
			ExtendedAttributes["linkpath"] = _linkName;
		}
		if (_size > 8589934591L)
		{
			ExtendedAttributes["size"] = _size.ToString();
		}
		else
		{
			ExtendedAttributes.Remove("size");
		}
		static void TryAddStringField(Dictionary<string, string> extendedAttributes, string key, string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || GetUtf8TextLength(value) <= maxLength)
			{
				extendedAttributes.Remove(key);
			}
			else
			{
				extendedAttributes[key] = value;
			}
		}
	}

	private static int WriteChecksum(int checksum, Span<byte> buffer)
	{
		checksum += 256;
		Span<byte> destination = stackalloc byte[8];
		destination.Clear();
		FormatOctal(checksum, destination);
		Span<byte> span = buffer.Slice(148, 8);
		span[span.Length - 1] = 32;
		span[span.Length - 2] = 0;
		int num = span.Length - 3;
		int num2 = destination.Length - 1;
		while (num >= 0)
		{
			if (num2 >= 0)
			{
				span[num] = destination[num2];
				num2--;
			}
			else
			{
				span[num] = 48;
			}
			num--;
		}
		return checksum;
	}

	private static int WriteLeftAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
	{
		bytesToWrite = bytesToWrite[..Math.Min(bytesToWrite.Length, destination.Length)];
		bytesToWrite.CopyTo(destination);
		return Checksum(bytesToWrite);
	}

	private static int WriteRightAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
	{
		destination[destination.Length - 1] = 0;
		bytesToWrite = bytesToWrite[..Math.Min(bytesToWrite.Length, destination.Length - 1)];
		int num = destination.Length - 1 - bytesToWrite.Length;
		bytesToWrite.CopyTo(destination.Slice(num));
		destination.Slice(0, num).Fill(48);
		return Checksum(destination);
	}

	private static int Checksum(ReadOnlySpan<byte> bytes)
	{
		int num = 0;
		ReadOnlySpan<byte> readOnlySpan = bytes;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			num += b;
		}
		return num;
	}

	private static int FormatOctal(long value, Span<byte> destination)
	{
		ulong num = (ulong)value;
		Span<byte> span = stackalloc byte[32];
		int num2 = span.Length - 1;
		while (true)
		{
			span[num2] = (byte)(48 + num % 8);
			num /= 8;
			if (num == 0L)
			{
				break;
			}
			num2--;
		}
		return WriteRightAlignedBytesAndGetChecksum(span.Slice(num2), destination);
	}

	private static int WriteAsTimestamp(DateTimeOffset timestamp, Span<byte> destination)
	{
		long value = timestamp.ToUnixTimeSeconds();
		return FormatOctal(value, destination);
	}

	private static int WriteAsUtf8String(ReadOnlySpan<char> text, Span<byte> buffer)
	{
		return WriteLeftAlignedBytesAndGetChecksum(buffer[..Encoding.UTF8.GetBytes(text, buffer)], buffer);
	}

	private string GenerateExtendedAttributeName()
	{
		ReadOnlySpan<char> directoryName = Path.GetDirectoryName(_name.AsSpan());
		directoryName = (directoryName.IsEmpty ? ((ReadOnlySpan<char>)".") : directoryName);
		ReadOnlySpan<char> fileName = Path.GetFileName(_name.AsSpan());
		fileName = (fileName.IsEmpty ? ((ReadOnlySpan<char>)".") : fileName);
		TarEntryType typeFlag = _typeFlag;
		if ((typeFlag == TarEntryType.Directory || typeFlag == TarEntryType.DirectoryList) ? true : false)
		{
			return $"{directoryName}/PaxHeaders.{Environment.ProcessId}/{fileName}{Path.DirectorySeparatorChar}";
		}
		return $"{directoryName}/PaxHeaders.{Environment.ProcessId}/{fileName}";
	}

	private static string GenerateGlobalExtendedAttributeName(int globalExtendedAttributesEntryNumber)
	{
		ReadOnlySpan<char> value = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
		string text = $"{value}/GlobalHead.{Environment.ProcessId}.{globalExtendedAttributesEntryNumber}";
		if (text.Length < 100)
		{
			return text;
		}
		return "/tmp" + text.AsSpan(value.Length);
	}

	private static int GetUtf8TextLength(ReadOnlySpan<char> text)
	{
		return Encoding.UTF8.GetByteCount(text);
	}

	private static int GetUtf16TruncatedTextLength(ReadOnlySpan<char> text, int utf8MaxLength)
	{
		int num = 0;
		int num2 = 0;
		SpanRuneEnumerator enumerator = text.EnumerateRunes().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Rune current = enumerator.Current;
			num += current.Utf8SequenceLength;
			if (num > utf8MaxLength)
			{
				break;
			}
			num2 += current.Utf16SequenceLength;
		}
		return num2;
	}
}
