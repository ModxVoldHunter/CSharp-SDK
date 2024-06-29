using System.Collections.Generic;

namespace System.IO.Compression;

internal struct Zip64ExtraField
{
	private ushort _size;

	private long? _uncompressedSize;

	private long? _compressedSize;

	private long? _localHeaderOffset;

	private uint? _startDiskNumber;

	public ushort TotalSize => (ushort)(_size + 4);

	public long? UncompressedSize
	{
		get
		{
			return _uncompressedSize;
		}
		set
		{
			_uncompressedSize = value;
			UpdateSize();
		}
	}

	public long? CompressedSize
	{
		get
		{
			return _compressedSize;
		}
		set
		{
			_compressedSize = value;
			UpdateSize();
		}
	}

	public long? LocalHeaderOffset
	{
		get
		{
			return _localHeaderOffset;
		}
		set
		{
			_localHeaderOffset = value;
			UpdateSize();
		}
	}

	public uint? StartDiskNumber => _startDiskNumber;

	private void UpdateSize()
	{
		_size = 0;
		if (_uncompressedSize.HasValue)
		{
			_size += 8;
		}
		if (_compressedSize.HasValue)
		{
			_size += 8;
		}
		if (_localHeaderOffset.HasValue)
		{
			_size += 8;
		}
		if (_startDiskNumber.HasValue)
		{
			_size += 4;
		}
	}

	public static Zip64ExtraField GetJustZip64Block(Stream extraFieldStream, bool readUncompressedSize, bool readCompressedSize, bool readLocalHeaderOffset, bool readStartDiskNumber)
	{
		Zip64ExtraField zip64Block;
		using (BinaryReader reader = new BinaryReader(extraFieldStream))
		{
			ZipGenericExtraField field;
			while (ZipGenericExtraField.TryReadBlock(reader, extraFieldStream.Length, out field))
			{
				if (TryGetZip64BlockFromGenericExtraField(field, readUncompressedSize, readCompressedSize, readLocalHeaderOffset, readStartDiskNumber, out zip64Block))
				{
					return zip64Block;
				}
			}
		}
		zip64Block = default(Zip64ExtraField);
		zip64Block._compressedSize = null;
		zip64Block._uncompressedSize = null;
		zip64Block._localHeaderOffset = null;
		zip64Block._startDiskNumber = null;
		return zip64Block;
	}

	private static bool TryGetZip64BlockFromGenericExtraField(ZipGenericExtraField extraField, bool readUncompressedSize, bool readCompressedSize, bool readLocalHeaderOffset, bool readStartDiskNumber, out Zip64ExtraField zip64Block)
	{
		zip64Block = default(Zip64ExtraField);
		zip64Block._compressedSize = null;
		zip64Block._uncompressedSize = null;
		zip64Block._localHeaderOffset = null;
		zip64Block._startDiskNumber = null;
		if (extraField.Tag != 1)
		{
			return false;
		}
		zip64Block._size = extraField.Size;
		using MemoryStream memoryStream = new MemoryStream(extraField.Data);
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		if (extraField.Size < 8)
		{
			return true;
		}
		bool flag = extraField.Size >= 28;
		if (readUncompressedSize)
		{
			zip64Block._uncompressedSize = binaryReader.ReadInt64();
		}
		else if (flag)
		{
			binaryReader.ReadInt64();
		}
		if (memoryStream.Position > extraField.Size - 8)
		{
			return true;
		}
		if (readCompressedSize)
		{
			zip64Block._compressedSize = binaryReader.ReadInt64();
		}
		else if (flag)
		{
			binaryReader.ReadInt64();
		}
		if (memoryStream.Position > extraField.Size - 8)
		{
			return true;
		}
		if (readLocalHeaderOffset)
		{
			zip64Block._localHeaderOffset = binaryReader.ReadInt64();
		}
		else if (flag)
		{
			binaryReader.ReadInt64();
		}
		if (memoryStream.Position > extraField.Size - 4)
		{
			return true;
		}
		if (readStartDiskNumber)
		{
			zip64Block._startDiskNumber = binaryReader.ReadUInt32();
		}
		else if (flag)
		{
			binaryReader.ReadInt32();
		}
		if (zip64Block._uncompressedSize < 0)
		{
			throw new InvalidDataException(System.SR.FieldTooBigUncompressedSize);
		}
		if (zip64Block._compressedSize < 0)
		{
			throw new InvalidDataException(System.SR.FieldTooBigCompressedSize);
		}
		if (zip64Block._localHeaderOffset < 0)
		{
			throw new InvalidDataException(System.SR.FieldTooBigLocalHeaderOffset);
		}
		return true;
	}

	public static Zip64ExtraField GetAndRemoveZip64Block(List<ZipGenericExtraField> extraFields, bool readUncompressedSize, bool readCompressedSize, bool readLocalHeaderOffset, bool readStartDiskNumber)
	{
		Zip64ExtraField zip64Block = default(Zip64ExtraField);
		zip64Block._compressedSize = null;
		zip64Block._uncompressedSize = null;
		zip64Block._localHeaderOffset = null;
		zip64Block._startDiskNumber = null;
		List<ZipGenericExtraField> list = new List<ZipGenericExtraField>();
		bool flag = false;
		foreach (ZipGenericExtraField extraField in extraFields)
		{
			if (extraField.Tag == 1)
			{
				list.Add(extraField);
				if (!flag && TryGetZip64BlockFromGenericExtraField(extraField, readUncompressedSize, readCompressedSize, readLocalHeaderOffset, readStartDiskNumber, out zip64Block))
				{
					flag = true;
				}
			}
		}
		foreach (ZipGenericExtraField item in list)
		{
			extraFields.Remove(item);
		}
		return zip64Block;
	}

	public static void RemoveZip64Blocks(List<ZipGenericExtraField> extraFields)
	{
		List<ZipGenericExtraField> list = new List<ZipGenericExtraField>();
		foreach (ZipGenericExtraField extraField in extraFields)
		{
			if (extraField.Tag == 1)
			{
				list.Add(extraField);
			}
		}
		foreach (ZipGenericExtraField item in list)
		{
			extraFields.Remove(item);
		}
	}

	public void WriteBlock(Stream stream)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write((ushort)1);
		binaryWriter.Write(_size);
		if (_uncompressedSize.HasValue)
		{
			binaryWriter.Write(_uncompressedSize.Value);
		}
		if (_compressedSize.HasValue)
		{
			binaryWriter.Write(_compressedSize.Value);
		}
		if (_localHeaderOffset.HasValue)
		{
			binaryWriter.Write(_localHeaderOffset.Value);
		}
		if (_startDiskNumber.HasValue)
		{
			binaryWriter.Write(_startDiskNumber.Value);
		}
	}
}
