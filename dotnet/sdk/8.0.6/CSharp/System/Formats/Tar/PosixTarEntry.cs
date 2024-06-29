namespace System.Formats.Tar;

public abstract class PosixTarEntry : TarEntry
{
	public int DeviceMajor
	{
		get
		{
			return _header._devMajor;
		}
		set
		{
			TarEntryType typeFlag = _header._typeFlag;
			if (typeFlag != TarEntryType.BlockDevice && typeFlag != TarEntryType.CharacterDevice)
			{
				throw new InvalidOperationException(System.SR.TarEntryBlockOrCharacterExpected);
			}
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2097151, "value");
			_header._devMajor = value;
		}
	}

	public int DeviceMinor
	{
		get
		{
			return _header._devMinor;
		}
		set
		{
			TarEntryType typeFlag = _header._typeFlag;
			if (typeFlag != TarEntryType.BlockDevice && typeFlag != TarEntryType.CharacterDevice)
			{
				throw new InvalidOperationException(System.SR.TarEntryBlockOrCharacterExpected);
			}
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2097151, "value");
			_header._devMinor = value;
		}
	}

	public string GroupName
	{
		get
		{
			return _header._gName ?? string.Empty;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_header._gName = value;
		}
	}

	public string UserName
	{
		get
		{
			return _header._uName ?? string.Empty;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_header._uName = value;
		}
	}

	internal PosixTarEntry(TarHeader header, TarReader readerOfOrigin, TarEntryFormat format)
		: base(header, readerOfOrigin, format)
	{
	}

	internal PosixTarEntry(TarEntryType entryType, string entryName, TarEntryFormat format, bool isGea)
		: base(entryType, entryName, format, isGea)
	{
		_header._uName = string.Empty;
		_header._gName = string.Empty;
		_header._devMajor = 0;
		_header._devMinor = 0;
	}

	internal PosixTarEntry(TarEntry other, TarEntryFormat format)
		: base(other, format)
	{
		if (other is PosixTarEntry)
		{
			_header._uName = other._header._uName;
			_header._gName = other._header._gName;
			_header._devMajor = other._header._devMajor;
			_header._devMinor = other._header._devMinor;
		}
		TarHeader header = _header;
		if (header._uName == null)
		{
			header._uName = string.Empty;
		}
		header = _header;
		if (header._gName == null)
		{
			header._gName = string.Empty;
		}
	}
}
