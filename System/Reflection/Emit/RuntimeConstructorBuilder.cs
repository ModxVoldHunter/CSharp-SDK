using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class RuntimeConstructorBuilder : ConstructorBuilder
{
	private readonly RuntimeMethodBuilder m_methodBuilder;

	internal bool m_isDefaultConstructor;

	public override int MetadataToken => m_methodBuilder.MetadataToken;

	public override Module Module => m_methodBuilder.Module;

	public override Type ReflectedType => m_methodBuilder.ReflectedType;

	public override Type DeclaringType => m_methodBuilder.DeclaringType;

	public override string Name => m_methodBuilder.Name;

	public override MethodAttributes Attributes => m_methodBuilder.Attributes;

	public override RuntimeMethodHandle MethodHandle => m_methodBuilder.MethodHandle;

	public override CallingConventions CallingConvention
	{
		get
		{
			if (DeclaringType.IsGenericType)
			{
				return CallingConventions.HasThis;
			}
			return CallingConventions.Standard;
		}
	}

	protected override bool InitLocalsCore
	{
		get
		{
			return m_methodBuilder.InitLocals;
		}
		set
		{
			m_methodBuilder.InitLocals = value;
		}
	}

	internal RuntimeConstructorBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, RuntimeModuleBuilder mod, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] RuntimeTypeBuilder type)
	{
		m_methodBuilder = new RuntimeMethodBuilder(name, attributes, callingConvention, null, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, mod, type);
		type.m_listMethods.Add(m_methodBuilder);
		m_methodBuilder.GetMethodSignature().InternalGetSignature(out var _);
		_ = m_methodBuilder.MetadataToken;
	}

	internal RuntimeConstructorBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, RuntimeModuleBuilder mod, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] RuntimeTypeBuilder type)
		: this(name, attributes, callingConvention, parameterTypes, null, null, mod, type)
	{
	}

	internal override Type[] GetParameterTypes()
	{
		return m_methodBuilder.GetParameterTypes();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private RuntimeTypeBuilder GetTypeBuilder()
	{
		return m_methodBuilder.GetTypeBuilder();
	}

	internal SignatureHelper GetMethodSignature()
	{
		return m_methodBuilder.GetMethodSignature();
	}

	public override string ToString()
	{
		return m_methodBuilder.ToString();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override ParameterInfo[] GetParameters()
	{
		ConstructorInfo constructor = GetTypeBuilder().GetConstructor(m_methodBuilder.m_parameterTypes);
		return constructor.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return m_methodBuilder.GetMethodImplementationFlags();
	}

	public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return m_methodBuilder.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return m_methodBuilder.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return m_methodBuilder.IsDefined(attributeType, inherit);
	}

	protected override ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string strParamName)
	{
		attributes &= ~ParameterAttributes.ReservedMask;
		return m_methodBuilder.DefineParameter(iSequence, attributes, strParamName);
	}

	protected override ILGenerator GetILGeneratorCore(int streamSize)
	{
		if (m_isDefaultConstructor)
		{
			throw new InvalidOperationException(SR.InvalidOperation_DefaultConstructorILGen);
		}
		return m_methodBuilder.GetILGenerator(streamSize);
	}

	internal override Type GetReturnType()
	{
		return m_methodBuilder.ReturnType;
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		m_methodBuilder.SetCustomAttribute(con, binaryAttribute);
	}

	protected override void SetImplementationFlagsCore(MethodImplAttributes attributes)
	{
		m_methodBuilder.SetImplementationFlags(attributes);
	}
}
