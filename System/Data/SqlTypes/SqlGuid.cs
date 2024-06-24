using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[XmlSchemaProvider("GetXsdType")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct SqlGuid : INullable, IComparable, ISerializable, IXmlSerializable, IEquatable<SqlGuid>
{
	private Guid? _value;

	public static readonly SqlGuid Null;

	public bool IsNull
	{
		get
		{
			Guid? value = _value;
			return !value.HasValue;
		}
	}

	public Guid Value => _value ?? throw new SqlNullValueException();

	public SqlGuid(byte[] value)
	{
		if (value == null || value.Length != 16)
		{
			throw new ArgumentException(SQLResource.InvalidArraySizeMessage);
		}
		_value = new Guid(value);
	}

	public SqlGuid(string s)
	{
		_value = new Guid(s);
	}

	public SqlGuid(Guid g)
	{
		_value = g;
	}

	public SqlGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
		: this(new Guid(a, b, c, d, e, f, g, h, i, j, k))
	{
	}

	private SqlGuid(SerializationInfo info, StreamingContext context)
	{
		byte[] array = (byte[])info.GetValue("m_value", typeof(byte[]));
		if (array == null)
		{
			_value = null;
		}
		else
		{
			_value = new Guid(array);
		}
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("m_value", ToByteArray(), typeof(byte[]));
	}

	public static implicit operator SqlGuid(Guid x)
	{
		return new SqlGuid(x);
	}

	public static explicit operator Guid(SqlGuid x)
	{
		return x.Value;
	}

	public byte[]? ToByteArray()
	{
		Guid? value = _value;
		if (!value.HasValue)
		{
			return null;
		}
		return _value.GetValueOrDefault().ToByteArray();
	}

	public override string ToString()
	{
		Guid? value = _value;
		if (!value.HasValue)
		{
			return SQLResource.NullString;
		}
		return _value.GetValueOrDefault().ToString();
	}

	public static SqlGuid Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlGuid(s);
	}

	private static EComparison Compare(SqlGuid x, SqlGuid y)
	{
		ReadOnlySpan<byte> readOnlySpan = new byte[16]
		{
			10, 11, 12, 13, 14, 15, 8, 9, 6, 7,
			4, 5, 0, 1, 2, 3
		};
		Span<byte> destination = stackalloc byte[16];
		bool flag = x._value.GetValueOrDefault().TryWriteBytes(destination);
		Span<byte> destination2 = stackalloc byte[16];
		bool flag2 = y._value.GetValueOrDefault().TryWriteBytes(destination2);
		for (int i = 0; i < 16; i++)
		{
			byte b = destination[readOnlySpan[i]];
			byte b2 = destination2[readOnlySpan[i]];
			if (b != b2)
			{
				if (b >= b2)
				{
					return EComparison.GT;
				}
				return EComparison.LT;
			}
		}
		return EComparison.EQ;
	}

	public static explicit operator SqlGuid(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlGuid(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlGuid(SqlBinary x)
	{
		if (!x.IsNull)
		{
			return new SqlGuid(x.Value);
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value.GetValueOrDefault() == y._value.GetValueOrDefault());
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlGuid x, SqlGuid y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(Compare(x, y) == EComparison.LT);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(Compare(x, y) == EComparison.GT);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlGuid x, SqlGuid y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = Compare(x, y);
		return new SqlBoolean(eComparison == EComparison.LT || eComparison == EComparison.EQ);
	}

	public static SqlBoolean operator >=(SqlGuid x, SqlGuid y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = Compare(x, y);
		return new SqlBoolean(eComparison == EComparison.GT || eComparison == EComparison.EQ);
	}

	public static SqlBoolean Equals(SqlGuid x, SqlGuid y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlGuid x, SqlGuid y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlGuid x, SqlGuid y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlGuid x, SqlGuid y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlGuid x, SqlGuid y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlGuid x, SqlGuid y)
	{
		return x >= y;
	}

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public SqlBinary ToSqlBinary()
	{
		return (SqlBinary)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlGuid value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlGuid));
	}

	public int CompareTo(SqlGuid value)
	{
		if (IsNull)
		{
			if (!value.IsNull)
			{
				return -1;
			}
			return 0;
		}
		if (value.IsNull)
		{
			return 1;
		}
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is SqlGuid other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(SqlGuid other)
	{
		Guid? value = _value;
		Guid? value2 = other._value;
		if (value.HasValue != value2.HasValue)
		{
			return false;
		}
		if (!value.HasValue)
		{
			return true;
		}
		return value.GetValueOrDefault() == value2.GetValueOrDefault();
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		string attribute = reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		if (attribute != null && XmlConvert.ToBoolean(attribute))
		{
			reader.ReadElementString();
			_value = null;
		}
		else
		{
			_value = new Guid(reader.ReadElementString());
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		Guid? value = _value;
		if (!value.HasValue)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			writer.WriteString(XmlConvert.ToString(_value.GetValueOrDefault()));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
	}
}
