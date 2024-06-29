using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Formats.Tar;

public sealed class PaxTarEntry : PosixTarEntry
{
	private ReadOnlyDictionary<string, string> _readOnlyExtendedAttributes;

	public IReadOnlyDictionary<string, string> ExtendedAttributes => _readOnlyExtendedAttributes ?? (_readOnlyExtendedAttributes = _header.ExtendedAttributes.AsReadOnly());

	internal PaxTarEntry(TarHeader header, TarReader readerOfOrigin)
		: base(header, readerOfOrigin, TarEntryFormat.Pax)
	{
	}

	public PaxTarEntry(TarEntryType entryType, string entryName)
		: base(entryType, entryName, TarEntryFormat.Pax, isGea: false)
	{
		_header._prefix = string.Empty;
		AddNewAccessAndChangeTimestampsIfNotExist(useMTime: true);
	}

	public PaxTarEntry(TarEntryType entryType, string entryName, IEnumerable<KeyValuePair<string, string>> extendedAttributes)
		: base(entryType, entryName, TarEntryFormat.Pax, isGea: false)
	{
		ArgumentNullException.ThrowIfNull(extendedAttributes, "extendedAttributes");
		_header._prefix = string.Empty;
		_header.InitializeExtendedAttributesWithExisting(extendedAttributes);
		AddNewAccessAndChangeTimestampsIfNotExist(useMTime: true);
	}

	public PaxTarEntry(TarEntry other)
		: base(other, TarEntryFormat.Pax)
	{
		TarEntryFormat format = other._header._format;
		if ((uint)(format - 2) <= 1u)
		{
			_header._prefix = other._header._prefix;
		}
		if (other is PaxTarEntry paxTarEntry)
		{
			_header.InitializeExtendedAttributesWithExisting(paxTarEntry.ExtendedAttributes);
		}
		else if (other is GnuTarEntry gnuTarEntry)
		{
			_header.ExtendedAttributes["atime"] = TarHelpers.GetTimestampStringFromDateTimeOffset(gnuTarEntry.AccessTime);
			_header.ExtendedAttributes["ctime"] = TarHelpers.GetTimestampStringFromDateTimeOffset(gnuTarEntry.ChangeTime);
		}
		AddNewAccessAndChangeTimestampsIfNotExist(useMTime: false);
	}

	internal override bool IsDataStreamSetterSupported()
	{
		return base.EntryType == TarEntryType.RegularFile;
	}

	private void AddNewAccessAndChangeTimestampsIfNotExist(bool useMTime)
	{
		bool flag = _header.ExtendedAttributes.ContainsKey("atime");
		bool flag2 = _header.ExtendedAttributes.ContainsKey("ctime");
		if (!flag || !flag2)
		{
			string timestampStringFromDateTimeOffset = TarHelpers.GetTimestampStringFromDateTimeOffset(useMTime ? _header._mTime : DateTimeOffset.UtcNow);
			if (!flag)
			{
				_header.ExtendedAttributes["atime"] = timestampStringFromDateTimeOffset;
			}
			if (!flag2)
			{
				_header.ExtendedAttributes["ctime"] = timestampStringFromDateTimeOffset;
			}
		}
	}
}
