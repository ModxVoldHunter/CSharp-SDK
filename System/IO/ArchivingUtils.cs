using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.IO;

internal static class ArchivingUtils
{
	private static readonly SearchValues<char> s_illegalChars = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\"*:<>?|");

	public static bool IsDirEmpty(string directoryFullName)
	{
		using IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(directoryFullName).GetEnumerator();
		return !enumerator.MoveNext();
	}

	public static void AttemptSetLastWriteTime(string destinationFileName, DateTimeOffset lastWriteTime)
	{
		try
		{
			File.SetLastWriteTime(destinationFileName, lastWriteTime.DateTime);
		}
		catch
		{
		}
	}

	internal static string SanitizeEntryFilePath(string entryPath, bool preserveDriveRoot = false)
	{
		int num = 0;
		if (preserveDriveRoot && entryPath.Length >= 3 && entryPath[1] == ':' && Path.IsPathFullyQualified(entryPath))
		{
			num = 3;
		}
		int num2 = entryPath.AsSpan(num).IndexOfAny(s_illegalChars);
		if (num2 < 0)
		{
			return entryPath;
		}
		num2 += num;
		return string.Create(entryPath.Length, (num2, entryPath), delegate(Span<char> dest, (int i, string entryPath) state)
		{
			string item = state.entryPath;
			item.AsSpan(0, state.i).CopyTo(dest);
			dest[state.i] = '_';
			for (int i = state.i + 1; i < item.Length; i++)
			{
				char c = item[i];
				dest[i] = (s_illegalChars.Contains(c) ? '_' : c);
			}
		});
	}

	public unsafe static string EntryFromPath(ReadOnlySpan<char> path, bool appendPathSeparator = false)
	{
		int num = path.IndexOfAnyExcept('/', '\\');
		if (num < 0)
		{
			num = path.Length;
		}
		path = path.Slice(num);
		if (path.IsEmpty)
		{
			if (!appendPathSeparator)
			{
				return string.Empty;
			}
			return "/";
		}
		ReadOnlySpan<char> readOnlySpan = path;
		return string.Create(appendPathSeparator ? (readOnlySpan.Length + 1) : readOnlySpan.Length, (appendPathSeparator, (nint)(&readOnlySpan)), delegate(Span<char> dest, (bool appendPathSeparator, nint RosPtr) state)
		{
			ReadOnlySpan<char> item = Unsafe.Read<ReadOnlySpan<char>>((void*)state.RosPtr);
			item.CopyTo(dest);
			if (state.appendPathSeparator)
			{
				dest[dest.Length - 1] = '/';
			}
			dest.Replace('\\', '/');
		});
	}
}
