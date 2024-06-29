using System.ComponentModel;

namespace System.IO.Compression;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ZipFileExtensions
{
	public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName)
	{
		return destination.DoCreateEntryFromFile(sourceFileName, entryName, null);
	}

	public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel compressionLevel)
	{
		return destination.DoCreateEntryFromFile(sourceFileName, entryName, compressionLevel);
	}

	internal static ZipArchiveEntry DoCreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel? compressionLevel)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		ArgumentNullException.ThrowIfNull(sourceFileName, "sourceFileName");
		ArgumentNullException.ThrowIfNull(entryName, "entryName");
		using FileStream fileStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
		ZipArchiveEntry zipArchiveEntry = (compressionLevel.HasValue ? destination.CreateEntry(entryName, compressionLevel.Value) : destination.CreateEntry(entryName));
		DateTime dateTime = File.GetLastWriteTime(sourceFileName);
		if (dateTime.Year < 1980 || dateTime.Year > 2107)
		{
			dateTime = new DateTime(1980, 1, 1, 0, 0, 0);
		}
		zipArchiveEntry.LastWriteTime = dateTime;
		using (Stream destination2 = zipArchiveEntry.Open())
		{
			fileStream.CopyTo(destination2);
		}
		return zipArchiveEntry;
	}

	public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName)
	{
		source.ExtractToDirectory(destinationDirectoryName, overwriteFiles: false);
	}

	public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwriteFiles)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(destinationDirectoryName, "destinationDirectoryName");
		foreach (ZipArchiveEntry entry in source.Entries)
		{
			entry.ExtractRelativeToDirectory(destinationDirectoryName, overwriteFiles);
		}
	}

	public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName)
	{
		source.ExtractToFile(destinationFileName, overwrite: false);
	}

	public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(destinationFileName, "destinationFileName");
		FileStreamOptions fileStreamOptions = new FileStreamOptions
		{
			Access = FileAccess.Write,
			Mode = ((!overwrite) ? FileMode.CreateNew : FileMode.Create),
			Share = FileShare.None,
			BufferSize = 4096
		};
		UnixFileMode unixFileMode = (UnixFileMode)((source.ExternalAttributes >> 16) & 0x1FF);
		if (unixFileMode != 0 && !OperatingSystem.IsWindows())
		{
			fileStreamOptions.UnixCreateMode = unixFileMode;
		}
		using (FileStream destination = new FileStream(destinationFileName, fileStreamOptions))
		{
			using Stream stream = source.Open();
			stream.CopyTo(destination);
		}
		ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, source.LastWriteTime);
	}

	internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(destinationDirectoryName, "destinationDirectoryName");
		DirectoryInfo directoryInfo = Directory.CreateDirectory(destinationDirectoryName);
		string text = directoryInfo.FullName;
		if (!text.EndsWith(Path.DirectorySeparatorChar))
		{
			char reference = Path.DirectorySeparatorChar;
			text += new ReadOnlySpan<char>(ref reference);
		}
		string fullPath = Path.GetFullPath(Path.Combine(text, ArchivingUtils.SanitizeEntryFilePath(source.FullName)));
		if (!fullPath.StartsWith(text, System.IO.PathInternal.StringComparison))
		{
			throw new IOException(System.SR.IO_ExtractingResultsInOutside);
		}
		if (Path.GetFileName(fullPath).Length == 0)
		{
			if (source.Length != 0L)
			{
				throw new IOException(System.SR.IO_DirectoryNameWithData);
			}
			Directory.CreateDirectory(fullPath);
		}
		else
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			source.ExtractToFile(fullPath, overwrite);
		}
	}
}
