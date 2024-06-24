using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class FieldOnTypeBuilderInstantiation : FieldInfo
{
	private FieldInfo _field;

	private TypeBuilderInstantiation _type;

	internal FieldInfo FieldInfo => _field;

	public override MemberTypes MemberType => MemberTypes.Field;

	public override string Name => _field.Name;

	public override Type DeclaringType => _type;

	public override Type ReflectedType => _type;

	public override int MetadataToken
	{
		get
		{
			FieldBuilder fieldBuilder = _field as FieldBuilder;
			if (fieldBuilder != null)
			{
				return fieldBuilder.MetadataToken;
			}
			return _field.MetadataToken;
		}
	}

	public override Module Module => _field.Module;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Type FieldType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override FieldAttributes Attributes => _field.Attributes;

	internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
	{
		FieldInfo fieldInfo;
		if (type._hashtable.Contains(Field))
		{
			fieldInfo = type._hashtable[Field] as FieldInfo;
		}
		else
		{
			fieldInfo = new FieldOnTypeBuilderInstantiation(Field, type);
			type._hashtable[Field] = fieldInfo;
		}
		return fieldInfo;
	}

	internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
	{
		_field = field;
		_type = type;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _field.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _field.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _field.IsDefined(attributeType, inherit);
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return _field.GetRequiredCustomModifiers();
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return _field.GetOptionalCustomModifiers();
	}

	public override void SetValueDirect(TypedReference obj, object value)
	{
		throw new NotImplementedException();
	}

	public override object GetValueDirect(TypedReference obj)
	{
		throw new NotImplementedException();
	}

	public override object GetValue(object obj)
	{
		throw new InvalidOperationException();
	}

	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		throw new InvalidOperationException();
	}
}
