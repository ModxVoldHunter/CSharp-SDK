namespace System.Formats.Tar;

public sealed class GnuTarEntry : PosixTarEntry
{
	public DateTimeOffset AccessTime
	{
		get
		{
			return _header._aTime;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, DateTimeOffset.UnixEpoch, "value");
			_header._aTime = value;
		}
	}

	public DateTimeOffset ChangeTime
	{
		get
		{
			return _header._cTime;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, DateTimeOffset.UnixEpoch, "value");
			_header._cTime = value;
		}
	}

	internal GnuTarEntry(TarHeader header, TarReader readerOfOrigin)
		: base(header, readerOfOrigin, TarEntryFormat.Gnu)
	{
	}

	public GnuTarEntry(TarEntryType entryType, string entryName)
		: base(entryType, entryName, TarEntryFormat.Gnu, isGea: false)
	{
		_header._aTime = _header._mTime;
		_header._cTime = _header._mTime;
	}

	public GnuTarEntry(TarEntry other)
		: base(other, TarEntryFormat.Gnu)
	{
		if (other is GnuTarEntry gnuTarEntry)
		{
			_header._aTime = gnuTarEntry.AccessTime;
			_header._cTime = gnuTarEntry.ChangeTime;
			_header._gnuUnusedBytes = other._header._gnuUnusedBytes;
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (other is PaxTarEntry paxTarEntry)
		{
			flag = TarHelpers.TryGetDateTimeOffsetFromTimestampString(paxTarEntry._header.ExtendedAttributes, "atime", out var dateTimeOffset);
			if (flag)
			{
				_header._aTime = dateTimeOffset;
			}
			flag2 = TarHelpers.TryGetDateTimeOffsetFromTimestampString(paxTarEntry._header.ExtendedAttributes, "ctime", out var dateTimeOffset2);
			if (flag2)
			{
				_header._cTime = dateTimeOffset2;
			}
		}
		if (!flag || !flag2)
		{
			DateTimeOffset utcNow = DateTimeOffset.UtcNow;
			if (!flag)
			{
				_header._aTime = utcNow;
			}
			if (!flag2)
			{
				_header._cTime = utcNow;
			}
		}
	}

	internal override bool IsDataStreamSetterSupported()
	{
		return base.EntryType == TarEntryType.RegularFile;
	}
}
