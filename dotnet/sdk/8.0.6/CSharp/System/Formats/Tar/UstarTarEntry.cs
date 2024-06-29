namespace System.Formats.Tar;

public sealed class UstarTarEntry : PosixTarEntry
{
	internal UstarTarEntry(TarHeader header, TarReader readerOfOrigin)
		: base(header, readerOfOrigin, TarEntryFormat.Ustar)
	{
	}

	public UstarTarEntry(TarEntryType entryType, string entryName)
		: base(entryType, entryName, TarEntryFormat.Ustar, isGea: false)
	{
		_header._prefix = string.Empty;
	}

	public UstarTarEntry(TarEntry other)
		: base(other, TarEntryFormat.Ustar)
	{
		TarEntryFormat format = other._header._format;
		if ((uint)(format - 2) <= 1u)
		{
			_header._prefix = other._header._prefix;
		}
		else
		{
			_header._prefix = string.Empty;
		}
	}

	internal override bool IsDataStreamSetterSupported()
	{
		return base.EntryType == TarEntryType.RegularFile;
	}
}
