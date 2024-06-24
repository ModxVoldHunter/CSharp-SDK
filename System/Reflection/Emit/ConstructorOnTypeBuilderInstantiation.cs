using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
{
	internal ConstructorInfo _ctor;

	private TypeBuilderInstantiation _type;

	public override MemberTypes MemberType => _ctor.MemberType;

	public override string Name => _ctor.Name;

	public override Type DeclaringType => _type;

	public override Type ReflectedType => _type;

	public override int MetadataToken
	{
		get
		{
			ConstructorBuilder constructorBuilder = _ctor as ConstructorBuilder;
			if (constructorBuilder != null)
			{
				return constructorBuilder.MetadataToken;
			}
			return _ctor.MetadataToken;
		}
	}

	public override Module Module => _ctor.Module;

	public override RuntimeMethodHandle MethodHandle => _ctor.MethodHandle;

	public override MethodAttributes Attributes => _ctor.Attributes;

	public override CallingConventions CallingConvention => _ctor.CallingConvention;

	public override bool IsGenericMethodDefinition => false;

	public override bool ContainsGenericParameters => false;

	public override bool IsGenericMethod => false;

	internal static ConstructorInfo GetConstructor(ConstructorInfo Constructor, TypeBuilderInstantiation type)
	{
		return new ConstructorOnTypeBuilderInstantiation(Constructor, type);
	}

	internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
	{
		_ctor = constructor;
		_type = type;
	}

	internal override Type[] GetParameterTypes()
	{
		return _ctor.GetParameterTypes();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _ctor.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _ctor.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _ctor.IsDefined(attributeType, inherit);
	}

	public override ParameterInfo[] GetParameters()
	{
		return _ctor.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return _ctor.GetMethodImplementationFlags();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

	public override Type[] GetGenericArguments()
	{
		return _ctor.GetGenericArguments();
	}

	public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new InvalidOperationException();
	}
}
