using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
{
	internal MethodInfo _method;

	private Type _type;

	public override MemberTypes MemberType => _method.MemberType;

	public override string Name => _method.Name;

	public override Type DeclaringType => _type;

	public override Type ReflectedType => _type;

	public override Module Module => _method.Module;

	public override RuntimeMethodHandle MethodHandle => _method.MethodHandle;

	public override MethodAttributes Attributes => _method.Attributes;

	public override CallingConventions CallingConvention => _method.CallingConvention;

	public override bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition;

	public override bool ContainsGenericParameters
	{
		get
		{
			if (_method.ContainsGenericParameters)
			{
				return true;
			}
			if (!_method.IsGenericMethodDefinition)
			{
				throw new NotSupportedException();
			}
			return _method.ContainsGenericParameters;
		}
	}

	public override bool IsGenericMethod => _method.IsGenericMethod;

	public override Type ReturnType => _method.ReturnType;

	public override ParameterInfo ReturnParameter
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override ICustomAttributeProvider ReturnTypeCustomAttributes
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
	{
		return new MethodOnTypeBuilderInstantiation(method, type);
	}

	internal MethodOnTypeBuilderInstantiation(MethodInfo method, Type type)
	{
		_method = method;
		_type = type;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _method.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _method.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _method.IsDefined(attributeType, inherit);
	}

	public override ParameterInfo[] GetParameters()
	{
		return _method.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return _method.GetMethodImplementationFlags();
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		return _method;
	}

	public override Type[] GetGenericArguments()
	{
		return _method.GetGenericArguments();
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
	{
		if (!IsGenericMethodDefinition)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
		}
		return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
	}

	public override MethodInfo GetBaseDefinition()
	{
		throw new NotSupportedException();
	}

	internal override Type[] GetParameterTypes()
	{
		return _method.GetParameterTypes();
	}
}
