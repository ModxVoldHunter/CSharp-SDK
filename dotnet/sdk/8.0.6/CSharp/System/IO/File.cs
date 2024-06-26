using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Strategies;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public static class File
{
	private static Encoding s_UTF8NoBOM;

	private static Encoding UTF8NoBOM => s_UTF8NoBOM ?? (s_UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));

	public static StreamReader OpenText(string path)
	{
		return new StreamReader(path);
	}

	public static StreamWriter CreateText(string path)
	{
		return new StreamWriter(path, append: false);
	}

	public static StreamWriter AppendText(string path)
	{
		return new StreamWriter(path, append: true);
	}

	public static void Copy(string sourceFileName, string destFileName)
	{
		Copy(sourceFileName, destFileName, overwrite: false);
	}

	public static void Copy(string sourceFileName, string destFileName, bool overwrite)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destFileName, "destFileName");
		FileSystem.CopyFile(Path.GetFullPath(sourceFileName), Path.GetFullPath(destFileName), overwrite);
	}

	public static FileStream Create(string path)
	{
		return Create(path, 4096);
	}

	public static FileStream Create(string path, int bufferSize)
	{
		return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
	}

	public static FileStream Create(string path, int bufferSize, FileOptions options)
	{
		return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
	}

	public static void Delete(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		FileSystem.DeleteFile(Path.GetFullPath(path));
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
			path = Path.GetFullPath(path);
			if (path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]))
			{
				return false;
			}
			return FileSystem.FileExists(path);
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

	public static FileStream Open(string path, FileStreamOptions options)
	{
		return new FileStream(path, options);
	}

	public static FileStream Open(string path, FileMode mode)
	{
		return Open(path, mode, (mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None);
	}

	public static FileStream Open(string path, FileMode mode, FileAccess access)
	{
		return Open(path, mode, access, FileShare.None);
	}

	public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return new FileStream(path, mode, access, share);
	}

	public static SafeFileHandle OpenHandle(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read, FileOptions options = FileOptions.None, long preallocationSize = 0L)
	{
		FileStreamHelpers.ValidateArguments(path, mode, access, share, 0, options, preallocationSize);
		return SafeFileHandle.Open(Path.GetFullPath(path), mode, access, share, options, preallocationSize);
	}

	internal static DateTimeOffset GetUtcDateTimeOffset(DateTime dateTime)
	{
		return (dateTime.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime();
	}

	public static void SetCreationTime(string path, DateTime creationTime)
	{
		FileSystem.SetCreationTime(Path.GetFullPath(path), creationTime, asDirectory: false);
	}

	public static void SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetCreationTime(fileHandle, creationTime);
	}

	public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
	{
		FileSystem.SetCreationTime(Path.GetFullPath(path), GetUtcDateTimeOffset(creationTimeUtc), asDirectory: false);
	}

	public static void SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetCreationTime(fileHandle, GetUtcDateTimeOffset(creationTimeUtc));
	}

	public static DateTime GetCreationTime(string path)
	{
		return FileSystem.GetCreationTime(Path.GetFullPath(path)).LocalDateTime;
	}

	public static DateTime GetCreationTime(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetCreationTime(fileHandle).LocalDateTime;
	}

	public static DateTime GetCreationTimeUtc(string path)
	{
		return FileSystem.GetCreationTime(Path.GetFullPath(path)).UtcDateTime;
	}

	public static DateTime GetCreationTimeUtc(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetCreationTime(fileHandle).UtcDateTime;
	}

	public static void SetLastAccessTime(string path, DateTime lastAccessTime)
	{
		FileSystem.SetLastAccessTime(Path.GetFullPath(path), lastAccessTime, asDirectory: false);
	}

	public static void SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetLastAccessTime(fileHandle, lastAccessTime);
	}

	public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
	{
		FileSystem.SetLastAccessTime(Path.GetFullPath(path), GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: false);
	}

	public static void SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetLastAccessTime(fileHandle, GetUtcDateTimeOffset(lastAccessTimeUtc));
	}

	public static DateTime GetLastAccessTime(string path)
	{
		return FileSystem.GetLastAccessTime(Path.GetFullPath(path)).LocalDateTime;
	}

	public static DateTime GetLastAccessTime(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetLastAccessTime(fileHandle).LocalDateTime;
	}

	public static DateTime GetLastAccessTimeUtc(string path)
	{
		return FileSystem.GetLastAccessTime(Path.GetFullPath(path)).UtcDateTime;
	}

	public static DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetLastAccessTime(fileHandle).UtcDateTime;
	}

	public static void SetLastWriteTime(string path, DateTime lastWriteTime)
	{
		FileSystem.SetLastWriteTime(Path.GetFullPath(path), lastWriteTime, asDirectory: false);
	}

	public static void SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetLastWriteTime(fileHandle, lastWriteTime);
	}

	public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
	{
		FileSystem.SetLastWriteTime(Path.GetFullPath(path), GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: false);
	}

	public static void SetLastWriteTimeUtc(SafeFileHandle fileHandle, DateTime lastWriteTimeUtc)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetLastWriteTime(fileHandle, GetUtcDateTimeOffset(lastWriteTimeUtc));
	}

	public static DateTime GetLastWriteTime(string path)
	{
		return FileSystem.GetLastWriteTime(Path.GetFullPath(path)).LocalDateTime;
	}

	public static DateTime GetLastWriteTime(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetLastWriteTime(fileHandle).LocalDateTime;
	}

	public static DateTime GetLastWriteTimeUtc(string path)
	{
		return FileSystem.GetLastWriteTime(Path.GetFullPath(path)).UtcDateTime;
	}

	public static DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetLastWriteTime(fileHandle).UtcDateTime;
	}

	public static FileAttributes GetAttributes(string path)
	{
		return FileSystem.GetAttributes(Path.GetFullPath(path));
	}

	public static FileAttributes GetAttributes(SafeFileHandle fileHandle)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		return FileSystem.GetAttributes(fileHandle);
	}

	public static void SetAttributes(string path, FileAttributes fileAttributes)
	{
		FileSystem.SetAttributes(Path.GetFullPath(path), fileAttributes);
	}

	public static void SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		FileSystem.SetAttributes(fileHandle, fileAttributes);
	}

	[UnsupportedOSPlatform("windows")]
	public static UnixFileMode GetUnixFileMode(string path)
	{
		return GetUnixFileModeCore(path);
	}

	[UnsupportedOSPlatform("windows")]
	public static UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle)
	{
		return GetUnixFileModeCore(fileHandle);
	}

	[UnsupportedOSPlatform("windows")]
	public static void SetUnixFileMode(string path, UnixFileMode mode)
	{
		SetUnixFileModeCore(path, mode);
	}

	[UnsupportedOSPlatform("windows")]
	public static void SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode)
	{
		SetUnixFileModeCore(fileHandle, mode);
	}

	public static FileStream OpenRead(string path)
	{
		return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
	}

	public static FileStream OpenWrite(string path)
	{
		return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
	}

	public static string ReadAllText(string path)
	{
		return ReadAllText(path, Encoding.UTF8);
	}

	public static string ReadAllText(string path, Encoding encoding)
	{
		Validate(path, encoding);
		using StreamReader streamReader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true);
		return streamReader.ReadToEnd();
	}

	public static void WriteAllText(string path, string? contents)
	{
		WriteAllText(path, contents, UTF8NoBOM);
	}

	public static void WriteAllText(string path, string? contents, Encoding encoding)
	{
		Validate(path, encoding);
		WriteToFile(path, FileMode.Create, contents, encoding);
	}

	public static byte[] ReadAllBytes(string path)
	{
		if (1 == 0)
		{
		}
		FileOptions options = FileOptions.SequentialScan;
		using SafeFileHandle safeFileHandle = OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, options, 0L);
		long num = 0L;
		if (safeFileHandle.CanSeek && (num = safeFileHandle.GetFileLength()) > 2147483591)
		{
			throw new IOException(SR.IO_FileTooLong2GB);
		}
		if (num == 0L)
		{
			return ReadAllBytesUnknownLength(safeFileHandle);
		}
		int num2 = 0;
		int num3 = (int)num;
		byte[] array = new byte[num3];
		while (num3 > 0)
		{
			int num4 = RandomAccess.ReadAtOffset(safeFileHandle, array.AsSpan(num2, num3), num2);
			if (num4 == 0)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			num2 += num4;
			num3 -= num4;
		}
		return array;
	}

	public static void WriteAllBytes(string path, byte[] bytes)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		using SafeFileHandle handle = OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.None, 0L);
		RandomAccess.WriteAtOffset(handle, bytes, 0L);
	}

	public static string[] ReadAllLines(string path)
	{
		return ReadAllLines(path, Encoding.UTF8);
	}

	public static string[] ReadAllLines(string path, Encoding encoding)
	{
		Validate(path, encoding);
		List<string> list = new List<string>();
		using StreamReader streamReader = new StreamReader(path, encoding);
		string item;
		while ((item = streamReader.ReadLine()) != null)
		{
			list.Add(item);
		}
		return list.ToArray();
	}

	public static IEnumerable<string> ReadLines(string path)
	{
		return ReadLines(path, Encoding.UTF8);
	}

	public static IEnumerable<string> ReadLines(string path, Encoding encoding)
	{
		Validate(path, encoding);
		return ReadLinesIterator.CreateIterator(path, encoding);
	}

	public static IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReadLinesAsync(path, Encoding.UTF8, cancellationToken);
	}

	public static IAsyncEnumerable<string> ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		StreamReader sr = AsyncStreamReader(path, encoding);
		return IterateFileLinesAsync(sr, path, encoding, cancellationToken);
	}

	public static void WriteAllLines(string path, string[] contents)
	{
		WriteAllLines(path, (IEnumerable<string>)contents);
	}

	public static void WriteAllLines(string path, IEnumerable<string> contents)
	{
		WriteAllLines(path, contents, UTF8NoBOM);
	}

	public static void WriteAllLines(string path, string[] contents, Encoding encoding)
	{
		WriteAllLines(path, (IEnumerable<string>)contents, encoding);
	}

	public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
	{
		Validate(path, encoding);
		ArgumentNullException.ThrowIfNull(contents, "contents");
		InternalWriteAllLines(new StreamWriter(path, append: false, encoding), contents);
	}

	private static void InternalWriteAllLines(StreamWriter writer, IEnumerable<string> contents)
	{
		using (writer)
		{
			foreach (string content in contents)
			{
				writer.WriteLine(content);
			}
		}
	}

	public static void AppendAllText(string path, string? contents)
	{
		AppendAllText(path, contents, UTF8NoBOM);
	}

	public static void AppendAllText(string path, string? contents, Encoding encoding)
	{
		Validate(path, encoding);
		WriteToFile(path, FileMode.Append, contents, encoding);
	}

	public static void AppendAllLines(string path, IEnumerable<string> contents)
	{
		AppendAllLines(path, contents, UTF8NoBOM);
	}

	public static void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)
	{
		Validate(path, encoding);
		ArgumentNullException.ThrowIfNull(contents, "contents");
		InternalWriteAllLines(new StreamWriter(path, append: true, encoding), contents);
	}

	public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName)
	{
		Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);
	}

	public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors)
	{
		ArgumentNullException.ThrowIfNull(sourceFileName, "sourceFileName");
		ArgumentNullException.ThrowIfNull(destinationFileName, "destinationFileName");
		FileSystem.ReplaceFile(Path.GetFullPath(sourceFileName), Path.GetFullPath(destinationFileName), (destinationBackupFileName != null) ? Path.GetFullPath(destinationBackupFileName) : null, ignoreMetadataErrors);
	}

	public static void Move(string sourceFileName, string destFileName)
	{
		Move(sourceFileName, destFileName, overwrite: false);
	}

	public static void Move(string sourceFileName, string destFileName, bool overwrite)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destFileName, "destFileName");
		string fullPath = Path.GetFullPath(sourceFileName);
		string fullPath2 = Path.GetFullPath(destFileName);
		if (!FileSystem.FileExists(fullPath))
		{
			throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, fullPath), fullPath);
		}
		FileSystem.MoveFile(fullPath, fullPath2, overwrite);
	}

	[SupportedOSPlatform("windows")]
	public static void Encrypt(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		FileSystem.Encrypt(path);
	}

	[SupportedOSPlatform("windows")]
	public static void Decrypt(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		FileSystem.Decrypt(path);
	}

	private static StreamReader AsyncStreamReader(string path, Encoding encoding)
	{
		return new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan), encoding, detectEncodingFromByteOrderMarks: true);
	}

	private static StreamWriter AsyncStreamWriter(string path, Encoding encoding, bool append)
	{
		return new StreamWriter(new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous), encoding);
	}

	public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
	}

	public static Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalReadAllTextAsync(path, encoding, cancellationToken);
		}
		return Task.FromCanceled<string>(cancellationToken);
	}

	private static async Task<string> InternalReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken)
	{
		char[] buffer = null;
		StreamReader sr = AsyncStreamReader(path, encoding);
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			buffer = ArrayPool<char>.Shared.Rent(sr.CurrentEncoding.GetMaxCharCount(4096));
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				int num = await sr.ReadAsync(new Memory<char>(buffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					break;
				}
				sb.Append(buffer, 0, num);
			}
			return sb.ToString();
		}
		finally
		{
			sr.Dispose();
			if (buffer != null)
			{
				ArrayPool<char>.Shared.Return(buffer);
			}
		}
	}

	public static Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAllTextAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return WriteToFileAsync(path, FileMode.Create, contents, encoding, cancellationToken);
	}

	public static Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<byte[]>(cancellationToken);
		}
		if (1 == 0)
		{
		}
		FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan;
		SafeFileHandle safeFileHandle = OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, options, 0L);
		long num = 0L;
		if (safeFileHandle.CanSeek && (num = safeFileHandle.GetFileLength()) > 2147483591)
		{
			safeFileHandle.Dispose();
			return Task.FromException<byte[]>(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.IO_FileTooLong2GB)));
		}
		if (num <= 0)
		{
			return InternalReadAllBytesUnknownLengthAsync(safeFileHandle, cancellationToken);
		}
		return InternalReadAllBytesAsync(safeFileHandle, (int)num, cancellationToken);
	}

	private static async Task<byte[]> InternalReadAllBytesAsync(SafeFileHandle sfh, int count, CancellationToken cancellationToken)
	{
		using (sfh)
		{
			int index = 0;
			byte[] bytes = new byte[count];
			do
			{
				int num = await RandomAccess.ReadAtOffsetAsync(sfh, bytes.AsMemory(index), index, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					ThrowHelper.ThrowEndOfFileException();
				}
				index += num;
			}
			while (index < count);
			return bytes;
		}
	}

	private static async Task<byte[]> InternalReadAllBytesUnknownLengthAsync(SafeFileHandle sfh, CancellationToken cancellationToken)
	{
		byte[] rentedArray = ArrayPool<byte>.Shared.Rent(512);
		try
		{
			int bytesRead = 0;
			while (true)
			{
				if (bytesRead == rentedArray.Length)
				{
					uint num = (uint)(rentedArray.Length * 2);
					if ((long)num > 2147483591L)
					{
						num = (uint)Math.Max(2147483591, rentedArray.Length + 1);
					}
					byte[] array = ArrayPool<byte>.Shared.Rent((int)num);
					Buffer.BlockCopy(rentedArray, 0, array, 0, bytesRead);
					byte[] array2 = rentedArray;
					rentedArray = array;
					ArrayPool<byte>.Shared.Return(array2);
				}
				int num2 = await RandomAccess.ReadAtOffsetAsync(sfh, rentedArray.AsMemory(bytesRead), bytesRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num2 == 0)
				{
					break;
				}
				bytesRead += num2;
			}
			return rentedArray.AsSpan(0, bytesRead).ToArray();
		}
		finally
		{
			sfh.Dispose();
			ArrayPool<byte>.Shared.Return(rentedArray);
		}
	}

	public static Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		if (!cancellationToken.IsCancellationRequested)
		{
			return Core(path, bytes, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
		static async Task Core(string path, byte[] bytes, CancellationToken cancellationToken)
		{
			using SafeFileHandle sfh = OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous, 0L);
			await RandomAccess.WriteAtOffsetAsync(sfh, bytes, 0L, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken);
	}

	public static Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalReadAllLinesAsync(path, encoding, cancellationToken);
		}
		return Task.FromCanceled<string[]>(cancellationToken);
	}

	private static async Task<string[]> InternalReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken)
	{
		using StreamReader sr = AsyncStreamReader(path, encoding);
		cancellationToken.ThrowIfCancellationRequested();
		List<string> lines = new List<string>();
		string item;
		while ((item = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) != null)
		{
			lines.Add(item);
			cancellationToken.ThrowIfCancellationRequested();
		}
		return lines.ToArray();
	}

	public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAllLinesAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		ArgumentNullException.ThrowIfNull(contents, "contents");
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalWriteAllLinesAsync(AsyncStreamWriter(path, encoding, append: false), contents, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
	}

	private static async Task InternalWriteAllLinesAsync(StreamWriter writer, IEnumerable<string> contents, CancellationToken cancellationToken)
	{
		using (writer)
		{
			foreach (string content in contents)
			{
				await writer.WriteLineAsync(content.AsMemory(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			await writer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return AppendAllTextAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task AppendAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return WriteToFileAsync(path, FileMode.Append, contents, encoding, cancellationToken);
	}

	public static Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return AppendAllLinesAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		Validate(path, encoding);
		ArgumentNullException.ThrowIfNull(contents, "contents");
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalWriteAllLinesAsync(AsyncStreamWriter(path, encoding, append: true), contents, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
	}

	public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.VerifyValidPath(pathToTarget, "pathToTarget");
		FileSystem.CreateSymbolicLink(path, pathToTarget, isDirectory: false);
		return new FileInfo(path, fullPath, null, isNormalized: true);
	}

	public static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget)
	{
		FileSystem.VerifyValidPath(linkPath, "linkPath");
		return FileSystem.ResolveLinkTarget(linkPath, returnFinalTarget, isDirectory: false);
	}

	private static void Validate(string path, Encoding encoding)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
	}

	private static byte[] ReadAllBytesUnknownLength(SafeFileHandle sfh)
	{
		byte[] array = null;
		Span<byte> span = stackalloc byte[512];
		try
		{
			int num = 0;
			while (true)
			{
				if (num == span.Length)
				{
					uint num2 = (uint)(span.Length * 2);
					if ((long)num2 > 2147483591L)
					{
						num2 = (uint)Math.Max(2147483591, span.Length + 1);
					}
					byte[] array2 = ArrayPool<byte>.Shared.Rent((int)num2);
					span.CopyTo(array2);
					byte[] array3 = array;
					span = (array = array2);
					if (array3 != null)
					{
						ArrayPool<byte>.Shared.Return(array3);
					}
				}
				int num3 = RandomAccess.ReadAtOffset(sfh, span.Slice(num), num);
				if (num3 == 0)
				{
					break;
				}
				num += num3;
			}
			return span.Slice(0, num).ToArray();
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private static void WriteToFile(string path, FileMode mode, string contents, Encoding encoding)
	{
		ReadOnlySpan<byte> buffer = encoding.GetPreamble();
		int num = buffer.Length;
		using SafeFileHandle safeFileHandle = OpenHandle(path, mode, FileAccess.Write, FileShare.Read, FileOptions.None, GetPreallocationSize(mode, contents, encoding, num));
		long num2 = ((mode == FileMode.Append && safeFileHandle.CanSeek) ? RandomAccess.GetLength(safeFileHandle) : 0);
		if (string.IsNullOrEmpty(contents))
		{
			if (num > 0 && num2 == 0L)
			{
				RandomAccess.WriteAtOffset(safeFileHandle, buffer, num2);
			}
			return;
		}
		int num3 = num + encoding.GetMaxByteCount(Math.Min(contents.Length, 8192));
		byte[] array = null;
		Span<byte> span = ((num3 > 1024) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(num3))) : stackalloc byte[1024]);
		Span<byte> destination = span;
		try
		{
			if (num2 == 0L)
			{
				buffer.CopyTo(destination);
			}
			else
			{
				num = 0;
			}
			Encoder encoder = encoding.GetEncoder();
			ReadOnlySpan<char> readOnlySpan = contents;
			while (!readOnlySpan.IsEmpty)
			{
				ReadOnlySpan<char> chars = readOnlySpan.Slice(0, Math.Min(readOnlySpan.Length, 8192));
				readOnlySpan = readOnlySpan.Slice(chars.Length);
				int bytes = encoder.GetBytes(chars, destination.Slice(num), readOnlySpan.IsEmpty);
				Span<byte> span2 = destination.Slice(0, num + bytes);
				RandomAccess.WriteAtOffset(safeFileHandle, span2, num2);
				num2 += span2.Length;
				num = 0;
			}
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private static async Task WriteToFileAsync(string path, FileMode mode, string contents, Encoding encoding, CancellationToken cancellationToken)
	{
		ReadOnlyMemory<byte> buffer = encoding.GetPreamble();
		int num = buffer.Length;
		using SafeFileHandle fileHandle = OpenHandle(path, mode, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous, GetPreallocationSize(mode, contents, encoding, num));
		long fileOffset = ((mode == FileMode.Append && fileHandle.CanSeek) ? RandomAccess.GetLength(fileHandle) : 0);
		if (string.IsNullOrEmpty(contents))
		{
			if (num > 0 && fileOffset == 0L)
			{
				await RandomAccess.WriteAtOffsetAsync(fileHandle, buffer, fileOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			return;
		}
		byte[] bytes = ArrayPool<byte>.Shared.Rent(num + encoding.GetMaxByteCount(Math.Min(contents.Length, 8192)));
		try
		{
			if (fileOffset == 0L)
			{
				buffer.CopyTo(bytes);
			}
			else
			{
				num = 0;
			}
			Encoder encoder = encoding.GetEncoder();
			ReadOnlyMemory<char> remaining = contents.AsMemory();
			while (!remaining.IsEmpty)
			{
				ReadOnlyMemory<char> readOnlyMemory = remaining.Slice(0, Math.Min(remaining.Length, 8192));
				remaining = remaining.Slice(readOnlyMemory.Length);
				int bytes2 = encoder.GetBytes(readOnlyMemory.Span, bytes.AsSpan(num), remaining.IsEmpty);
				ReadOnlyMemory<byte> toStore = new ReadOnlyMemory<byte>(bytes, 0, num + bytes2);
				await RandomAccess.WriteAtOffsetAsync(fileHandle, toStore, fileOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				fileOffset += toStore.Length;
				num = 0;
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	private static long GetPreallocationSize(FileMode mode, string contents, Encoding encoding, int preambleSize)
	{
		if (contents == null || contents.Length < 8192)
		{
			return 0L;
		}
		if (mode == FileMode.Append)
		{
			return 0L;
		}
		return preambleSize + encoding.GetByteCount(contents);
	}

	private static async IAsyncEnumerable<string> IterateFileLinesAsync(StreamReader sr, string path, Encoding encoding, CancellationToken ctEnumerable, [EnumeratorCancellation] CancellationToken ctEnumerator = default(CancellationToken))
	{
		if (!sr.BaseStream.CanRead)
		{
			sr = AsyncStreamReader(path, encoding);
		}
		using (sr)
		{
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ctEnumerable, ctEnumerator);
			while (true)
			{
				string text;
				string text2 = (text = await sr.ReadLineAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false));
				if (text2 == null)
				{
					break;
				}
				yield return text;
			}
		}
	}

	private static UnixFileMode GetUnixFileModeCore(string path)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
	}

	private static UnixFileMode GetUnixFileModeCore(SafeFileHandle fileHandle)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
	}

	private static void SetUnixFileModeCore(string path, UnixFileMode mode)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
	}

	private static void SetUnixFileModeCore(SafeFileHandle fileHandle, UnixFileMode mode)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
	}
}
