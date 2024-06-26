using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection;

internal class ModifiedType : Type
{
	internal struct TypeSignature
	{
		internal Signature _signature;

		internal int _offset;
	}

	private readonly TypeSignature _typeSignature;

	private readonly Type _unmodifiedType;

	protected Type UnmodifiedType => _unmodifiedType;

	public override Type UnderlyingSystemType => _unmodifiedType;

	public override GenericParameterAttributes GenericParameterAttributes => _unmodifiedType.GenericParameterAttributes;

	public override bool ContainsGenericParameters => _unmodifiedType.ContainsGenericParameters;

	public override bool IsGenericType => _unmodifiedType.IsGenericType;

	public override Guid GUID => _unmodifiedType.GUID;

	public override int MetadataToken
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_ModifiedType);
		}
	}

	public override Module Module => _unmodifiedType.Module;

	public override Assembly Assembly => _unmodifiedType.Assembly;

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_ModifiedType);
		}
	}

	public override string Name => _unmodifiedType.Name;

	public override string FullName => _unmodifiedType.FullName;

	public override string Namespace => _unmodifiedType.Namespace;

	public override string AssemblyQualifiedName => _unmodifiedType.AssemblyQualifiedName;

	public override Type BaseType
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_ModifiedType);
		}
	}

	public override Type DeclaringType
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_ModifiedType);
		}
	}

	public override Type ReflectedType
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_ModifiedType);
		}
	}

	public override bool IsTypeDefinition => _unmodifiedType.IsTypeDefinition;

	public override bool IsSZArray => _unmodifiedType.IsSZArray;

	public override bool IsVariableBoundArray => _unmodifiedType.IsVariableBoundArray;

	public override bool IsEnum => _unmodifiedType.IsEnum;

	public override bool IsGenericTypeParameter => _unmodifiedType.IsGenericTypeParameter;

	public override bool IsGenericMethodParameter => _unmodifiedType.IsGenericMethodParameter;

	public override bool IsByRefLike => _unmodifiedType.IsByRefLike;

	public override bool IsConstructedGenericType => _unmodifiedType.IsConstructedGenericType;

	public override bool IsCollectible => _unmodifiedType.IsCollectible;

	public override bool IsFunctionPointer => _unmodifiedType.IsFunctionPointer;

	public override bool IsUnmanagedFunctionPointer => _unmodifiedType.IsUnmanagedFunctionPointer;

	public override bool IsSecurityCritical => _unmodifiedType.IsSecurityCritical;

	public override bool IsSecuritySafeCritical => _unmodifiedType.IsSecuritySafeCritical;

	public override bool IsSecurityTransparent => _unmodifiedType.IsSecurityTransparent;

	internal Type GetTypeParameter(Type unmodifiedType, int index)
	{
		return Create(unmodifiedType, new TypeSignature
		{
			_signature = _typeSignature._signature,
			_offset = (_typeSignature._signature?.GetTypeParameterOffset(_typeSignature._offset, index) ?? 0)
		});
	}

	internal SignatureCallingConvention GetCallingConventionFromFunctionPointer()
	{
		return _typeSignature._signature?.GetCallingConventionFromFunctionPointerAtOffset(_typeSignature._offset) ?? SignatureCallingConvention.Default;
	}

	internal static Type Create(Type unmodifiedType, Signature signature, int parameterIndex = 0)
	{
		return Create(unmodifiedType, new TypeSignature
		{
			_signature = signature,
			_offset = (signature?.GetParameterOffset(parameterIndex) ?? 0)
		});
	}

	private Type[] GetCustomModifiers(bool required)
	{
		if (_typeSignature._signature == null)
		{
			return Type.EmptyTypes;
		}
		return _typeSignature._signature.GetCustomModifiersAtOffset(_typeSignature._offset, required);
	}

	internal ModifiedType(Type unmodifiedType, TypeSignature typeSignature)
	{
		_unmodifiedType = unmodifiedType;
		_typeSignature = typeSignature;
	}

	protected static Type Create(Type unmodifiedType, TypeSignature typeSignature)
	{
		if (unmodifiedType.IsFunctionPointer)
		{
			return new ModifiedFunctionPointerType(unmodifiedType, typeSignature);
		}
		if (unmodifiedType.HasElementType)
		{
			return new ModifiedHasElementType(unmodifiedType, typeSignature);
		}
		if (unmodifiedType.IsGenericType)
		{
			return new ModifiedGenericType(unmodifiedType, typeSignature);
		}
		return new ModifiedType(unmodifiedType, typeSignature);
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return GetCustomModifiers(required: true);
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return GetCustomModifiers(required: false);
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override bool Equals(Type other)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override int GetHashCode()
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override string ToString()
	{
		return _unmodifiedType.ToString();
	}

	public override Type GetGenericTypeDefinition()
	{
		return _unmodifiedType.GetGenericTypeDefinition();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override Type[] GetFunctionPointerCallingConventions()
	{
		return _unmodifiedType.GetFunctionPointerCallingConventions();
	}

	public override Type[] GetFunctionPointerParameterTypes()
	{
		return _unmodifiedType.GetFunctionPointerParameterTypes();
	}

	public override Type GetFunctionPointerReturnType()
	{
		return _unmodifiedType.GetFunctionPointerReturnType();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		return _unmodifiedType.GetInterfaceMap(interfaceType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
	{
		throw new NotSupportedException(SR.NotSupported_ModifiedType);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return _unmodifiedType.Attributes;
	}

	public override int GetArrayRank()
	{
		return _unmodifiedType.GetArrayRank();
	}

	protected override bool IsArrayImpl()
	{
		return _unmodifiedType.IsArray;
	}

	protected override bool IsPrimitiveImpl()
	{
		return _unmodifiedType.IsPrimitive;
	}

	protected override bool IsByRefImpl()
	{
		return _unmodifiedType.IsByRef;
	}

	protected override bool IsPointerImpl()
	{
		return _unmodifiedType.IsPointer;
	}

	protected override bool IsValueTypeImpl()
	{
		return _unmodifiedType.IsValueType;
	}

	protected override bool IsCOMObjectImpl()
	{
		return _unmodifiedType.IsCOMObject;
	}

	public override Type GetElementType()
	{
		return _unmodifiedType.GetElementType();
	}

	protected override bool HasElementTypeImpl()
	{
		return _unmodifiedType.HasElementType;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _unmodifiedType.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _unmodifiedType.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _unmodifiedType.IsDefined(attributeType, inherit);
	}
}
