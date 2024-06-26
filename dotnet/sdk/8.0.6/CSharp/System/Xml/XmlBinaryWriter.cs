using System.IO;

namespace System.Xml;

internal sealed class XmlBinaryWriter : XmlBaseWriter, IXmlBinaryWriterInitializer
{
	private XmlBinaryNodeWriter _writer;

	private char[] _chars;

	private byte[] _bytes;

	public void SetOutput(Stream stream, IXmlDictionary dictionary, XmlBinaryWriterSession session, bool ownsStream)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		if (_writer == null)
		{
			_writer = new XmlBinaryNodeWriter();
		}
		_writer.SetOutput(stream, dictionary, session, ownsStream);
		SetOutput(_writer);
	}

	protected override XmlSigningNodeWriter CreateSigningNodeWriter()
	{
		return new XmlSigningNodeWriter(text: false);
	}

	protected override void WriteTextNode(XmlDictionaryReader reader, bool attribute)
	{
		Type valueType = reader.ValueType;
		if (valueType == typeof(string))
		{
			if (reader.TryGetValueAsDictionaryString(out XmlDictionaryString value))
			{
				WriteString(value);
			}
			else if (reader.CanReadValueChunk)
			{
				if (_chars == null)
				{
					_chars = new char[256];
				}
				int count;
				while ((count = reader.ReadValueChunk(_chars, 0, _chars.Length)) > 0)
				{
					WriteChars(_chars, 0, count);
				}
			}
			else
			{
				WriteString(reader.Value);
			}
			if (!attribute)
			{
				reader.Read();
			}
		}
		else if (valueType == typeof(byte[]))
		{
			if (reader.CanReadBinaryContent)
			{
				if (_bytes == null)
				{
					_bytes = new byte[384];
				}
				int count2;
				while ((count2 = reader.ReadValueAsBase64(_bytes, 0, _bytes.Length)) > 0)
				{
					WriteBase64(_bytes, 0, count2);
				}
			}
			else
			{
				WriteString(reader.Value);
			}
			if (!attribute)
			{
				reader.Read();
			}
		}
		else if (valueType == typeof(int))
		{
			WriteValue(reader.ReadContentAsInt());
		}
		else if (valueType == typeof(long))
		{
			WriteValue(reader.ReadContentAsLong());
		}
		else if (valueType == typeof(bool))
		{
			WriteValue(reader.ReadContentAsBoolean());
		}
		else if (valueType == typeof(double))
		{
			WriteValue(reader.ReadContentAsDouble());
		}
		else if (valueType == typeof(DateTime))
		{
			WriteValue(reader.ReadContentAsDateTimeOffset().DateTime);
		}
		else if (valueType == typeof(float))
		{
			WriteValue(reader.ReadContentAsFloat());
		}
		else if (valueType == typeof(decimal))
		{
			WriteValue(reader.ReadContentAsDecimal());
		}
		else if (valueType == typeof(UniqueId))
		{
			WriteValue(reader.ReadContentAsUniqueId());
		}
		else if (valueType == typeof(Guid))
		{
			WriteValue(reader.ReadContentAsGuid());
		}
		else if (valueType == typeof(TimeSpan))
		{
			WriteValue(reader.ReadContentAsTimeSpan());
		}
		else
		{
			WriteValue(reader.ReadContentAsObject());
		}
	}

	private void WriteStartArray(string prefix, string localName, string namespaceUri, int count)
	{
		StartArray(count);
		_writer.WriteArrayNode();
		WriteStartElement(prefix, localName, namespaceUri);
		WriteEndElement();
	}

	private void WriteStartArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int count)
	{
		StartArray(count);
		_writer.WriteArrayNode();
		WriteStartElement(prefix, localName, namespaceUri);
		WriteEndElement();
	}

	private void WriteArray(string prefix, string localName, string namespaceUri, XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
	{
		WriteStartArray(prefix, localName, namespaceUri, count);
		_writer.WriteArray(nodeType, count, bytes);
	}

	private void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
	{
		WriteStartArray(prefix, localName, namespaceUri, count);
		_writer.WriteArray(nodeType, count, bytes);
	}

	private static void CheckArray(Array array, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (offset > array.Length)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, array.Length));
		}
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (count > array.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, bool[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteBoolArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, short[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteInt16Array(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, int[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteInt32Array(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, long[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteInt64Array(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, float[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteFloatArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, double[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteDoubleArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteDecimalArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteDateTimeArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, Guid[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteGuidArray(array.AsSpan(offset, count));
		}
	}

	public override void WriteArray(string prefix, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
	{
		if (base.Signing)
		{
			base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			return;
		}
		CheckArray(array, offset, count);
		if (count > 0)
		{
			WriteStartArray(prefix, localName, namespaceUri, count);
			_writer.WriteTimeSpanArray(array.AsSpan(offset, count));
		}
	}
}
