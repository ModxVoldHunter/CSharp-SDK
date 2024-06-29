using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

public static class TarFile
{
	public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName, "sourceDirectoryName");
		ArgumentNullException.ThrowIfNull(destination, "destination");
		if (!destination.CanWrite)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnwritableStream, "destination");
		}
		if (!Directory.Exists(sourceDirectoryName))
		{
			throw new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, sourceDirectoryName));
		}
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		CreateFromDirectoryInternal(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: true);
	}

	public static Task CreateFromDirectoryAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName, "sourceDirectoryName");
		ArgumentNullException.ThrowIfNull(destination, "destination");
		if (!destination.CanWrite)
		{
			return Task.FromException(new ArgumentException(System.SR.IO_NotSupported_UnwritableStream, "destination"));
		}
		if (!Directory.Exists(sourceDirectoryName))
		{
			return Task.FromException(new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, sourceDirectoryName)));
		}
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		return CreateFromDirectoryInternalAsync(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: true, cancellationToken);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName, "sourceDirectoryName");
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		destinationFileName = Path.GetFullPath(destinationFileName);
		if (!Directory.Exists(sourceDirectoryName))
		{
			throw new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, sourceDirectoryName));
		}
		using FileStream destination = new FileStream(destinationFileName, FileMode.CreateNew, FileAccess.Write);
		CreateFromDirectoryInternal(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: false);
	}

	public static Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName, "sourceDirectoryName");
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		destinationFileName = Path.GetFullPath(destinationFileName);
		if (!Directory.Exists(sourceDirectoryName))
		{
			return Task.FromException(new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, sourceDirectoryName)));
		}
		return CreateFromDirectoryInternalAsync(sourceDirectoryName, destinationFileName, includeBaseDirectory, cancellationToken);
	}

	public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName, "destinationDirectoryName");
		if (!source.CanRead)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnreadableStream, "source");
		}
		if (!Directory.Exists(destinationDirectoryName))
		{
			throw new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, destinationDirectoryName));
		}
		destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);
		destinationDirectoryName = System.IO.PathInternal.EnsureTrailingSeparator(destinationDirectoryName);
		ExtractToDirectoryInternal(source, destinationDirectoryName, overwriteFiles, leaveOpen: true);
	}

	public static Task ExtractToDirectoryAsync(Stream source, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName, "destinationDirectoryName");
		if (!source.CanRead)
		{
			return Task.FromException(new ArgumentException(System.SR.IO_NotSupported_UnreadableStream, "source"));
		}
		if (!Directory.Exists(destinationDirectoryName))
		{
			return Task.FromException(new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, destinationDirectoryName)));
		}
		destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);
		destinationDirectoryName = System.IO.PathInternal.EnsureTrailingSeparator(destinationDirectoryName);
		return ExtractToDirectoryInternalAsync(source, destinationDirectoryName, overwriteFiles, leaveOpen: true, cancellationToken);
	}

	public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwriteFiles)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName, "destinationDirectoryName");
		sourceFileName = Path.GetFullPath(sourceFileName);
		destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);
		destinationDirectoryName = System.IO.PathInternal.EnsureTrailingSeparator(destinationDirectoryName);
		if (!File.Exists(sourceFileName))
		{
			throw new FileNotFoundException(System.SR.Format(System.SR.IO_FileNotFound_FileName, sourceFileName));
		}
		if (!Directory.Exists(destinationDirectoryName))
		{
			throw new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, destinationDirectoryName));
		}
		using FileStream source = File.OpenRead(sourceFileName);
		ExtractToDirectoryInternal(source, destinationDirectoryName, overwriteFiles, leaveOpen: false);
	}

	public static Task ExtractToDirectoryAsync(string sourceFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName, "destinationDirectoryName");
		sourceFileName = Path.GetFullPath(sourceFileName);
		destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);
		destinationDirectoryName = System.IO.PathInternal.EnsureTrailingSeparator(destinationDirectoryName);
		if (!File.Exists(sourceFileName))
		{
			return Task.FromException(new FileNotFoundException(System.SR.Format(System.SR.IO_FileNotFound_FileName, sourceFileName)));
		}
		if (!Directory.Exists(destinationDirectoryName))
		{
			return Task.FromException(new DirectoryNotFoundException(System.SR.Format(System.SR.IO_PathNotFound_Path, destinationDirectoryName)));
		}
		return ExtractToDirectoryInternalAsync(sourceFileName, destinationDirectoryName, overwriteFiles, cancellationToken);
	}

	private static void CreateFromDirectoryInternal(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool leaveOpen)
	{
		using TarWriter tarWriter = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen);
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
		bool flag = false;
		if (includeBaseDirectory)
		{
			tarWriter.WriteEntry(directoryInfo.FullName, GetEntryNameForBaseDirectory(directoryInfo.Name));
			flag = (directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0;
		}
		if (flag)
		{
			return;
		}
		string basePathForCreateFromDirectory = GetBasePathForCreateFromDirectory(directoryInfo, includeBaseDirectory);
		foreach (var (fileName, entryName) in GetFilesForCreation(sourceDirectoryName, basePathForCreateFromDirectory.Length))
		{
			tarWriter.WriteEntry(fileName, entryName);
		}
	}

	private static async Task CreateFromDirectoryInternalAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		FileStreamOptions options = new FileStreamOptions
		{
			Access = FileAccess.Write,
			Mode = FileMode.CreateNew,
			Options = FileOptions.Asynchronous
		};
		FileStream fileStream = new FileStream(destinationFileName, options);
		ConfiguredAsyncDisposable configuredAsyncDisposable = fileStream.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await CreateFromDirectoryInternalAsync(sourceDirectoryName, fileStream, includeBaseDirectory, leaveOpen: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
			if (asyncDisposable != null)
			{
				await asyncDisposable.DisposeAsync();
			}
		}
	}

	private static async Task CreateFromDirectoryInternalAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool leaveOpen, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen);
		ConfiguredAsyncDisposable configuredAsyncDisposable = writer.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			DirectoryInfo di = new DirectoryInfo(sourceDirectoryName);
			bool flag = false;
			if (includeBaseDirectory)
			{
				await writer.WriteEntryAsync(di.FullName, GetEntryNameForBaseDirectory(di.Name), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				flag = (di.Attributes & FileAttributes.ReparsePoint) != 0;
			}
			if (flag)
			{
				return;
			}
			string basePathForCreateFromDirectory = GetBasePathForCreateFromDirectory(di, includeBaseDirectory);
			foreach (var (fileName, entryName) in GetFilesForCreation(sourceDirectoryName, basePathForCreateFromDirectory.Length))
			{
				await writer.WriteEntryAsync(fileName, entryName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
	}

	private static IEnumerable<(string fullpath, string entryname)> GetFilesForCreation(string sourceDirectoryName, int basePathLength)
	{
		FileSystemEnumerable<(string, string, bool)> fileSystemEnumerable = new FileSystemEnumerable<(string, string, bool)>(sourceDirectoryName, delegate(ref FileSystemEntry entry)
		{
			string text = entry.ToFullPath();
			bool flag = entry.IsDirectory && (entry.Attributes & FileAttributes.ReparsePoint) == 0;
			string item2 = ArchivingUtils.EntryFromPath(text.AsSpan(basePathLength), flag);
			return (fullpath: text, entryname: item2, recurse: flag);
		});
		foreach (var (fullpath, item, recurse) in fileSystemEnumerable)
		{
			yield return (fullpath: fullpath, entryname: item);
			if (!recurse)
			{
				continue;
			}
			foreach (var item3 in GetFilesForCreation(fullpath, basePathLength))
			{
				yield return item3;
			}
		}
	}

	private static string GetBasePathForCreateFromDirectory(DirectoryInfo di, bool includeBaseDirectory)
	{
		if (!includeBaseDirectory || di.Parent == null)
		{
			return di.FullName;
		}
		return di.Parent.FullName;
	}

	private static string GetEntryNameForBaseDirectory(string name)
	{
		return ArchivingUtils.EntryFromPath(name, appendPathSeparator: true);
	}

	private static void ExtractToDirectoryInternal(Stream source, string destinationDirectoryFullPath, bool overwriteFiles, bool leaveOpen)
	{
		using TarReader tarReader = new TarReader(source, leaveOpen);
		SortedDictionary<string, UnixFileMode> pendingModes = null;
		Stack<(string, DateTimeOffset)> stack = new Stack<(string, DateTimeOffset)>();
		TarEntry nextEntry;
		while ((nextEntry = tarReader.GetNextEntry()) != null)
		{
			if (nextEntry.EntryType != TarEntryType.GlobalExtendedAttributes)
			{
				nextEntry.ExtractRelativeToDirectory(destinationDirectoryFullPath, overwriteFiles, pendingModes, stack);
			}
		}
		TarHelpers.SetPendingModes(pendingModes);
		TarHelpers.SetPendingModificationTimes(stack);
	}

	private static async Task ExtractToDirectoryInternalAsync(string sourceFileName, string destinationDirectoryFullPath, bool overwriteFiles, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		FileStreamOptions options = new FileStreamOptions
		{
			Access = FileAccess.Read,
			Mode = FileMode.Open,
			Options = FileOptions.Asynchronous
		};
		FileStream source = new FileStream(sourceFileName, options);
		ConfiguredAsyncDisposable configuredAsyncDisposable = source.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await ExtractToDirectoryInternalAsync(source, destinationDirectoryFullPath, overwriteFiles, leaveOpen: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
			if (asyncDisposable != null)
			{
				await asyncDisposable.DisposeAsync();
			}
		}
	}

	private static async Task ExtractToDirectoryInternalAsync(Stream source, string destinationDirectoryFullPath, bool overwriteFiles, bool leaveOpen, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		SortedDictionary<string, UnixFileMode> pendingModes = null;
		Stack<(string, DateTimeOffset)> directoryModificationTimes = new Stack<(string, DateTimeOffset)>();
		TarReader reader = new TarReader(source, leaveOpen);
		ConfiguredAsyncDisposable configuredAsyncDisposable = reader.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			TarEntry tarEntry;
			while ((tarEntry = await reader.GetNextEntryAsync(copyData: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) != null)
			{
				if (tarEntry.EntryType != TarEntryType.GlobalExtendedAttributes)
				{
					await tarEntry.ExtractRelativeToDirectoryAsync(destinationDirectoryFullPath, overwriteFiles, pendingModes, directoryModificationTimes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
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
		TarHelpers.SetPendingModes(pendingModes);
		TarHelpers.SetPendingModificationTimes(directoryModificationTimes);
	}
}
