using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

internal sealed class ModuleBuilderImpl : ModuleBuilder
{
	private readonly Assembly _coreAssembly;

	private readonly string _name;

	private readonly MetadataBuilder _metadataBuilder;

	private readonly Dictionary<Assembly, AssemblyReferenceHandle> _assemblyReferences = new Dictionary<Assembly, AssemblyReferenceHandle>();

	private readonly Dictionary<Type, TypeReferenceHandle> _typeReferences = new Dictionary<Type, TypeReferenceHandle>();

	private readonly List<TypeBuilderImpl> _typeDefinitions = new List<TypeBuilderImpl>();

	private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorReferences = new Dictionary<ConstructorInfo, MemberReferenceHandle>();

	private Dictionary<string, ModuleReferenceHandle> _moduleReferences;

	private List<CustomAttributeWrapper> _customAttributes;

	private int _nextTypeDefRowId = 1;

	private int _nextMethodDefRowId = 1;

	private int _nextFieldDefRowId = 1;

	private int _nextParameterRowId = 1;

	private bool _coreTypesFullyPopulated;

	private Type[] _coreTypes;

	private static readonly Type[] s_coreTypes = new Type[18]
	{
		typeof(void),
		typeof(object),
		typeof(bool),
		typeof(char),
		typeof(sbyte),
		typeof(byte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(string),
		typeof(nint),
		typeof(nuint),
		typeof(TypedReference)
	};

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string Name => "<In Memory Module>";

	public override string ScopeName => _name;

	internal ModuleBuilderImpl(string name, Assembly coreAssembly, MetadataBuilder builder)
	{
		_coreAssembly = coreAssembly;
		_name = name;
		_metadataBuilder = builder;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved via s_coreTypes")]
	internal Type GetTypeFromCoreAssembly(CoreTypeId typeId)
	{
		if (_coreTypes == null)
		{
			if (_coreAssembly == typeof(object).Assembly)
			{
				_coreTypes = s_coreTypes;
				_coreTypesFullyPopulated = true;
			}
			else
			{
				_coreTypes = new Type[s_coreTypes.Length];
			}
		}
		return _coreTypes[(int)typeId] ?? (_coreTypes[(int)typeId] = _coreAssembly.GetType(s_coreTypes[(int)typeId].FullName, throwOnError: true));
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved via s_coreTypes")]
	internal CoreTypeId? GetTypeIdFromCoreTypes(Type type)
	{
		if (_coreTypes == null)
		{
			if (_coreAssembly == typeof(object).Assembly)
			{
				_coreTypes = s_coreTypes;
				_coreTypesFullyPopulated = true;
			}
			else
			{
				_coreTypes = new Type[s_coreTypes.Length];
			}
		}
		if (!_coreTypesFullyPopulated)
		{
			for (int i = 0; i < _coreTypes.Length; i++)
			{
				if (_coreTypes[i] == null)
				{
					_coreTypes[i] = _coreAssembly.GetType(s_coreTypes[i].FullName, throwOnError: false);
				}
			}
			_coreTypesFullyPopulated = true;
		}
		for (int j = 0; j < _coreTypes.Length; j++)
		{
			if (_coreTypes[j] == type)
			{
				return (CoreTypeId)j;
			}
		}
		return null;
	}

	internal void AppendMetadata()
	{
		ModuleDefinitionHandle moduleDefinitionHandle = _metadataBuilder.AddModule(0, _metadataBuilder.GetOrAddString(_name), _metadataBuilder.GetOrAddGuid(Guid.NewGuid()), default(GuidHandle), default(GuidHandle));
		_metadataBuilder.AddTypeDefinition(TypeAttributes.NotPublic, default(StringHandle), _metadataBuilder.GetOrAddString("<Module>"), default(EntityHandle), MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
		WriteCustomAttributes(_customAttributes, moduleDefinitionHandle);
		List<GenericTypeParameterBuilderImpl> list = new List<GenericTypeParameterBuilderImpl>();
		foreach (TypeBuilderImpl typeDefinition in _typeDefinitions)
		{
			EntityHandle parent = default(EntityHandle);
			if ((object)typeDefinition.BaseType != null)
			{
				parent = GetTypeHandle(typeDefinition.BaseType);
			}
			TypeDefinitionHandle typeDefinitionHandle = AddTypeDefinition(typeDefinition, parent, _nextMethodDefRowId, _nextFieldDefRowId);
			if (typeDefinition.IsGenericType)
			{
				Type[] genericTypeParameters = typeDefinition.GenericTypeParameters;
				for (int i = 0; i < genericTypeParameters.Length; i++)
				{
					GenericTypeParameterBuilderImpl item = (GenericTypeParameterBuilderImpl)genericTypeParameters[i];
					list.Add(item);
				}
			}
			if ((typeDefinition.Attributes & TypeAttributes.ExplicitLayout) != 0)
			{
				_metadataBuilder.AddTypeLayout(typeDefinitionHandle, (ushort)typeDefinition.PackingSize, (uint)typeDefinition.Size);
			}
			if (typeDefinition._interfaces != null)
			{
				foreach (Type @interface in typeDefinition._interfaces)
				{
					_metadataBuilder.AddInterfaceImplementation(typeDefinitionHandle, GetTypeHandle(@interface));
				}
			}
			if (typeDefinition.DeclaringType != null)
			{
				_metadataBuilder.AddNestedType(typeDefinitionHandle, (TypeDefinitionHandle)GetTypeHandle(typeDefinition.DeclaringType));
			}
			WriteCustomAttributes(typeDefinition._customAttributes, typeDefinitionHandle);
			WriteMethods(typeDefinition, list);
			WriteFields(typeDefinition);
		}
		list.Sort(delegate(GenericTypeParameterBuilderImpl x, GenericTypeParameterBuilderImpl y)
		{
			int num = CodedIndex.TypeOrMethodDef(x._parentHandle).CompareTo(CodedIndex.TypeOrMethodDef(y._parentHandle));
			return (num != 0) ? num : x.GenericParameterPosition.CompareTo(y.GenericParameterPosition);
		});
		foreach (GenericTypeParameterBuilderImpl item2 in list)
		{
			AddGenericTypeParametersAndConstraintsCustomAttributes(item2._parentHandle, item2);
		}
	}

	private void WriteMethods(TypeBuilderImpl typeBuilder, List<GenericTypeParameterBuilderImpl> genericParams)
	{
		foreach (MethodBuilderImpl methodDefinition in typeBuilder._methodDefinitions)
		{
			MethodDefinitionHandle methodDefinitionHandle = AddMethodDefinition(methodDefinition, methodDefinition.GetMethodSignatureBlob(), _nextParameterRowId);
			WriteCustomAttributes(methodDefinition._customAttributes, methodDefinitionHandle);
			_nextMethodDefRowId++;
			if (methodDefinition.IsGenericMethodDefinition)
			{
				Type[] genericArguments = methodDefinition.GetGenericArguments();
				for (int i = 0; i < genericArguments.Length; i++)
				{
					GenericTypeParameterBuilderImpl genericTypeParameterBuilderImpl = (GenericTypeParameterBuilderImpl)genericArguments[i];
					genericTypeParameterBuilderImpl._parentHandle = methodDefinitionHandle;
					genericParams.Add(genericTypeParameterBuilderImpl);
				}
			}
			if (methodDefinition._parameters != null)
			{
				ParameterBuilderImpl[] parameters = methodDefinition._parameters;
				foreach (ParameterBuilderImpl parameterBuilderImpl in parameters)
				{
					if (parameterBuilderImpl != null)
					{
						ParameterHandle parameterHandle = AddParameter(parameterBuilderImpl);
						WriteCustomAttributes(parameterBuilderImpl._customAttributes, parameterHandle);
						_nextParameterRowId++;
						if (parameterBuilderImpl._marshallingData != null)
						{
							AddMarshalling(parameterHandle, parameterBuilderImpl._marshallingData.SerializeMarshallingData());
						}
						if (parameterBuilderImpl._defaultValue != DBNull.Value)
						{
							AddDefaultValue(parameterHandle, parameterBuilderImpl._defaultValue);
						}
					}
				}
			}
			if (methodDefinition._dllImportData != null)
			{
				AddMethodImport(methodDefinitionHandle, methodDefinition._dllImportData.EntryPoint ?? methodDefinition.Name, methodDefinition._dllImportData.Flags, GetModuleReference(methodDefinition._dllImportData.ModuleName));
			}
		}
	}

	private void WriteFields(TypeBuilderImpl typeBuilder)
	{
		foreach (FieldBuilderImpl fieldDefinition in typeBuilder._fieldDefinitions)
		{
			FieldDefinitionHandle fieldDefinitionHandle = AddFieldDefinition(fieldDefinition, MetadataSignatureHelper.FieldSignatureEncoder(fieldDefinition.FieldType, this));
			WriteCustomAttributes(fieldDefinition._customAttributes, fieldDefinitionHandle);
			_nextFieldDefRowId++;
			if (fieldDefinition._offset > 0 && (typeBuilder.Attributes & TypeAttributes.ExplicitLayout) != 0)
			{
				AddFieldLayout(fieldDefinitionHandle, fieldDefinition._offset);
			}
			if (fieldDefinition._marshallingData != null)
			{
				AddMarshalling(fieldDefinitionHandle, fieldDefinition._marshallingData.SerializeMarshallingData());
			}
			if (fieldDefinition._defaultValue != DBNull.Value)
			{
				AddDefaultValue(fieldDefinitionHandle, fieldDefinition._defaultValue);
			}
		}
	}

	private ModuleReferenceHandle GetModuleReference(string moduleName)
	{
		if (_moduleReferences == null)
		{
			_moduleReferences = new Dictionary<string, ModuleReferenceHandle>();
		}
		if (!_moduleReferences.TryGetValue(moduleName, out var value))
		{
			value = AddModuleReference(moduleName);
			_moduleReferences.Add(moduleName, value);
		}
		return value;
	}

	internal void WriteCustomAttributes(List<CustomAttributeWrapper> customAttributes, EntityHandle parent)
	{
		if (customAttributes == null)
		{
			return;
		}
		foreach (CustomAttributeWrapper customAttribute in customAttributes)
		{
			_metadataBuilder.AddCustomAttribute(parent, GetConstructorHandle(customAttribute.Ctor), _metadataBuilder.GetOrAddBlob(customAttribute.Data));
		}
	}

	private MemberReferenceHandle GetConstructorHandle(ConstructorInfo constructorInfo)
	{
		if (!_ctorReferences.TryGetValue(constructorInfo, out var value))
		{
			TypeReferenceHandle typeReference = GetTypeReference(constructorInfo.DeclaringType);
			value = AddConstructorReference(typeReference, constructorInfo);
			_ctorReferences.Add(constructorInfo, value);
		}
		return value;
	}

	private TypeReferenceHandle GetTypeReference(Type type)
	{
		if (!_typeReferences.TryGetValue(type, out var value))
		{
			value = AddTypeReference(type, GetAssemblyReference(type.Assembly));
			_typeReferences.Add(type, value);
		}
		return value;
	}

	private AssemblyReferenceHandle GetAssemblyReference(Assembly assembly)
	{
		if (!_assemblyReferences.TryGetValue(assembly, out var value))
		{
			AssemblyName name = assembly.GetName();
			value = AddAssemblyReference(name.Name, name.Version, name.CultureName, name.GetPublicKeyToken(), name.Flags, name.ContentType);
			_assemblyReferences.Add(assembly, value);
		}
		return value;
	}

	private void AddGenericTypeParametersAndConstraintsCustomAttributes(EntityHandle parentHandle, GenericTypeParameterBuilderImpl gParam)
	{
		GenericParameterHandle genericParameterHandle = _metadataBuilder.AddGenericParameter(parentHandle, gParam.GenericParameterAttributes, _metadataBuilder.GetOrAddString(gParam.Name), gParam.GenericParameterPosition);
		WriteCustomAttributes(gParam._customAttributes, genericParameterHandle);
		Type[] genericParameterConstraints = gParam.GetGenericParameterConstraints();
		foreach (Type type in genericParameterConstraints)
		{
			_metadataBuilder.AddGenericParameterConstraint(genericParameterHandle, GetTypeHandle(type));
		}
	}

	private void AddDefaultValue(EntityHandle parentHandle, object defaultValue)
	{
		_metadataBuilder.AddConstant(parentHandle, defaultValue);
	}

	private FieldDefinitionHandle AddFieldDefinition(FieldBuilderImpl field, BlobBuilder fieldSignature)
	{
		return _metadataBuilder.AddFieldDefinition(field.Attributes, _metadataBuilder.GetOrAddString(field.Name), _metadataBuilder.GetOrAddBlob(fieldSignature));
	}

	private TypeDefinitionHandle AddTypeDefinition(TypeBuilderImpl type, EntityHandle parent, int methodToken, int fieldToken)
	{
		return _metadataBuilder.AddTypeDefinition(type.Attributes, (type.Namespace == null) ? default(StringHandle) : _metadataBuilder.GetOrAddString(type.Namespace), _metadataBuilder.GetOrAddString(type.Name), parent, MetadataTokens.FieldDefinitionHandle(fieldToken), MetadataTokens.MethodDefinitionHandle(methodToken));
	}

	private MethodDefinitionHandle AddMethodDefinition(MethodBuilderImpl method, BlobBuilder methodSignature, int parameterToken)
	{
		return _metadataBuilder.AddMethodDefinition(method.Attributes, method.GetMethodImplementationFlags(), _metadataBuilder.GetOrAddString(method.Name), _metadataBuilder.GetOrAddBlob(methodSignature), -1, MetadataTokens.ParameterHandle(parameterToken));
	}

	private TypeReferenceHandle AddTypeReference(Type type, AssemblyReferenceHandle parent)
	{
		return _metadataBuilder.AddTypeReference(parent, (type.Namespace == null) ? default(StringHandle) : _metadataBuilder.GetOrAddString(type.Namespace), _metadataBuilder.GetOrAddString(type.Name));
	}

	private MemberReferenceHandle AddConstructorReference(TypeReferenceHandle parent, ConstructorInfo method)
	{
		BlobBuilder value = MetadataSignatureHelper.ConstructorSignatureEncoder(method.GetParameters(), this);
		return _metadataBuilder.AddMemberReference(parent, _metadataBuilder.GetOrAddString(method.Name), _metadataBuilder.GetOrAddBlob(value));
	}

	private void AddMethodImport(MethodDefinitionHandle methodHandle, string name, MethodImportAttributes attributes, ModuleReferenceHandle moduleHandle)
	{
		_metadataBuilder.AddMethodImport(methodHandle, attributes, _metadataBuilder.GetOrAddString(name), moduleHandle);
	}

	private ModuleReferenceHandle AddModuleReference(string moduleName)
	{
		return _metadataBuilder.AddModuleReference(_metadataBuilder.GetOrAddString(moduleName));
	}

	private void AddFieldLayout(FieldDefinitionHandle fieldHandle, int offset)
	{
		_metadataBuilder.AddFieldLayout(fieldHandle, offset);
	}

	private void AddMarshalling(EntityHandle parent, BlobBuilder builder)
	{
		_metadataBuilder.AddMarshallingDescriptor(parent, _metadataBuilder.GetOrAddBlob(builder));
	}

	private ParameterHandle AddParameter(ParameterBuilderImpl parameter)
	{
		return _metadataBuilder.AddParameter((ParameterAttributes)parameter.Attributes, (parameter.Name != null) ? _metadataBuilder.GetOrAddString(parameter.Name) : default(StringHandle), parameter.Position);
	}

	private AssemblyReferenceHandle AddAssemblyReference(string name, Version version, string culture, byte[] publicKeyToken, AssemblyNameFlags flags, AssemblyContentType contentType)
	{
		return _metadataBuilder.AddAssemblyReference(_metadataBuilder.GetOrAddString(name), version ?? new Version(0, 0, 0, 0), (culture == null) ? default(StringHandle) : _metadataBuilder.GetOrAddString(culture), (publicKeyToken == null) ? default(BlobHandle) : _metadataBuilder.GetOrAddBlob(publicKeyToken), (AssemblyFlags)(((int)contentType << 9) | (int)(((flags & AssemblyNameFlags.Retargetable) != 0) ? AssemblyFlags.Retargetable : ((AssemblyFlags)0))), default(BlobHandle));
	}

	internal EntityHandle GetTypeHandle(Type type)
	{
		if (type is TypeBuilderImpl typeBuilderImpl && Equals(typeBuilderImpl.Module))
		{
			return typeBuilderImpl._handle;
		}
		if (type is EnumBuilderImpl enumBuilderImpl && Equals(enumBuilderImpl.Module))
		{
			return enumBuilderImpl._typeBuilder._handle;
		}
		return GetTypeReference(type);
	}

	internal TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packingSize, int typesize, TypeBuilderImpl enclosingType)
	{
		TypeDefinitionHandle handle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
		TypeBuilderImpl typeBuilderImpl = new TypeBuilderImpl(name, attr, parent, this, handle, interfaces, packingSize, typesize, enclosingType);
		_typeDefinitions.Add(typeBuilderImpl);
		return typeBuilderImpl;
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotImplementedException();
	}

	public override int GetFieldMetadataToken(FieldInfo field)
	{
		throw new NotImplementedException();
	}

	public override int GetMethodMetadataToken(ConstructorInfo constructor)
	{
		throw new NotImplementedException();
	}

	public override int GetMethodMetadataToken(MethodInfo method)
	{
		throw new NotImplementedException();
	}

	public override int GetStringMetadataToken(string stringConstant)
	{
		throw new NotImplementedException();
	}

	public override int GetTypeMetadataToken(Type type)
	{
		throw new NotImplementedException();
	}

	protected override void CreateGlobalFunctionsCore()
	{
		throw new NotImplementedException();
	}

	protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType)
	{
		TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
		EnumBuilderImpl enumBuilderImpl = new EnumBuilderImpl(name, underlyingType, visibility, this, typeHandle);
		_typeDefinitions.Add(enumBuilderImpl._typeBuilder);
		return enumBuilderImpl;
	}

	protected override MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		throw new NotImplementedException();
	}

	protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
	{
		throw new NotImplementedException();
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		throw new NotImplementedException();
	}

	protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
	{
		TypeDefinitionHandle handle = MetadataTokens.TypeDefinitionHandle(++_nextTypeDefRowId);
		TypeBuilderImpl typeBuilderImpl = new TypeBuilderImpl(name, attr, parent, this, handle, interfaces, packingSize, typesize, null);
		_typeDefinitions.Add(typeBuilderImpl);
		return typeBuilderImpl;
	}

	protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
	{
		throw new NotImplementedException();
	}

	protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		throw new NotImplementedException();
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		if (_customAttributes == null)
		{
			_customAttributes = new List<CustomAttributeWrapper>();
		}
		_customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
	}

	public override int GetSignatureMetadataToken(SignatureHelper signature)
	{
		throw new NotImplementedException();
	}
}
