using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace System.IO;

public static class Directory
{
	public static DirectoryInfo? GetParent(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		string fullPath = Path.GetFullPath(path);
		string directoryName = Path.GetDirectoryName(fullPath);
		if (directoryName == null)
		{
			return null;
		}
		return new DirectoryInfo(directoryName);
	}

	public static DirectoryInfo CreateDirectory(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		string fullPath = Path.GetFullPath(path);
		FileSystem.CreateDirectory(fullPath);
		return new DirectoryInfo(path, fullPath, null, isNormalized: true);
	}

	[UnsupportedOSPlatform("windows")]
	public static DirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode)
	{
		return CreateDirectoryCore(path, unixCreateMode);
	}

	public static DirectoryInfo CreateTempSubdirectory(string? prefix = null)
	{
		EnsureNoDirectorySeparators(prefix, "prefix");
		string originalPath = CreateTempSubdirectoryCore(prefix);
		return new DirectoryInfo(originalPath, null, null, isNormalized: true);
	}

	private static void EnsureNoDirectorySeparators(string value, [CallerArgumentExpression("value")] string paramName = null)
	{
		if (value != null && value.AsSpan().ContainsAny("\\/"))
		{
			throw new ArgumentException(SR.Argument_DirectorySeparatorInvalid, paramName);
		}
	}

	public static bool Exists([NotNullWhen(true)] string? path)
	{
		try
		{
			if (path == null)
			{
				return false;
			}
			if (path.Length == 0)
			{
				return false;
			}
			string fullPath = Path.GetFullPath(path);
			return FileSystem.DirectoryExists(fullPath);
		}
		catch (ArgumentException)
		{
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return false;
	}

	public static void SetCreationTime(string path, DateTime creationTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, creationTime, asDirectory: true);
	}

	public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, File.GetUtcDateTimeOffset(creationTimeUtc), asDirectory: true);
	}

	public static DateTime GetCreationTime(string path)
	{
		return File.GetCreationTime(path);
	}

	public static DateTime GetCreationTimeUtc(string path)
	{
		return File.GetCreationTimeUtc(path);
	}

	public static void SetLastWriteTime(string path, DateTime lastWriteTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, lastWriteTime, asDirectory: true);
	}

	public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, File.GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: true);
	}

	public static DateTime GetLastWriteTime(string path)
	{
		return File.GetLastWriteTime(path);
	}

	public static DateTime GetLastWriteTimeUtc(string path)
	{
		return File.GetLastWriteTimeUtc(path);
	}

	public static void SetLastAccessTime(string path, DateTime lastAccessTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, lastAccessTime, asDirectory: true);
	}

	public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, File.GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: true);
	}

	public static DateTime GetLastAccessTime(string path)
	{
		return File.GetLastAccessTime(path);
	}

	public static DateTime GetLastAccessTimeUtc(string path)
	{
		return File.GetLastAccessTimeUtc(path);
	}

	public static string[] GetFiles(string path)
	{
		return GetFiles(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetFiles(string path, string searchPattern)
	{
		return GetFiles(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
	{
		return GetFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();
	}

	public static string[] GetDirectories(string path)
	{
		return GetDirectories(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetDirectories(string path, string searchPattern)
	{
		return GetDirectories(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
	{
		return GetDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();
	}

	public static string[] GetFileSystemEntries(string path)
	{
		return GetFileSystemEntries(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern)
	{
		return GetFileSystemEntries(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
	{
		return GetFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions)).ToArray();
	}

	internal static IEnumerable<string> InternalEnumeratePaths(string path, string searchPattern, SearchTarget searchTarget, EnumerationOptions options)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		ArgumentNullException.ThrowIfNull(searchPattern, "searchPattern");
		FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options.MatchType);
		return searchTarget switch
		{
			SearchTarget.Files => FileSystemEnumerableFactory.UserFiles(path, searchPattern, options), 
			SearchTarget.Directories => FileSystemEnumerableFactory.UserDirectories(path, searchPattern, options), 
			SearchTarget.Both => FileSystemEnumerableFactory.UserEntries(path, searchPattern, options), 
			_ => throw new ArgumentOutOfRangeException("searchTarget"), 
		};
	}

	public static IEnumerable<string> EnumerateDirectories(string path)
	{
		return EnumerateDirectories(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
	{
		return EnumerateDirectories(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions);
	}

	public static IEnumerable<string> EnumerateFiles(string path)
	{
		return EnumerateFiles(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
	{
		return EnumerateFiles(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path)
	{
		return EnumerateFileSystemEntries(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
	{
		return EnumerateFileSystemEntries(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions);
	}

	public static string GetDirectoryRoot(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		string fullPath = Path.GetFullPath(path);
		return Path.GetPathRoot(fullPath);
	}

	public static string GetCurrentDirectory()
	{
		return Environment.CurrentDirectory;
	}

	public static void SetCurrentDirectory(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		Environment.CurrentDirectory = Path.GetFullPath(path);
	}

	public static void Move(string sourceDirName, string destDirName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceDirName, "sourceDirName");
		ArgumentException.ThrowIfNullOrEmpty(destDirName, "destDirName");
		FileSystem.MoveDirectory(Path.GetFullPath(sourceDirName), Path.GetFullPath(destDirName));
	}

	public static void Delete(string path)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.RemoveDirectory(fullPath, recursive: false);
	}

	public static void Delete(string path, bool recursive)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.RemoveDirectory(fullPath, recursive);
	}

	public static string[] GetLogicalDrives()
	{
		return FileSystem.GetLogicalDrives();
	}

	public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.VerifyValidPath(pathToTarget, "pathToTarget");
		FileSystem.CreateSymbolicLink(path, pathToTarget, isDirectory: true);
		return new DirectoryInfo(path, fullPath, null, isNormalized: true);
	}

	public static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget)
	{
		FileSystem.VerifyValidPath(linkPath, "linkPath");
		return FileSystem.ResolveLinkTarget(linkPath, returnFinalTarget, isDirectory: true);
	}

	private static DirectoryInfo CreateDirectoryCore(string path, UnixFileMode unixCreateMode)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
	}

	private unsafe static string CreateTempSubdirectoryCore(string prefix)
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
		Path.GetTempPath(ref builder);
		CreateDirectory(PathHelper.Normalize(ref builder));
		builder.Append(prefix);
		int length = builder.Length;
		builder.EnsureCapacity(length + 12);
		byte* ptr = stackalloc byte[8];
		for (int i = 0; i < 65535; i++)
		{
			Interop.GetRandomBytes(ptr, 8);
			Path.Populate83FileNameFromRandomBytes(ptr, 8, builder.RawChars.Slice(builder.Length, 12));
			builder.Length += 12;
			string text = PathHelper.Normalize(ref builder);
			if (!Interop.Kernel32.CreateDirectory(text, null))
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				if (lastPInvokeError == 183)
				{
					builder.Length = length;
					continue;
				}
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, text);
			}
			builder.Dispose();
			return text;
		}
		throw new IOException(SR.IO_MaxAttemptsReached);
	}
}
