namespace System.Formats.Tar;

public enum TarEntryType : byte
{
	RegularFile = 48,
	HardLink = 49,
	SymbolicLink = 50,
	CharacterDevice = 51,
	BlockDevice = 52,
	Directory = 53,
	Fifo = 54,
	ContiguousFile = 55,
	ExtendedAttributes = 120,
	GlobalExtendedAttributes = 103,
	DirectoryList = 68,
	LongLink = 75,
	LongPath = 76,
	MultiVolume = 77,
	V7RegularFile = 0,
	RenamedOrSymlinked = 78,
	SparseFile = 83,
	TapeVolume = 86
}
