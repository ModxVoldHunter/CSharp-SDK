using System.ComponentModel;
using System.Runtime.Serialization;

namespace System;

[Serializable]
public sealed class DBNull : ISerializable, IConvertible
{
	public static readonly DBNull Value = new DBNull();

	private DBNull()
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		UnitySerializationHolder.GetUnitySerializationInfo(info, 2);
	}

	public override string ToString()
	{
		return string.Empty;
	}

	public string ToString(IFormatProvider? provider)
	{
		return string.Empty;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.DBNull;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.InvalidCast_FromDBNull);
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}
}
