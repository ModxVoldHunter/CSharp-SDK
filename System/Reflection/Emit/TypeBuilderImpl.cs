using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

internal sealed class TypeBuilderImpl : TypeBuilder
{
	private readonly ModuleBuilderImpl _module;

	private readonly string _name;

	private readonly string _namespace;

	private string _strFullName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type _typeParent;

	private readonly TypeBuilderImpl _declaringType;

	private GenericTypeParameterBuilderImpl[] _typeParameters;

	private TypeAttributes _attributes;

	private PackingSize _packingSize;

	private int _typeSize;

	private Type _enumUnderlyingType;

	internal readonly TypeDefinitionHandle _handle;

	internal readonly List<MethodBuilderImpl> _methodDefinitions = new List<MethodBuilderImpl>();

	internal readonly List<FieldBuilderImpl> _fieldDefinitions = new List<FieldBuilderImpl>();

	internal List<Type> _interfaces;

	internal List<CustomAttributeWrapper> _customAttributes;

	protected override PackingSize PackingSizeCore => _packingSize;

	protected override int SizeCore => _typeSize;

	public override string Name => _name;

	public override Type DeclaringType => _declaringType;

	public override Type ReflectedType => _declaringType;

	public override bool IsGenericTypeDefinition => IsGenericType;

	public override bool IsGenericType => _typeParameters != null;

	public override Type[] GenericTypeParameters
	{
		get
		{
			Type[] typeParameters = _typeParameters;
			return typeParameters ?? Type.EmptyTypes;
		}
	}

	public override string AssemblyQualifiedName
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override string FullName => _strFullName ?? (_strFullName = System.Reflection.Emit.TypeNameBuilder.ToString(this, System.Reflection.Emit.TypeNameBuilder.Format.FullName));

	public override string Namespace => _namespace;

	public override Assembly Assembly => _module.Assembly;

	public override Module Module => _module;

	public override Type UnderlyingSystemType
	{
		get
		{
			if (IsEnum)
			{
				if (_enumUnderlyingType == null)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_NoUnderlyingTypeOnEnum);
				}
				return _enumUnderlyingType;
			}
			return this;
		}
	}

	public override Guid GUID
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override Type BaseType => _typeParent;

	public override int MetadataToken => MetadataTokens.GetToken(_handle);

	internal TypeBuilderImpl(string fullName, TypeAttributes typeAttributes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, ModuleBuilderImpl module, TypeDefinitionHandle handle, Type[] interfaces, PackingSize packingSize, int typeSize, TypeBuilderImpl enclosingType)
	{
		_name = fullName;
		_module = module;
		_attributes = typeAttributes;
		_packingSize = packingSize;
		_typeSize = typeSize;
		SetParent(parent);
		_handle = handle;
		_declaringType = enclosingType;
		int num = _name.LastIndexOf('.');
		if (num != -1)
		{
			_namespace = _name.Substring(0, num);
			string name = _name;
			int num2 = num + 1;
			_name = name.Substring(num2, name.Length - num2);
		}
		if (interfaces != null)
		{
			_interfaces = new List<Type>();
			foreach (Type type in interfaces)
			{
				ArgumentNullException.ThrowIfNull(type, "interfaces");
				_interfaces.Add(type);
			}
		}
	}

	protected override void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		if (_interfaces == null)
		{
			_interfaces = new List<Type>();
		}
		_interfaces.Add(interfaceType);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:DynamicallyAccessedMembers", Justification = "Not sure how to handle")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	protected override TypeInfo CreateTypeInfoCore()
	{
		return this;
	}

	protected override ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
	{
		throw new NotImplementedException();
	}

	protected override ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes)
	{
		throw new NotImplementedException();
	}

	protected override EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype)
	{
		throw new NotImplementedException();
	}

	protected override FieldBuilder DefineFieldCore(string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
	{
		if (_enumUnderlyingType == null && IsEnum && (attributes & FieldAttributes.Static) == 0)
		{
			_enumUnderlyingType = type;
		}
		FieldBuilderImpl fieldBuilderImpl = new FieldBuilderImpl(this, fieldName, type, attributes);
		_fieldDefinitions.Add(fieldBuilderImpl);
		return fieldBuilderImpl;
	}

	protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
	{
		if (_typeParameters != null)
		{
			throw new InvalidOperationException();
		}
		GenericTypeParameterBuilderImpl[] array = new GenericTypeParameterBuilderImpl[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			string text = names[i];
			ArgumentNullException.ThrowIfNull(text, "names");
			array[i] = new GenericTypeParameterBuilderImpl(text, i, this, _handle);
		}
		return _typeParameters = array;
	}

	protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
	{
		throw new NotImplementedException();
	}

	protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		MethodBuilderImpl methodBuilderImpl = new MethodBuilderImpl(name, attributes, callingConvention, returnType, parameterTypes, _module, this);
		_methodDefinitions.Add(methodBuilderImpl);
		return methodBuilderImpl;
	}

	protected override void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
	{
		throw new NotImplementedException();
	}

	protected override TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packSize, int typeSize)
	{
		return _module.DefineNestedType(name, attr, parent, interfaces, packSize, typeSize, this);
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		throw new NotImplementedException();
	}

	protected override PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		throw new NotImplementedException();
	}

	protected override ConstructorBuilder DefineTypeInitializerCore()
	{
		throw new NotImplementedException();
	}

	protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
	{
		throw new NotImplementedException();
	}

	protected override bool IsCreatedCore()
	{
		return false;
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		switch (con.ReflectedType.FullName)
		{
		case "System.Runtime.InteropServices.StructLayoutAttribute":
			ParseStructLayoutAttribute(con, binaryAttribute);
			return;
		case "System.Runtime.CompilerServices.SpecialNameAttribute":
			_attributes |= TypeAttributes.SpecialName;
			return;
		case "System.SerializableAttribute":
			_attributes |= TypeAttributes.Serializable;
			return;
		case "System.Runtime.InteropServices.ComImportAttribute":
			_attributes |= TypeAttributes.Import;
			return;
		case "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute":
			_attributes |= TypeAttributes.WindowsRuntime;
			return;
		case "System.Security.SuppressUnmanagedCodeSecurityAttribute":
			_attributes |= TypeAttributes.HasSecurity;
			break;
		}
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}

	internal void SetCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		SetCustomAttributeCore(con, binaryAttribute);
	}

	private void ParseStructLayoutAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		CustomAttributeInfo customAttributeInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
		LayoutKind layoutKind = (LayoutKind)customAttributeInfo._ctorArgs[0];
		_attributes &= ~TypeAttributes.LayoutMask;
		TypeAttributes attributes = _attributes;
		_attributes = attributes | (layoutKind switch
		{
			LayoutKind.Auto => TypeAttributes.NotPublic, 
			LayoutKind.Explicit => TypeAttributes.ExplicitLayout, 
			LayoutKind.Sequential => TypeAttributes.SequentialLayout, 
			_ => TypeAttributes.NotPublic, 
		});
		for (int i = 0; i < customAttributeInfo._namedParamNames.Length; i++)
		{
			string text = customAttributeInfo._namedParamNames[i];
			int num = (int)customAttributeInfo._namedParamValues[i];
			switch (text)
			{
			case "CharSet":
				switch ((CharSet)num)
				{
				case CharSet.None:
				case CharSet.Ansi:
					_attributes &= ~TypeAttributes.StringFormatMask;
					break;
				case CharSet.Unicode:
					_attributes &= ~TypeAttributes.AutoClass;
					_attributes |= TypeAttributes.UnicodeClass;
					break;
				case CharSet.Auto:
					_attributes &= ~TypeAttributes.UnicodeClass;
					_attributes |= TypeAttributes.AutoClass;
					break;
				}
				break;
			case "Pack":
				_packingSize = (PackingSize)num;
				break;
			case "Size":
				_typeSize = num;
				break;
			default:
				throw new ArgumentException(System.SR.Format(System.SR.Argument_UnknownNamedType, con.DeclaringType, text), "binaryAttribute");
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:DynamicallyAccessedMembers", Justification = "TODO: Need to figure out how to preserve System.Object public constructor")]
	protected override void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent)
	{
		if (parent != null)
		{
			if (parent.IsInterface)
			{
				throw new ArgumentException(System.SR.Argument_CannotSetParentToInterface);
			}
			_typeParent = parent;
		}
		else if ((_attributes & TypeAttributes.ClassSemanticsMask) != TypeAttributes.ClassSemanticsMask)
		{
			_typeParent = _module.GetTypeFromCoreAssembly(CoreTypeId.Object);
		}
		else
		{
			if ((_attributes & TypeAttributes.Abstract) == 0)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_BadInterfaceNotAbstract);
			}
			_typeParent = null;
		}
	}

	public override Type[] GetGenericArguments()
	{
		Type[] typeParameters = _typeParameters;
		return typeParameters ?? Type.EmptyTypes;
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotImplementedException();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotImplementedException();
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotImplementedException();
	}

	public override Type GetElementType()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		throw new NotSupportedException();
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

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return _attributes;
	}

	protected override bool IsCOMObjectImpl()
	{
		return (GetAttributeFlagsImpl() & TypeAttributes.Import) != 0;
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

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
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
		if (_interfaces != null)
		{
			return _interfaces.ToArray();
		}
		return Type.EmptyTypes;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
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

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		throw new NotSupportedException();
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] Type c)
	{
		throw new NotSupportedException();
	}
}
