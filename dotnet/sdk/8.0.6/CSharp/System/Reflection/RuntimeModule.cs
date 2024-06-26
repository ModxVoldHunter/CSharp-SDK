using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection;

internal sealed class RuntimeModule : Module
{
	private RuntimeType m_runtimeType;

	private readonly RuntimeAssembly m_runtimeAssembly;

	private readonly nint m_pRefClass;

	private readonly nint m_pData;

	private readonly nint m_pGlobals;

	private readonly nint m_pFields;

	public override int MDStreamVersion => ModuleHandle.GetMDStreamVersion(this);

	internal RuntimeType RuntimeType => m_runtimeType ?? (m_runtimeType = ModuleHandle.GetModuleType(this));

	internal MetadataImport MetadataImport => ModuleHandle.GetMetadataImport(this);

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string FullyQualifiedName => GetFullyQualifiedName();

	public override Guid ModuleVersionId
	{
		get
		{
			MetadataImport.GetScopeProps(out var mvid);
			return mvid;
		}
	}

	public override int MetadataToken => ModuleHandle.GetToken(this);

	public override string ScopeName
	{
		get
		{
			string s = null;
			RuntimeModule module = this;
			GetScopeName(new QCallModule(ref module), new StringHandleOnStack(ref s));
			return s;
		}
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	public override string Name
	{
		get
		{
			string fullyQualifiedName = GetFullyQualifiedName();
			int num = fullyQualifiedName.LastIndexOf(Path.DirectorySeparatorChar);
			if (num < 0)
			{
				return fullyQualifiedName;
			}
			return fullyQualifiedName.Substring(num + 1);
		}
	}

	public override Assembly Assembly => GetRuntimeAssembly();

	internal RuntimeModule()
	{
		throw new NotSupportedException();
	}

	[DllImport("QCall", EntryPoint = "RuntimeModule_GetScopeName", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeModule_GetScopeName")]
	private static extern void GetScopeName(QCallModule module, StringHandleOnStack retString);

	[DllImport("QCall", EntryPoint = "RuntimeModule_GetFullyQualifiedName", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeModule_GetFullyQualifiedName")]
	private static extern void GetFullyQualifiedName(QCallModule module, StringHandleOnStack retString);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern RuntimeType[] GetTypes(RuntimeModule module);

	internal RuntimeType[] GetDefinedTypes()
	{
		return GetTypes(this);
	}

	private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
	{
		if (genericArguments == null)
		{
			return null;
		}
		int num = genericArguments.Length;
		RuntimeTypeHandle[] array = new RuntimeTypeHandle[num];
		for (int i = 0; i < num; i++)
		{
			Type type = genericArguments[i];
			if (type == null)
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			type = type.UnderlyingSystemType;
			if (type == null)
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			if (!(type is RuntimeType))
			{
				throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
			}
			array[i] = type.TypeHandle;
		}
		return array;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override byte[] ResolveSignature(int metadataToken)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!MetadataImport.IsValidToken(metadataToken2))
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		if (!metadataToken2.IsMemberRef && !metadataToken2.IsMethodDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsSignature && !metadataToken2.IsFieldDef)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidToken, metadataToken2, this), "metadataToken");
		}
		ConstArray constArray = ((!metadataToken2.IsMemberRef) ? MetadataImport.GetSignatureFromToken(metadataToken) : MetadataImport.GetMemberRefProps(metadataToken));
		byte[] array = new byte[constArray.Length];
		for (int i = 0; i < constArray.Length; i++)
		{
			array[i] = constArray[i];
		}
		return array;
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!metadataToken2.IsMethodDef && !metadataToken2.IsMethodSpec)
			{
				if (!metadataToken2.IsMemberRef)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, metadataToken2, this), "metadataToken");
				}
				if (*(byte*)MetadataImport.GetMemberRefProps(metadataToken2).Signature == 6)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, metadataToken2, this), "metadataToken");
				}
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			IRuntimeMethodInfo methodInfo = new ModuleHandle(this).ResolveMethodHandle(metadataToken2, typeInstantiationContext, methodInstantiationContext).GetMethodInfo();
			Type type = RuntimeMethodHandle.GetDeclaringType(methodInfo);
			if (type.IsGenericType || type.IsArray)
			{
				MetadataToken metadataToken3 = new MetadataToken(MetadataImport.GetParentToken(metadataToken2));
				if (metadataToken2.IsMethodSpec)
				{
					metadataToken3 = new MetadataToken(MetadataImport.GetParentToken(metadataToken3));
				}
				type = ResolveType(metadataToken3, genericTypeArguments, genericMethodArguments);
			}
			return RuntimeType.GetMethodBase(type as RuntimeType, methodInfo);
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	private FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!MetadataImport.IsValidToken(metadataToken2) || !metadataToken2.IsFieldDef)
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		string name = MetadataImport.GetName(metadataToken2).ToString();
		int parentToken = MetadataImport.GetParentToken(metadataToken2);
		Type type = ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
		try
		{
			return type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			ModuleHandle moduleHandle = new ModuleHandle(this);
			if (!metadataToken2.IsFieldDef)
			{
				if (!metadataToken2.IsMemberRef)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
				}
				if (*(byte*)MetadataImport.GetMemberRefProps(metadataToken2).Signature != 6)
				{
					throw new ArgumentException(SR.Format(SR.Argument_ResolveField, metadataToken2, this), "metadataToken");
				}
			}
			IRuntimeFieldInfo runtimeFieldInfo = moduleHandle.ResolveFieldHandle(metadataToken, typeInstantiationContext, methodInstantiationContext).GetRuntimeFieldInfo();
			RuntimeType runtimeType = RuntimeFieldHandle.GetApproxDeclaringType(runtimeFieldInfo.Value);
			if (runtimeType.IsGenericType || runtimeType.IsArray)
			{
				int parentToken = ModuleHandle.GetMetadataImport(this).GetParentToken(metadataToken);
				runtimeType = (RuntimeType)ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
			}
			return RuntimeType.GetFieldInfo(runtimeType, runtimeFieldInfo);
		}
		catch (MissingFieldException)
		{
			return ResolveLiteralField(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		try
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (metadataToken2.IsGlobalTypeDefToken)
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveModuleType, metadataToken2), "metadataToken");
			}
			if (!metadataToken2.IsTypeDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsTypeRef)
			{
				throw new ArgumentException(SR.Format(SR.Argument_ResolveType, metadataToken2, this), "metadataToken");
			}
			RuntimeTypeHandle[] typeInstantiationContext = null;
			RuntimeTypeHandle[] methodInstantiationContext = null;
			if (genericTypeArguments != null && genericTypeArguments.Length != 0)
			{
				typeInstantiationContext = ConvertToTypeHandleArray(genericTypeArguments);
			}
			if (genericMethodArguments != null && genericMethodArguments.Length != 0)
			{
				methodInstantiationContext = ConvertToTypeHandleArray(genericMethodArguments);
			}
			return GetModuleHandleImpl().ResolveTypeHandle(metadataToken, typeInstantiationContext, methodInstantiationContext).GetRuntimeType();
		}
		catch (BadImageFormatException innerException)
		{
			throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, innerException);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (metadataToken2.IsProperty)
		{
			throw new ArgumentException(SR.InvalidOperation_PropertyInfoNotAvailable);
		}
		if (metadataToken2.IsEvent)
		{
			throw new ArgumentException(SR.InvalidOperation_EventInfoNotAvailable);
		}
		if (metadataToken2.IsMethodSpec || metadataToken2.IsMethodDef)
		{
			return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsFieldDef)
		{
			return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsTypeRef || metadataToken2.IsTypeDef || metadataToken2.IsTypeSpec)
		{
			return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
		}
		if (metadataToken2.IsMemberRef)
		{
			if (!MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
			}
			if (*(byte*)MetadataImport.GetMemberRefProps(metadataToken2).Signature == 6)
			{
				return ResolveField(metadataToken2, genericTypeArguments, genericMethodArguments);
			}
			return ResolveMethod(metadataToken2, genericTypeArguments, genericMethodArguments);
		}
		throw new ArgumentException(SR.Format(SR.Argument_ResolveMember, metadataToken2, this), "metadataToken");
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public override string ResolveString(int metadataToken)
	{
		MetadataToken metadataToken2 = new MetadataToken(metadataToken);
		if (!metadataToken2.IsString)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveString, metadataToken, this));
		}
		if (!MetadataImport.IsValidToken(metadataToken2))
		{
			throw new ArgumentOutOfRangeException("metadataToken", SR.Format(SR.Argument_InvalidToken, metadataToken2, this));
		}
		string userString = MetadataImport.GetUserString(metadataToken);
		if (userString == null)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResolveString, metadataToken, this));
		}
		return userString;
	}

	public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		ModuleHandle.GetPEKind(this, out peKind, out machine);
	}

	[RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		return GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
	internal MethodInfo GetMethodInternal(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		if (RuntimeType == null)
		{
			return null;
		}
		if (types == null)
		{
			return RuntimeType.GetMethod(name, bindingAttr);
		}
		return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, caType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, caType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string className, bool throwOnError, bool ignoreCase)
	{
		ArgumentException.ThrowIfNullOrEmpty(className, "className");
		Assembly assembly = Assembly;
		return TypeNameParser.GetType(className, throwOnError, ignoreCase, assembly);
	}

	[RequiresAssemblyFiles("Returns <Unknown> for modules with no file path")]
	internal string GetFullyQualifiedName()
	{
		string s = null;
		RuntimeModule module = this;
		GetFullyQualifiedName(new QCallModule(ref module), new StringHandleOnStack(ref s));
		return s;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetTypes()
	{
		return GetTypes(this);
	}

	public override bool IsResource()
	{
		return false;
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo[] GetFields(BindingFlags bindingFlags)
	{
		if (RuntimeType == null)
		{
			return Array.Empty<FieldInfo>();
		}
		return RuntimeType.GetFields(bindingFlags);
	}

	[RequiresUnreferencedCode("Fields might be removed")]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		ArgumentNullException.ThrowIfNull(name, "name");
		return RuntimeType?.GetField(name, bindingAttr);
	}

	[RequiresUnreferencedCode("Methods might be removed")]
	public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
	{
		if (RuntimeType == null)
		{
			return Array.Empty<MethodInfo>();
		}
		return RuntimeType.GetMethods(bindingFlags);
	}

	internal RuntimeAssembly GetRuntimeAssembly()
	{
		return m_runtimeAssembly;
	}

	private protected override ModuleHandle GetModuleHandleImpl()
	{
		return new ModuleHandle(this);
	}

	internal nint GetUnderlyingNativeHandle()
	{
		return m_pData;
	}
}
