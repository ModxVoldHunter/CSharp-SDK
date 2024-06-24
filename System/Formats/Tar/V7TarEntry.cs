namespace System.Formats.Tar;

public sealed class V7TarEntry : TarEntry
{
	internal V7TarEntry(TarHeader header, TarReader readerOfOrigin)
		: base(header, readerOfOrigin, TarEntryFormat.V7)
	{
	}

	public V7TarEntry(TarEntryType entryType, string entryName)
		: base(entryType, entryName, TarEntryFormat.V7, isGea: false)
	{
	}

	public V7TarEntry(TarEntry other)
		: base(other, TarEntryFormat.V7)
	{
	}

	internal override bool IsDataStreamSetterSupported()
	{
		return base.EntryType == TarEntryType.V7RegularFile;
	}
}
