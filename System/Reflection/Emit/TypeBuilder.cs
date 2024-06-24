using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

public abstract class TypeBuilder : TypeInfo
{
	public const int UnspecifiedTypeSize = 0;

	public PackingSize PackingSize => PackingSizeCore;

	protected abstract PackingSize PackingSizeCore { get; }

	public int Size => SizeCore;

	protected abstract int SizeCore { get; }

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static MethodInfo GetMethod(Type type, MethodInfo method)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder, "type");
		}
		if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
		{
			throw new ArgumentException(SR.Argument_NeedGenericMethodDefinition, "method");
		}
		if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_MethodNeedGenericDeclaringType, "method");
		}
		if (type.GetGenericTypeDefinition() != method.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidMethodDeclaringType, "type");
		}
		if (type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (!(type is TypeBuilderInstantiation type2))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		return MethodOnTypeBuilderInstantiation.GetMethod(method, type2);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder, "type");
		}
		if (!constructor.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_ConstructorNeedGenericDeclaringType, "constructor");
		}
		if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidConstructorDeclaringType, "type");
		}
		if (type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (!(type is TypeBuilderInstantiation type2))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, type2);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
	public static FieldInfo GetField(Type type, FieldInfo field)
	{
		if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
		{
			throw new ArgumentException(SR.Argument_MustBeTypeBuilder, "type");
		}
		if (!field.DeclaringType.IsGenericTypeDefinition)
		{
			throw new ArgumentException(SR.Argument_FieldNeedGenericDeclaringType, "field");
		}
		if (type.GetGenericTypeDefinition() != field.DeclaringType)
		{
			throw new ArgumentException(SR.Argument_InvalidFieldDeclaringType, "type");
		}
		if (type.IsGenericTypeDefinition)
		{
			type = type.MakeGenericType(type.GetGenericArguments());
		}
		if (!(type is TypeBuilderInstantiation type2))
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		return FieldOnTypeBuilderInstantiation.GetField(field, type2);
	}

	public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		ArgumentNullException.ThrowIfNull(interfaceType, "interfaceType");
		AddInterfaceImplementationCore(interfaceType);
	}

	protected abstract void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType);

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type CreateType()
	{
		return CreateTypeInfo();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public TypeInfo CreateTypeInfo()
	{
		return CreateTypeInfoCore();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	protected abstract TypeInfo CreateTypeInfoCore();

	public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
	{
		return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
	}

	public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
	{
		return DefineConstructorCore(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
	}

	protected abstract ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers);

	public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
	{
		return DefineDefaultConstructorCore(attributes);
	}

	protected abstract ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes);

	public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return DefineEventCore(name, attributes, eventtype);
	}

	protected abstract EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype);

	public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
	{
		return DefineField(fieldName, type, null, null, attributes);
	}

	public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
	{
		ArgumentException.ThrowIfNullOrEmpty(fieldName, "fieldName");
		ArgumentNullException.ThrowIfNull(type, "type");
		return DefineFieldCore(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
	}

	protected abstract FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes);

	public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
	{
		ArgumentNullException.ThrowIfNull(names, "names");
		if (names.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "names");
		}
		return DefineGenericParametersCore(names);
	}

	protected abstract GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);

	public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		return DefineInitializedDataCore(name, data, attributes);
	}

	protected abstract FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes);

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
	{
		return DefineMethod(name, attributes, CallingConventions.Standard, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
	{
		return DefineMethod(name, attributes, callingConvention, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
	{
		return DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
	{
		return DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
	}

	public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		if (parameterTypes != null)
		{
			if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
			{
				throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "parameterTypeOptionalCustomModifiers", "parameterTypes"));
			}
			if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
			{
				throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, "parameterTypeRequiredCustomModifiers", "parameterTypes"));
			}
		}
		return DefineMethodCore(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
	}

	protected abstract MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

	public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
	{
		ArgumentNullException.ThrowIfNull(methodInfoBody, "methodInfoBody");
		ArgumentNullException.ThrowIfNull(methodInfoDeclaration, "methodInfoDeclaration");
		DefineMethodOverrideCore(methodInfoBody, methodInfoDeclaration);
	}

	protected abstract void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration);

	public TypeBuilder DefineNestedType(string name)
	{
		return DefineNestedType(name, TypeAttributes.NestedPrivate, null, null);
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
	{
		return DefineNestedType(name, attr, null, null);
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
	{
		return DefineNestedType(name, attr, parent, null);
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return DefineNestedTypeCore(name, attr, parent, interfaces, PackingSize.Unspecified, 0);
	}

	protected abstract TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packSize, int typeSize);

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
	{
		return DefineNestedType(name, attr, parent, PackingSize.Unspecified, typeSize);
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
	{
		return DefineNestedType(name, attr, parent, packSize, 0);
	}

	public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize, int typeSize)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return DefineNestedTypeCore(name, attr, parent, null, packSize, typeSize);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		return DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		ArgumentException.ThrowIfNullOrEmpty(dllName, "dllName");
		ArgumentException.ThrowIfNullOrEmpty(entryName, "entryName");
		return DefinePInvokeMethodCore(name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	protected abstract MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet);

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? parameterTypes)
	{
		return DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? parameterTypes)
	{
		return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		return DefineProperty(name, attributes, (CallingConventions)0, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
	}

	public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return DefinePropertyCore(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
	}

	protected abstract PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

	public ConstructorBuilder DefineTypeInitializer()
	{
		return DefineTypeInitializerCore();
	}

	protected abstract ConstructorBuilder DefineTypeInitializerCore();

	public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
	{
		return DefineUninitializedDataCore(name, size, attributes);
	}

	protected abstract FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes);

	public bool IsCreated()
	{
		return IsCreatedCore();
	}

	protected abstract bool IsCreatedCore();

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		ArgumentNullException.ThrowIfNull(con, "con");
		ArgumentNullException.ThrowIfNull(binaryAttribute, "binaryAttribute");
		SetCustomAttributeCore(con, binaryAttribute);
	}

	protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		ArgumentNullException.ThrowIfNull(customBuilder, "customBuilder");
		SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
	}

	public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
	{
		SetParentCore(parent);
	}

	protected abstract void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent);

	public override Type MakePointerType()
	{
		return SymbolType.FormCompoundType("*", this, 0);
	}

	public override Type MakeByRefType()
	{
		return SymbolType.FormCompoundType("&", this, 0);
	}

	[RequiresDynamicCode("The code for an array of the specified type might not be available.")]
	public override Type MakeArrayType()
	{
		return SymbolType.FormCompoundType("[]", this, 0);
	}

	[RequiresDynamicCode("The code for an array of the specified type might not be available.")]
	public override Type MakeArrayType(int rank)
	{
		string rankString = TypeInfo.GetRankString(rank);
		return SymbolType.FormCompoundType(rankString, this, 0);
	}

	[RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override Type MakeGenericType(params Type[] typeArguments)
	{
		return TypeBuilderInstantiation.MakeGenericType(this, typeArguments);
	}
}
