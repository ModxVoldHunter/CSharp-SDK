using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;

namespace System.Reflection.Emit;

internal sealed class GenericTypeParameterBuilderImpl : GenericTypeParameterBuilder
{
	private readonly string _name;

	private readonly TypeBuilder _type;

	private readonly int _genParamPosition;

	private GenericParameterAttributes _genParamAttributes;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type _parent;

	internal List<CustomAttributeWrapper> _customAttributes;

	internal List<Type> _interfaces;

	private MethodBuilderImpl _methodBuilder;

	internal EntityHandle _parentHandle;

	public override bool IsGenericTypeParameter => (object)_methodBuilder == null;

	public override bool IsGenericMethodParameter => (object)_methodBuilder != null;

	public override int GenericParameterPosition => _genParamPosition;

	public override GenericParameterAttributes GenericParameterAttributes => _genParamAttributes;

	public override string Name => _name;

	public override Module Module => _type.Module;

	public override Assembly Assembly => _type.Assembly;

	public override string FullName => null;

	public override string Namespace => null;

	public override string AssemblyQualifiedName => null;

	public override Type UnderlyingSystemType => this;

	public override bool IsGenericTypeDefinition => false;

	public override bool IsGenericType => false;

	public override bool IsGenericParameter => true;

	public override bool IsConstructedGenericType => false;

	public override bool ContainsGenericParameters => _type.ContainsGenericParameters;

	public override MethodBase DeclaringMethod
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Type BaseType => _parent;

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override Guid GUID
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	internal GenericTypeParameterBuilderImpl(string name, int genParamPosition, TypeBuilderImpl typeBuilder, EntityHandle parentHandle)
	{
		_name = name;
		_genParamPosition = genParamPosition;
		_type = typeBuilder;
		_parentHandle = parentHandle;
	}

	public GenericTypeParameterBuilderImpl(string name, int genParamPosition, MethodBuilderImpl methodBuilder)
	{
		_name = name;
		_genParamPosition = genParamPosition;
		_methodBuilder = methodBuilder;
		_type = methodBuilder.DeclaringType;
	}

	protected override void SetBaseTypeConstraintCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type baseTypeConstraint)
	{
		_parent = baseTypeConstraint;
		if (_parent != null)
		{
			if (_interfaces == null)
			{
				_interfaces = new List<Type>();
			}
			_interfaces.Add(_parent);
		}
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}

	protected override void SetGenericParameterAttributesCore(GenericParameterAttributes genericParameterAttributes)
	{
		_genParamAttributes = genericParameterAttributes;
	}

	protected override void SetInterfaceConstraintsCore(params Type[] interfaceConstraints)
	{
		if (interfaceConstraints != null)
		{
			if (_interfaces == null)
			{
				_interfaces = new List<Type>(interfaceConstraints.Length);
			}
			_interfaces.AddRange(interfaceConstraints);
		}
	}

	public override Type[] GetGenericParameterConstraints()
	{
		if (_interfaces != null)
		{
			return _interfaces.ToArray();
		}
		return Type.EmptyTypes;
	}

	protected override bool IsArrayImpl()
	{
		return false;
	}

	protected override bool IsByRefImpl()
	{
		return false;
	}

	protected override bool IsPointerImpl()
	{
		return false;
	}

	protected override bool IsPrimitiveImpl()
	{
		return false;
	}

	protected override bool IsCOMObjectImpl()
	{
		return false;
	}

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return TypeAttributes.Public;
	}

	public override Type GetElementType()
	{
		throw new NotSupportedException();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException();
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException();
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException();
	}
}
