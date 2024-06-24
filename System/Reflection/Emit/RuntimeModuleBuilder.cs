using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Reflection.Emit;

internal sealed class RuntimeModuleBuilder : ModuleBuilder
{
	private readonly Dictionary<string, Type> _typeBuilderDict;

	private readonly TypeBuilder _globalTypeBuilder;

	private bool _hasGlobalBeenCreated;

	internal readonly RuntimeModule _internalModule;

	private readonly RuntimeAssemblyBuilder _assemblyBuilder;

	internal RuntimeAssemblyBuilder ContainingAssemblyBuilder => _assemblyBuilder;

	internal object SyncRoot => ContainingAssemblyBuilder.SyncRoot;

	internal RuntimeModule InternalModule => _internalModule;

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string FullyQualifiedName => "RefEmit_InMemoryManifestModule";

	public override int MDStreamVersion => InternalModule.MDStreamVersion;

	public override Guid ModuleVersionId => InternalModule.ModuleVersionId;

	public override int MetadataToken => InternalModule.MetadataToken;

	public override string ScopeName => InternalModule.ScopeName;

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string Name => InternalModule.Name;

	public override Assembly Assembly => _assemblyBuilder;

	internal static string UnmangleTypeName(string typeName)
	{
		int startIndex = typeName.Length - 1;
		while (true)
		{
			startIndex = typeName.LastIndexOf('+', startIndex);
			if (startIndex < 0)
			{
				break;
			}
			bool flag = true;
			int num = startIndex;
			while (typeName[--num] == '\\')
			{
				flag = !flag;
			}
			if (flag)
			{
				break;
			}
			startIndex = num;
		}
		return typeName.Substring(startIndex + 1);
	}

	internal RuntimeModuleBuilder(RuntimeAssemblyBuilder assemblyBuilder, RuntimeModule internalModule)
	{
		_internalModule = internalModule;
		_assemblyBuilder = assemblyBuilder;
		_globalTypeBuilder = new RuntimeTypeBuilder(this);
		_typeBuilderDict = new Dictionary<string, Type>();
	}

	internal void AddType(string name, Type type)
	{
		_typeBuilderDict.Add(name, type);
	}

	internal void CheckTypeNameConflict(string strTypeName, Type enclosingType)
	{
		if (_typeBuilderDict.TryGetValue(strTypeName, out var value) && (object)value.DeclaringType == enclosingType)
		{
			throw new ArgumentException(SR.Argument_DuplicateTypeName);
		}
	}

	private static Type GetType(string strFormat, Type baseType)
	{
		if (string.IsNullOrEmpty(strFormat))
		{
			return baseType;
		}
		return SymbolType.FormCompoundType(strFormat, baseType, 0);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetTypeRef", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetTypeRef(QCallModule module, string strFullName, QCallModule refedModule, int tkResolution)
	{
		int result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(strFullName))
		{
			void* _strFullName_native = ptr;
			result = __PInvoke(module, (ushort*)_strFullName_native, refedModule, tkResolution);
		}
		return result;
		[DllImport("QCall", EntryPoint = "ModuleBuilder_GetTypeRef", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, ushort* __strFullName_native, QCallModule __refedModule_native, int __tkResolution_native);
	}

	[DllImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRef", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRef")]
	private static extern int GetMemberRef(QCallModule module, QCallModule refedModule, int tr, int defToken);

	private int GetMemberRef(Module refedModule, int tr, int defToken)
	{
		RuntimeModuleBuilder module = this;
		RuntimeModule module2 = GetRuntimeModuleFromModule(refedModule);
		return GetMemberRef(new QCallModule(ref module), new QCallModule(ref module2), tr, defToken);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefFromSignature", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetMemberRefFromSignature(QCallModule module, int tr, string methodName, byte[] signature, int length)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(methodName))
			{
				void* _methodName_native = ptr2;
				result = __PInvoke(module, tr, (ushort*)_methodName_native, (byte*)_signature_native, length);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefFromSignature", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tr_native, ushort* __methodName_native, byte* __signature_native, int __length_native);
	}

	private int GetMemberRefFromSignature(int tr, string methodName, byte[] signature, int length)
	{
		RuntimeModuleBuilder module = this;
		return GetMemberRefFromSignature(new QCallModule(ref module), tr, methodName, signature, length);
	}

	[DllImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefOfMethodInfo", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefOfMethodInfo")]
	private static extern int GetMemberRefOfMethodInfo(QCallModule module, int tr, RuntimeMethodHandleInternal method);

	private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
	{
		RuntimeModuleBuilder module = this;
		int memberRefOfMethodInfo = GetMemberRefOfMethodInfo(new QCallModule(ref module), tr, ((IRuntimeMethodInfo)method).Value);
		GC.KeepAlive(method);
		return memberRefOfMethodInfo;
	}

	private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
	{
		RuntimeModuleBuilder module = this;
		int memberRefOfMethodInfo = GetMemberRefOfMethodInfo(new QCallModule(ref module), tr, ((IRuntimeMethodInfo)method).Value);
		GC.KeepAlive(method);
		return memberRefOfMethodInfo;
	}

	[DllImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefOfFieldInfo", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetMemberRefOfFieldInfo")]
	private static extern int GetMemberRefOfFieldInfo(QCallModule module, int tkType, QCallTypeHandle declaringType, int tkField);

	private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
	{
		RuntimeModuleBuilder module = this;
		return GetMemberRefOfFieldInfo(new QCallModule(ref module), tkType, new QCallTypeHandle(ref declaringType), runtimeField.MetadataToken);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetTokenFromTypeSpec")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetTokenFromTypeSpec(QCallModule pModule, byte[] signature, int length)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			result = __PInvoke(pModule, (byte*)_signature_native, length);
		}
		return result;
		[DllImport("QCall", EntryPoint = "ModuleBuilder_GetTokenFromTypeSpec", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __pModule_native, byte* __signature_native, int __length_native);
	}

	private int GetTokenFromTypeSpec(byte[] signature, int length)
	{
		RuntimeModuleBuilder module = this;
		return GetTokenFromTypeSpec(new QCallModule(ref module), signature, length);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetArrayMethodToken", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetArrayMethodToken(QCallModule module, int tkTypeSpec, string methodName, byte[] signature, int sigLength)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(methodName))
			{
				void* _methodName_native = ptr2;
				result = __PInvoke(module, tkTypeSpec, (ushort*)_methodName_native, (byte*)_signature_native, sigLength);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "ModuleBuilder_GetArrayMethodToken", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkTypeSpec_native, ushort* __methodName_native, byte* __signature_native, int __sigLength_native);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_GetStringConstant", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetStringConstant(QCallModule module, string str, int length)
	{
		int result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(str))
		{
			void* _str_native = ptr;
			result = __PInvoke(module, (ushort*)_str_native, length);
		}
		return result;
		[DllImport("QCall", EntryPoint = "ModuleBuilder_GetStringConstant", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, ushort* __str_native, int __length_native);
	}

	[LibraryImport("QCall", EntryPoint = "ModuleBuilder_SetFieldRVAContent")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static void SetFieldRVAContent(QCallModule module, int fdToken, byte[] data, int length)
	{
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(data))
		{
			void* _data_native = ptr;
			__PInvoke(module, fdToken, (byte*)_data_native, length);
		}
		[DllImport("QCall", EntryPoint = "ModuleBuilder_SetFieldRVAContent", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallModule __module_native, int __fdToken_native, byte* __data_native, int __length_native);
	}

	internal Type FindTypeBuilderWithName(string strTypeName, bool ignoreCase)
	{
		Type value;
		if (ignoreCase)
		{
			foreach (string key in _typeBuilderDict.Keys)
			{
				if (string.Equals(key, strTypeName, StringComparison.OrdinalIgnoreCase))
				{
					return _typeBuilderDict[key];
				}
			}
		}
		else if (_typeBuilderDict.TryGetValue(strTypeName, out value))
		{
			return value;
		}
		return null;
	}

	private int GetTypeRefNested(Type type, Module refedModule)
	{
		Type declaringType = type.DeclaringType;
		int tkResolution = 0;
		string text = type.FullName;
		if (declaringType != null)
		{
			tkResolution = GetTypeRefNested(declaringType, refedModule);
			text = UnmangleTypeName(text);
		}
		RuntimeModuleBuilder module = this;
		RuntimeModule module2 = GetRuntimeModuleFromModule(refedModule);
		return GetTypeRef(new QCallModule(ref module), text, new QCallModule(ref module2), tkResolution);
	}

	public override int GetMethodMetadataToken(ConstructorInfo constructor)
	{
		int typeTokenInternal;
		if (constructor is ConstructorBuilder constructorBuilder)
		{
			if (constructorBuilder.Module.Equals(this))
			{
				return constructorBuilder.MetadataToken;
			}
			typeTokenInternal = GetTypeTokenInternal(constructor.ReflectedType);
			return GetMemberRef(constructor.ReflectedType.Module, typeTokenInternal, constructorBuilder.MetadataToken);
		}
		if (constructor is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
		{
			typeTokenInternal = GetTypeTokenInternal(constructor.DeclaringType);
			return GetMemberRef(constructor.DeclaringType.Module, typeTokenInternal, constructorOnTypeBuilderInstantiation.MetadataToken);
		}
		if (constructor is RuntimeConstructorInfo method && !constructor.ReflectedType.IsArray)
		{
			typeTokenInternal = GetTypeTokenInternal(constructor.ReflectedType);
			return GetMemberRefOfMethodInfo(typeTokenInternal, method);
		}
		ParameterInfo[] parameters = constructor.GetParameters();
		if (parameters == null)
		{
			throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
		}
		Type[] array = new Type[parameters.Length];
		Type[][] array2 = new Type[parameters.Length][];
		Type[][] array3 = new Type[parameters.Length][];
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] == null)
			{
				throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
			}
			array[i] = parameters[i].ParameterType;
			array2[i] = parameters[i].GetRequiredCustomModifiers();
			array3[i] = parameters[i].GetOptionalCustomModifiers();
		}
		typeTokenInternal = GetTypeTokenInternal(constructor.ReflectedType);
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, constructor.CallingConvention, null, null, null, array, array2, array3);
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		return GetMemberRefFromSignature(typeTokenInternal, constructor.Name, signature, length);
	}

	private protected override ModuleHandle GetModuleHandleImpl()
	{
		return new ModuleHandle(InternalModule);
	}

	internal static RuntimeModule GetRuntimeModuleFromModule(Module m)
	{
		if (m is RuntimeModuleBuilder runtimeModuleBuilder)
		{
			return runtimeModuleBuilder.InternalModule;
		}
		return m as RuntimeModule;
	}

	private int GetMemberRefToken(MethodBase method, Type[] optionalParameterTypes)
	{
		int cGenericParameters = 0;
		if (method.IsGenericMethod)
		{
			if (!method.IsGenericMethodDefinition)
			{
				throw new InvalidOperationException();
			}
			cGenericParameters = method.GetGenericArguments().Length;
		}
		if (optionalParameterTypes != null && (method.CallingConvention & CallingConventions.VarArgs) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
		}
		MethodInfo methodInfo = method as MethodInfo;
		SignatureHelper memberRefSignature;
		if (method.DeclaringType.IsGenericType)
		{
			MethodBase genericMethodBaseDefinition = GetGenericMethodBaseDefinition(method);
			memberRefSignature = GetMemberRefSignature(genericMethodBaseDefinition, cGenericParameters);
		}
		else
		{
			memberRefSignature = GetMemberRefSignature(method, cGenericParameters);
		}
		if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
		{
			memberRefSignature.AddSentinel();
			memberRefSignature.AddArguments(optionalParameterTypes, null, null);
		}
		int length;
		byte[] signature = memberRefSignature.InternalGetSignature(out length);
		int tr;
		if (!method.DeclaringType.IsGenericType)
		{
			tr = ((!method.Module.Equals(this)) ? GetTypeMetadataToken(method.DeclaringType) : ((!(methodInfo != null)) ? GetMethodMetadataToken(method as ConstructorInfo) : GetMethodMetadataToken(methodInfo)));
		}
		else
		{
			int length2;
			byte[] signature2 = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out length2);
			tr = GetTokenFromTypeSpec(signature2, length2);
		}
		return GetMemberRefFromSignature(tr, method.Name, signature, length);
	}

	internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, Type[] optionalParameterTypes, int cGenericParameters)
	{
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, call, cGenericParameters, returnType, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
		if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
		{
			methodSigHelper.AddSentinel();
			methodSigHelper.AddArguments(optionalParameterTypes, null, null);
		}
		return methodSigHelper;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Module.ResolveMethod is marked as RequiresUnreferencedCode because it relies on tokens which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break. The usage here is not like that as all these tokens come from existing metadata loaded from some IL and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
	private static MethodBase GetGenericMethodBaseDefinition(MethodBase methodBase)
	{
		MethodInfo methodInfo = methodBase as MethodInfo;
		if (methodBase is MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation)
		{
			return methodOnTypeBuilderInstantiation._method;
		}
		if (methodBase is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
		{
			return constructorOnTypeBuilderInstantiation._ctor;
		}
		if (methodBase is MethodBuilder || methodBase is ConstructorBuilder)
		{
			return methodBase;
		}
		if (methodBase.IsGenericMethod)
		{
			MethodBase genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
			return genericMethodDefinition.Module.ResolveMethod(methodBase.MetadataToken, genericMethodDefinition.DeclaringType?.GetGenericArguments(), genericMethodDefinition.GetGenericArguments());
		}
		return methodBase.Module.ResolveMethod(methodBase.MetadataToken, methodBase.DeclaringType?.GetGenericArguments(), null);
	}

	internal SignatureHelper GetMemberRefSignature(MethodBase method, int cGenericParameters)
	{
		MethodBase methodBase = method;
		if (!(methodBase is RuntimeMethodBuilder runtimeMethodBuilder))
		{
			if (!(methodBase is RuntimeConstructorBuilder runtimeConstructorBuilder))
			{
				if (!(methodBase is MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation))
				{
					if (methodBase is ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation)
					{
						if (constructorOnTypeBuilderInstantiation._ctor is RuntimeConstructorBuilder runtimeConstructorBuilder2)
						{
							return runtimeConstructorBuilder2.GetMethodSignature();
						}
						ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation2 = constructorOnTypeBuilderInstantiation;
						method = constructorOnTypeBuilderInstantiation2._ctor;
					}
				}
				else
				{
					if (methodOnTypeBuilderInstantiation._method is RuntimeMethodBuilder runtimeMethodBuilder2)
					{
						return runtimeMethodBuilder2.GetMethodSignature();
					}
					MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation2 = methodOnTypeBuilderInstantiation;
					method = methodOnTypeBuilderInstantiation2._method;
				}
				ParameterInfo[] parametersNoCopy = method.GetParametersNoCopy();
				Type[] array = new Type[parametersNoCopy.Length];
				Type[][] array2 = new Type[array.Length][];
				Type[][] array3 = new Type[array.Length][];
				for (int i = 0; i < parametersNoCopy.Length; i++)
				{
					array[i] = parametersNoCopy[i].ParameterType;
					array2[i] = parametersNoCopy[i].GetRequiredCustomModifiers();
					array3[i] = parametersNoCopy[i].GetOptionalCustomModifiers();
				}
				ParameterInfo parameterInfo = ((method is MethodInfo methodInfo) ? methodInfo.ReturnParameter : null);
				return SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, cGenericParameters, parameterInfo?.ParameterType, parameterInfo?.GetRequiredCustomModifiers(), parameterInfo?.GetOptionalCustomModifiers(), array, array2, array3);
			}
			return runtimeConstructorBuilder.GetMethodSignature();
		}
		return runtimeMethodBuilder.GetMethodSignature();
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return InternalModule.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return InternalModule.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return InternalModule.IsDefined(attributeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return InternalModule.GetCustomAttributesData();
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetTypes()
	{
		lock (SyncRoot)
		{
			return GetTypesNoLock();
		}
	}

	internal Type[] GetTypesNoLock()
	{
		Type[] array = new Type[_typeBuilderDict.Count];
		int num = 0;
		foreach (Type value in _typeBuilderDict.Values)
		{
			RuntimeTypeBuilder runtimeTypeBuilder = ((!(value is RuntimeEnumBuilder runtimeEnumBuilder)) ? ((RuntimeTypeBuilder)value) : runtimeEnumBuilder.m_typeBuilder);
			if (runtimeTypeBuilder.IsCreated())
			{
				array[num++] = runtimeTypeBuilder.UnderlyingSystemType;
			}
			else
			{
				array[num++] = value;
			}
		}
		return array;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string className)
	{
		return GetType(className, throwOnError: false, ignoreCase: false);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string className, bool ignoreCase)
	{
		return GetType(className, throwOnError: false, ignoreCase);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string className, bool throwOnError, bool ignoreCase)
	{
		lock (SyncRoot)
		{
			return GetTypeNoLock(className, throwOnError, ignoreCase);
		}
	}

	[RequiresUnreferencedCode("Types might be removed")]
	private Type GetTypeNoLock(string className, bool throwOnError, bool ignoreCase)
	{
		Type type = InternalModule.GetType(className, throwOnError, ignoreCase);
		if (type != null)
		{
			return type;
		}
		string text = null;
		string text2 = null;
		int num = 0;
		while (num <= className.Length)
		{
			int num2 = className.AsSpan(num).IndexOfAny('[', '*', '&');
			if (num2 < 0)
			{
				text = className;
				text2 = null;
				break;
			}
			num2 += num;
			int num3 = 0;
			int num4 = num2 - 1;
			while (num4 >= 0 && className[num4] == '\\')
			{
				num3++;
				num4--;
			}
			if (num3 % 2 == 1)
			{
				num = num2 + 1;
				continue;
			}
			text = className.Substring(0, num2);
			text2 = className.Substring(num2);
			break;
		}
		if (text == null)
		{
			text = className;
			text2 = null;
		}
		text = text.Replace("\\\\", "\\").Replace("\\[", "[").Replace("\\*", "*")
			.Replace("\\&", "&");
		if (text2 != null)
		{
			type = InternalModule.GetType(text, throwOnError: false, ignoreCase);
		}
		if (type == null)
		{
			type = FindTypeBuilderWithName(text, ignoreCase);
			if (type == null)
			{
				return null;
			}
		}
		if (text2 == null)
		{
			return type;
		}
		return GetType(text2, type);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override byte[] ResolveSignature(int metadataToken)
	{
		return InternalModule.ResolveSignature(metadataToken);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		return InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		return InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		return InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		return InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override string ResolveString(int metadataToken)
	{
		return InternalModule.ResolveString(metadataToken);
	}

	public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		InternalModule.GetPEKind(out peKind, out machine);
	}

	public override bool IsResource()
	{
		return InternalModule.IsResource();
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo[] GetFields(BindingFlags bindingFlags)
	{
		return InternalModule.GetFields(bindingFlags);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		return InternalModule.GetField(name, bindingAttr);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
	{
		return InternalModule.GetMethods(bindingFlags);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		return InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
	{
		lock (SyncRoot)
		{
			return new RuntimeTypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null);
		}
	}

	protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType)
	{
		lock (SyncRoot)
		{
			RuntimeEnumBuilder runtimeEnumBuilder = new RuntimeEnumBuilder(name, underlyingType, visibility, this);
			_typeBuilderDict[name] = runtimeEnumBuilder;
			return runtimeEnumBuilder;
		}
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		lock (SyncRoot)
		{
			if ((attributes & MethodAttributes.Static) == 0)
			{
				throw new ArgumentException(SR.Argument_GlobalMembersMustBeStatic);
			}
			return _globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
		}
	}

	protected override MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		lock (SyncRoot)
		{
			return DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		}
	}

	private MethodBuilder DefineGlobalMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
	{
		if (_hasGlobalBeenCreated)
		{
			throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
		}
		if ((attributes & MethodAttributes.Static) == 0)
		{
			throw new ArgumentException(SR.Argument_GlobalMembersMustBeStatic);
		}
		return _globalTypeBuilder.DefineMethod(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
	}

	protected override void CreateGlobalFunctionsCore()
	{
		lock (SyncRoot)
		{
			if (_hasGlobalBeenCreated)
			{
				throw new InvalidOperationException(SR.InvalidOperation_NotADebugModule);
			}
			_globalTypeBuilder.CreateType();
			_hasGlobalBeenCreated = true;
		}
	}

	protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			if (_hasGlobalBeenCreated)
			{
				throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
			}
			return _globalTypeBuilder.DefineInitializedData(name, data, attributes);
		}
	}

	protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			if (_hasGlobalBeenCreated)
			{
				throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
			}
			return _globalTypeBuilder.DefineUninitializedData(name, size, attributes);
		}
	}

	internal int GetTypeTokenInternal(Type type, bool getGenericDefinition = false)
	{
		lock (SyncRoot)
		{
			return GetTypeTokenWorkerNoLock(type, getGenericDefinition);
		}
	}

	public override int GetTypeMetadataToken(Type type)
	{
		return GetTypeTokenInternal(type, getGenericDefinition: true);
	}

	private int GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition)) || type.IsGenericParameter || type.IsArray || type.IsPointer || type.IsByRef)
		{
			int length;
			byte[] signature = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out length);
			return GetTokenFromTypeSpec(signature, length);
		}
		Module module = type.Module;
		if (module.Equals(this))
		{
			RuntimeTypeBuilder runtimeTypeBuilder = ((type is RuntimeEnumBuilder runtimeEnumBuilder) ? runtimeEnumBuilder.m_typeBuilder : (type as RuntimeTypeBuilder));
			if (runtimeTypeBuilder != null)
			{
				return runtimeTypeBuilder.TypeToken;
			}
			if (type is GenericTypeParameterBuilder genericTypeParameterBuilder)
			{
				return genericTypeParameterBuilder.MetadataToken;
			}
			return GetTypeRefNested(type, this);
		}
		return GetTypeRefNested(type, module);
	}

	public override int GetMethodMetadataToken(MethodInfo method)
	{
		lock (SyncRoot)
		{
			return GetMethodTokenNoLock(method, getGenericTypeDefinition: false);
		}
	}

	private int GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
	{
		ArgumentNullException.ThrowIfNull(method, "method");
		int tr;
		if (method is MethodBuilder { MetadataToken: var metadataToken })
		{
			if (method.Module.Equals(this))
			{
				return metadataToken;
			}
			if (method.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			tr = (getGenericTypeDefinition ? GetTypeMetadataToken(method.DeclaringType) : GetTypeTokenInternal(method.DeclaringType));
			return GetMemberRef(method.DeclaringType.Module, tr, metadataToken);
		}
		if (method is MethodOnTypeBuilderInstantiation)
		{
			return GetMemberRefToken(method, null);
		}
		if (method is SymbolMethod symbolMethod)
		{
			if (symbolMethod.GetModule() == this)
			{
				return symbolMethod.MetadataToken;
			}
			return symbolMethod.GetToken(this);
		}
		Type declaringType = method.DeclaringType;
		if (declaringType == null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
		}
		if (declaringType.IsArray)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Type[] array = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
			}
			return GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, array);
		}
		if (method is RuntimeMethodInfo method2)
		{
			tr = (getGenericTypeDefinition ? GetTypeMetadataToken(declaringType) : GetTypeTokenInternal(declaringType));
			return GetMemberRefOfMethodInfo(tr, method2);
		}
		ParameterInfo[] parameters2 = method.GetParameters();
		Type[] array2 = new Type[parameters2.Length];
		Type[][] array3 = new Type[array2.Length][];
		Type[][] array4 = new Type[array2.Length][];
		for (int j = 0; j < parameters2.Length; j++)
		{
			array2[j] = parameters2[j].ParameterType;
			array3[j] = parameters2[j].GetRequiredCustomModifiers();
			array4[j] = parameters2[j].GetOptionalCustomModifiers();
		}
		tr = (getGenericTypeDefinition ? GetTypeMetadataToken(declaringType) : GetTypeTokenInternal(declaringType));
		SignatureHelper methodSigHelper;
		try
		{
			methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, method.ReturnType, method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), array2, array3, array4);
		}
		catch (NotImplementedException)
		{
			methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, array2);
		}
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		return GetMemberRefFromSignature(tr, method.Name, signature, length);
	}

	internal int GetMethodTokenInternal(MethodBase method, Type[] optionalParameterTypes, bool useMethodDef)
	{
		MethodInfo methodInfo = method as MethodInfo;
		if (method.IsGenericMethod)
		{
			MethodInfo methodInfo2 = methodInfo;
			bool isGenericMethodDefinition = methodInfo.IsGenericMethodDefinition;
			if (!isGenericMethodDefinition)
			{
				methodInfo2 = methodInfo.GetGenericMethodDefinition();
			}
			int num = ((Equals(methodInfo2.Module) && (!(methodInfo2.DeclaringType != null) || !methodInfo2.DeclaringType.IsGenericType)) ? GetMethodMetadataToken(methodInfo2) : GetMemberRefToken(methodInfo2, null));
			if (isGenericMethodDefinition && useMethodDef)
			{
				return num;
			}
			int length;
			byte[] signature = SignatureHelper.GetMethodSpecSigHelper(this, methodInfo.GetGenericArguments()).InternalGetSignature(out length);
			RuntimeModuleBuilder module = this;
			return RuntimeTypeBuilder.DefineMethodSpec(new QCallModule(ref module), num, signature, length);
		}
		if ((method.CallingConvention & CallingConventions.VarArgs) == 0 && (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
		{
			if (methodInfo != null)
			{
				return GetMethodMetadataToken(methodInfo);
			}
			return GetMethodMetadataToken(method as ConstructorInfo);
		}
		return GetMemberRefToken(method, optionalParameterTypes);
	}

	internal int GetArrayMethodToken(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		lock (SyncRoot)
		{
			return GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
		}
	}

	private int GetArrayMethodTokenNoLock(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		if (!arrayClass.IsArray)
		{
			throw new ArgumentException(SR.Argument_HasToBeArrayClass);
		}
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, callingConvention, returnType, null, null, parameterTypes, null, null);
		int length;
		byte[] signature = methodSigHelper.InternalGetSignature(out length);
		int typeTokenInternal = GetTypeTokenInternal(arrayClass);
		RuntimeModuleBuilder module = this;
		return GetArrayMethodToken(new QCallModule(ref module), typeTokenInternal, methodName, signature, length);
	}

	protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		int arrayMethodToken = GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);
		return new SymbolMethod(this, arrayMethodToken, arrayClass, methodName, callingConvention, returnType, parameterTypes);
	}

	public override int GetFieldMetadataToken(FieldInfo field)
	{
		lock (SyncRoot)
		{
			return GetFieldTokenNoLock(field);
		}
	}

	private int GetFieldTokenNoLock(FieldInfo field)
	{
		ArgumentNullException.ThrowIfNull(field, "field");
		int tokenFromTypeSpec;
		if (field is FieldBuilder fieldBuilder)
		{
			if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
			{
				int length;
				byte[] signature = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
				tokenFromTypeSpec = GetTokenFromTypeSpec(signature, length);
				return GetMemberRef(this, tokenFromTypeSpec, fieldBuilder.MetadataToken);
			}
			if (fieldBuilder.Module.Equals(this))
			{
				return fieldBuilder.MetadataToken;
			}
			if (field.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			tokenFromTypeSpec = GetTypeTokenInternal(field.DeclaringType);
			return GetMemberRef(field.ReflectedType.Module, tokenFromTypeSpec, fieldBuilder.MetadataToken);
		}
		if (field is RuntimeFieldInfo runtimeField)
		{
			if (field.DeclaringType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
			}
			if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
			{
				int length2;
				byte[] signature2 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length2);
				tokenFromTypeSpec = GetTokenFromTypeSpec(signature2, length2);
				return GetMemberRefOfFieldInfo(tokenFromTypeSpec, field.DeclaringType.TypeHandle, runtimeField);
			}
			tokenFromTypeSpec = GetTypeTokenInternal(field.DeclaringType);
			return GetMemberRefOfFieldInfo(tokenFromTypeSpec, field.DeclaringType.TypeHandle, runtimeField);
		}
		if (field is FieldOnTypeBuilderInstantiation { FieldInfo: var fieldInfo } fieldOnTypeBuilderInstantiation)
		{
			int length3;
			byte[] signature3 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length3);
			tokenFromTypeSpec = GetTokenFromTypeSpec(signature3, length3);
			return GetMemberRef(fieldInfo.ReflectedType.Module, tokenFromTypeSpec, fieldOnTypeBuilderInstantiation.MetadataToken);
		}
		tokenFromTypeSpec = GetTypeTokenInternal(field.ReflectedType);
		SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(this);
		fieldSigHelper.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());
		int length4;
		byte[] signature4 = fieldSigHelper.InternalGetSignature(out length4);
		return GetMemberRefFromSignature(tokenFromTypeSpec, field.Name, signature4, length4);
	}

	public override int GetStringMetadataToken(string stringConstant)
	{
		ArgumentNullException.ThrowIfNull(stringConstant, "stringConstant");
		RuntimeModuleBuilder module = this;
		return GetStringConstant(new QCallModule(ref module), stringConstant, stringConstant.Length);
	}

	public override int GetSignatureMetadataToken(SignatureHelper signature)
	{
		ArgumentNullException.ThrowIfNull(signature, "signature");
		int length;
		byte[] signature2 = signature.InternalGetSignature(out length);
		RuntimeModuleBuilder module = this;
		return RuntimeTypeBuilder.GetTokenFromSig(new QCallModule(ref module), signature2, length);
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		RuntimeTypeBuilder.DefineCustomAttribute(this, 1, GetMethodMetadataToken(con), binaryAttribute);
	}
}
