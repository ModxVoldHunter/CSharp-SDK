using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

internal sealed class RuntimePropertyBuilder : PropertyBuilder
{
	private readonly string m_name;

	private readonly int m_tkProperty;

	private readonly RuntimeModuleBuilder m_moduleBuilder;

	private readonly PropertyAttributes m_attributes;

	private readonly Type m_returnType;

	private MethodInfo m_getMethod;

	private MethodInfo m_setMethod;

	private readonly RuntimeTypeBuilder m_containingType;

	public override Module Module => m_containingType.Module;

	public override Type PropertyType => m_returnType;

	public override PropertyAttributes Attributes => m_attributes;

	public override bool CanRead
	{
		get
		{
			if (m_getMethod != null)
			{
				return true;
			}
			return false;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (m_setMethod != null)
			{
				return true;
			}
			return false;
		}
	}

	public override string Name => m_name;

	public override Type DeclaringType => m_containingType;

	public override Type ReflectedType => m_containingType;

	internal RuntimePropertyBuilder(RuntimeModuleBuilder mod, string name, PropertyAttributes attr, Type returnType, int prToken, RuntimeTypeBuilder containingType)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "name");
		}
		m_name = name;
		m_moduleBuilder = mod;
		m_attributes = attr;
		m_returnType = returnType;
		m_tkProperty = prToken;
		m_containingType = containingType;
	}

	protected override void SetConstantCore(object defaultValue)
	{
		m_containingType.ThrowIfCreated();
		RuntimeTypeBuilder.SetConstantValue(m_moduleBuilder, m_tkProperty, m_returnType, defaultValue);
	}

	private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
	{
		ArgumentNullException.ThrowIfNull(mdBuilder, "mdBuilder");
		m_containingType.ThrowIfCreated();
		RuntimeModuleBuilder module = m_moduleBuilder;
		RuntimeTypeBuilder.DefineMethodSemantics(new QCallModule(ref module), m_tkProperty, semantics, mdBuilder.MetadataToken);
	}

	protected override void SetGetMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Getter);
		m_getMethod = mdBuilder;
	}

	protected override void SetSetMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Setter);
		m_setMethod = mdBuilder;
	}

	protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		m_containingType.ThrowIfCreated();
		RuntimeTypeBuilder.DefineCustomAttribute(m_moduleBuilder, m_tkProperty, m_moduleBuilder.GetMethodMetadataToken(con), binaryAttribute);
	}

	public override object GetValue(object obj, object[] index)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object obj, object value, object[] index)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodInfo[] GetAccessors(bool nonPublic)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodInfo GetGetMethod(bool nonPublic)
	{
		if (nonPublic || m_getMethod == null)
		{
			return m_getMethod;
		}
		if ((m_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
		{
			return m_getMethod;
		}
		return null;
	}

	public override MethodInfo GetSetMethod(bool nonPublic)
	{
		if (nonPublic || m_setMethod == null)
		{
			return m_setMethod;
		}
		if ((m_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
		{
			return m_setMethod;
		}
		return null;
	}

	public override ParameterInfo[] GetIndexParameters()
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
}
