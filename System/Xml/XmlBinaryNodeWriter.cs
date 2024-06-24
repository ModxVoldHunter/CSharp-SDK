using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal sealed class XmlBinaryNodeWriter : XmlStreamNodeWriter
{
	private struct AttributeValue
	{
		private string _captureText;

		private XmlDictionaryString _captureXText;

		private MemoryStream _captureStream;

		public void Clear()
		{
			_captureText = null;
			_captureXText = null;
			_captureStream = null;
		}

		public void WriteText(string s)
		{
			if (_captureStream != null)
			{
				ArraySegment<byte> buffer;
				bool flag = _captureStream.TryGetBuffer(out buffer);
				_captureText = DataContractSerializer.Base64Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
				_captureStream = null;
			}
			if (_captureXText != null)
			{
				_captureText = _captureXText.Value;
				_captureXText = null;
			}
			if (string.IsNullOrEmpty(_captureText))
			{
				_captureText = s;
			}
			else
			{
				_captureText += s;
			}
		}

		public void WriteText(XmlDictionaryString s)
		{
			if (_captureText != null || _captureStream != null)
			{
				WriteText(s.Value);
			}
			else
			{
				_captureXText = s;
			}
		}

		public void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
		{
			if (_captureText != null || _captureXText != null)
			{
				if (trailByteCount > 0)
				{
					WriteText(DataContractSerializer.Base64Encoding.GetString(trailBytes, 0, trailByteCount));
				}
				WriteText(DataContractSerializer.Base64Encoding.GetString(buffer, offset, count));
				return;
			}
			if (_captureStream == null)
			{
				_captureStream = new MemoryStream();
			}
			if (trailByteCount > 0)
			{
				_captureStream.Write(trailBytes, 0, trailByteCount);
			}
			_captureStream.Write(buffer, offset, count);
		}

		public void WriteTo(XmlBinaryNodeWriter writer)
		{
			if (_captureText != null)
			{
				writer.WriteText(_captureText);
				_captureText = null;
			}
			else if (_captureXText != null)
			{
				writer.WriteText(_captureXText);
				_captureXText = null;
			}
			else if (_captureStream != null)
			{
				ArraySegment<byte> buffer;
				bool flag = _captureStream.TryGetBuffer(out buffer);
				writer.WriteBase64Text(null, 0, buffer.Array, buffer.Offset, buffer.Count);
				_captureStream = null;
			}
			else
			{
				writer.WriteEmptyText();
			}
		}
	}

	private IXmlDictionary _dictionary;

	private XmlBinaryWriterSession _session;

	private bool _inAttribute;

	private bool _inList;

	private bool _wroteAttributeValue;

	private AttributeValue _attributeValue;

	private const int maxBytesPerChar = 3;

	private int _textNodeOffset;

	public void SetOutput(Stream stream, IXmlDictionary dictionary, XmlBinaryWriterSession session, bool ownsStream)
	{
		_dictionary = dictionary;
		_session = session;
		_inAttribute = false;
		_inList = false;
		_attributeValue.Clear();
		_textNodeOffset = -1;
		SetOutput(stream, ownsStream, null);
	}

	private void WriteNode(XmlBinaryNodeType nodeType)
	{
		WriteByte((byte)nodeType);
		_textNodeOffset = -1;
	}

	private void WroteAttributeValue()
	{
		if (_wroteAttributeValue && !_inList)
		{
			throw new InvalidOperationException(System.SR.XmlOnlySingleValue);
		}
		_wroteAttributeValue = true;
	}

	private void WriteTextNode(XmlBinaryNodeType nodeType)
	{
		if (_inAttribute)
		{
			WroteAttributeValue();
		}
		WriteByte((byte)nodeType);
		_textNodeOffset = base.BufferOffset - 1;
	}

	private byte[] GetTextNodeBuffer(int size, out int offset)
	{
		if (_inAttribute)
		{
			WroteAttributeValue();
		}
		byte[] buffer = GetBuffer(size, out offset);
		_textNodeOffset = offset;
		return buffer;
	}

	private void WriteTextNodeWithLength(XmlBinaryNodeType nodeType, int length)
	{
		if (length < 256)
		{
			WriteTextNodeWithInt8(nodeType, (byte)length);
		}
		else if (length < 65536)
		{
			WriteTextNodeWithInt16(nodeType + 2, (short)length);
		}
		else
		{
			WriteTextNodeWithInt32(nodeType + 4, length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteTextNodeRaw<T>(XmlBinaryNodeType nodeType, T value) where T : unmanaged
	{
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(1 + Unsafe.SizeOf<T>(), out offset);
		ref byte reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(textNodeBuffer), offset);
		reference = (byte)nodeType;
		Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, 1), value);
		Advance(1 + Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteRaw<T>(T value) where T : unmanaged
	{
		int offset;
		byte[] buffer = GetBuffer(Unsafe.SizeOf<T>(), out offset);
		Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset), value);
		Advance(Unsafe.SizeOf<T>());
	}

	private void WriteTextNodeWithInt8(XmlBinaryNodeType nodeType, byte value)
	{
		WriteTextNodeRaw(nodeType, value);
	}

	private void WriteTextNodeWithInt16(XmlBinaryNodeType nodeType, short value)
	{
		WriteTextNodeRaw(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value));
	}

	private void WriteTextNodeWithInt32(XmlBinaryNodeType nodeType, int value)
	{
		WriteTextNodeRaw(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value));
	}

	private void WriteTextNodeWithInt64(XmlBinaryNodeType nodeType, long value)
	{
		WriteTextNodeRaw(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value));
	}

	public override void WriteDeclaration()
	{
	}

	public override void WriteStartElement(string prefix, string localName)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.MinElement);
			WriteName(localName);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && char.IsAsciiLetterLower(c))
		{
			WritePrefixNode(XmlBinaryNodeType.PrefixElementA, c - 97);
			WriteName(localName);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.Element);
			WriteName(prefix);
			WriteName(localName);
		}
	}

	private void WritePrefixNode(XmlBinaryNodeType nodeType, int ch)
	{
		WriteNode(nodeType + ch);
	}

	public override void WriteStartElement(string prefix, XmlDictionaryString localName)
	{
		if (!TryGetKey(localName, out var key))
		{
			WriteStartElement(prefix, localName.Value);
			return;
		}
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryElement);
			WriteDictionaryString(key);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && char.IsAsciiLetterLower(c))
		{
			WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryElementA, c - 97);
			WriteDictionaryString(key);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.DictionaryElement);
			WriteName(prefix);
			WriteDictionaryString(key);
		}
	}

	public override void WriteEndStartElement(bool isEmpty)
	{
		if (isEmpty)
		{
			WriteEndElement();
		}
	}

	public override void WriteEndElement(string prefix, string localName)
	{
		WriteEndElement();
	}

	private void WriteEndElement()
	{
		if (_textNodeOffset != -1)
		{
			byte[] streamBuffer = base.StreamBuffer;
			XmlBinaryNodeType xmlBinaryNodeType = (XmlBinaryNodeType)streamBuffer[_textNodeOffset];
			streamBuffer[_textNodeOffset] = (byte)(xmlBinaryNodeType + 1);
			_textNodeOffset = -1;
		}
		else
		{
			WriteNode(XmlBinaryNodeType.EndElement);
		}
	}

	public override void WriteStartAttribute(string prefix, string localName)
	{
		if (prefix.Length == 0)
		{
			WriteNode(XmlBinaryNodeType.MinAttribute);
			WriteName(localName);
		}
		else
		{
			char c = prefix[0];
			if (prefix.Length == 1 && char.IsAsciiLetterLower(c))
			{
				WritePrefixNode(XmlBinaryNodeType.PrefixAttributeA, c - 97);
				WriteName(localName);
			}
			else
			{
				WriteNode(XmlBinaryNodeType.Attribute);
				WriteName(prefix);
				WriteName(localName);
			}
		}
		_inAttribute = true;
		_wroteAttributeValue = false;
	}

	public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
	{
		if (!TryGetKey(localName, out var key))
		{
			WriteStartAttribute(prefix, localName.Value);
			return;
		}
		if (prefix.Length == 0)
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryAttribute);
			WriteDictionaryString(key);
		}
		else
		{
			char c = prefix[0];
			if (prefix.Length == 1 && char.IsAsciiLetterLower(c))
			{
				WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryAttributeA, c - 97);
				WriteDictionaryString(key);
			}
			else
			{
				WriteNode(XmlBinaryNodeType.DictionaryAttribute);
				WriteName(prefix);
				WriteDictionaryString(key);
			}
		}
		_inAttribute = true;
		_wroteAttributeValue = false;
	}

	public override void WriteEndAttribute()
	{
		_inAttribute = false;
		if (!_wroteAttributeValue)
		{
			_attributeValue.WriteTo(this);
		}
		_textNodeOffset = -1;
	}

	public override void WriteXmlnsAttribute(string prefix, string ns)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortXmlnsAttribute);
			WriteName(ns);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.XmlnsAttribute);
			WriteName(prefix);
			WriteName(ns);
		}
	}

	public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
	{
		if (!TryGetKey(ns, out var key))
		{
			WriteXmlnsAttribute(prefix, ns.Value);
		}
		else if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryXmlnsAttribute);
			WriteDictionaryString(key);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.DictionaryXmlnsAttribute);
			WriteName(prefix);
			WriteDictionaryString(key);
		}
	}

	private bool TryGetKey(XmlDictionaryString s, out int key)
	{
		key = -1;
		if (s.Dictionary == _dictionary)
		{
			key = s.Key * 2;
			return true;
		}
		if (_dictionary != null && _dictionary.TryLookup(s, out XmlDictionaryString result))
		{
			key = result.Key * 2;
			return true;
		}
		if (_session == null)
		{
			return false;
		}
		if (!_session.TryLookup(s, out var key2) && !_session.TryAdd(s, out key2))
		{
			return false;
		}
		key = key2 * 2 + 1;
		return true;
	}

	private void WriteDictionaryString(int key)
	{
		WriteMultiByteInt32(key);
	}

	private unsafe void WriteName(string s)
	{
		int length = s.Length;
		if (length == 0)
		{
			WriteByte(0);
			return;
		}
		fixed (char* chars = s)
		{
			UnsafeWriteName(chars, length);
		}
	}

	private unsafe void UnsafeWriteName(char* chars, int charCount)
	{
		if (charCount < 42)
		{
			int offset;
			byte[] buffer = GetBuffer(1 + charCount * 3, out offset);
			int num = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 1);
			buffer[offset] = (byte)num;
			Advance(1 + num);
		}
		else
		{
			int i = UnsafeGetUTF8Length(chars, charCount);
			WriteMultiByteInt32(i);
			UnsafeWriteUTF8Chars(chars, charCount);
		}
	}

	private void WriteMultiByteInt32(int i)
	{
		int offset;
		byte[] buffer = GetBuffer(5, out offset);
		int num = offset;
		while ((i & 0xFFFFFF80u) != 0L)
		{
			buffer[offset++] = (byte)(((uint)i & 0x7Fu) | 0x80u);
			i >>= 7;
		}
		buffer[offset++] = (byte)i;
		Advance(offset - num);
	}

	public override void WriteComment(string value)
	{
		WriteNode(XmlBinaryNodeType.Comment);
		WriteName(value);
	}

	public override void WriteCData(string value)
	{
		WriteText(value);
	}

	private void WriteEmptyText()
	{
		WriteTextNode(XmlBinaryNodeType.EmptyText);
	}

	public override void WriteBoolText(bool value)
	{
		if (value)
		{
			WriteTextNode(XmlBinaryNodeType.TrueText);
		}
		else
		{
			WriteTextNode(XmlBinaryNodeType.FalseText);
		}
	}

	public override void WriteInt32Text(int value)
	{
		if (value == (sbyte)value)
		{
			switch (value)
			{
			case 0:
				WriteTextNode(XmlBinaryNodeType.MinText);
				break;
			case 1:
				WriteTextNode(XmlBinaryNodeType.OneText);
				break;
			default:
				WriteTextNodeWithInt8(XmlBinaryNodeType.Int8Text, (byte)value);
				break;
			}
		}
		else if (value == (short)value)
		{
			WriteTextNodeWithInt16(XmlBinaryNodeType.Int16Text, (short)value);
		}
		else
		{
			WriteTextNodeWithInt32(XmlBinaryNodeType.Int32Text, value);
		}
	}

	public override void WriteInt64Text(long value)
	{
		if (value == (int)value)
		{
			WriteInt32Text((int)value);
		}
		else
		{
			WriteTextNodeWithInt64(XmlBinaryNodeType.Int64Text, value);
		}
	}

	public override void WriteUInt64Text(ulong value)
	{
		if (value <= long.MaxValue)
		{
			WriteInt64Text((long)value);
		}
		else
		{
			WriteTextNodeWithInt64(XmlBinaryNodeType.UInt64Text, (long)value);
		}
	}

	private void WriteInt64(long value)
	{
		WriteRaw(BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value));
	}

	public override void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] base64Buffer, int base64Offset, int base64Count)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteBase64Text(trailBytes, trailByteCount, base64Buffer, base64Offset, base64Count);
			return;
		}
		int num = trailByteCount + base64Count;
		if (num > 0)
		{
			WriteTextNodeWithLength(XmlBinaryNodeType.Bytes8Text, num);
			if (trailByteCount > 0)
			{
				int offset;
				byte[] buffer = GetBuffer(trailByteCount, out offset);
				for (int i = 0; i < trailByteCount; i++)
				{
					buffer[offset + i] = trailBytes[i];
				}
				Advance(trailByteCount);
			}
			if (base64Count > 0)
			{
				WriteBytes(base64Buffer, base64Offset, base64Count);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	public override void WriteText(XmlDictionaryString value)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(value);
			return;
		}
		if (!TryGetKey(value, out var key))
		{
			WriteText(value.Value);
			return;
		}
		WriteTextNode(XmlBinaryNodeType.DictionaryText);
		WriteDictionaryString(key);
	}

	public override void WriteText(string value)
	{
		WriteTextImpl(value);
	}

	public override void WriteText(char[] chars, int offset, int count)
	{
		WriteTextImpl(chars.AsSpan(offset, count));
	}

	private unsafe void WriteTextImpl(string value)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(value);
		}
		else if (value.Length > 0)
		{
			fixed (char* chars = value)
			{
				UnsafeWriteText(chars, value.Length);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	private unsafe void WriteTextImpl(ReadOnlySpan<char> chars)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(chars.ToString());
		}
		else if (chars.Length > 0)
		{
			fixed (char* chars2 = &MemoryMarshal.GetReference(chars))
			{
				UnsafeWriteText(chars2, chars.Length);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	public override void WriteText(byte[] chars, int charOffset, int charCount)
	{
		WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, charCount);
		WriteBytes(chars, charOffset, charCount);
	}

	private unsafe void UnsafeWriteText(char* chars, int charCount)
	{
		if (charCount == 1)
		{
			switch (*chars)
			{
			case '0':
				WriteTextNode(XmlBinaryNodeType.MinText);
				return;
			case '1':
				WriteTextNode(XmlBinaryNodeType.OneText);
				return;
			}
		}
		if (charCount <= 85)
		{
			int offset;
			byte[] buffer = GetBuffer(2 + charCount * 3, out offset);
			int num = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 2);
			if (num / 2 <= charCount)
			{
				buffer[offset] = 152;
			}
			else
			{
				buffer[offset] = 182;
				num = XmlStreamNodeWriter.UnsafeGetUnicodeChars(chars, charCount, buffer, offset + 2);
			}
			_textNodeOffset = offset;
			buffer[offset + 1] = (byte)num;
			Advance(2 + num);
		}
		else
		{
			int num2 = UnsafeGetUTF8Length(chars, charCount);
			if (num2 / 2 > charCount)
			{
				WriteTextNodeWithLength(XmlBinaryNodeType.UnicodeChars8Text, charCount * 2);
				UnsafeWriteUnicodeChars(chars, charCount);
			}
			else
			{
				WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, num2);
				UnsafeWriteUTF8Chars(chars, charCount);
			}
		}
	}

	public override void WriteEscapedText(string value)
	{
		WriteText(value);
	}

	public override void WriteEscapedText(XmlDictionaryString value)
	{
		WriteText(value);
	}

	public override void WriteEscapedText(char[] chars, int offset, int count)
	{
		WriteText(chars, offset, count);
	}

	public override void WriteEscapedText(byte[] chars, int offset, int count)
	{
		WriteText(chars, offset, count);
	}

	public override void WriteCharEntity(int ch)
	{
		if (ch > 65535)
		{
			SurrogateChar surrogateChar = new SurrogateChar(ch);
			Span<char> span = stackalloc char[2] { surrogateChar.HighChar, surrogateChar.LowChar };
			WriteTextImpl(span);
		}
		else
		{
			char reference = (char)ch;
			WriteTextImpl(new ReadOnlySpan<char>(ref reference));
		}
	}

	public override void WriteFloatText(float f)
	{
		int value;
		if (f >= -32768f && f <= 32767f && (float)(value = (int)f) == f)
		{
			WriteInt32Text(value);
			return;
		}
		if (BitConverter.IsLittleEndian)
		{
			WriteTextNodeRaw(XmlBinaryNodeType.FloatText, f);
			return;
		}
		int offset;
		Span<byte> span = GetTextNodeBuffer(5, out offset).AsSpan(offset, 5);
		span[0] = 144;
		BinaryPrimitives.WriteSingleLittleEndian(span.Slice(1), f);
		Advance(5);
	}

	public override void WriteDoubleText(double d)
	{
		float value;
		if ((double)(value = (float)d) == d)
		{
			WriteFloatText(value);
			return;
		}
		if (BitConverter.IsLittleEndian)
		{
			WriteTextNodeRaw(XmlBinaryNodeType.DoubleText, d);
			return;
		}
		int offset;
		Span<byte> span = GetTextNodeBuffer(9, out offset).AsSpan(offset, 9);
		span[0] = 146;
		BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(1), d);
		Advance(9);
	}

	public override void WriteDecimalText(decimal d)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteTextNodeRaw(XmlBinaryNodeType.DecimalText, d);
			return;
		}
		Span<int> destination = stackalloc int[4];
		decimal.TryGetBits(d, destination, out var _);
		int offset;
		Span<byte> span = GetTextNodeBuffer(17, out offset).AsSpan(offset, 17);
		span[0] = 148;
		BinaryPrimitives.WriteInt32LittleEndian(span.Slice(1), destination[3]);
		BinaryPrimitives.WriteInt32LittleEndian(span.Slice(5), destination[2]);
		BinaryPrimitives.WriteInt32LittleEndian(span.Slice(9), destination[0]);
		BinaryPrimitives.WriteInt32LittleEndian(span.Slice(13), destination[1]);
		Advance(17);
	}

	public override void WriteDateTimeText(DateTime dt)
	{
		WriteTextNodeWithInt64(XmlBinaryNodeType.DateTimeText, dt.ToBinary());
	}

	public override void WriteUniqueIdText(UniqueId value)
	{
		if (value.IsGuid)
		{
			int offset;
			byte[] textNodeBuffer = GetTextNodeBuffer(17, out offset);
			textNodeBuffer[offset] = 172;
			value.TryGetGuid(textNodeBuffer, offset + 1);
			Advance(17);
		}
		else
		{
			WriteText(value.ToString());
		}
	}

	public override void WriteGuidText(Guid guid)
	{
		int offset;
		Span<byte> span = GetTextNodeBuffer(17, out offset).AsSpan(offset, 17);
		span[0] = 176;
		guid.TryWriteBytes(span.Slice(1));
		Advance(17);
	}

	public override void WriteTimeSpanText(TimeSpan value)
	{
		WriteTextNodeWithInt64(XmlBinaryNodeType.TimeSpanText, value.Ticks);
	}

	public override void WriteStartListText()
	{
		_inList = true;
		WriteNode(XmlBinaryNodeType.StartListText);
	}

	public override void WriteListSeparator()
	{
	}

	public override void WriteEndListText()
	{
		_inList = false;
		_wroteAttributeValue = true;
		WriteNode(XmlBinaryNodeType.EndListText);
	}

	public void WriteArrayNode()
	{
		WriteNode(XmlBinaryNodeType.Array);
	}

	private void WriteArrayInfo(XmlBinaryNodeType nodeType, int count)
	{
		WriteNode(nodeType);
		WriteMultiByteInt32(count);
	}

	public void WriteArray(XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
	{
		WriteArrayInfo(nodeType, count);
		WriteBytes(bytes);
	}

	public void WriteBoolArray(ReadOnlySpan<bool> items)
	{
		WriteArray(XmlBinaryNodeType.BoolTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
	}

	public void WriteInt16Array(ReadOnlySpan<short> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.Int16TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.Int16TextWithEndElement, items.Length);
		ReadOnlySpan<short> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			short value = readOnlySpan[i];
			WriteRaw(BinaryPrimitives.ReverseEndianness(value));
		}
	}

	public void WriteInt32Array(ReadOnlySpan<int> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.Int32TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.Int32TextWithEndElement, items.Length);
		ReadOnlySpan<int> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			int value = readOnlySpan[i];
			WriteRaw(BinaryPrimitives.ReverseEndianness(value));
		}
	}

	public void WriteInt64Array(ReadOnlySpan<long> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.Int64TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.Int64TextWithEndElement, items.Length);
		ReadOnlySpan<long> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			long value = readOnlySpan[i];
			WriteRaw(BinaryPrimitives.ReverseEndianness(value));
		}
	}

	public void WriteFloatArray(ReadOnlySpan<float> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.FloatTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.FloatTextWithEndElement, items.Length);
		ReadOnlySpan<float> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			float value = readOnlySpan[i];
			int offset;
			Span<byte> destination = GetBuffer(4, out offset).AsSpan(offset, 4);
			BinaryPrimitives.WriteSingleLittleEndian(destination, value);
			Advance(4);
		}
	}

	public void WriteDoubleArray(ReadOnlySpan<double> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.DoubleTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.DoubleTextWithEndElement, items.Length);
		ReadOnlySpan<double> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			double value = readOnlySpan[i];
			int offset;
			Span<byte> destination = GetBuffer(8, out offset).AsSpan(offset, 8);
			BinaryPrimitives.WriteDoubleLittleEndian(destination, value);
			Advance(8);
		}
	}

	public void WriteDecimalArray(ReadOnlySpan<decimal> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.DecimalTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		Span<int> destination = stackalloc int[4];
		WriteArrayInfo(XmlBinaryNodeType.DecimalTextWithEndElement, items.Length);
		ReadOnlySpan<decimal> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			decimal.TryGetBits(readOnlySpan[i], destination, out var _);
			int offset;
			Span<byte> destination2 = GetBuffer(16, out offset).AsSpan(offset, 16);
			BinaryPrimitives.WriteInt32LittleEndian(destination2, destination[3]);
			BinaryPrimitives.WriteInt32LittleEndian(destination2.Slice(4), destination[2]);
			BinaryPrimitives.WriteInt32LittleEndian(destination2.Slice(8), destination[0]);
			BinaryPrimitives.WriteInt32LittleEndian(destination2.Slice(12), destination[1]);
			Advance(16);
		}
	}

	public void WriteDateTimeArray(ReadOnlySpan<DateTime> items)
	{
		WriteArrayInfo(XmlBinaryNodeType.DateTimeTextWithEndElement, items.Length);
		ReadOnlySpan<DateTime> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			DateTime dateTime = readOnlySpan[i];
			WriteInt64(dateTime.ToBinary());
		}
	}

	public void WriteGuidArray(ReadOnlySpan<Guid> items)
	{
		if (BitConverter.IsLittleEndian)
		{
			WriteArray(XmlBinaryNodeType.GuidTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
			return;
		}
		WriteArrayInfo(XmlBinaryNodeType.GuidTextWithEndElement, items.Length);
		ReadOnlySpan<Guid> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			ref readonly Guid reference = ref readOnlySpan[i];
			int offset;
			Span<byte> destination = GetBuffer(16, out offset).AsSpan(offset, 16);
			reference.TryWriteBytes(destination);
			Advance(16);
		}
	}

	public void WriteTimeSpanArray(ReadOnlySpan<TimeSpan> items)
	{
		WriteArrayInfo(XmlBinaryNodeType.TimeSpanTextWithEndElement, items.Length);
		ReadOnlySpan<TimeSpan> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			WriteInt64(readOnlySpan[i].Ticks);
		}
	}

	public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
	{
		if (prefix.Length == 0)
		{
			WriteText(localName);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && char.IsAsciiLetterLower(c) && TryGetKey(localName, out var key))
		{
			WriteTextNodeWithInt8(XmlBinaryNodeType.QNameDictionaryText, (byte)(c - 97));
			WriteDictionaryString(key);
		}
		else
		{
			WriteText(prefix);
			WriteText(":");
			WriteText(localName);
		}
	}

	protected override void FlushBuffer()
	{
		base.FlushBuffer();
		_textNodeOffset = -1;
	}

	public override void Close()
	{
		base.Close();
		_attributeValue.Clear();
	}
}
