using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlDecimalStorage : DataStorage
{
	private SqlDecimal[] _values;

	public SqlDecimalStorage(DataColumn column)
		: base(column, typeof(SqlDecimal), SqlDecimal.Null, SqlDecimal.Null, StorageType.SqlDecimal)
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
				SqlDecimal sqlDecimal2 = 0L;
				foreach (int num3 in records)
				{
					if (!IsNull(num3))
					{
						sqlDecimal2 += _values[num3];
						flag = true;
					}
				}
				if (flag)
				{
					return sqlDecimal2;
				}
				return _nullValue;
			}
			case AggregateType.Mean:
			{
				SqlDecimal sqlDecimal3 = 0L;
				int num5 = 0;
				foreach (int num6 in records)
				{
					if (!IsNull(num6))
					{
						sqlDecimal3 += _values[num6];
						num5++;
						flag = true;
					}
				}
				if (flag)
				{
					SqlDecimal sqlDecimal4 = 0L;
					sqlDecimal4 = sqlDecimal3 / num5;
					return sqlDecimal4;
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
				SqlDecimal sqlDecimal = SqlDecimal.MaxValue;
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						if (SqlDecimal.LessThan(_values[num2], sqlDecimal).IsTrue)
						{
							sqlDecimal = _values[num2];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlDecimal;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				SqlDecimal sqlDecimal5 = SqlDecimal.MinValue;
				foreach (int num7 in records)
				{
					if (!IsNull(num7))
					{
						if (SqlDecimal.GreaterThan(_values[num7], sqlDecimal5).IsTrue)
						{
							sqlDecimal5 = _values[num7];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlDecimal5;
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
			throw ExprException.Overflow(typeof(SqlDecimal));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return _values[recordNo1].CompareTo(_values[recordNo2]);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		return _values[recordNo].CompareTo((SqlDecimal)value);
	}

	public override object ConvertValue(object value)
	{
		if (value != null)
		{
			return SqlConvert.ConvertToSqlDecimal(value);
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
		_values[record] = SqlConvert.ConvertToSqlDecimal(value);
	}

	public override void SetCapacity(int capacity)
	{
		Array.Resize(ref _values, capacity);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		SqlDecimal sqlDecimal = default(SqlDecimal);
		string s2 = "<col>" + s + "</col>";
		StringReader input = new StringReader(s2);
		IXmlSerializable xmlSerializable = sqlDecimal;
		using (XmlTextReader reader = new XmlTextReader(input))
		{
			xmlSerializable.ReadXml(reader);
		}
		return (SqlDecimal)(object)xmlSerializable;
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
		return new SqlDecimal[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		SqlDecimal[] array = (SqlDecimal[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (SqlDecimal[])store;
	}
}
