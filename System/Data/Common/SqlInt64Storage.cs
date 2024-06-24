using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlInt64Storage : DataStorage
{
	private SqlInt64[] _values;

	public SqlInt64Storage(DataColumn column)
		: base(column, typeof(SqlInt64), SqlInt64.Null, SqlInt64.Null, StorageType.SqlInt64)
	{
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		bool flag = false;
		try
		{
			switch (kind)
			{
			case AggregateType.Sum:
			{
				SqlInt64 sqlInt2 = 0L;
				foreach (int num3 in records)
				{
					if (!IsNull(num3))
					{
						sqlInt2 += _values[num3];
						flag = true;
					}
				}
				if (flag)
				{
					return sqlInt2;
				}
				return _nullValue;
			}
			case AggregateType.Mean:
			{
				SqlDecimal sqlDecimal = 0L;
				int num5 = 0;
				foreach (int num6 in records)
				{
					if (!IsNull(num6))
					{
						sqlDecimal += _values[num6].ToSqlDecimal();
						num5++;
						flag = true;
					}
				}
				if (flag)
				{
					SqlInt64 sqlInt3 = 0L;
					sqlInt3 = (sqlDecimal / num5).ToSqlInt64();
					return sqlInt3;
				}
				return _nullValue;
			}
			case AggregateType.Var:
			case AggregateType.StDev:
			{
				int num = 0;
				SqlDouble sqlDouble = 0.0;
				SqlDouble sqlDouble2 = 0.0;
				SqlDouble sqlDouble3 = 0.0;
				SqlDouble sqlDouble4 = 0.0;
				foreach (int num4 in records)
				{
					if (!IsNull(num4))
					{
						sqlDouble3 += _values[num4].ToSqlDouble();
						sqlDouble4 += _values[num4].ToSqlDouble() * _values[num4].ToSqlDouble();
						num++;
					}
				}
				if (num > 1)
				{
					sqlDouble = num * sqlDouble4 - sqlDouble3 * sqlDouble3;
					sqlDouble2 = sqlDouble / (sqlDouble3 * sqlDouble3);
					if (sqlDouble2 < 1E-15 || sqlDouble < 0.0)
					{
						sqlDouble = 0.0;
					}
					else
					{
						sqlDouble /= (SqlDouble)(num * (num - 1));
					}
					if (kind == AggregateType.StDev)
					{
						return Math.Sqrt(sqlDouble.Value);
					}
					return sqlDouble;
				}
				return _nullValue;
			}
			case AggregateType.Min:
			{
				SqlInt64 sqlInt = SqlInt64.MaxValue;
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						if (SqlInt64.LessThan(_values[num2], sqlInt).IsTrue)
						{
							sqlInt = _values[num2];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlInt;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				SqlInt64 sqlInt4 = SqlInt64.MinValue;
				foreach (int num7 in records)
				{
					if (!IsNull(num7))
					{
						if (SqlInt64.GreaterThan(_values[num7], sqlInt4).IsTrue)
						{
							sqlInt4 = _values[num7];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlInt4;
				}
				return _nullValue;
			}
			case AggregateType.First:
				if (records.Length != 0)
				{
					return _values[records[0]];
				}
				return null;
			case AggregateType.Count:
			{
				int num = 0;
				for (int i = 0; i < records.Length; i++)
				{
					if (!IsNull(records[i]))
					{
						num++;
					}
				}
				return num;
			}
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(typeof(SqlInt64));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return _values[recordNo1].CompareTo(_values[recordNo2]);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		return _values[recordNo].CompareTo((SqlInt64)value);
	}

	public override object ConvertValue(object value)
	{
		if (value != null)
		{
			return SqlConvert.ConvertToSqlInt64(value);
		}
		return _nullValue;
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int record)
	{
		return _values[record];
	}

	public override bool IsNull(int record)
	{
		return _values[record].IsNull;
	}

	public override void Set(int record, object value)
	{
		_values[record] = SqlConvert.ConvertToSqlInt64(value);
	}

	public override void SetCapacity(int capacity)
	{
		Array.Resize(ref _values, capacity);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		SqlInt64 sqlInt = default(SqlInt64);
		string s2 = "<col>" + s + "</col>";
		StringReader input = new StringReader(s2);
		IXmlSerializable xmlSerializable = sqlInt;
		using (XmlTextReader reader = new XmlTextReader(input))
		{
			xmlSerializable.ReadXml(reader);
		}
		return (SqlInt64)(object)xmlSerializable;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		StringWriter stringWriter = new StringWriter(base.FormatProvider);
		using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
		{
			((IXmlSerializable)value).WriteXml(writer);
		}
		return stringWriter.ToString();
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new SqlInt64[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		SqlInt64[] array = (SqlInt64[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (SqlInt64[])store;
	}
}
