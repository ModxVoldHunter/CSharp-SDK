using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

internal sealed class RuntimeFieldBuilder : FieldBuilder
{
	private readonly int m_fieldTok;

	private readonly RuntimeTypeBuilder m_typeBuilder;

	private readonly string m_fieldName;

	private readonly FieldAttributes m_Attributes;

	private readonly Type m_fieldType;

	public override int MetadataToken => m_fieldTok;

	public override Module Module => m_typeBuilder.Module;

	public override string Name => m_fieldName;

	public override Type DeclaringType
	{
		get
		{
			if (m_typeBuilder.m_isHiddenGlobalType)
			{
				return null;
			}
			return m_typeBuilder;
		}
	}

	public override Type ReflectedType
	{
		get
		{
			if (m_typeBuilder.m_isHiddenGlobalType)
			{
				return null;
			}
			return m_typeBuilder;
		}
	}

	public override Type FieldType => m_fieldType;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicModule);
		}
	}

	public override FieldAttributes Attributes => m_Attributes;

	internal RuntimeFieldBuilder(RuntimeTypeBuilder typeBuilder, string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
	{
		if (fieldName[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "fieldName");
		}
		if (type == typeof(void))
		{
			throw new ArgumentException(SR.Argument_BadFieldType);
		}
		m_fieldName = fieldName;
		m_typeBuilder = typeBuilder;
		m_fieldType = type;
		m_Attributes = attributes & ~FieldAttributes.ReservedMask;
		SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(m_typeBuilder.Module);
		fieldSigHelper.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);
		int length;
		byte[] signature = fieldSigHelper.InternalGetSignature(out length);
		RuntimeModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		m_fieldTok = RuntimeTypeBuilder.DefineField(new QCallModule(ref module), typeBuilder.TypeToken, fieldName, signature, length, m_Attributes);
	}

	internal void SetData(byte[] data, int size)
	{
		RuntimeModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		RuntimeModuleBuilder.SetFieldRVAContent(new QCallModule(ref module), m_fieldTok, data, size);
	}

	public override object GetValue(object obj)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object obj, object val, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	protected override void SetOffsetCore(int iOffset)
	{
		m_typeBuilder.ThrowIfCreated();
		RuntimeModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		RuntimeTypeBuilder.SetFieldLayoutOffset(new QCallModule(ref module), m_fieldTok, iOffset);
	}

	protected override void SetConstantCore(object defaultValue)
	{
		m_typeBuilder.ThrowIfCreated();
		if (defaultValue == null && m_fieldType.IsValueType && (!m_fieldType.IsGenericType || !(m_fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))))
		{
			throw new ArgumentException(SR.Argument_ConstantNull);
		}
		RuntimeTypeBuilder.SetConstantValue(m_typeBuilder.GetModuleBuilder(), m_fieldTok, m_fieldType, defaultValue);
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		RuntimeModuleBuilder runtimeModuleBuilder = (RuntimeModuleBuilder)m_typeBuilder.Module;
		m_typeBuilder.ThrowIfCreated();
		RuntimeTypeBuilder.DefineCustomAttribute(runtimeModuleBuilder, m_fieldTok, runtimeModuleBuilder.GetMethodMetadataToken(con), binaryAttribute);
	}
}
