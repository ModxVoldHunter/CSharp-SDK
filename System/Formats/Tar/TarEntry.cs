using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

public abstract class TarEntry
{
	internal TarHeader _header;

	private TarReader _readerOfOrigin;

	public int Checksum => _header._checksum;

	public TarEntryType EntryType => _header._typeFlag;

	public TarEntryFormat Format => _header._format;

	public int Gid
	{
		get
		{
			return _header._gid;
		}
		set
		{
			_header._gid = value;
		}
	}

	public DateTimeOffset ModificationTime
	{
		get
		{
			return _header._mTime;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, DateTimeOffset.UnixEpoch, "value");
			_header._mTime = value;
		}
	}

	public long Length
	{
		get
		{
			if (_header._dataStream == null)
			{
				return _header._size;
			}
			return _header._dataStream.Length;
		}
	}

	public string LinkName
	{
		get
		{
			return _header._linkName ?? string.Empty;
		}
		set
		{
			TarEntryType typeFlag = _header._typeFlag;
			if (typeFlag != TarEntryType.HardLink && typeFlag != TarEntryType.SymbolicLink)
			{
				throw new InvalidOperationException(System.SR.TarEntryHardLinkOrSymLinkExpected);
			}
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			_header._linkName = value;
		}
	}

	public UnixFileMode Mode
	{
		get
		{
			return (UnixFileMode)(_header._mode & 0xFFF);
		}
		set
		{
			if (((uint)value & 0xFFFFF000u) != 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_header._mode = (int)value;
		}
	}

	public string Name
	{
		get
		{
			return _header._name;
		}
		set
		{
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			_header._name = value;
		}
	}

	public int Uid
	{
		get
		{
			return _header._uid;
		}
		set
		{
			_header._uid = value;
		}
	}

	public Stream? DataStream
	{
		get
		{
			return _header._dataStream;
		}
		set
		{
			if (!IsDataStreamSetterSupported())
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.TarEntryDoesNotSupportDataStream, Name, EntryType));
			}
			if (value != null && !value.CanRead)
			{
				throw new ArgumentException(System.SR.IO_NotSupported_UnreadableStream, "value");
			}
			if (_readerOfOrigin != null)
			{
				_readerOfOrigin.AdvanceDataStreamIfNeeded();
				_readerOfOrigin = null;
			}
			_header._dataStream?.Dispose();
			_header._dataStream = value;
		}
	}

	internal TarEntry(TarHeader header, TarReader readerOfOrigin, TarEntryFormat format)
	{
		_header = header;
		_readerOfOrigin = readerOfOrigin;
	}

	internal TarEntry(TarEntryType entryType, string entryName, TarEntryFormat format, bool isGea)
	{
		ArgumentException.ThrowIfNullOrEmpty(entryName, "entryName");
		if (!isGea)
		{
			TarHelpers.ThrowIfEntryTypeNotSupported(entryType, format, "entryType");
		}
		_header = new TarHeader(format, entryName, TarHelpers.GetDefaultMode(entryType), DateTimeOffset.UtcNow, entryType);
	}

	internal TarEntry(TarEntry other, TarEntryFormat format)
	{
		if (other is PaxGlobalExtendedAttributesTarEntry)
		{
			throw new ArgumentException(System.SR.TarCannotConvertPaxGlobalExtendedAttributesEntry, "other");
		}
		TarEntryType correctTypeFlagForFormat = TarHelpers.GetCorrectTypeFlagForFormat(format, other.EntryType);
		TarHelpers.ThrowIfEntryTypeNotSupported(correctTypeFlagForFormat, format, "other");
		_readerOfOrigin = other._readerOfOrigin;
		_header = new TarHeader(format, correctTypeFlagForFormat, other._header);
	}

	public void ExtractToFile(string destinationFileName, bool overwrite)
	{
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		TarEntryType entryType = EntryType;
		if ((entryType - 49 <= (TarEntryType)1 || entryType == TarEntryType.GlobalExtendedAttributes) ? true : false)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.TarEntryTypeNotSupportedForExtracting, EntryType));
		}
		ExtractToFileInternal(destinationFileName, null, overwrite);
	}

	public Task ExtractToFileAsync(string destinationFileName, bool overwrite, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		TarEntryType entryType = EntryType;
		if ((entryType - 49 <= (TarEntryType)1 || entryType == TarEntryType.GlobalExtendedAttributes) ? true : false)
		{
			return Task.FromException(new InvalidOperationException(System.SR.Format(System.SR.TarEntryTypeNotSupportedForExtracting, EntryType)));
		}
		return ExtractToFileInternalAsync(destinationFileName, null, overwrite, cancellationToken);
	}

	public override string ToString()
	{
		return Name;
	}

	internal abstract bool IsDataStreamSetterSupported();

	internal void ExtractRelativeToDirectory(string destinationDirectoryPath, bool overwrite, SortedDictionary<string, UnixFileMode> pendingModes, Stack<(string, DateTimeOffset)> directoryModificationTimes)
	{
		var (text, linkTargetPath) = GetDestinationAndLinkPaths(destinationDirectoryPath);
		if (EntryType == TarEntryType.Directory)
		{
			TarHelpers.CreateDirectory(text, Mode, pendingModes);
			TarHelpers.UpdatePendingModificationTimes(directoryModificationTimes, text, ModificationTime);
		}
		else
		{
			TarHelpers.CreateDirectory(Path.GetDirectoryName(text), null, pendingModes);
			ExtractToFileInternal(text, linkTargetPath, overwrite);
		}
	}

	internal Task ExtractRelativeToDirectoryAsync(string destinationDirectoryPath, bool overwrite, SortedDictionary<string, UnixFileMode> pendingModes, Stack<(string, DateTimeOffset)> directoryModificationTimes, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		var (text, linkTargetPath) = GetDestinationAndLinkPaths(destinationDirectoryPath);
		if (EntryType == TarEntryType.Directory)
		{
			TarHelpers.CreateDirectory(text, Mode, pendingModes);
			TarHelpers.UpdatePendingModificationTimes(directoryModificationTimes, text, ModificationTime);
			return Task.CompletedTask;
		}
		TarHelpers.CreateDirectory(Path.GetDirectoryName(text), null, pendingModes);
		return ExtractToFileInternalAsync(text, linkTargetPath, overwrite, cancellationToken);
	}

	private (string, string) GetDestinationAndLinkPaths(string destinationDirectoryPath)
	{
		string text = ArchivingUtils.SanitizeEntryFilePath(Name, preserveDriveRoot: true);
		string fullDestinationPath = GetFullDestinationPath(destinationDirectoryPath, Path.IsPathFullyQualified(text) ? text : Path.Join(destinationDirectoryPath, text));
		if (fullDestinationPath == null)
		{
			throw new IOException(System.SR.Format(System.SR.TarExtractingResultsFileOutside, text, destinationDirectoryPath));
		}
		string item = null;
		if (EntryType == TarEntryType.SymbolicLink)
		{
			string text2 = ArchivingUtils.SanitizeEntryFilePath(LinkName, preserveDriveRoot: true);
			string fullDestinationPath2 = GetFullDestinationPath(destinationDirectoryPath, Path.IsPathFullyQualified(text2) ? text2 : Path.Join(Path.GetDirectoryName(fullDestinationPath), text2));
			if (fullDestinationPath2 == null)
			{
				throw new IOException(System.SR.Format(System.SR.TarExtractingResultsLinkOutside, text2, destinationDirectoryPath));
			}
			item = text2;
		}
		else if (EntryType == TarEntryType.HardLink)
		{
			string text3 = ArchivingUtils.SanitizeEntryFilePath(LinkName);
			string fullDestinationPath3 = GetFullDestinationPath(destinationDirectoryPath, Path.Join(destinationDirectoryPath, text3));
			if (fullDestinationPath3 == null)
			{
				throw new IOException(System.SR.Format(System.SR.TarExtractingResultsLinkOutside, text3, destinationDirectoryPath));
			}
			item = fullDestinationPath3;
		}
		return (fullDestinationPath, item);
	}

	private static string GetFullDestinationPath(string destinationDirectoryFullPath, string qualifiedPath)
	{
		string fullPath = Path.GetFullPath(qualifiedPath);
		if (!fullPath.StartsWith(destinationDirectoryFullPath, System.IO.PathInternal.StringComparison))
		{
			return null;
		}
		return fullPath;
	}

	private void ExtractToFileInternal(string filePath, string linkTargetPath, bool overwrite)
	{
		VerifyDestinationPath(filePath, overwrite);
		TarEntryType entryType = EntryType;
		if ((entryType == TarEntryType.V7RegularFile || entryType == TarEntryType.RegularFile || entryType == TarEntryType.ContiguousFile) ? true : false)
		{
			ExtractAsRegularFile(filePath);
		}
		else
		{
			CreateNonRegularFile(filePath, linkTargetPath);
		}
	}

	private Task ExtractToFileInternalAsync(string filePath, string linkTargetPath, bool overwrite, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		VerifyDestinationPath(filePath, overwrite);
		TarEntryType entryType = EntryType;
		if ((entryType == TarEntryType.V7RegularFile || entryType == TarEntryType.RegularFile || entryType == TarEntryType.ContiguousFile) ? true : false)
		{
			return ExtractAsRegularFileAsync(filePath, cancellationToken);
		}
		CreateNonRegularFile(filePath, linkTargetPath);
		return Task.CompletedTask;
	}

	private void CreateNonRegularFile(string filePath, string linkTargetPath)
	{
		switch (EntryType)
		{
		case TarEntryType.Directory:
		case TarEntryType.DirectoryList:
			if (!OperatingSystem.IsWindows())
			{
				Directory.CreateDirectory(filePath, Mode);
			}
			else
			{
				Directory.CreateDirectory(filePath);
			}
			break;
		case TarEntryType.SymbolicLink:
		{
			FileInfo fileInfo = new FileInfo(filePath);
			fileInfo.CreateAsSymbolicLink(linkTargetPath);
			break;
		}
		case TarEntryType.HardLink:
			ExtractAsHardLink(linkTargetPath, filePath);
			break;
		case TarEntryType.BlockDevice:
			ExtractAsBlockDevice(filePath);
			break;
		case TarEntryType.CharacterDevice:
			ExtractAsCharacterDevice(filePath);
			break;
		case TarEntryType.Fifo:
			ExtractAsFifo(filePath);
			break;
		default:
			throw new InvalidOperationException(System.SR.Format(System.SR.TarEntryTypeNotSupportedForExtracting, EntryType));
		case TarEntryType.LongLink:
		case TarEntryType.LongPath:
		case TarEntryType.GlobalExtendedAttributes:
		case TarEntryType.ExtendedAttributes:
			break;
		}
	}

	private static void VerifyDestinationPath(string filePath, bool overwrite)
	{
		string directoryName = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(directoryName) && !Path.Exists(directoryName))
		{
			throw new IOException(System.SR.Format(System.SR.IO_PathNotFound_Path, filePath));
		}
		if (Path.Exists(filePath))
		{
			if (Directory.Exists(filePath))
			{
				throw new IOException(System.SR.Format(System.SR.IO_AlreadyExists_Name, filePath));
			}
			if (!overwrite)
			{
				throw new IOException(System.SR.Format(System.SR.IO_AlreadyExists_Name, filePath));
			}
			File.Delete(filePath);
		}
	}

	private void ExtractAsRegularFile(string destinationFileName)
	{
		using (FileStream destination = new FileStream(destinationFileName, CreateFileStreamOptions(isAsync: false)))
		{
			DataStream?.CopyTo(destination);
		}
		AttemptSetLastWriteTime(destinationFileName, ModificationTime);
	}

	private async Task ExtractAsRegularFileAsync(string destinationFileName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		FileStream fileStream = new FileStream(destinationFileName, CreateFileStreamOptions(isAsync: true));
		ConfiguredAsyncDisposable configuredAsyncDisposable = fileStream.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (DataStream != null)
			{
				await DataStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
			if (asyncDisposable != null)
			{
				await asyncDisposable.DisposeAsync();
			}
		}
		AttemptSetLastWriteTime(destinationFileName, ModificationTime);
	}

	private static void AttemptSetLastWriteTime(string destinationFileName, DateTimeOffset lastWriteTime)
	{
		try
		{
			File.SetLastWriteTime(destinationFileName, lastWriteTime.UtcDateTime);
		}
		catch
		{
		}
	}

	private FileStreamOptions CreateFileStreamOptions(bool isAsync)
	{
		FileStreamOptions fileStreamOptions = new FileStreamOptions
		{
			Access = FileAccess.Write,
			Mode = FileMode.CreateNew,
			Share = FileShare.None,
			PreallocationSize = Length,
			Options = (isAsync ? FileOptions.Asynchronous : FileOptions.None)
		};
		if (!OperatingSystem.IsWindows())
		{
			fileStreamOptions.UnixCreateMode = Mode & (UnixFileMode.OtherExecute | UnixFileMode.OtherWrite | UnixFileMode.OtherRead | UnixFileMode.GroupExecute | UnixFileMode.GroupWrite | UnixFileMode.GroupRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead);
		}
		return fileStreamOptions;
	}

	private void ExtractAsBlockDevice(string destinationFileName)
	{
		throw new InvalidOperationException(System.SR.IO_DeviceFiles_NotSupported);
	}

	private void ExtractAsCharacterDevice(string destinationFileName)
	{
		throw new InvalidOperationException(System.SR.IO_DeviceFiles_NotSupported);
	}

	private void ExtractAsFifo(string destinationFileName)
	{
		throw new InvalidOperationException(System.SR.IO_FifoFiles_NotSupported);
	}

	private void ExtractAsHardLink(string targetFilePath, string hardLinkFilePath)
	{
		global::Interop.Kernel32.CreateHardLink(hardLinkFilePath, targetFilePath);
	}
}
