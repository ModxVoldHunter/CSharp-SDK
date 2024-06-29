using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.Emit;

internal struct CustomAttributeInfo
{
	public ConstructorInfo _ctor;

	public object[] _ctorArgs;

	public string[] _namedParamNames;

	public object[] _namedParamValues;

	internal static CustomAttributeInfo DecodeCustomAttribute(ConstructorInfo ctor, ReadOnlySpan<byte> binaryAttribute)
	{
		int rpos = 2;
		CustomAttributeInfo result = default(CustomAttributeInfo);
		if (binaryAttribute.Length < 2)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidCustomAttributeLength, ctor.DeclaringType, binaryAttribute.Length), "binaryAttribute");
		}
		if (binaryAttribute[0] != 1 || binaryAttribute[1] != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidProlog, ctor.DeclaringType), "binaryAttribute");
		}
		ParameterInfo[] parameters = ctor.GetParameters();
		result._ctor = ctor;
		result._ctorArgs = new object[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			result._ctorArgs[i] = DecodeCustomAttributeValue(parameters[i].ParameterType, binaryAttribute, rpos, out rpos);
		}
		int num = BinaryPrimitives.ReadUInt16LittleEndian(binaryAttribute.Slice(rpos));
		rpos += 2;
		result._namedParamNames = new string[num];
		result._namedParamValues = new object[num];
		for (int j = 0; j < num; j++)
		{
			int num2 = binaryAttribute[rpos++];
			int num3 = binaryAttribute[rpos++];
			if (num3 == 85)
			{
				int num4 = DecodeLen(binaryAttribute, rpos, out rpos);
				rpos += num4;
			}
			int num5 = DecodeLen(binaryAttribute, rpos, out rpos);
			string text = StringFromBytes(binaryAttribute, rpos, num5);
			result._namedParamNames[j] = text;
			rpos += num5;
			if (num2 == 83)
			{
				Type t = ((num3 == 85) ? typeof(int) : ElementTypeToType((PrimitiveSerializationTypeCode)num3));
				result._namedParamValues[j] = DecodeCustomAttributeValue(t, binaryAttribute, rpos, out rpos);
				continue;
			}
			throw new ArgumentException(System.SR.Format(System.SR.Argument_UnknownNamedType, ctor.DeclaringType, num2), "binaryAttribute");
		}
		return result;
	}

	private static string StringFromBytes(ReadOnlySpan<byte> data, int pos, int len)
	{
		return Encoding.UTF8.GetString(data.Slice(pos, len));
	}

	private static int DecodeLen(ReadOnlySpan<byte> data, int pos, out int rpos)
	{
		int result;
		if ((data[pos] & 0x80) == 0)
		{
			result = data[pos++] & 0x7F;
		}
		else if ((data[pos] & 0x40) == 0)
		{
			result = ((data[pos] & 0x3F) << 8) + data[pos + 1];
			pos += 2;
		}
		else
		{
			result = ((data[pos] & 0x1F) << 24) + (data[pos + 1] << 16) + (data[pos + 2] << 8) + data[pos + 3];
			pos += 4;
		}
		rpos = pos;
		return result;
	}

	private static object DecodeCustomAttributeValue(Type t, ReadOnlySpan<byte> data, int pos, out int rpos)
	{
		switch (Type.GetTypeCode(t))
		{
		case TypeCode.String:
		{
			if (data[pos] == byte.MaxValue)
			{
				rpos = pos + 1;
				return null;
			}
			int num2 = DecodeLen(data, pos, out pos);
			rpos = pos + num2;
			return StringFromBytes(data, pos, num2);
		}
		case TypeCode.Int32:
			rpos = pos + 4;
			return BinaryPrimitives.ReadInt32LittleEndian(data.Slice(pos));
		case TypeCode.Int16:
			rpos = pos + 2;
			return BinaryPrimitives.ReadInt16LittleEndian(data.Slice(pos));
		case TypeCode.Boolean:
			rpos = pos + 1;
			return data[pos] != 0;
		case TypeCode.Object:
		{
			int num = data[pos];
			pos++;
			if (num >= 2 && num <= 14)
			{
				return DecodeCustomAttributeValue(ElementTypeToType((PrimitiveSerializationTypeCode)num), data, pos, out rpos);
			}
			break;
		}
		}
		throw new NotImplementedException(System.SR.Format(System.SR.NotImplemented_TypeForValue, t));
	}

	private static Type ElementTypeToType(PrimitiveSerializationTypeCode elementType)
	{
		return elementType switch
		{
			PrimitiveSerializationTypeCode.Boolean => typeof(bool), 
			PrimitiveSerializationTypeCode.Char => typeof(char), 
			PrimitiveSerializationTypeCode.SByte => typeof(sbyte), 
			PrimitiveSerializationTypeCode.Byte => typeof(byte), 
			PrimitiveSerializationTypeCode.Int16 => typeof(short), 
			PrimitiveSerializationTypeCode.UInt16 => typeof(ushort), 
			PrimitiveSerializationTypeCode.Int32 => typeof(int), 
			PrimitiveSerializationTypeCode.UInt32 => typeof(uint), 
			PrimitiveSerializationTypeCode.Int64 => typeof(long), 
			PrimitiveSerializationTypeCode.UInt64 => typeof(ulong), 
			PrimitiveSerializationTypeCode.Single => typeof(float), 
			PrimitiveSerializationTypeCode.Double => typeof(double), 
			PrimitiveSerializationTypeCode.String => typeof(string), 
			_ => throw new ArgumentException(System.SR.Argument_InvalidTypeCodeForTypeArgument, "binaryAttribute"), 
		};
	}
}
