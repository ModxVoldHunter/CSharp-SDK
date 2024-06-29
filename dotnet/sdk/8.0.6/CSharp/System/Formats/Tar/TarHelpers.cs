using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

internal static class TarHelpers
{
	internal static int GetDefaultMode(TarEntryType type)
	{
		if ((type != TarEntryType.Directory && type != TarEntryType.DirectoryList) || 1 == 0)
		{
			return 420;
		}
		return 493;
	}

	internal static void AdvanceStream(Stream archiveStream, long bytesToDiscard)
	{
		if (archiveStream.CanSeek)
		{
			archiveStream.Position += bytesToDiscard;
		}
		else if (bytesToDiscard > 0)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent((int)Math.Min(4096L, bytesToDiscard));
			while (bytesToDiscard > 0)
			{
				int num = (int)Math.Min(4096L, bytesToDiscard);
				archiveStream.ReadExactly(array.AsSpan(0, num));
				bytesToDiscard -= num;
			}
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	internal static async ValueTask AdvanceStreamAsync(Stream archiveStream, long bytesToDiscard, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (archiveStream.CanSeek)
		{
			archiveStream.Position += bytesToDiscard;
		}
		else if (bytesToDiscard > 0)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(4096L, bytesToDiscard));
			while (bytesToDiscard > 0)
			{
				int currentLengthToRead = (int)Math.Min(4096L, bytesToDiscard);
				await archiveStream.ReadExactlyAsync(buffer, 0, currentLengthToRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				bytesToDiscard -= currentLengthToRead;
			}
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	internal static void CopyBytes(Stream origin, Stream destination, long bytesToCopy)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent((int)Math.Min(4096L, bytesToCopy));
		while (bytesToCopy > 0)
		{
			int num = (int)Math.Min(4096L, bytesToCopy);
			origin.ReadExactly(array.AsSpan(0, num));
			destination.Write(array.AsSpan(0, num));
			bytesToCopy -= num;
		}
		ArrayPool<byte>.Shared.Return(array);
	}

	internal static async ValueTask CopyBytesAsync(Stream origin, Stream destination, long bytesToCopy, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		byte[] buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(4096L, bytesToCopy));
		while (bytesToCopy > 0)
		{
			int currentLengthToRead = (int)Math.Min(4096L, bytesToCopy);
			Memory<byte> memory = buffer.AsMemory(0, currentLengthToRead);
			await origin.ReadExactlyAsync(buffer, 0, currentLengthToRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await destination.WriteAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			bytesToCopy -= currentLengthToRead;
		}
		ArrayPool<byte>.Shared.Return(buffer);
	}

	internal static int CalculatePadding(long size)
	{
		long num = (0x1FF | (size - 1)) + 1;
		return (int)(num - size);
	}

	internal static bool IsAllNullBytes(Span<byte> buffer)
	{
		return !buffer.ContainsAnyExcept<byte>(0);
	}

	internal static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(long secondsSinceUnixEpoch)
	{
		return new DateTimeOffset(secondsSinceUnixEpoch * 10000000 + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);
	}

	private static DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(decimal secondsSinceUnixEpoch)
	{
		return new DateTimeOffset((long)(secondsSinceUnixEpoch * 10000000m) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);
	}

	private static decimal GetSecondsSinceEpochFromDateTimeOffset(DateTimeOffset dateTimeOffset)
	{
		return (decimal)(dateTimeOffset.UtcDateTime - DateTime.UnixEpoch).Ticks / 10000000m;
	}

	internal static bool TryGetDateTimeOffsetFromTimestampString(Dictionary<string, string> dict, string fieldName, out DateTimeOffset dateTimeOffset)
	{
		dateTimeOffset = default(DateTimeOffset);
		if (dict != null && dict.TryGetValue(fieldName, out var value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			dateTimeOffset = GetDateTimeOffsetFromSecondsSinceEpoch(result);
			return true;
		}
		return false;
	}

	internal static string GetTimestampStringFromDateTimeOffset(DateTimeOffset timestamp)
	{
		return GetSecondsSinceEpochFromDateTimeOffset(timestamp).ToString("G", CultureInfo.InvariantCulture);
	}

	internal static bool TryGetStringAsBaseTenInteger(IReadOnlyDictionary<string, string> dict, string fieldName, out int baseTenInteger)
	{
		if (dict.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
		{
			baseTenInteger = int.Parse(value, CultureInfo.InvariantCulture);
			return true;
		}
		baseTenInteger = 0;
		return false;
	}

	internal static bool TryGetStringAsBaseTenLong(IReadOnlyDictionary<string, string> dict, string fieldName, out long baseTenLong)
	{
		if (dict.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
		{
			baseTenLong = long.Parse(value, CultureInfo.InvariantCulture);
			return true;
		}
		baseTenLong = 0L;
		return false;
	}

	internal static TarEntryType GetCorrectTypeFlagForFormat(TarEntryFormat format, TarEntryType entryType)
	{
		if (format == TarEntryFormat.V7)
		{
			if (entryType == TarEntryType.RegularFile)
			{
				return TarEntryType.V7RegularFile;
			}
		}
		else if (entryType == TarEntryType.V7RegularFile)
		{
			return TarEntryType.RegularFile;
		}
		return entryType;
	}

	internal static T ParseOctal<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>
	{
		buffer = TrimEndingNullsAndSpaces(buffer);
		buffer = TrimLeadingNullsAndSpaces(buffer);
		if (buffer.Length == 0)
		{
			return T.Zero;
		}
		T val = T.CreateTruncating(8u);
		T val2 = T.Zero;
		ReadOnlySpan<byte> readOnlySpan = buffer;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			uint num = (uint)(b - 48);
			if (num >= 8)
			{
				ThrowInvalidNumber();
			}
			val2 = checked(val2 * val + T.CreateTruncating(num));
		}
		return val2;
	}

	[DoesNotReturn]
	private static void ThrowInvalidNumber()
	{
		throw new InvalidDataException(System.SR.Format(System.SR.TarInvalidNumber));
	}

	private static string GetTrimmedString(ReadOnlySpan<byte> buffer, Encoding encoding)
	{
		buffer = TrimEndingNullsAndSpaces(buffer);
		if (!buffer.IsEmpty)
		{
			return encoding.GetString(buffer);
		}
		return string.Empty;
	}

	internal static ReadOnlySpan<byte> TrimEndingNullsAndSpaces(ReadOnlySpan<byte> buffer)
	{
		int num = buffer.Length;
		while (true)
		{
			bool flag = num > 0;
			bool flag2 = flag;
			if (flag2)
			{
				byte b = buffer[num - 1];
				bool flag3 = ((b == 0 || b == 32) ? true : false);
				flag2 = flag3;
			}
			if (!flag2)
			{
				break;
			}
			num--;
		}
		return buffer.Slice(0, num);
	}

	private static ReadOnlySpan<byte> TrimLeadingNullsAndSpaces(ReadOnlySpan<byte> buffer)
	{
		int num = 0;
		while (true)
		{
			bool flag = num < buffer.Length;
			bool flag2 = flag;
			if (flag2)
			{
				byte b = buffer[num];
				bool flag3 = ((b == 0 || b == 32) ? true : false);
				flag2 = flag3;
			}
			if (!flag2)
			{
				break;
			}
			num++;
		}
		return buffer.Slice(num);
	}

	internal static string GetTrimmedUtf8String(ReadOnlySpan<byte> buffer)
	{
		return GetTrimmedString(buffer, Encoding.UTF8);
	}

	internal static int SkipBlockAlignmentPadding(Stream archiveStream, long size)
	{
		int num = CalculatePadding(size);
		AdvanceStream(archiveStream, num);
		return num;
	}

	internal static async ValueTask<int> SkipBlockAlignmentPaddingAsync(Stream archiveStream, long size, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		int bytesToSkip = CalculatePadding(size);
		await AdvanceStreamAsync(archiveStream, bytesToSkip, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return bytesToSkip;
	}

	internal static void ThrowIfEntryTypeNotSupported(TarEntryType entryType, TarEntryFormat archiveFormat, [CallerArgumentExpression("entryType")] string paramName = null)
	{
		switch (archiveFormat)
		{
		case TarEntryFormat.V7:
			if ((entryType == TarEntryType.V7RegularFile || entryType - 49 <= (TarEntryType)1 || entryType == TarEntryType.Directory) ? true : false)
			{
				return;
			}
			break;
		case TarEntryFormat.Ustar:
			if (entryType - 48 <= (TarEntryType)6)
			{
				return;
			}
			break;
		case TarEntryFormat.Pax:
			if (entryType - 48 <= (TarEntryType)6)
			{
				return;
			}
			break;
		case TarEntryFormat.Gnu:
			if (entryType - 48 <= (TarEntryType)6)
			{
				return;
			}
			break;
		default:
			throw new InvalidDataException(System.SR.Format(System.SR.TarInvalidFormat, archiveFormat));
		}
		throw new ArgumentException(System.SR.Format(System.SR.TarEntryTypeNotSupportedInFormat, entryType, archiveFormat), paramName);
	}

	public static void SetPendingModificationTimes(Stack<(string, DateTimeOffset)> directoryModificationTimes)
	{
		(string, DateTimeOffset) result;
		while (directoryModificationTimes.TryPop(out result))
		{
			AttemptDirectorySetLastWriteTime(result.Item1, result.Item2);
		}
	}

	public static void UpdatePendingModificationTimes(Stack<(string, DateTimeOffset)> directoryModificationTimes, string fullPath, DateTimeOffset modified)
	{
		(string, DateTimeOffset) result;
		while (directoryModificationTimes.TryPeek(out result) && !IsChildPath(result.Item1, fullPath))
		{
			directoryModificationTimes.TryPop(out result);
			AttemptDirectorySetLastWriteTime(result.Item1, result.Item2);
		}
		directoryModificationTimes.Push((fullPath, modified));
	}

	private static bool IsChildPath(string parentFullPath, string childFullPath)
	{
		if (IsDirectorySeparatorChar(parentFullPath[parentFullPath.Length - 1]))
		{
			if (childFullPath.Length <= parentFullPath.Length)
			{
				return false;
			}
		}
		else if (childFullPath.Length < parentFullPath.Length + 2 || !IsDirectorySeparatorChar(childFullPath[parentFullPath.Length]))
		{
			return false;
		}
		return childFullPath.StartsWith(parentFullPath, System.IO.PathInternal.StringComparison);
		static bool IsDirectorySeparatorChar(char c)
		{
			return c == Path.DirectorySeparatorChar;
		}
	}

	private static void AttemptDirectorySetLastWriteTime(string fullPath, DateTimeOffset lastWriteTime)
	{
		try
		{
			Directory.SetLastWriteTime(fullPath, lastWriteTime.UtcDateTime);
		}
		catch
		{
		}
	}

	internal static void CreateDirectory(string fullPath, UnixFileMode? mode, SortedDictionary<string, UnixFileMode> pendingModes)
	{
		Directory.CreateDirectory(fullPath);
	}

	internal static void SetPendingModes(SortedDictionary<string, UnixFileMode> pendingModes)
	{
	}
}
