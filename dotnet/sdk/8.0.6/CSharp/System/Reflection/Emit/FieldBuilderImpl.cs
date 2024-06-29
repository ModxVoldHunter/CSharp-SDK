using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class FieldBuilderImpl : FieldBuilder
{
	private readonly TypeBuilderImpl _typeBuilder;

	private readonly string _fieldName;

	private readonly Type _fieldType;

	private FieldAttributes _attributes;

	internal MarshallingData _marshallingData;

	internal int _offset;

	internal List<CustomAttributeWrapper> _customAttributes;

	internal object _defaultValue = DBNull.Value;

	public override int MetadataToken
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Module Module => _typeBuilder.Module;

	public override string Name => _fieldName;

	public override Type DeclaringType => _typeBuilder;

	public override Type ReflectedType => _typeBuilder;

	public override Type FieldType => _fieldType;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
		}
	}

	public override FieldAttributes Attributes => _attributes;

	internal FieldBuilderImpl(TypeBuilderImpl typeBuilder, string fieldName, Type type, FieldAttributes attributes)
	{
		_fieldName = fieldName;
		_typeBuilder = typeBuilder;
		_fieldType = type;
		_attributes = attributes & ~FieldAttributes.ReservedMask;
		_offset = -1;
	}

	protected override void SetConstantCore(object defaultValue)
	{
		if (defaultValue == null)
		{
			if (_fieldType.IsValueType && (!_fieldType.IsGenericType || !(_fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))))
			{
				throw new ArgumentException(System.SR.Argument_ConstantNull);
			}
			return;
		}
		Type type = defaultValue.GetType();
		Type type2 = _fieldType;
		if (type2.IsByRef)
		{
			type2 = type2.GetElementType();
		}
		type2 = Nullable.GetUnderlyingType(type2) ?? type2;
		if (type2.IsEnum)
		{
			if (type2 is EnumBuilderImpl enumBuilderImpl)
			{
				Type enumUnderlyingType = enumBuilderImpl.GetEnumUnderlyingType();
				if (type != enumBuilderImpl._typeBuilder.UnderlyingSystemType && type != enumUnderlyingType)
				{
					throw new ArgumentException(System.SR.Argument_ConstantDoesntMatch);
				}
			}
			else if (type2 is TypeBuilderImpl typeBuilderImpl)
			{
				Type enumUnderlyingType = typeBuilderImpl.UnderlyingSystemType;
				if (enumUnderlyingType == null || (type != typeBuilderImpl.UnderlyingSystemType && type != enumUnderlyingType))
				{
					throw new ArgumentException(System.SR.Argument_ConstantDoesntMatch);
				}
			}
			else
			{
				Type enumUnderlyingType = Enum.GetUnderlyingType(type2);
				if (type != type2 && type != enumUnderlyingType)
				{
					throw new ArgumentException(System.SR.Argument_ConstantDoesntMatch);
				}
			}
		}
		else if (!type2.IsAssignableFrom(type))
		{
			throw new ArgumentException(System.SR.Argument_ConstantDoesntMatch);
		}
		_defaultValue = defaultValue;
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		switch (con.ReflectedType.FullName)
		{
		case "System.Runtime.InteropServices.FieldOffsetAttribute":
			_offset = BinaryPrimitives.ReadInt32LittleEndian(binaryAttribute.Slice(2));
			return;
		case "System.NonSerializedAttribute":
			_attributes |= FieldAttributes.NotSerialized;
			return;
		case "System.Runtime.CompilerServices.SpecialNameAttribute":
			_attributes |= FieldAttributes.SpecialName;
			return;
		case "System.Runtime.InteropServices.MarshalAsAttribute":
			_attributes |= FieldAttributes.HasFieldMarshal;
			_marshallingData = MarshallingData.CreateMarshallingData(con, binaryAttribute, isField: true);
			return;
		}
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}

	protected override void SetOffsetCore(int iOffset)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(iOffset, "iOffset");
		_offset = iOffset;
	}

	public override object GetValue(object obj)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object obj, object val, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(System.SR.NotSupported_DynamicModule);
	}
}
