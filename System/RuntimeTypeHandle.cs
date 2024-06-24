using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System;

[NonVersionable]
public struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>, ISerializable
{
	internal struct IntroducedMethodEnumerator
	{
		private bool _firstCall;

		private RuntimeMethodHandleInternal _handle;

		public RuntimeMethodHandleInternal Current => _handle;

		internal IntroducedMethodEnumerator(RuntimeType type)
		{
			_handle = GetFirstIntroducedMethod(type);
			_firstCall = true;
		}

		public bool MoveNext()
		{
			if (_firstCall)
			{
				_firstCall = false;
			}
			else if (_handle.Value != IntPtr.Zero)
			{
				GetNextIntroducedMethod(ref _handle);
			}
			return _handle.Value != IntPtr.Zero;
		}

		public IntroducedMethodEnumerator GetEnumerator()
		{
			return this;
		}
	}

	internal RuntimeType m_type;

	public nint Value => m_type?.m_handle ?? 0;

	internal RuntimeTypeHandle GetNativeHandle()
	{
		RuntimeType type = m_type;
		if (type == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		return new RuntimeTypeHandle(type);
	}

	internal RuntimeType GetTypeChecked()
	{
		RuntimeType type = m_type;
		if (type == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		return type;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsInstanceOfType(RuntimeType type, [NotNullWhen(true)] object o);

	public static RuntimeTypeHandle FromIntPtr(nint value)
	{
		return new RuntimeTypeHandle(Type.GetTypeFromHandleUnsafe(value));
	}

	[Intrinsic]
	public static nint ToIntPtr(RuntimeTypeHandle value)
	{
		return value.Value;
	}

	public static bool operator ==(RuntimeTypeHandle left, object? right)
	{
		return left.Equals(right);
	}

	public static bool operator ==(object? left, RuntimeTypeHandle right)
	{
		return right.Equals(left);
	}

	public static bool operator !=(RuntimeTypeHandle left, object? right)
	{
		return !left.Equals(right);
	}

	public static bool operator !=(object? left, RuntimeTypeHandle right)
	{
		return !right.Equals(left);
	}

	public override int GetHashCode()
	{
		return m_type?.GetHashCode() ?? 0;
	}

	public override bool Equals(object? obj)
	{
		if (obj is RuntimeTypeHandle runtimeTypeHandle)
		{
			return (object)runtimeTypeHandle.m_type == m_type;
		}
		return false;
	}

	public bool Equals(RuntimeTypeHandle handle)
	{
		return (object)handle.m_type == m_type;
	}

	internal RuntimeTypeHandle(RuntimeType type)
	{
		m_type = type;
	}

	internal static bool IsTypeDefinition(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (((int)corElementType < 1 || (int)corElementType >= 15) && corElementType != CorElementType.ELEMENT_TYPE_VALUETYPE && corElementType != CorElementType.ELEMENT_TYPE_CLASS && corElementType != CorElementType.ELEMENT_TYPE_TYPEDBYREF && corElementType != CorElementType.ELEMENT_TYPE_I && corElementType != CorElementType.ELEMENT_TYPE_U && corElementType != CorElementType.ELEMENT_TYPE_OBJECT)
		{
			return false;
		}
		if (type.IsConstructedGenericType)
		{
			return false;
		}
		return true;
	}

	internal static bool IsPrimitive(RuntimeType type)
	{
		return GetCorElementType(type).IsPrimitiveType();
	}

	internal static bool IsByRef(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_BYREF;
	}

	internal static bool IsPointer(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_PTR;
	}

	internal static bool IsArray(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (corElementType != CorElementType.ELEMENT_TYPE_ARRAY)
		{
			return corElementType == CorElementType.ELEMENT_TYPE_SZARRAY;
		}
		return true;
	}

	internal static bool IsSZArray(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_SZARRAY;
	}

	internal static bool IsFunctionPointer(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_FNPTR;
	}

	internal static bool HasElementType(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (corElementType != CorElementType.ELEMENT_TYPE_ARRAY && corElementType != CorElementType.ELEMENT_TYPE_SZARRAY && corElementType != CorElementType.ELEMENT_TYPE_PTR)
		{
			return corElementType == CorElementType.ELEMENT_TYPE_BYREF;
		}
		return true;
	}

	internal static ReadOnlySpan<nint> CopyRuntimeTypeHandles(RuntimeTypeHandle[] inHandles, Span<nint> stackScratch)
	{
		if (inHandles == null || inHandles.Length == 0)
		{
			return default(ReadOnlySpan<nint>);
		}
		Span<nint> span = ((inHandles.Length <= stackScratch.Length) ? stackScratch.Slice(0, inHandles.Length) : ((Span<nint>)new nint[inHandles.Length]));
		for (int i = 0; i < inHandles.Length; i++)
		{
			span[i] = inHandles[i].Value;
		}
		return span;
	}

	internal static nint[] CopyRuntimeTypeHandles(Type[] inHandles, out int length)
	{
		if (inHandles == null || inHandles.Length == 0)
		{
			length = 0;
			return null;
		}
		nint[] array = new nint[inHandles.Length];
		for (int i = 0; i < inHandles.Length; i++)
		{
			array[i] = inHandles[i].TypeHandle.Value;
		}
		length = array.Length;
		return array;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:ParameterDoesntMeetParameterRequirements", Justification = "The parameter 'type' is passed by ref to QCallTypeHandle which only instantiatesthe type using the public parameterless constructor and doesn't modify it")]
	internal unsafe static object CreateInstanceForAnotherGenericParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type, RuntimeType genericParameter)
	{
		object o = null;
		nint value = genericParameter.TypeHandle.Value;
		CreateInstanceForAnotherGenericParameter(new QCallTypeHandle(ref type), &value, 1, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(genericParameter);
		return o;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:ParameterDoesntMeetParameterRequirements", Justification = "The parameter 'type' is passed by ref to QCallTypeHandle which only instantiatesthe type using the public parameterless constructor and doesn't modify it")]
	internal unsafe static object CreateInstanceForAnotherGenericParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type, RuntimeType genericParameter1, RuntimeType genericParameter2)
	{
		object o = null;
		byte* num = stackalloc byte[(int)checked((nuint)2u * (nuint)8u)];
		*(nint*)num = genericParameter1.TypeHandle.Value;
		*(nint*)(num + 8) = genericParameter2.TypeHandle.Value;
		nint* pTypeHandles = (nint*)num;
		CreateInstanceForAnotherGenericParameter(new QCallTypeHandle(ref type), pTypeHandles, 2, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(genericParameter1);
		GC.KeepAlive(genericParameter2);
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter")]
	private unsafe static extern void CreateInstanceForAnotherGenericParameter(QCallTypeHandle baseType, nint* pTypeHandles, int cTypeHandles, ObjectHandleOnStack instantiatedObject);

	internal unsafe static void GetActivationInfo(RuntimeType rt, out delegate*<void*, object> pfnAllocator, out void* vAllocatorFirstArg, out delegate*<object, void> pfnCtor, out bool ctorIsPublic)
	{
		delegate*<void*, object> delegate_002A = default(delegate*<void*, object>);
		void* ptr = default(void*);
		delegate*<object, void> delegate_002A2 = default(delegate*<object, void>);
		Interop.BOOL bOOL = Interop.BOOL.FALSE;
		GetActivationInfo(ObjectHandleOnStack.Create(ref rt), &delegate_002A, &ptr, &delegate_002A2, &bOOL);
		Unsafe.As<delegate*<void*, object>, IntPtr>(ref pfnAllocator) = (nint)delegate_002A;
		vAllocatorFirstArg = ptr;
		Unsafe.As<delegate*<object, void>, IntPtr>(ref pfnCtor) = (nint)delegate_002A2;
		ctorIsPublic = bOOL != Interop.BOOL.FALSE;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_GetActivationInfo", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_GetActivationInfo")]
	private unsafe static extern void GetActivationInfo(ObjectHandleOnStack pRuntimeType, delegate*<void*, object>* ppfnAllocator, void** pvAllocatorFirstArg, delegate*<object, void>* ppfnCtor, Interop.BOOL* pfCtorIsPublic);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern object AllocateComObject(void* pClassFactory);

	internal RuntimeType GetRuntimeType()
	{
		return m_type;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern CorElementType GetCorElementType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeAssembly GetAssembly(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeModule GetModule(RuntimeType type);

	public ModuleHandle GetModuleHandle()
	{
		return new ModuleHandle(GetModule(m_type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetBaseType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern TypeAttributes GetAttributes(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetElementType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CompareCanonicalHandles(RuntimeType left, RuntimeType right);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetArrayRank(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeMethodHandleInternal GetMethodAt(RuntimeType type, int slot);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Type[] GetArgumentTypesFromFunctionPointer(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsUnmanagedFunctionPointer(RuntimeType type);

	internal static IntroducedMethodEnumerator GetIntroducedMethods(RuntimeType type)
	{
		return new IntroducedMethodEnumerator(type);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern RuntimeMethodHandleInternal GetFirstIntroducedMethod(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetNextIntroducedMethod(ref RuntimeMethodHandleInternal method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern bool GetFields(RuntimeType type, nint* result, int* count);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Type[] GetInterfaces(RuntimeType type);

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_GetConstraints", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_GetConstraints")]
	private static extern void GetConstraints(QCallTypeHandle handle, ObjectHandleOnStack types);

	internal Type[] GetConstraints()
	{
		Type[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetConstraints(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "QCall_GetGCHandleForTypeHandle", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "QCall_GetGCHandleForTypeHandle")]
	private static extern nint GetGCHandle(QCallTypeHandle handle, GCHandleType type);

	internal nint GetGCHandle(GCHandleType type)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		return GetGCHandle(new QCallTypeHandle(ref rth), type);
	}

	[DllImport("QCall", EntryPoint = "QCall_FreeGCHandleForTypeHandle", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "QCall_FreeGCHandleForTypeHandle")]
	private static extern nint FreeGCHandle(QCallTypeHandle typeHandle, nint objHandle);

	internal nint FreeGCHandle(nint objHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		return FreeGCHandle(new QCallTypeHandle(ref rth), objHandle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetNumVirtuals(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetNumVirtualsAndStaticVirtuals(RuntimeType type);

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_VerifyInterfaceIsImplemented", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_VerifyInterfaceIsImplemented")]
	private static extern void VerifyInterfaceIsImplemented(QCallTypeHandle handle, QCallTypeHandle interfaceHandle);

	internal void VerifyInterfaceIsImplemented(RuntimeTypeHandle interfaceHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		RuntimeTypeHandle rth2 = interfaceHandle.GetNativeHandle();
		VerifyInterfaceIsImplemented(new QCallTypeHandle(ref rth), new QCallTypeHandle(ref rth2));
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_GetInterfaceMethodImplementation", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_GetInterfaceMethodImplementation")]
	private static extern RuntimeMethodHandleInternal GetInterfaceMethodImplementation(QCallTypeHandle handle, QCallTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle);

	internal RuntimeMethodHandleInternal GetInterfaceMethodImplementation(RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		RuntimeTypeHandle rth2 = interfaceHandle.GetNativeHandle();
		return GetInterfaceMethodImplementation(new QCallTypeHandle(ref rth), new QCallTypeHandle(ref rth2), interfaceMethodHandle);
	}

	internal static bool IsComObject(RuntimeType type, bool isGenericCOM)
	{
		if (isGenericCOM)
		{
			return type.TypeHandle.Value == typeof(__ComObject).TypeHandle.Value;
		}
		return CanCastTo(type, (RuntimeType)typeof(__ComObject));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsInterface(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsByRefLike(RuntimeType type);

	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_IsVisible")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool _IsVisible(QCallTypeHandle typeHandle)
	{
		int num = __PInvoke(typeHandle);
		return num != 0;
		[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_IsVisible", ExactSpelling = true)]
		static extern int __PInvoke(QCallTypeHandle __typeHandle_native);
	}

	internal static bool IsVisible(RuntimeType type)
	{
		return _IsVisible(new QCallTypeHandle(ref type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsValueType(RuntimeType type);

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_ConstructName", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_ConstructName")]
	private static extern void ConstructName(QCallTypeHandle handle, TypeNameFormatFlags formatFlags, StringHandleOnStack retString);

	internal string ConstructName(TypeNameFormatFlags formatFlags)
	{
		string s = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		ConstructName(new QCallTypeHandle(ref rth), formatFlags, new StringHandleOnStack(ref s));
		return s;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void* _GetUtf8Name(RuntimeType type);

	internal unsafe static MdUtf8String GetUtf8Name(RuntimeType type)
	{
		return new MdUtf8String(_GetUtf8Name(type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CanCastTo(RuntimeType type, RuntimeType target);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetDeclaringType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IRuntimeMethodInfo GetDeclaringMethod(RuntimeType type);

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_GetInstantiation", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_GetInstantiation")]
	internal static extern void GetInstantiation(QCallTypeHandle type, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

	internal RuntimeType[] GetInstantiationInternal()
	{
		RuntimeType[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetInstantiation(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o), Interop.BOOL.TRUE);
		return o;
	}

	internal Type[] GetInstantiationPublic()
	{
		Type[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetInstantiation(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o), Interop.BOOL.FALSE);
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_Instantiate", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_Instantiate")]
	private unsafe static extern void Instantiate(QCallTypeHandle handle, nint* pInst, int numGenericArgs, ObjectHandleOnStack type);

	internal unsafe RuntimeType Instantiate(RuntimeType inst)
	{
		nint value = inst.TypeHandle.Value;
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		Instantiate(new QCallTypeHandle(ref rth), &value, 1, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(inst);
		return o;
	}

	internal unsafe RuntimeType Instantiate(Type[] inst)
	{
		int length;
		fixed (nint* pInst = CopyRuntimeTypeHandles(inst, out length))
		{
			RuntimeType o = null;
			RuntimeTypeHandle rth = GetNativeHandle();
			Instantiate(new QCallTypeHandle(ref rth), pInst, length, ObjectHandleOnStack.Create(ref o));
			GC.KeepAlive(inst);
			return o;
		}
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeArray", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeArray")]
	private static extern void MakeArray(QCallTypeHandle handle, int rank, ObjectHandleOnStack type);

	internal RuntimeType MakeArray(int rank)
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeArray(new QCallTypeHandle(ref rth), rank, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeSZArray", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeSZArray")]
	private static extern void MakeSZArray(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakeSZArray()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeSZArray(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeByRef", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_MakeByRef")]
	private static extern void MakeByRef(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakeByRef()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeByRef(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_MakePointer", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_MakePointer")]
	private static extern void MakePointer(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakePointer()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakePointer(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_IsCollectible", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_IsCollectible")]
	internal static extern Interop.BOOL IsCollectible(QCallTypeHandle handle);

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_GetGenericTypeDefinition", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_GetGenericTypeDefinition")]
	internal static extern void GetGenericTypeDefinition(QCallTypeHandle type, ObjectHandleOnStack retType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsGenericVariable(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetGenericVariableIndex(RuntimeType type);

	internal int GetGenericVariableIndex()
	{
		RuntimeType typeChecked = GetTypeChecked();
		if (!IsGenericVariable(typeChecked))
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
		return GetGenericVariableIndex(typeChecked);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool ContainsGenericVariables(RuntimeType handle);

	internal bool ContainsGenericVariables()
	{
		return ContainsGenericVariables(GetTypeChecked());
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern bool SatisfiesConstraints(RuntimeType paramType, nint* pTypeContext, int typeContextLength, nint* pMethodContext, int methodContextLength, RuntimeType toType);

	internal unsafe static bool SatisfiesConstraints(RuntimeType paramType, RuntimeType[] typeContext, RuntimeType[] methodContext, RuntimeType toType)
	{
		Type[] inHandles = typeContext;
		int length;
		nint[] array = CopyRuntimeTypeHandles(inHandles, out length);
		inHandles = methodContext;
		int length2;
		nint[] array2 = CopyRuntimeTypeHandles(inHandles, out length2);
		fixed (nint* pTypeContext = array)
		{
			fixed (nint* pMethodContext = array2)
			{
				bool result = SatisfiesConstraints(paramType, pTypeContext, length, pMethodContext, length2, toType);
				GC.KeepAlive(typeContext);
				GC.KeepAlive(methodContext);
				return result;
			}
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint _GetMetadataImport(RuntimeType type);

	internal static MetadataImport GetMetadataImport(RuntimeType type)
	{
		return new MetadataImport(_GetMetadataImport(type), type);
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_RegisterCollectibleTypeDependency", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_RegisterCollectibleTypeDependency")]
	private static extern void RegisterCollectibleTypeDependency(QCallTypeHandle type, QCallAssembly assembly);

	internal static void RegisterCollectibleTypeDependency(RuntimeType type, RuntimeAssembly assembly)
	{
		RegisterCollectibleTypeDependency(new QCallTypeHandle(ref type), new QCallAssembly(ref assembly));
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2);
}
