using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

public sealed class TarWriter : IDisposable, IAsyncDisposable
{
	private bool _wroteEntries;

	private bool _isDisposed;

	private readonly bool _leaveOpen;

	private readonly Stream _archiveStream;

	private int _nextGlobalExtendedAttributesEntryNumber;

	public TarEntryFormat Format { get; private set; }

	public TarWriter(Stream archiveStream)
		: this(archiveStream, TarEntryFormat.Pax, leaveOpen: false)
	{
	}

	public TarWriter(Stream archiveStream, bool leaveOpen = false)
		: this(archiveStream, TarEntryFormat.Pax, leaveOpen)
	{
	}

	public TarWriter(Stream archiveStream, TarEntryFormat format = TarEntryFormat.Pax, bool leaveOpen = false)
	{
		ArgumentNullException.ThrowIfNull(archiveStream, "archiveStream");
		if (!archiveStream.CanWrite)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnwritableStream);
		}
		if (format != TarEntryFormat.V7 && format != TarEntryFormat.Ustar && format != TarEntryFormat.Pax && format != TarEntryFormat.Gnu)
		{
			throw new ArgumentOutOfRangeException("format");
		}
		_archiveStream = archiveStream;
		Format = format;
		_leaveOpen = leaveOpen;
		_isDisposed = false;
		_wroteEntries = false;
		_nextGlobalExtendedAttributesEntryNumber = 1;
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			if (_wroteEntries)
			{
				WriteFinalRecords();
			}
			if (!_leaveOpen)
			{
				_archiveStream.Dispose();
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			if (_wroteEntries)
			{
				await WriteFinalRecordsAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!_leaveOpen)
			{
				await _archiveStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	public void WriteEntry(string fileName, string? entryName)
	{
		var (fullPath, entryName2) = ValidateWriteEntryArguments(fileName, entryName);
		ReadFileFromDiskAndWriteToArchiveStreamAsEntry(fullPath, entryName2);
	}

	public Task WriteEntryAsync(string fileName, string? entryName, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		var (fullPath, entryName2) = ValidateWriteEntryArguments(fileName, entryName);
		return ReadFileFromDiskAndWriteToArchiveStreamAsEntryAsync(fullPath, entryName2, cancellationToken);
	}

	private void ReadFileFromDiskAndWriteToArchiveStreamAsEntry(string fullPath, string entryName)
	{
		TarEntry tarEntry = ConstructEntryForWriting(fullPath, entryName, FileOptions.None);
		WriteEntry(tarEntry);
		tarEntry._header._dataStream?.Dispose();
	}

	private async Task ReadFileFromDiskAndWriteToArchiveStreamAsEntryAsync(string fullPath, string entryName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarEntry entry = ConstructEntryForWriting(fullPath, entryName, FileOptions.Asynchronous);
		await WriteEntryAsync(entry, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (entry._header._dataStream != null)
		{
			await entry._header._dataStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void WriteEntry(TarEntry entry)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		ArgumentNullException.ThrowIfNull(entry, "entry");
		ValidateEntryLinkName(entry._header._typeFlag, entry._header._linkName);
		ValidateStreamsSeekability(entry);
		WriteEntryInternal(entry);
	}

	public Task WriteEntryAsync(TarEntry entry, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		ArgumentNullException.ThrowIfNull(entry, "entry");
		ValidateEntryLinkName(entry._header._typeFlag, entry._header._linkName);
		ValidateStreamsSeekability(entry);
		return WriteEntryAsyncInternal(entry, cancellationToken);
	}

	private void WriteEntryInternal(TarEntry entry)
	{
		Span<byte> buffer = stackalloc byte[512];
		buffer.Clear();
		switch (entry.Format)
		{
		case TarEntryFormat.V7:
			entry._header.WriteAsV7(_archiveStream, buffer);
			break;
		case TarEntryFormat.Ustar:
			entry._header.WriteAsUstar(_archiveStream, buffer);
			break;
		case TarEntryFormat.Pax:
			if (entry._header._typeFlag == TarEntryType.GlobalExtendedAttributes)
			{
				entry._header.WriteAsPaxGlobalExtendedAttributes(_archiveStream, buffer, _nextGlobalExtendedAttributesEntryNumber++);
			}
			else
			{
				entry._header.WriteAsPax(_archiveStream, buffer);
			}
			break;
		case TarEntryFormat.Gnu:
			entry._header.WriteAsGnu(_archiveStream, buffer);
			break;
		default:
			throw new InvalidDataException(System.SR.Format(System.SR.TarInvalidFormat, Format));
		}
		_wroteEntries = true;
	}

	private async Task WriteEntryAsyncInternal(TarEntry entry, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		byte[] rented = ArrayPool<byte>.Shared.Rent(512);
		Memory<byte> buffer = rented.AsMemory(0, 512);
		buffer.Span.Clear();
		await (entry.Format switch
		{
			TarEntryFormat.V7 => entry._header.WriteAsV7Async(_archiveStream, buffer, cancellationToken), 
			TarEntryFormat.Ustar => entry._header.WriteAsUstarAsync(_archiveStream, buffer, cancellationToken), 
			TarEntryFormat.Pax => (entry._header._typeFlag != TarEntryType.GlobalExtendedAttributes) ? entry._header.WriteAsPaxAsync(_archiveStream, buffer, cancellationToken) : entry._header.WriteAsPaxGlobalExtendedAttributesAsync(_archiveStream, buffer, _nextGlobalExtendedAttributesEntryNumber++, cancellationToken), 
			TarEntryFormat.Gnu => entry._header.WriteAsGnuAsync(_archiveStream, buffer, cancellationToken), 
			_ => throw new InvalidDataException(System.SR.Format(System.SR.TarInvalidFormat, Format)), 
		}).ConfigureAwait(continueOnCapturedContext: false);
		_wroteEntries = true;
		ArrayPool<byte>.Shared.Return(rented);
	}

	private void WriteFinalRecords()
	{
		Span<byte> span = stackalloc byte[512];
		span.Clear();
		_archiveStream.Write(span);
		_archiveStream.Write(span);
	}

	private async ValueTask WriteFinalRecordsAsync()
	{
		byte[] twoEmptyRecords = ArrayPool<byte>.Shared.Rent(1024);
		Array.Clear(twoEmptyRecords, 0, 1024);
		await _archiveStream.WriteAsync(twoEmptyRecords.AsMemory(0, 1024)).ConfigureAwait(continueOnCapturedContext: false);
		ArrayPool<byte>.Shared.Return(twoEmptyRecords);
	}

	private (string, string) ValidateWriteEntryArguments(string fileName, string entryName)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		ArgumentException.ThrowIfNullOrEmpty(fileName, "fileName");
		string fullPath = Path.GetFullPath(fileName);
		string item = (string.IsNullOrEmpty(entryName) ? Path.GetFileName(fileName) : entryName);
		return (fullPath, item);
	}

	private void ValidateStreamsSeekability(TarEntry entry)
	{
		if (!_archiveStream.CanSeek && entry._header._dataStream != null && !entry._header._dataStream.CanSeek)
		{
			throw new IOException(System.SR.Format(System.SR.TarStreamSeekabilityUnsupportedCombination, entry.Name));
		}
	}

	private static void ValidateEntryLinkName(TarEntryType entryType, string linkName)
	{
		bool flag = entryType - 49 <= (TarEntryType)1;
		if (flag && string.IsNullOrEmpty(linkName))
		{
			throw new ArgumentException(System.SR.TarEntryHardLinkOrSymlinkLinkNameEmpty, "entry");
		}
	}

	private TarEntry ConstructEntryForWriting(string fullPath, string entryName, FileOptions fileOptions)
	{
		FileAttributes attributes = File.GetAttributes(fullPath);
		TarEntryType entryType;
		if ((attributes & FileAttributes.ReparsePoint) != 0)
		{
			entryType = TarEntryType.SymbolicLink;
		}
		else if ((attributes & FileAttributes.Directory) != 0)
		{
			entryType = TarEntryType.Directory;
		}
		else
		{
			if ((attributes & (FileAttributes.Archive | FileAttributes.Normal)) == 0)
			{
				throw new IOException(System.SR.Format(System.SR.TarUnsupportedFile, fullPath));
			}
			entryType = ((Format != TarEntryFormat.V7) ? TarEntryType.RegularFile : TarEntryType.V7RegularFile);
		}
		TarEntry tarEntry = Format switch
		{
			TarEntryFormat.V7 => new V7TarEntry(entryType, entryName), 
			TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName), 
			TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName), 
			TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName), 
			_ => throw new InvalidDataException(System.SR.Format(System.SR.TarInvalidFormat, Format)), 
		};
		FileSystemInfo fileSystemInfo = (((attributes & FileAttributes.Directory) != 0) ? ((FileSystemInfo)new DirectoryInfo(fullPath)) : ((FileSystemInfo)new FileInfo(fullPath)));
		tarEntry._header._mTime = fileSystemInfo.LastWriteTimeUtc;
		tarEntry._header._aTime = fileSystemInfo.LastAccessTimeUtc;
		tarEntry._header._cTime = fileSystemInfo.LastWriteTimeUtc;
		tarEntry.Mode = UnixFileMode.OtherExecute | UnixFileMode.OtherRead | UnixFileMode.GroupExecute | UnixFileMode.GroupRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead;
		if (tarEntry.EntryType == TarEntryType.SymbolicLink)
		{
			tarEntry.LinkName = fileSystemInfo.LinkTarget ?? string.Empty;
		}
		TarEntryType entryType2 = tarEntry.EntryType;
		if ((entryType2 == TarEntryType.V7RegularFile || entryType2 == TarEntryType.RegularFile) ? true : false)
		{
			tarEntry._header._dataStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOptions);
		}
		return tarEntry;
	}
}
