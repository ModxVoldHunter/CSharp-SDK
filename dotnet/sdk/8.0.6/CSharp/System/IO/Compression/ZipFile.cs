using System.IO.Enumeration;
using System.Text;

namespace System.IO.Compression;

public static class ZipFile
{
	private enum CreateEntryType
	{
		File,
		Directory,
		Unsupported
	}

	public static ZipArchive OpenRead(string archiveFileName)
	{
		return Open(archiveFileName, ZipArchiveMode.Read);
	}

	public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode)
	{
		return Open(archiveFileName, mode, null);
	}

	public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode, Encoding? entryNameEncoding)
	{
		FileMode mode2;
		FileAccess access;
		FileShare share;
		switch (mode)
		{
		case ZipArchiveMode.Read:
			mode2 = FileMode.Open;
			access = FileAccess.Read;
			share = FileShare.Read;
			break;
		case ZipArchiveMode.Create:
			mode2 = FileMode.CreateNew;
			access = FileAccess.Write;
			share = FileShare.None;
			break;
		case ZipArchiveMode.Update:
			mode2 = FileMode.OpenOrCreate;
			access = FileAccess.ReadWrite;
			share = FileShare.None;
			break;
		default:
			throw new ArgumentOutOfRangeException("mode");
		}
		FileStream fileStream = new FileStream(archiveFileName, mode2, access, share, 4096, useAsync: false);
		try
		{
			return new ZipArchive(fileStream, mode, leaveOpen: false, entryNameEncoding);
		}
		catch
		{
			fileStream.Dispose();
			throw;
		}
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, null, includeBaseDirectory: false, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, Stream destination)
	{
		DoCreateFromDirectory(sourceDirectoryName, destination, null, includeBaseDirectory: false, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, CompressionLevel compressionLevel, bool includeBaseDirectory)
	{
		DoCreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
	{
		DoCreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, entryNameEncoding);
	}

	private static void DoCreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding entryNameEncoding)
	{
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);
		using ZipArchive archive = Open(destinationArchiveFileName, ZipArchiveMode.Create, entryNameEncoding);
		CreateZipArchiveFromDirectory(sourceDirectoryName, archive, compressionLevel, includeBaseDirectory);
	}

	private static void DoCreateFromDirectory(string sourceDirectoryName, Stream destination, CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding entryNameEncoding)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		if (!destination.CanWrite)
		{
			throw new ArgumentException(System.SR.UnwritableStream, "destination");
		}
		if (compressionLevel.HasValue && !Enum.IsDefined(compressionLevel.Value))
		{
			throw new ArgumentOutOfRangeException("compressionLevel");
		}
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		using ZipArchive archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding);
		CreateZipArchiveFromDirectory(sourceDirectoryName, archive, compressionLevel, includeBaseDirectory);
	}

	private static void CreateZipArchiveFromDirectory(string sourceDirectoryName, ZipArchive archive, CompressionLevel? compressionLevel, bool includeBaseDirectory)
	{
		bool flag = true;
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
		string fullName = directoryInfo.FullName;
		if (includeBaseDirectory && directoryInfo.Parent != null)
		{
			fullName = directoryInfo.Parent.FullName;
		}
		FileSystemEnumerable<(string, CreateEntryType)> fileSystemEnumerable = CreateEnumerableForCreate(directoryInfo.FullName);
		foreach (var item3 in fileSystemEnumerable)
		{
			string item = item3.Item1;
			CreateEntryType item2 = item3.Item2;
			flag = false;
			switch (item2)
			{
			case CreateEntryType.File:
			{
				string entryName2 = ArchivingUtils.EntryFromPath(item.AsSpan(fullName.Length));
				archive.DoCreateEntryFromFile(item, entryName2, compressionLevel);
				break;
			}
			case CreateEntryType.Directory:
				if (ArchivingUtils.IsDirEmpty(item))
				{
					string entryName = ArchivingUtils.EntryFromPath(item.AsSpan(fullName.Length), appendPathSeparator: true);
					archive.CreateEntry(entryName);
				}
				break;
			default:
				throw new IOException(System.SR.Format(System.SR.ZipUnsupportedFile, item));
			}
		}
		if (includeBaseDirectory && flag)
		{
			archive.CreateEntry(ArchivingUtils.EntryFromPath(directoryInfo.Name, appendPathSeparator: true));
		}
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, null, overwriteFiles: false);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, null, overwriteFiles);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding, overwriteFiles: false);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
	{
		ArgumentNullException.ThrowIfNull(sourceArchiveFileName, "sourceArchiveFileName");
		using ZipArchive source = Open(sourceArchiveFileName, ZipArchiveMode.Read, entryNameEncoding);
		source.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
	}

	public static void ExtractToDirectory(Stream source, string destinationDirectoryName)
	{
		ExtractToDirectory(source, destinationDirectoryName, null, overwriteFiles: false);
	}

	public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles)
	{
		ExtractToDirectory(source, destinationDirectoryName, null, overwriteFiles);
	}

	public static void ExtractToDirectory(Stream source, string destinationDirectoryName, Encoding? entryNameEncoding)
	{
		ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding, overwriteFiles: false);
	}

	public static void ExtractToDirectory(Stream source, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.UnreadableStream, "source");
		}
		using ZipArchive source2 = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding);
		source2.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
	}

	private static FileSystemEnumerable<(string, CreateEntryType)> CreateEnumerableForCreate(string directoryFullPath)
	{
		return new FileSystemEnumerable<(string, CreateEntryType)>(directoryFullPath, delegate(ref FileSystemEntry entry)
		{
			return (entry.ToFullPath(), entry.IsDirectory ? CreateEntryType.Directory : CreateEntryType.File);
		}, new EnumerationOptions
		{
			RecurseSubdirectories = true,
			AttributesToSkip = FileAttributes.None,
			IgnoreInaccessible = false
		});
	}
}
