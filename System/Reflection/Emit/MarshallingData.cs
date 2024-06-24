using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

internal sealed class MarshallingData
{
	private UnmanagedType _marshalType;

	private int _marshalArrayElementType;

	private int _marshalArrayElementCount;

	private int _marshalParameterIndex;

	private object _marshalTypeNameOrSymbol;

	private string _marshalCookie;

	internal BlobBuilder SerializeMarshallingData()
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		blobBuilder.WriteCompressedInteger((int)_marshalType);
		switch (_marshalType)
		{
		case UnmanagedType.ByValArray:
			blobBuilder.WriteCompressedInteger(_marshalArrayElementCount);
			if (_marshalArrayElementType >= 0)
			{
				blobBuilder.WriteCompressedInteger(_marshalArrayElementType);
			}
			break;
		case UnmanagedType.CustomMarshaler:
		{
			blobBuilder.WriteUInt16(0);
			object marshalTypeNameOrSymbol = _marshalTypeNameOrSymbol;
			if (!(marshalTypeNameOrSymbol is Type type2))
			{
				if (marshalTypeNameOrSymbol == null)
				{
					blobBuilder.WriteByte(0);
				}
				else
				{
					blobBuilder.WriteSerializedString((string)_marshalTypeNameOrSymbol);
				}
			}
			else
			{
				blobBuilder.WriteSerializedString(type2.FullName);
			}
			if (_marshalCookie != null)
			{
				blobBuilder.WriteSerializedString(_marshalCookie);
			}
			else
			{
				blobBuilder.WriteByte(0);
			}
			break;
		}
		case UnmanagedType.LPArray:
			blobBuilder.WriteCompressedInteger(_marshalArrayElementType);
			if (_marshalParameterIndex >= 0)
			{
				blobBuilder.WriteCompressedInteger(_marshalParameterIndex);
				if (_marshalArrayElementCount >= 0)
				{
					blobBuilder.WriteCompressedInteger(_marshalArrayElementCount);
					blobBuilder.WriteByte(1);
				}
			}
			else if (_marshalArrayElementCount >= 0)
			{
				blobBuilder.WriteByte(0);
				blobBuilder.WriteCompressedInteger(_marshalArrayElementCount);
				blobBuilder.WriteByte(0);
			}
			break;
		case UnmanagedType.SafeArray:
		{
			VarEnum marshalArrayElementType = (VarEnum)_marshalArrayElementType;
			if (marshalArrayElementType >= VarEnum.VT_EMPTY)
			{
				blobBuilder.WriteCompressedInteger((int)marshalArrayElementType);
				if (_marshalTypeNameOrSymbol is Type type)
				{
					blobBuilder.WriteSerializedString(type.FullName);
				}
			}
			break;
		}
		case UnmanagedType.ByValTStr:
			blobBuilder.WriteCompressedInteger(_marshalArrayElementCount);
			break;
		case UnmanagedType.IUnknown:
		case UnmanagedType.IDispatch:
		case UnmanagedType.Interface:
			if (_marshalParameterIndex >= 0)
			{
				blobBuilder.WriteCompressedInteger(_marshalParameterIndex);
			}
			break;
		}
		return blobBuilder;
	}

	internal void SetMarshalAsCustom(object typeSymbolOrName, string cookie)
	{
		_marshalType = UnmanagedType.CustomMarshaler;
		_marshalTypeNameOrSymbol = typeSymbolOrName;
		_marshalCookie = cookie;
	}

	internal void SetMarshalAsComInterface(UnmanagedType unmanagedType, int? parameterIndex)
	{
		_marshalType = unmanagedType;
		_marshalParameterIndex = parameterIndex ?? (-1);
	}

	internal void SetMarshalAsArray(UnmanagedType? elementType, int? elementCount, short? parameterIndex)
	{
		_marshalType = UnmanagedType.LPArray;
		_marshalArrayElementType = (int)(elementType ?? ((UnmanagedType)80));
		_marshalArrayElementCount = elementCount ?? (-1);
		_marshalParameterIndex = parameterIndex ?? (-1);
	}

	internal void SetMarshalAsFixedArray(UnmanagedType? elementType, int? elementCount)
	{
		_marshalType = UnmanagedType.ByValArray;
		_marshalArrayElementType = (int)(elementType ?? ((UnmanagedType)(-1)));
		_marshalArrayElementCount = elementCount ?? (-1);
	}

	internal void SetMarshalAsSafeArray(VarEnum? elementType, Type type)
	{
		_marshalType = UnmanagedType.SafeArray;
		_marshalArrayElementType = (int)(elementType ?? ((VarEnum)(-1)));
		_marshalTypeNameOrSymbol = type;
	}

	internal void SetMarshalAsFixedString(int elementCount)
	{
		_marshalType = UnmanagedType.ByValTStr;
		_marshalArrayElementCount = elementCount;
	}

	internal void SetMarshalAsSimpleType(UnmanagedType type)
	{
		_marshalType = type;
	}

	internal static MarshallingData CreateMarshallingData(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute, bool isField)
	{
		CustomAttributeInfo customAttributeInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
		MarshallingData marshallingData = new MarshallingData();
		UnmanagedType unmanagedType = ((!(customAttributeInfo._ctorArgs[0] is short num)) ? ((UnmanagedType)customAttributeInfo._ctorArgs[0]) : ((UnmanagedType)num));
		switch (unmanagedType)
		{
		case UnmanagedType.CustomMarshaler:
			DecodeMarshalAsCustom(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, marshallingData);
			break;
		case UnmanagedType.IUnknown:
		case UnmanagedType.IDispatch:
		case UnmanagedType.Interface:
			DecodeMarshalAsComInterface(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, unmanagedType, marshallingData);
			break;
		case UnmanagedType.LPArray:
			DecodeMarshalAsArray(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, isFixed: false, marshallingData);
			break;
		case UnmanagedType.ByValArray:
			if (!isField)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.NotSupported_UnmanagedTypeOnlyForFields, "ByValArray"));
			}
			DecodeMarshalAsArray(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, isFixed: true, marshallingData);
			break;
		case UnmanagedType.SafeArray:
			DecodeMarshalAsSafeArray(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, marshallingData);
			break;
		case UnmanagedType.ByValTStr:
			if (!isField)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.NotSupported_UnmanagedTypeOnlyForFields, "ByValArray"));
			}
			DecodeMarshalAsFixedString(customAttributeInfo._namedParamNames, customAttributeInfo._namedParamValues, marshallingData);
			break;
		case UnmanagedType.VBByRefStr:
			marshallingData.SetMarshalAsSimpleType(unmanagedType);
			break;
		default:
			if (unmanagedType < (UnmanagedType)0 || unmanagedType > (UnmanagedType)536870911)
			{
				throw new ArgumentException(System.SR.Argument_InvalidArgumentForAttribute, "con");
			}
			marshallingData.SetMarshalAsSimpleType(unmanagedType);
			break;
		}
		return marshallingData;
	}

	private static void DecodeMarshalAsFixedString(string[] paramNames, object[] values, MarshallingData info)
	{
		int num = -1;
		for (int i = 0; i < paramNames.Length; i++)
		{
			switch (paramNames[i])
			{
			case "SizeConst":
				num = (int)values[i];
				break;
			case "ArraySubType":
			case "SizeParamIndex":
				throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidParameterForUnmanagedType, paramNames[i], "ByValTStr"), "binaryAttribute");
			}
		}
		if (num < 0)
		{
			throw new ArgumentException(System.SR.Argument_SizeConstMustBeSpecified, "binaryAttribute");
		}
		info.SetMarshalAsFixedString(num);
	}

	private static void DecodeMarshalAsSafeArray(string[] paramNames, object[] values, MarshallingData info)
	{
		VarEnum? varEnum = null;
		Type type = null;
		int num = -1;
		for (int i = 0; i < paramNames.Length; i++)
		{
			switch (paramNames[i])
			{
			case "SafeArraySubType":
				varEnum = (VarEnum)values[i];
				break;
			case "SafeArrayUserDefinedSubType":
				type = (Type)values[i];
				num = i;
				break;
			case "ArraySubType":
			case "SizeConst":
			case "SizeParamIndex":
				throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidParameterForUnmanagedType, paramNames[i], "SafeArray"), "binaryAttribute");
			}
		}
		switch (varEnum)
		{
		default:
			if (varEnum.HasValue && num >= 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidParameterForUnmanagedType, type, "SafeArray"), "binaryAttribute");
			}
			type = null;
			break;
		case VarEnum.VT_DISPATCH:
		case VarEnum.VT_UNKNOWN:
		case VarEnum.VT_RECORD:
			break;
		}
		info.SetMarshalAsSafeArray(varEnum, type);
	}

	private static void DecodeMarshalAsArray(string[] paramNames, object[] values, bool isFixed, MarshallingData info)
	{
		UnmanagedType? elementType = null;
		int? elementCount = (isFixed ? new int?(1) : null);
		short? parameterIndex = null;
		for (int i = 0; i < paramNames.Length; i++)
		{
			switch (paramNames[i])
			{
			case "ArraySubType":
				elementType = (UnmanagedType)values[i];
				continue;
			case "SizeConst":
				elementCount = (int?)values[i];
				continue;
			case "SizeParamIndex":
				if (!isFixed)
				{
					parameterIndex = (short?)values[i];
					continue;
				}
				break;
			case "SafeArraySubType":
				break;
			default:
				continue;
			}
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidParameterForUnmanagedType, paramNames[i], isFixed ? "ByValArray" : "LPArray"), "binaryAttribute");
		}
		if (isFixed)
		{
			info.SetMarshalAsFixedArray(elementType, elementCount);
		}
		else
		{
			info.SetMarshalAsArray(elementType, elementCount, parameterIndex);
		}
	}

	private static void DecodeMarshalAsComInterface(string[] paramNames, object[] values, UnmanagedType unmanagedType, MarshallingData info)
	{
		int? parameterIndex = null;
		for (int i = 0; i < paramNames.Length; i++)
		{
			if (paramNames[i] == "IidParameterIndex")
			{
				parameterIndex = (int?)values[i];
				break;
			}
		}
		info.SetMarshalAsComInterface(unmanagedType, parameterIndex);
	}

	private static void DecodeMarshalAsCustom(string[] paramNames, object[] values, MarshallingData info)
	{
		string cookie = null;
		Type type = null;
		string text = null;
		for (int i = 0; i < paramNames.Length; i++)
		{
			switch (paramNames[i])
			{
			case "MarshalType":
				text = (string)values[i];
				break;
			case "MarshalTypeRef":
				type = (Type)values[i];
				break;
			case "MarshalCookie":
				cookie = (string)values[i];
				break;
			}
		}
		info.SetMarshalAsCustom(((object)text) ?? ((object)type), cookie);
	}
}
