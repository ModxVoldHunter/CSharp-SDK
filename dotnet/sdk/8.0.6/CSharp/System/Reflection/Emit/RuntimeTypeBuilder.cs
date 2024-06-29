using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Reflection.Emit;

internal sealed class RuntimeTypeBuilder : TypeBuilder
{
	private sealed class CustAttr
	{
		private readonly ConstructorInfo m_con;

		private readonly byte[] m_binaryAttribute;

		private readonly CustomAttributeBuilder m_customBuilder;

		public CustAttr(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
		{
			ArgumentNullException.ThrowIfNull(con, "con");
			m_con = con;
			m_binaryAttribute = binaryAttribute.ToArray();
		}

		public void Bake(RuntimeModuleBuilder module, int token)
		{
			if (m_customBuilder == null)
			{
				DefineCustomAttribute(module, token, module.GetMethodMetadataToken(m_con), m_binaryAttribute);
			}
			else
			{
				m_customBuilder.CreateCustomAttribute(module, token);
			}
		}
	}

	private List<CustAttr> m_ca;

	private int m_tdType;

	private readonly RuntimeModuleBuilder m_module;

	private readonly string m_strName;

	private readonly string m_strNameSpace;

	private string m_strFullQualName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type m_typeParent;

	private List<Type> m_typeInterfaces;

	private readonly TypeAttributes m_iAttr;

	private GenericParameterAttributes m_genParamAttributes;

	internal List<RuntimeMethodBuilder> m_listMethods;

	internal int m_lastTokenizedMethod;

	private int m_constructorCount;

	private readonly int m_iTypeSize;

	private readonly PackingSize m_iPackingSize;

	private readonly RuntimeTypeBuilder m_DeclaringType;

	private Type m_enumUnderlyingType;

	internal bool m_isHiddenGlobalType;

	private bool m_hasBeenCreated;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private RuntimeType m_bakedRuntimeType;

	private readonly int m_genParamPos;

	private RuntimeGenericTypeParameterBuilder[] m_inst;

	private readonly bool m_bIsGenParam;

	private readonly RuntimeMethodBuilder m_declMeth;

	private readonly RuntimeTypeBuilder m_genTypeDef;

	internal object SyncRoot => m_module.SyncRoot;

	internal RuntimeType BakedRuntimeType => m_bakedRuntimeType;

	public override Type DeclaringType => m_DeclaringType;

	public override Type ReflectedType => m_DeclaringType;

	public override string Name => m_strName;

	public override Module Module => GetModuleBuilder();

	public override bool IsByRefLike => false;

	public override int MetadataToken => m_tdType;

	public override Guid GUID
	{
		get
		{
			if (!IsCreated())
			{
				throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
			}
			return m_bakedRuntimeType.GUID;
		}
	}

	public override Assembly Assembly => m_module.Assembly;

	public override RuntimeTypeHandle TypeHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicModule);
		}
	}

	public override string FullName => m_strFullQualName ?? (m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName));

	public override string Namespace => m_strNameSpace;

	public override string AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

	public override Type BaseType => m_typeParent;

	public override bool IsTypeDefinition => true;

	public override bool IsSZArray => false;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override Type UnderlyingSystemType
	{
		get
		{
			if (m_bakedRuntimeType != null)
			{
				return m_bakedRuntimeType;
			}
			if (IsEnum)
			{
				if (m_enumUnderlyingType == null)
				{
					throw new InvalidOperationException(SR.InvalidOperation_NoUnderlyingTypeOnEnum);
				}
				return m_enumUnderlyingType;
			}
			return this;
		}
	}

	public override GenericParameterAttributes GenericParameterAttributes => m_genParamAttributes;

	public override bool IsGenericTypeDefinition => IsGenericType;

	public override bool IsGenericType => m_inst != null;

	public override bool IsGenericParameter => m_bIsGenParam;

	public override bool IsConstructedGenericType => false;

	public override int GenericParameterPosition => m_genParamPos;

	public override MethodBase DeclaringMethod => m_declMeth;

	protected override int SizeCore => m_iTypeSize;

	protected override PackingSize PackingSizeCore => m_iPackingSize;

	internal int TypeToken
	{
		get
		{
			if (IsGenericParameter)
			{
				ThrowIfCreated();
			}
			return m_tdType;
		}
	}

	public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			return false;
		}
		return IsAssignableFrom(typeInfo.AsType());
	}

	[DllImport("QCall", EntryPoint = "TypeBuilder_SetParentType", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetParentType")]
	private static extern void SetParentType(QCallModule module, int tdTypeDef, int tkParent);

	[DllImport("QCall", EntryPoint = "TypeBuilder_AddInterfaceImpl", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_AddInterfaceImpl")]
	private static extern void AddInterfaceImpl(QCallModule module, int tdTypeDef, int tkInterface);

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineMethod", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int DefineMethod(QCallModule module, int tkParent, string name, byte[] signature, int sigLength, MethodAttributes attributes)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(name))
			{
				void* _name_native = ptr2;
				result = __PInvoke(module, tkParent, (ushort*)_name_native, (byte*)_signature_native, sigLength, attributes);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineMethod", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkParent_native, ushort* __name_native, byte* __signature_native, int __sigLength_native, MethodAttributes __attributes_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineMethodSpec")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int DefineMethodSpec(QCallModule module, int tkParent, byte[] signature, int sigLength)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			result = __PInvoke(module, tkParent, (byte*)_signature_native, sigLength);
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineMethodSpec", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkParent_native, byte* __signature_native, int __sigLength_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineField", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int DefineField(QCallModule module, int tkParent, string name, byte[] signature, int sigLength, FieldAttributes attributes)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(name))
			{
				void* _name_native = ptr2;
				result = __PInvoke(module, tkParent, (ushort*)_name_native, (byte*)_signature_native, sigLength, attributes);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineField", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkParent_native, ushort* __name_native, byte* __signature_native, int __sigLength_native, FieldAttributes __attributes_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetMethodIL")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void SetMethodIL(QCallModule module, int tk, [MarshalAs(UnmanagedType.Bool)] bool isInitLocals, byte[] body, int bodyLength, byte[] LocalSig, int sigLength, int maxStackSize, ExceptionHandler[] exceptions, int numExceptions, int[] tokenFixups, int numTokenFixups)
	{
		int _isInitLocals_native = (isInitLocals ? 1 : 0);
		fixed (int* ptr = &ArrayMarshaller<int, int>.ManagedToUnmanagedIn.GetPinnableReference(tokenFixups))
		{
			void* _tokenFixups_native = ptr;
			fixed (ExceptionHandler* ptr2 = &ArrayMarshaller<ExceptionHandler, ExceptionHandler>.ManagedToUnmanagedIn.GetPinnableReference(exceptions))
			{
				void* _exceptions_native = ptr2;
				fixed (byte* ptr3 = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(LocalSig))
				{
					void* _LocalSig_native = ptr3;
					fixed (byte* ptr4 = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(body))
					{
						void* _body_native = ptr4;
						__PInvoke(module, tk, _isInitLocals_native, (byte*)_body_native, bodyLength, (byte*)_LocalSig_native, sigLength, maxStackSize, (ExceptionHandler*)_exceptions_native, numExceptions, (int*)_tokenFixups_native, numTokenFixups);
					}
				}
			}
		}
		[DllImport("QCall", EntryPoint = "TypeBuilder_SetMethodIL", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallModule __module_native, int __tk_native, int __isInitLocals_native, byte* __body_native, int __bodyLength_native, byte* __LocalSig_native, int __sigLength_native, int __maxStackSize_native, ExceptionHandler* __exceptions_native, int __numExceptions_native, int* __tokenFixups_native, int __numTokenFixups_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineCustomAttribute")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void DefineCustomAttribute(QCallModule module, int tkAssociate, int tkConstructor, ReadOnlySpan<byte> attr, int attrLength)
	{
		fixed (byte* ptr = &ReadOnlySpanMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(attr))
		{
			void* _attr_native = ptr;
			__PInvoke(module, tkAssociate, tkConstructor, (byte*)_attr_native, attrLength);
		}
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineCustomAttribute", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallModule __module_native, int __tkAssociate_native, int __tkConstructor_native, byte* __attr_native, int __attrLength_native);
	}

	internal static void DefineCustomAttribute(RuntimeModuleBuilder module, int tkAssociate, int tkConstructor, ReadOnlySpan<byte> attr)
	{
		DefineCustomAttribute(new QCallModule(ref module), tkAssociate, tkConstructor, attr, attr.Length);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineProperty", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int DefineProperty(QCallModule module, int tkParent, string name, PropertyAttributes attributes, byte[] signature, int sigLength)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(name))
			{
				void* _name_native = ptr2;
				result = __PInvoke(module, tkParent, (ushort*)_name_native, attributes, (byte*)_signature_native, sigLength);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineProperty", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkParent_native, ushort* __name_native, PropertyAttributes __attributes_native, byte* __signature_native, int __sigLength_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineEvent", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int DefineEvent(QCallModule module, int tkParent, string name, EventAttributes attributes, int tkEventType)
	{
		int result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			result = __PInvoke(module, tkParent, (ushort*)_name_native, attributes, tkEventType);
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineEvent", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkParent_native, ushort* __name_native, EventAttributes __attributes_native, int __tkEventType_native);
	}

	[DllImport("QCall", EntryPoint = "TypeBuilder_DefineMethodSemantics", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineMethodSemantics")]
	internal static extern void DefineMethodSemantics(QCallModule module, int tkAssociation, MethodSemanticsAttributes semantics, int tkMethod);

	[DllImport("QCall", EntryPoint = "TypeBuilder_DefineMethodImpl", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineMethodImpl")]
	internal static extern void DefineMethodImpl(QCallModule module, int tkType, int tkBody, int tkDecl);

	[DllImport("QCall", EntryPoint = "TypeBuilder_SetMethodImpl", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetMethodImpl")]
	internal static extern void SetMethodImpl(QCallModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetParamInfo", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int SetParamInfo(QCallModule module, int tkMethod, int iSequence, ParameterAttributes iParamAttributes, string strParamName)
	{
		int result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(strParamName))
		{
			void* _strParamName_native = ptr;
			result = __PInvoke(module, tkMethod, iSequence, iParamAttributes, (ushort*)_strParamName_native);
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_SetParamInfo", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, int __tkMethod_native, int __iSequence_native, ParameterAttributes __iParamAttributes_native, ushort* __strParamName_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_GetTokenFromSig")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static int GetTokenFromSig(QCallModule module, byte[] signature, int sigLength)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(signature))
		{
			void* _signature_native = ptr;
			result = __PInvoke(module, (byte*)_signature_native, sigLength);
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_GetTokenFromSig", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, byte* __signature_native, int __sigLength_native);
	}

	[DllImport("QCall", EntryPoint = "TypeBuilder_SetFieldLayoutOffset", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetFieldLayoutOffset")]
	internal static extern void SetFieldLayoutOffset(QCallModule module, int fdToken, int iOffset);

	[DllImport("QCall", EntryPoint = "TypeBuilder_SetClassLayout", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetClassLayout")]
	internal static extern void SetClassLayout(QCallModule module, int tk, PackingSize iPackingSize, int iTypeSize);

	[DllImport("QCall", EntryPoint = "TypeBuilder_SetConstantValue", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetConstantValue")]
	private unsafe static extern void SetConstantValue(QCallModule module, int tk, int corType, void* pValue);

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_SetPInvokeData", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void SetPInvokeData(QCallModule module, string DllName, string name, int token, int linkFlags)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(DllName))
			{
				void* _DllName_native = ptr2;
				__PInvoke(module, (ushort*)_DllName_native, (ushort*)_name_native, token, linkFlags);
			}
		}
		[DllImport("QCall", EntryPoint = "TypeBuilder_SetPInvokeData", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallModule __module_native, ushort* __DllName_native, ushort* __name_native, int __token_native, int __linkFlags_native);
	}

	internal static bool IsTypeEqual(Type t1, Type t2)
	{
		if (t1 == t2)
		{
			return true;
		}
		RuntimeTypeBuilder runtimeTypeBuilder = null;
		RuntimeTypeBuilder runtimeTypeBuilder2 = null;
		Type type;
		if (t1 is RuntimeTypeBuilder)
		{
			runtimeTypeBuilder = (RuntimeTypeBuilder)t1;
			type = runtimeTypeBuilder.m_bakedRuntimeType;
		}
		else
		{
			type = t1;
		}
		Type type2;
		if (t2 is RuntimeTypeBuilder)
		{
			runtimeTypeBuilder2 = (RuntimeTypeBuilder)t2;
			type2 = runtimeTypeBuilder2.m_bakedRuntimeType;
		}
		else
		{
			type2 = t2;
		}
		if (runtimeTypeBuilder != null && runtimeTypeBuilder2 != null && (object)runtimeTypeBuilder == runtimeTypeBuilder2)
		{
			return true;
		}
		if (type != null && type2 != null && type == type2)
		{
			return true;
		}
		return false;
	}

	internal unsafe static void SetConstantValue(RuntimeModuleBuilder module, int tk, Type destType, object value)
	{
		if (value != null)
		{
			Type type = value.GetType();
			if (destType.IsByRef)
			{
				destType = destType.GetElementType();
			}
			destType = Nullable.GetUnderlyingType(destType) ?? destType;
			if (destType.IsEnum)
			{
				Type type2;
				if (destType is RuntimeEnumBuilder runtimeEnumBuilder)
				{
					type2 = runtimeEnumBuilder.GetEnumUnderlyingType();
					if (type != runtimeEnumBuilder.m_typeBuilder.m_bakedRuntimeType && type != type2)
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				else if (destType is RuntimeTypeBuilder runtimeTypeBuilder)
				{
					type2 = runtimeTypeBuilder.m_enumUnderlyingType;
					if (type2 == null || (type != runtimeTypeBuilder.UnderlyingSystemType && type != type2))
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				else
				{
					type2 = Enum.GetUnderlyingType(destType);
					if (type != destType && type != type2)
					{
						throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
					}
				}
				type = type2;
			}
			else if (!destType.IsAssignableFrom(type))
			{
				throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
			}
			CorElementType corElementType = RuntimeTypeHandle.GetCorElementType((RuntimeType)type);
			if (corElementType - 2 <= CorElementType.ELEMENT_TYPE_U8)
			{
				fixed (byte* pValue = &value.GetRawData())
				{
					SetConstantValue(new QCallModule(ref module), tk, (int)corElementType, pValue);
				}
				return;
			}
			if (type == typeof(string))
			{
				fixed (char* pValue2 = (string)value)
				{
					SetConstantValue(new QCallModule(ref module), tk, 14, pValue2);
				}
				return;
			}
			if (!(type == typeof(DateTime)))
			{
				throw new ArgumentException(SR.Format(SR.Argument_ConstantNotSupported, type));
			}
			long ticks = ((DateTime)value).Ticks;
			SetConstantValue(new QCallModule(ref module), tk, 10, &ticks);
		}
		else
		{
			SetConstantValue(new QCallModule(ref module), tk, 18, null);
		}
	}

	internal RuntimeTypeBuilder(RuntimeModuleBuilder module)
	{
		m_tdType = 33554432;
		m_isHiddenGlobalType = true;
		m_module = module;
		m_listMethods = new List<RuntimeMethodBuilder>();
		m_lastTokenizedMethod = -1;
	}

	internal RuntimeTypeBuilder(string szName, int genParamPos, RuntimeMethodBuilder declMeth)
	{
		m_strName = szName;
		m_genParamPos = genParamPos;
		m_bIsGenParam = true;
		m_typeInterfaces = new List<Type>();
		m_declMeth = declMeth;
		m_DeclaringType = m_declMeth.GetTypeBuilder();
		m_module = declMeth.GetModuleBuilder();
	}

	private RuntimeTypeBuilder(string szName, int genParamPos, RuntimeTypeBuilder declType)
	{
		m_strName = szName;
		m_genParamPos = genParamPos;
		m_bIsGenParam = true;
		m_typeInterfaces = new List<Type>();
		m_DeclaringType = declType;
		m_module = declType.GetModuleBuilder();
	}

	internal RuntimeTypeBuilder(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, RuntimeModuleBuilder module, PackingSize iPackingSize, int iTypeSize, RuntimeTypeBuilder enclosingType)
	{
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "name");
		}
		if (name.Length > 1023)
		{
			throw new ArgumentException(SR.Argument_TypeNameTooLong, "name");
		}
		m_module = module;
		m_DeclaringType = enclosingType;
		RuntimeAssemblyBuilder containingAssemblyBuilder = m_module.ContainingAssemblyBuilder;
		containingAssemblyBuilder.CheckTypeNameConflict(name, enclosingType);
		if (enclosingType != null && ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public || (attr & TypeAttributes.VisibilityMask) == 0))
		{
			throw new ArgumentException(SR.Argument_BadNestedTypeFlags, "attr");
		}
		int[] array = null;
		if (interfaces != null)
		{
			array = new int[interfaces.Length + 1];
			for (int i = 0; i < interfaces.Length; i++)
			{
				ArgumentNullException.ThrowIfNull(interfaces[i], "interfaces");
				array[i] = m_module.GetTypeTokenInternal(interfaces[i]);
			}
		}
		int num = name.LastIndexOf('.');
		if (num <= 0)
		{
			m_strNameSpace = string.Empty;
			m_strName = name;
		}
		else
		{
			m_strNameSpace = name.Substring(0, num);
			m_strName = name.Substring(num + 1);
		}
		VerifyTypeAttributes(attr);
		m_iAttr = attr;
		SetParent(parent);
		m_listMethods = new List<RuntimeMethodBuilder>();
		m_lastTokenizedMethod = -1;
		SetInterfaces(interfaces);
		int tkParent = 0;
		if (m_typeParent != null)
		{
			tkParent = m_module.GetTypeTokenInternal(m_typeParent);
		}
		int tkEnclosingType = 0;
		if (enclosingType != null)
		{
			tkEnclosingType = enclosingType.m_tdType;
		}
		m_tdType = DefineType(new QCallModule(ref module), name, tkParent, m_iAttr, tkEnclosingType, array);
		m_iPackingSize = iPackingSize;
		m_iTypeSize = iTypeSize;
		if (m_iPackingSize != 0 || m_iTypeSize != 0)
		{
			SetClassLayout(new QCallModule(ref module), m_tdType, m_iPackingSize, m_iTypeSize);
		}
		m_module.AddType(FullName, this);
	}

	private FieldBuilder DefineDataHelper(string name, byte[] data, int size, FieldAttributes attributes)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		if (size <= 0 || size >= 4128768)
		{
			throw new ArgumentException(SR.Argument_BadSizeForData);
		}
		ThrowIfCreated();
		string text = $"$ArrayType${size}";
		Type type = m_module.FindTypeBuilderWithName(text, ignoreCase: false);
		TypeBuilder typeBuilder = type as TypeBuilder;
		if (typeBuilder == null)
		{
			TypeAttributes attr = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed;
			typeBuilder = m_module.DefineType(text, attr, typeof(ValueType), PackingSize.Size1, size);
			typeBuilder.CreateType();
		}
		FieldBuilder fieldBuilder = DefineField(name, typeBuilder, attributes | FieldAttributes.Static);
		((RuntimeFieldBuilder)fieldBuilder).SetData(data, size);
		return fieldBuilder;
	}

	private void VerifyTypeAttributes(TypeAttributes attr)
	{
		if (DeclaringType == null)
		{
			if ((attr & TypeAttributes.VisibilityMask) != 0 && (attr & TypeAttributes.VisibilityMask) != TypeAttributes.Public)
			{
				throw new ArgumentException(SR.Argument_BadTypeAttrNestedVisibilityOnNonNestedType);
			}
		}
		else if ((attr & TypeAttributes.VisibilityMask) == 0 || (attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrNonNestedVisibilityNestedType);
		}
		if ((attr & TypeAttributes.LayoutMask) != 0 && (attr & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout && (attr & TypeAttributes.LayoutMask) != TypeAttributes.ExplicitLayout)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrInvalidLayout);
		}
		if ((attr & TypeAttributes.ReservedMask) != 0)
		{
			throw new ArgumentException(SR.Argument_BadTypeAttrReservedBitsSet);
		}
	}

	protected override bool IsCreatedCore()
	{
		return m_hasBeenCreated;
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineType", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int DefineType(QCallModule module, string fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens)
	{
		int result;
		fixed (int* ptr = &ArrayMarshaller<int, int>.ManagedToUnmanagedIn.GetPinnableReference(interfaceTokens))
		{
			void* _interfaceTokens_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(fullname))
			{
				void* _fullname_native = ptr2;
				result = __PInvoke(module, (ushort*)_fullname_native, tkParent, attributes, tkEnclosingType, (int*)_interfaceTokens_native);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineType", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, ushort* __fullname_native, int __tkParent_native, TypeAttributes __attributes_native, int __tkEnclosingType_native, int* __interfaceTokens_native);
	}

	[LibraryImport("QCall", EntryPoint = "TypeBuilder_DefineGenericParam", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int DefineGenericParam(QCallModule module, string name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints)
	{
		int result;
		fixed (int* ptr = &ArrayMarshaller<int, int>.ManagedToUnmanagedIn.GetPinnableReference(constraints))
		{
			void* _constraints_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(name))
			{
				void* _name_native = ptr2;
				result = __PInvoke(module, (ushort*)_name_native, tkParent, attributes, position, (int*)_constraints_native);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "TypeBuilder_DefineGenericParam", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallModule __module_native, ushort* __name_native, int __tkParent_native, GenericParameterAttributes __attributes_native, int __position_native, int* __constraints_native);
	}

	[DllImport("QCall", EntryPoint = "TypeBuilder_TermCreateClass", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "TypeBuilder_TermCreateClass")]
	private static extern void TermCreateClass(QCallModule module, int tk, ObjectHandleOnStack type);

	internal void ThrowIfCreated()
	{
		if (IsCreated())
		{
			throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
		}
	}

	internal RuntimeModuleBuilder GetModuleBuilder()
	{
		return m_module;
	}

	internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
	{
		m_genParamAttributes = genericParameterAttributes;
	}

	internal void SetGenParamCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		CustAttr genParamCustomAttributeNoLock = new CustAttr(con, binaryAttribute);
		lock (SyncRoot)
		{
			SetGenParamCustomAttributeNoLock(genParamCustomAttributeNoLock);
		}
	}

	private void SetGenParamCustomAttributeNoLock(CustAttr ca)
	{
		if (m_ca == null)
		{
			m_ca = new List<CustAttr>();
		}
		m_ca.Add(ca);
	}

	public override string ToString()
	{
		return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetConstructors(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		if (types == null)
		{
			return m_bakedRuntimeType.GetMethod(name, bindingAttr);
		}
		return m_bakedRuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMethods(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetField(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetFields(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type GetInterface(string name, bool ignoreCase)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetInterface(name, ignoreCase);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
	public override Type[] GetInterfaces()
	{
		if (m_bakedRuntimeType != null)
		{
			return m_bakedRuntimeType.GetInterfaces();
		}
		if (m_typeInterfaces == null)
		{
			return Type.EmptyTypes;
		}
		return m_typeInterfaces.ToArray();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvent(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
	public override EventInfo[] GetEvents()
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvents();
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetProperties(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetNestedTypes(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetNestedType(name, bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
	}

	public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetInterfaceMap(interfaceType);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetEvents(bindingAttr);
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return m_bakedRuntimeType.GetMembers(bindingAttr);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The GetInterfaces technically requires all interfaces to be preservedBut in this case it acts only on TypeBuilder which is never trimmed (as it's runtime created).")]
	public override bool IsAssignableFrom([NotNullWhen(true)] Type c)
	{
		if (IsTypeEqual(c, this))
		{
			return true;
		}
		RuntimeTypeBuilder runtimeTypeBuilder = c as RuntimeTypeBuilder;
		Type type = ((!(runtimeTypeBuilder != null)) ? c : runtimeTypeBuilder.m_bakedRuntimeType);
		if (type != null && type is RuntimeType)
		{
			if (m_bakedRuntimeType == null)
			{
				return false;
			}
			return m_bakedRuntimeType.IsAssignableFrom(type);
		}
		if (runtimeTypeBuilder == null)
		{
			return false;
		}
		if (runtimeTypeBuilder.IsSubclassOf(this))
		{
			return true;
		}
		if (!base.IsInterface)
		{
			return false;
		}
		Type[] interfaces = runtimeTypeBuilder.GetInterfaces();
		for (int i = 0; i < interfaces.Length; i++)
		{
			if (IsTypeEqual(interfaces[i], this))
			{
				return true;
			}
			if (interfaces[i].IsSubclassOf(this))
			{
				return true;
			}
		}
		return false;
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return m_iAttr;
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
		return (GetAttributeFlagsImpl() & TypeAttributes.Import) != 0;
	}

	public override Type GetElementType()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	public override bool IsSubclassOf(Type c)
	{
		Type type = this;
		if (IsTypeEqual(type, c))
		{
			return false;
		}
		type = type.BaseType;
		while (type != null)
		{
			if (IsTypeEqual(type, c))
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, typeof(object) as RuntimeType, inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, caType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (!IsCreated())
		{
			throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
		}
		ArgumentNullException.ThrowIfNull(attributeType, "attributeType");
		if (!(attributeType.UnderlyingSystemType is RuntimeType caType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(m_bakedRuntimeType, caType, inherit);
	}

	internal void SetInterfaces(params Type[] interfaces)
	{
		ThrowIfCreated();
		m_typeInterfaces = new List<Type>();
		if (interfaces != null)
		{
			m_typeInterfaces.AddRange(interfaces);
		}
	}

	protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
	{
		if (m_inst != null)
		{
			throw new InvalidOperationException();
		}
		m_inst = new RuntimeGenericTypeParameterBuilder[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			string text = names[i];
			ArgumentNullException.ThrowIfNull(text, "names");
			m_inst[i] = new RuntimeGenericTypeParameterBuilder(new RuntimeTypeBuilder(text, i, this));
		}
		return m_inst;
	}

	public override Type[] GetGenericArguments()
	{
		Type[] inst = m_inst;
		return inst ?? Type.EmptyTypes;
	}

	public override Type GetGenericTypeDefinition()
	{
		if (IsGenericTypeDefinition)
		{
			return this;
		}
		if (m_genTypeDef == null)
		{
			throw new InvalidOperationException();
		}
		return m_genTypeDef;
	}

	protected override void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
	{
		lock (SyncRoot)
		{
			ThrowIfCreated();
			if ((object)methodInfoBody.DeclaringType != this)
			{
				throw new ArgumentException(SR.ArgumentException_BadMethodImplBody);
			}
			int methodMetadataToken = m_module.GetMethodMetadataToken(methodInfoBody);
			int methodMetadataToken2 = m_module.GetMethodMetadataToken(methodInfoDeclaration);
			RuntimeModuleBuilder module = m_module;
			DefineMethodImpl(new QCallModule(ref module), m_tdType, methodMetadataToken, methodMetadataToken2);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		lock (SyncRoot)
		{
			ThrowIfCreated();
			RuntimeMethodBuilder runtimeMethodBuilder = new RuntimeMethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this);
			if (!m_isHiddenGlobalType && (runtimeMethodBuilder.Attributes & MethodAttributes.SpecialName) != 0 && runtimeMethodBuilder.Name.Equals(ConstructorInfo.ConstructorName))
			{
				m_constructorCount++;
			}
			m_listMethods.Add(runtimeMethodBuilder);
			return runtimeMethodBuilder;
		}
	}

	[RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
	protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
	{
		lock (SyncRoot)
		{
			if ((attributes & MethodAttributes.Abstract) != 0)
			{
				throw new ArgumentException(SR.Argument_BadPInvokeMethod);
			}
			if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask)
			{
				throw new ArgumentException(SR.Argument_BadPInvokeOnInterface);
			}
			ThrowIfCreated();
			attributes |= MethodAttributes.PinvokeImpl;
			RuntimeMethodBuilder runtimeMethodBuilder = new RuntimeMethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this);
			runtimeMethodBuilder.GetMethodSignature().InternalGetSignature(out var _);
			if (m_listMethods.Contains(runtimeMethodBuilder))
			{
				throw new ArgumentException(SR.Argument_MethodRedefined);
			}
			m_listMethods.Add(runtimeMethodBuilder);
			int metadataToken = runtimeMethodBuilder.MetadataToken;
			int num = 0;
			switch (nativeCallConv)
			{
			case CallingConvention.Winapi:
				num = 256;
				break;
			case CallingConvention.Cdecl:
				num = 512;
				break;
			case CallingConvention.StdCall:
				num = 768;
				break;
			case CallingConvention.ThisCall:
				num = 1024;
				break;
			case CallingConvention.FastCall:
				num = 1280;
				break;
			}
			switch (nativeCharSet)
			{
			case CharSet.None:
				num |= 0;
				break;
			case CharSet.Ansi:
				num |= 2;
				break;
			case CharSet.Unicode:
				num |= 4;
				break;
			case CharSet.Auto:
				num |= 6;
				break;
			}
			RuntimeModuleBuilder module = m_module;
			SetPInvokeData(new QCallModule(ref module), dllName, entryName, metadataToken, num);
			runtimeMethodBuilder.SetToken(metadataToken);
			return runtimeMethodBuilder;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	protected override ConstructorBuilder DefineTypeInitializerCore()
	{
		lock (SyncRoot)
		{
			ThrowIfCreated();
			return new RuntimeConstructorBuilder(ConstructorInfo.TypeConstructorName, MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName, CallingConventions.Standard, null, m_module, this);
		}
	}

	protected override ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes)
	{
		if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
		}
		lock (SyncRoot)
		{
			return DefineDefaultConstructorNoLock(attributes);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern", Justification = "MakeGenericType is only called on a TypeBuilderInstantiation which is not subject to trimming")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "GetConstructor is only called on a TypeBuilderInstantiation which is not subject to trimming")]
	private RuntimeConstructorBuilder DefineDefaultConstructorNoLock(MethodAttributes attributes)
	{
		ConstructorInfo constructorInfo = null;
		if (m_typeParent is TypeBuilderInstantiation)
		{
			Type type = m_typeParent.GetGenericTypeDefinition();
			if (type is RuntimeTypeBuilder runtimeTypeBuilder)
			{
				type = runtimeTypeBuilder.m_bakedRuntimeType;
			}
			if (type == null)
			{
				throw new NotSupportedException(SR.NotSupported_DynamicModule);
			}
			Type type2 = type.MakeGenericType(m_typeParent.GetGenericArguments());
			constructorInfo = ((!(type2 is TypeBuilderInstantiation)) ? type2.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) : TypeBuilder.GetConstructor(type2, type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)));
		}
		if ((object)constructorInfo == null)
		{
			constructorInfo = m_typeParent.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		}
		if (constructorInfo == null)
		{
			throw new NotSupportedException(SR.NotSupported_NoParentDefaultConstructor);
		}
		RuntimeConstructorBuilder runtimeConstructorBuilder = (RuntimeConstructorBuilder)DefineConstructor(attributes, CallingConventions.Standard, null);
		m_constructorCount++;
		ILGenerator iLGenerator = runtimeConstructorBuilder.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Call, constructorInfo);
		iLGenerator.Emit(OpCodes.Ret);
		runtimeConstructorBuilder.m_isDefaultConstructor = true;
		return runtimeConstructorBuilder;
	}

	protected override ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
	{
		if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
		}
		lock (SyncRoot)
		{
			return DefineConstructorNoLock(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
	{
		ThrowIfCreated();
		string name = (((attributes & MethodAttributes.Static) != 0) ? ConstructorInfo.TypeConstructorName : ConstructorInfo.ConstructorName);
		attributes |= MethodAttributes.SpecialName;
		ConstructorBuilder result = new RuntimeConstructorBuilder(name, attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);
		m_constructorCount++;
		return result;
	}

	protected override TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces, PackingSize packSize, int typeSize)
	{
		lock (SyncRoot)
		{
			return new RuntimeTypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
		}
	}

	protected override FieldBuilder DefineFieldCore(string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			ThrowIfCreated();
			if (m_enumUnderlyingType == null && IsEnum && (attributes & FieldAttributes.Static) == 0)
			{
				m_enumUnderlyingType = type;
			}
			return new RuntimeFieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
		}
	}

	protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			return DefineDataHelper(name, data, data.Length, attributes);
		}
	}

	protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
	{
		lock (SyncRoot)
		{
			return DefineDataHelper(name, null, size, attributes);
		}
	}

	protected override PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
	{
		lock (SyncRoot)
		{
			ThrowIfCreated();
			SignatureHelper propertySigHelper = SignatureHelper.GetPropertySigHelper(m_module, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
			int length;
			byte[] signature = propertySigHelper.InternalGetSignature(out length);
			RuntimeModuleBuilder module = m_module;
			int prToken = DefineProperty(new QCallModule(ref module), m_tdType, name, attributes, signature, length);
			return new RuntimePropertyBuilder(m_module, name, attributes, returnType, prToken, this);
		}
	}

	protected override EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype)
	{
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "name");
		}
		lock (SyncRoot)
		{
			ThrowIfCreated();
			int typeTokenInternal = m_module.GetTypeTokenInternal(eventtype);
			RuntimeModuleBuilder module = m_module;
			int evToken = DefineEvent(new QCallModule(ref module), m_tdType, name, attributes, typeTokenInternal);
			return new RuntimeEventBuilder(m_module, name, attributes, this, evToken);
		}
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	protected override TypeInfo CreateTypeInfoCore()
	{
		return CreateTypeInfoImpl();
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal TypeInfo CreateTypeInfoImpl()
	{
		lock (SyncRoot)
		{
			return CreateTypeNoLock();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2068:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2069:UnrecognizedReflectionPattern", Justification = "Reflection.Emit is not subject to trimming")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private TypeInfo CreateTypeNoLock()
	{
		if (IsCreated())
		{
			return m_bakedRuntimeType;
		}
		if (m_typeInterfaces == null)
		{
			m_typeInterfaces = new List<Type>();
		}
		int[] array = new int[m_typeInterfaces.Count];
		for (int i = 0; i < m_typeInterfaces.Count; i++)
		{
			array[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]);
		}
		int num = 0;
		if (m_typeParent != null)
		{
			num = m_module.GetTypeTokenInternal(m_typeParent);
		}
		RuntimeModuleBuilder module = m_module;
		if (IsGenericParameter)
		{
			int[] array2;
			if (m_typeParent != null)
			{
				array2 = new int[m_typeInterfaces.Count + 2];
				array2[^2] = num;
			}
			else
			{
				array2 = new int[m_typeInterfaces.Count + 1];
			}
			for (int j = 0; j < m_typeInterfaces.Count; j++)
			{
				array2[j] = m_module.GetTypeTokenInternal(m_typeInterfaces[j]);
			}
			int tkParent = ((m_declMeth == null) ? m_DeclaringType.m_tdType : m_declMeth.MetadataToken);
			m_tdType = DefineGenericParam(new QCallModule(ref module), m_strName, tkParent, m_genParamAttributes, m_genParamPos, array2);
			if (m_ca != null)
			{
				foreach (CustAttr item in m_ca)
				{
					item.Bake(m_module, MetadataToken);
				}
			}
			m_hasBeenCreated = true;
			return this;
		}
		if (((uint)m_tdType & 0xFFFFFFu) != 0 && ((uint)num & 0xFFFFFFu) != 0)
		{
			SetParentType(new QCallModule(ref module), m_tdType, num);
		}
		if (m_inst != null)
		{
			RuntimeGenericTypeParameterBuilder[] inst = m_inst;
			foreach (RuntimeGenericTypeParameterBuilder runtimeGenericTypeParameterBuilder in inst)
			{
				runtimeGenericTypeParameterBuilder.m_type.CreateType();
			}
		}
		if (!m_isHiddenGlobalType && m_constructorCount == 0 && (m_iAttr & TypeAttributes.ClassSemanticsMask) == 0 && !base.IsValueType && (m_iAttr & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed))
		{
			DefineDefaultConstructor(MethodAttributes.Public);
		}
		int count = m_listMethods.Count;
		for (int l = 0; l < count; l++)
		{
			RuntimeMethodBuilder runtimeMethodBuilder = m_listMethods[l];
			if (runtimeMethodBuilder.IsGenericMethodDefinition)
			{
				_ = runtimeMethodBuilder.MetadataToken;
			}
			MethodAttributes attributes = runtimeMethodBuilder.Attributes;
			if ((runtimeMethodBuilder.GetMethodImplementationFlags() & (MethodImplAttributes)135) != 0 || (attributes & MethodAttributes.PinvokeImpl) != 0)
			{
				continue;
			}
			int signatureLength;
			byte[] localSignature = runtimeMethodBuilder.GetLocalSignature(out signatureLength);
			if ((attributes & MethodAttributes.Abstract) != 0 && (m_iAttr & TypeAttributes.Abstract) == 0)
			{
				throw new InvalidOperationException(SR.InvalidOperation_BadTypeAttributesNotAbstract);
			}
			byte[] body = runtimeMethodBuilder.GetBody();
			if ((attributes & MethodAttributes.Abstract) != 0)
			{
				if (body != null)
				{
					throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadMethodBody, runtimeMethodBuilder.Name));
				}
			}
			else if (body == null || body.Length == 0)
			{
				if (runtimeMethodBuilder.m_ilGenerator != null)
				{
					runtimeMethodBuilder.CreateMethodBodyHelper((RuntimeILGenerator)runtimeMethodBuilder.GetILGenerator());
				}
				body = runtimeMethodBuilder.GetBody();
				if ((body == null || body.Length == 0) && !runtimeMethodBuilder.m_canBeRuntimeImpl)
				{
					throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, runtimeMethodBuilder.Name));
				}
			}
			int maxStack = runtimeMethodBuilder.GetMaxStack();
			ExceptionHandler[] exceptionHandlers = runtimeMethodBuilder.GetExceptionHandlers();
			int[] tokenFixups = runtimeMethodBuilder.GetTokenFixups();
			SetMethodIL(new QCallModule(ref module), runtimeMethodBuilder.MetadataToken, runtimeMethodBuilder.InitLocals, body, (body != null) ? body.Length : 0, localSignature, signatureLength, maxStack, exceptionHandlers, (exceptionHandlers != null) ? exceptionHandlers.Length : 0, tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);
			if (m_module.ContainingAssemblyBuilder._access == AssemblyBuilderAccess.Run)
			{
				runtimeMethodBuilder.ReleaseBakedStructures();
			}
		}
		m_hasBeenCreated = true;
		RuntimeType o = null;
		TermCreateClass(new QCallModule(ref module), m_tdType, ObjectHandleOnStack.Create(ref o));
		if (!m_isHiddenGlobalType)
		{
			m_bakedRuntimeType = o;
			if (m_DeclaringType != null && m_DeclaringType.m_bakedRuntimeType != null)
			{
				m_DeclaringType.m_bakedRuntimeType.InvalidateCachedNestedType();
			}
			return o;
		}
		return null;
	}

	protected override void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent)
	{
		ThrowIfCreated();
		if (parent != null)
		{
			if (parent.IsInterface)
			{
				throw new ArgumentException(SR.Argument_CannotSetParentToInterface);
			}
			m_typeParent = parent;
		}
		else if ((m_iAttr & TypeAttributes.ClassSemanticsMask) != TypeAttributes.ClassSemanticsMask)
		{
			m_typeParent = typeof(object);
		}
		else
		{
			if ((m_iAttr & TypeAttributes.Abstract) == 0)
			{
				throw new InvalidOperationException(SR.InvalidOperation_BadInterfaceNotAbstract);
			}
			m_typeParent = null;
		}
	}

	protected override void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		ThrowIfCreated();
		int typeTokenInternal = m_module.GetTypeTokenInternal(interfaceType);
		RuntimeModuleBuilder module = m_module;
		AddInterfaceImpl(new QCallModule(ref module), m_tdType, typeTokenInternal);
		m_typeInterfaces.Add(interfaceType);
	}

	internal void SetCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		SetCustomAttributeCore(con, binaryAttribute);
	}

	protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
	{
		DefineCustomAttribute(m_module, m_tdType, m_module.GetMethodMetadataToken(con), binaryAttribute);
	}
}
