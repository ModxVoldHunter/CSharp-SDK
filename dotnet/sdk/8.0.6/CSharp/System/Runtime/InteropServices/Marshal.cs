using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Security;
using System.StubHelpers;
using System.Text;

namespace System.Runtime.InteropServices;

public static class Marshal
{
	internal static Guid IID_IUnknown = new Guid(0, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	public static readonly int SystemDefaultCharSize = 2;

	public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();

	internal static bool IsBuiltInComSupported { get; } = IsBuiltInComSupportedInternal();


	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static nint OffsetOf(Type t, string fieldName)
	{
		ArgumentNullException.ThrowIfNull(t, "t");
		FieldInfo field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if ((object)field == null)
		{
			throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, t.FullName), "fieldName");
		}
		if (!(field is RtFieldInfo f))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, "fieldName");
		}
		return OffsetOfHelper(f);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint OffsetOfHelper(IRuntimeFieldInfo f);

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("ReadByte(Object, Int32) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static byte ReadByte(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, ReadByte);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("ReadInt16(Object, Int32) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static short ReadInt16(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, ReadInt16);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("ReadInt32(Object, Int32) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static int ReadInt32(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, ReadInt32);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("ReadInt64(Object, Int32) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static long ReadInt64([In][MarshalAs(UnmanagedType.AsAny)] object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, ReadInt64);
	}

	private unsafe static T ReadValueSlow<T>(object ptr, int ofs, Func<nint, int, T> readValueHelper)
	{
		if (ptr == null)
		{
			throw new AccessViolationException();
		}
		MngdNativeArrayMarshaler.MarshalerState marshalerState = default(MngdNativeArrayMarshaler.MarshalerState);
		AsAnyMarshaler asAnyMarshaler = new AsAnyMarshaler(new IntPtr(&marshalerState));
		nint num = IntPtr.Zero;
		try
		{
			num = asAnyMarshaler.ConvertToNative(ptr, 285147391);
			return readValueHelper(num, ofs);
		}
		finally
		{
			asAnyMarshaler.ClearNative(num);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteByte(Object, Int32, Byte) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static void WriteByte(object ptr, int ofs, byte val)
	{
		WriteValueSlow(ptr, ofs, val, WriteByte);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static void WriteInt16(object ptr, int ofs, short val)
	{
		WriteValueSlow(ptr, ofs, val, WriteInt16);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static void WriteInt32(object ptr, int ofs, int val)
	{
		WriteValueSlow(ptr, ofs, val, WriteInt32);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	public static void WriteInt64(object ptr, int ofs, long val)
	{
		WriteValueSlow(ptr, ofs, val, WriteInt64);
	}

	private unsafe static void WriteValueSlow<T>(object ptr, int ofs, T val, Action<nint, int, T> writeValueHelper)
	{
		if (ptr == null)
		{
			throw new AccessViolationException();
		}
		MngdNativeArrayMarshaler.MarshalerState marshalerState = default(MngdNativeArrayMarshaler.MarshalerState);
		AsAnyMarshaler asAnyMarshaler = new AsAnyMarshaler(new IntPtr(&marshalerState));
		nint num = IntPtr.Zero;
		try
		{
			num = asAnyMarshaler.ConvertToNative(ptr, 822018303);
			writeValueHelper(num, ofs, val);
			asAnyMarshaler.ConvertToManaged(ptr, num);
		}
		finally
		{
			asAnyMarshaler.ClearNative(num);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetLastPInvokeError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void SetLastPInvokeError(int error);

	private static void PrelinkCore(MethodInfo m)
	{
		if (!(m is RuntimeMethodInfo runtimeMethodInfo))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "m");
		}
		InternalPrelink(((IRuntimeMethodInfo)runtimeMethodInfo).Value);
		GC.KeepAlive(runtimeMethodInfo);
	}

	[DllImport("QCall", EntryPoint = "MarshalNative_Prelink", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "MarshalNative_Prelink")]
	private static extern void InternalPrelink(RuntimeMethodHandleInternal m);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern nint GetExceptionPointers();

	[MethodImpl(MethodImplOptions.InternalCall)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("GetExceptionCode() may be unavailable in future releases.")]
	public static extern int GetExceptionCode();

	[MethodImpl(MethodImplOptions.InternalCall)]
	[RequiresDynamicCode("Marshalling code for the object might not be available. Use the StructureToPtr<T> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static extern void StructureToPtr(object structure, nint ptr, bool fDeleteOld);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void PtrToStructureHelper(nint ptr, object structure, bool allowValueClasses);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static extern void DestroyStructure(nint ptr, Type structuretype);

	internal unsafe static bool IsPinnable(object obj)
	{
		if (obj != null)
		{
			return !RuntimeHelpers.GetMethodTable(obj)->ContainsGCPointers;
		}
		return true;
	}

	[LibraryImport("QCall", EntryPoint = "MarshalNative_IsBuiltInComSupported")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool IsBuiltInComSupportedInternal()
	{
		int num = __PInvoke();
		return num != 0;
		[DllImport("QCall", EntryPoint = "MarshalNative_IsBuiltInComSupported", ExactSpelling = true)]
		static extern int __PInvoke();
	}

	[RequiresAssemblyFiles("Windows only assigns HINSTANCE to assemblies loaded from disk. This API will return -1 for modules without a file on disk.")]
	public static nint GetHINSTANCE(Module m)
	{
		ArgumentNullException.ThrowIfNull(m, "m");
		RuntimeModule module = m as RuntimeModule;
		if ((object)module != null)
		{
			return GetHINSTANCE(new QCallModule(ref module));
		}
		return -1;
	}

	[DllImport("QCall", EntryPoint = "MarshalNative_GetHINSTANCE", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "MarshalNative_GetHINSTANCE")]
	private static extern nint GetHINSTANCE(QCallModule m);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception GetExceptionForHRInternal(int errorCode, nint errorInfo);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetHRForException(Exception? e);

	[SupportedOSPlatform("windows")]
	public static string GetTypeInfoName(ITypeInfo typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo, "typeInfo");
		typeInfo.GetDocumentation(-1, out string strName, out string _, out int _, out string _);
		return strName;
	}

	internal static Type GetTypeFromCLSID(Guid clsid, string server, bool throwOnError)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		Type o = null;
		GetTypeFromCLSID(in clsid, server, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[LibraryImport("QCall", EntryPoint = "MarshalNative_GetTypeFromCLSID", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetTypeFromCLSID(in Guid clsid, string server, ObjectHandleOnStack retType)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(server))
		{
			void* _server_native = ptr;
			fixed (Guid* _clsid_native = &clsid)
			{
				__PInvoke(_clsid_native, (ushort*)_server_native, retType);
			}
		}
		[DllImport("QCall", EntryPoint = "MarshalNative_GetTypeFromCLSID", ExactSpelling = true)]
		static extern unsafe void __PInvoke(Guid* __clsid_native, ushort* __server_native, ObjectHandleOnStack __retType_native);
	}

	[SupportedOSPlatform("windows")]
	public static nint GetIUnknownForObject(object o)
	{
		ArgumentNullException.ThrowIfNull(o, "o");
		return GetIUnknownForObjectNative(o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint GetIUnknownForObjectNative(object o);

	[SupportedOSPlatform("windows")]
	public static nint GetIDispatchForObject(object o)
	{
		ArgumentNullException.ThrowIfNull(o, "o");
		return GetIDispatchForObjectNative(o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint GetIDispatchForObjectNative(object o);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static nint GetComInterfaceForObject(object o, Type T)
	{
		ArgumentNullException.ThrowIfNull(o, "o");
		ArgumentNullException.ThrowIfNull(T, "T");
		return GetComInterfaceForObjectNative(o, T, fEnableCustomizedQueryInterface: true);
	}

	[SupportedOSPlatform("windows")]
	public static nint GetComInterfaceForObject<T, TInterface>([DisallowNull] T o)
	{
		return GetComInterfaceForObject(o, typeof(TInterface));
	}

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static nint GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
	{
		ArgumentNullException.ThrowIfNull(o, "o");
		ArgumentNullException.ThrowIfNull(T, "T");
		bool fEnableCustomizedQueryInterface = mode == CustomQueryInterfaceMode.Allow;
		return GetComInterfaceForObjectNative(o, T, fEnableCustomizedQueryInterface);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint GetComInterfaceForObjectNative(object o, Type t, bool fEnableCustomizedQueryInterface);

	[SupportedOSPlatform("windows")]
	public static object GetObjectForIUnknown(nint pUnk)
	{
		ArgumentNullException.ThrowIfNull(pUnk, "pUnk");
		return GetObjectForIUnknownNative(pUnk);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetObjectForIUnknownNative(nint pUnk);

	[SupportedOSPlatform("windows")]
	public static object GetUniqueObjectForIUnknown(nint unknown)
	{
		ArgumentNullException.ThrowIfNull(unknown, "unknown");
		return GetUniqueObjectForIUnknownNative(unknown);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetUniqueObjectForIUnknownNative(nint unknown);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern object GetTypedObjectForIUnknown(nint pUnk, Type t);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static nint CreateAggregatedObject(nint pOuter, object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return CreateAggregatedObjectNative(pOuter, o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint CreateAggregatedObjectNative(nint pOuter, object o);

	[SupportedOSPlatform("windows")]
	public static nint CreateAggregatedObject<T>(nint pOuter, T o) where T : notnull
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return CreateAggregatedObject(pOuter, (object)o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void CleanupUnusedObjectsInCurrentContext();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool AreComObjectsAvailableForCleanup();

	public static bool IsComObject(object o)
	{
		ArgumentNullException.ThrowIfNull(o, "o");
		return o is __ComObject;
	}

	[SupportedOSPlatform("windows")]
	public static int ReleaseComObject(object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (o == null)
		{
			throw new NullReferenceException();
		}
		if (!(o is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		return _ComObject.ReleaseSelf();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int InternalReleaseComObject(object o);

	[SupportedOSPlatform("windows")]
	public static int FinalReleaseComObject(object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ArgumentNullException.ThrowIfNull(o, "o");
		if (!(o is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		_ComObject.FinalReleaseSelf();
		return 0;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalFinalReleaseComObject(object o);

	[SupportedOSPlatform("windows")]
	public static object? GetComObjectData(object obj, object key)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ArgumentNullException.ThrowIfNull(obj, "obj");
		ArgumentNullException.ThrowIfNull(key, "key");
		if (!(obj is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "obj");
		}
		return _ComObject.GetData(key);
	}

	[SupportedOSPlatform("windows")]
	public static bool SetComObjectData(object obj, object key, object? data)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ArgumentNullException.ThrowIfNull(obj, "obj");
		ArgumentNullException.ThrowIfNull(key, "key");
		if (!(obj is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "obj");
		}
		return _ComObject.SetData(key, data);
	}

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[return: NotNullIfNotNull("o")]
	public static object? CreateWrapperOfType(object? o, Type t)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ArgumentNullException.ThrowIfNull(t, "t");
		if (!t.IsCOMObject)
		{
			throw new ArgumentException(SR.Argument_TypeNotComObject, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		if (o == null)
		{
			return null;
		}
		if (!o.GetType().IsCOMObject)
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		if (o.GetType() == t)
		{
			return o;
		}
		object obj = GetComObjectData(o, t);
		if (obj == null)
		{
			obj = InternalCreateWrapperOfType(o, t);
			if (!SetComObjectData(o, t, obj))
			{
				obj = GetComObjectData(o, t);
			}
		}
		return obj;
	}

	[SupportedOSPlatform("windows")]
	public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return (TWrapper)CreateWrapperOfType(o, typeof(TWrapper));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalCreateWrapperOfType(object o, Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool IsTypeVisibleFromCom(Type t);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void GetNativeVariantForObject(object? obj, nint pDstNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		GetNativeVariantForObjectNative(obj, pDstNativeVariant);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetNativeVariantForObjectNative(object obj, nint pDstNativeVariant);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void GetNativeVariantForObject<T>(T? obj, nint pDstNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		GetNativeVariantForObject((object?)obj, pDstNativeVariant);
	}

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static object? GetObjectForNativeVariant(nint pSrcNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return GetObjectForNativeVariantNative(pSrcNativeVariant);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetObjectForNativeVariantNative(nint pSrcNativeVariant);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static T? GetObjectForNativeVariant<T>(nint pSrcNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return (T)GetObjectForNativeVariant(pSrcNativeVariant);
	}

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static object?[] GetObjectsForNativeVariants(nint aSrcNativeVariant, int cVars)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return GetObjectsForNativeVariantsNative(aSrcNativeVariant, cVars);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object[] GetObjectsForNativeVariantsNative(nint aSrcNativeVariant, int cVars);

	[SupportedOSPlatform("windows")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static T[] GetObjectsForNativeVariants<T>(nint aSrcNativeVariant, int cVars)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		object[] objectsForNativeVariants = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);
		T[] array = new T[objectsForNativeVariants.Length];
		Array.Copy(objectsForNativeVariants, array, objectsForNativeVariants.Length);
		return array;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern int GetStartComSlot(Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern int GetEndComSlot(Type t);

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[SupportedOSPlatform("windows")]
	public static object BindToMoniker(string monikerName)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ThrowExceptionForHR(CreateBindCtx(0u, out var ppbc));
		try
		{
			ThrowExceptionForHR(MkParseDisplayName(ppbc, monikerName, out var _, out var ppmk));
			try
			{
				ThrowExceptionForHR(BindMoniker(ppmk, 0u, ref IID_IUnknown, out var ppvResult));
				try
				{
					return GetObjectForIUnknown(ppvResult);
				}
				finally
				{
					Release(ppvResult);
				}
			}
			finally
			{
				Release(ppmk);
			}
		}
		finally
		{
			Release(ppbc);
		}
	}

	[LibraryImport("ole32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int CreateBindCtx(uint reserved, out nint ppbc)
	{
		Unsafe.SkipInit<nint>(out ppbc);
		int result;
		fixed (nint* _ppbc_native = &ppbc)
		{
			result = __PInvoke(reserved, _ppbc_native);
		}
		return result;
		[DllImport("ole32.dll", EntryPoint = "CreateBindCtx", ExactSpelling = true)]
		static extern unsafe int __PInvoke(uint __reserved_native, nint* __ppbc_native);
	}

	[LibraryImport("ole32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int MkParseDisplayName(nint pbc, [MarshalAs(UnmanagedType.LPWStr)] string szUserName, out uint pchEaten, out nint ppmk)
	{
		Unsafe.SkipInit<uint>(out pchEaten);
		Unsafe.SkipInit<nint>(out ppmk);
		int result;
		fixed (nint* _ppmk_native = &ppmk)
		{
			fixed (uint* _pchEaten_native = &pchEaten)
			{
				fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(szUserName))
				{
					void* _szUserName_native = ptr;
					result = __PInvoke(pbc, (ushort*)_szUserName_native, _pchEaten_native, _ppmk_native);
				}
			}
		}
		return result;
		[DllImport("ole32.dll", EntryPoint = "MkParseDisplayName", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __pbc_native, ushort* __szUserName_native, uint* __pchEaten_native, nint* __ppmk_native);
	}

	[LibraryImport("ole32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int BindMoniker(nint pmk, uint grfOpt, ref Guid iidResult, out nint ppvResult)
	{
		Unsafe.SkipInit<nint>(out ppvResult);
		int result;
		fixed (nint* _ppvResult_native = &ppvResult)
		{
			fixed (Guid* _iidResult_native = &iidResult)
			{
				result = __PInvoke(pmk, grfOpt, _iidResult_native, _ppvResult_native);
			}
		}
		return result;
		[DllImport("ole32.dll", EntryPoint = "BindMoniker", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __pmk_native, uint __grfOpt_native, Guid* __iidResult_native, nint* __ppvResult_native);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern void ChangeWrapperHandleStrength(object otp, bool fIsWeak);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Delegate GetDelegateForFunctionPointerInternal(nint ptr, Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint GetFunctionPointerForDelegateInternal(Delegate d);

	public static nint AllocHGlobal(int cb)
	{
		return AllocHGlobal((nint)cb);
	}

	public unsafe static string? PtrToStringAnsi(nint ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		return new string((sbyte*)ptr);
	}

	public unsafe static string PtrToStringAnsi(nint ptr, int len)
	{
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		ArgumentOutOfRangeException.ThrowIfNegative(len, "len");
		return new string((sbyte*)ptr, 0, len);
	}

	public unsafe static string? PtrToStringUni(nint ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		return new string((char*)ptr);
	}

	public unsafe static string PtrToStringUni(nint ptr, int len)
	{
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		ArgumentOutOfRangeException.ThrowIfNegative(len, "len");
		return new string((char*)ptr, 0, len);
	}

	public unsafe static string? PtrToStringUTF8(nint ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		int byteLength = string.strlen((byte*)ptr);
		return string.CreateStringFromEncoding((byte*)ptr, byteLength, Encoding.UTF8);
	}

	public unsafe static string PtrToStringUTF8(nint ptr, int byteLen)
	{
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		ArgumentOutOfRangeException.ThrowIfNegative(byteLen, "byteLen");
		return string.CreateStringFromEncoding((byte*)ptr, byteLen, Encoding.UTF8);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static int SizeOf(object structure)
	{
		ArgumentNullException.ThrowIfNull(structure, "structure");
		return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
	}

	public static int SizeOf<T>(T structure)
	{
		ArgumentNullException.ThrowIfNull(structure, "structure");
		return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static int SizeOf(Type t)
	{
		ArgumentNullException.ThrowIfNull(t, "t");
		if (!(t is RuntimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		return SizeOfHelper(t, throwIfNotMarshalable: true);
	}

	public static int SizeOf<T>()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "T");
		}
		return SizeOfHelper(typeFromHandle, throwIfNotMarshalable: true);
	}

	public unsafe static int QueryInterface(nint pUnk, [In][RequiresLocation] ref Guid iid, out nint ppv)
	{
		ArgumentNullException.ThrowIfNull(pUnk, "pUnk");
		fixed (Guid* ptr = &iid)
		{
			fixed (nint* ptr2 = &ppv)
			{
				return ((delegate* unmanaged<nint, Guid*, nint*, int>)(*(*(IntPtr**)pUnk)))(pUnk, ptr, ptr2);
			}
		}
	}

	public unsafe static int AddRef(nint pUnk)
	{
		ArgumentNullException.ThrowIfNull(pUnk, "pUnk");
		return ((delegate* unmanaged<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)pUnk) + sizeof(void*))))(pUnk);
	}

	public unsafe static int Release(nint pUnk)
	{
		ArgumentNullException.ThrowIfNull(pUnk, "pUnk");
		return ((delegate* unmanaged<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)pUnk) + (nint)2 * (nint)sizeof(void*))))(pUnk);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public unsafe static nint UnsafeAddrOfPinnedArrayElement(Array arr, int index)
	{
		ArgumentNullException.ThrowIfNull(arr, "arr");
		void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
		return (nint)((byte*)ptr + (nuint)((nint)(uint)index * (nint)arr.GetElementSize()));
	}

	public unsafe static nint UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
	{
		ArgumentNullException.ThrowIfNull(arr, "arr");
		void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
		return (nint)((byte*)ptr + (nuint)((nint)(uint)index * (nint)Unsafe.SizeOf<T>()));
	}

	public static nint OffsetOf<T>(string fieldName)
	{
		return OffsetOf(typeof(T), fieldName);
	}

	public static void Copy(int[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(char[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(short[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(long[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(float[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(double[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(byte[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(nint[] source, int startIndex, nint destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	private unsafe static void CopyToNative<T>(T[] source, int startIndex, nint destination, int length)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(destination, "destination");
		new Span<T>(source, startIndex, length).CopyTo(new Span<T>((void*)destination, length));
	}

	public static void Copy(nint source, int[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, char[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, short[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, long[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, float[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, double[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, byte[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(nint source, nint[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	private unsafe static void CopyToManaged<T>(nint source, T[] destination, int startIndex, int length)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentOutOfRangeException.ThrowIfNegative(startIndex, "startIndex");
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		new Span<T>((void*)source, length).CopyTo(new Span<T>(destination, startIndex, length));
	}

	public unsafe static byte ReadByte(nint ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			return *ptr2;
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static byte ReadByte(nint ptr)
	{
		return ReadByte(ptr, 0);
	}

	public unsafe static short ReadInt16(nint ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 1) == 0)
			{
				return *(short*)ptr2;
			}
			return Unsafe.ReadUnaligned<short>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static short ReadInt16(nint ptr)
	{
		return ReadInt16(ptr, 0);
	}

	public unsafe static int ReadInt32(nint ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 3) == 0)
			{
				return *(int*)ptr2;
			}
			return Unsafe.ReadUnaligned<int>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static int ReadInt32(nint ptr)
	{
		return ReadInt32(ptr, 0);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("ReadIntPtr(Object, Int32) may be unavailable in future releases.")]
	public static nint ReadIntPtr(object ptr, int ofs)
	{
		return (nint)ReadInt64(ptr, ofs);
	}

	public static nint ReadIntPtr(nint ptr, int ofs)
	{
		return (nint)ReadInt64(ptr, ofs);
	}

	public static nint ReadIntPtr(nint ptr)
	{
		return ReadIntPtr(ptr, 0);
	}

	public unsafe static long ReadInt64(nint ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 7) == 0)
			{
				return *(long*)ptr2;
			}
			return Unsafe.ReadUnaligned<long>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static long ReadInt64(nint ptr)
	{
		return ReadInt64(ptr, 0);
	}

	public unsafe static void WriteByte(nint ptr, int ofs, byte val)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			*ptr2 = val;
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteByte(nint ptr, byte val)
	{
		WriteByte(ptr, 0, val);
	}

	public unsafe static void WriteInt16(nint ptr, int ofs, short val)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 1) == 0)
			{
				*(short*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt16(nint ptr, short val)
	{
		WriteInt16(ptr, 0, val);
	}

	public static void WriteInt16(nint ptr, int ofs, char val)
	{
		WriteInt16(ptr, ofs, (short)val);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteInt16(Object, Int32, Char) may be unavailable in future releases.")]
	public static void WriteInt16([In][Out] object ptr, int ofs, char val)
	{
		WriteInt16(ptr, ofs, (short)val);
	}

	public static void WriteInt16(nint ptr, char val)
	{
		WriteInt16(ptr, 0, (short)val);
	}

	public unsafe static void WriteInt32(nint ptr, int ofs, int val)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 3) == 0)
			{
				*(int*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt32(nint ptr, int val)
	{
		WriteInt32(ptr, 0, val);
	}

	public static void WriteIntPtr(nint ptr, int ofs, nint val)
	{
		WriteInt64(ptr, ofs, val);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("WriteIntPtr(Object, Int32, IntPtr) may be unavailable in future releases.")]
	public static void WriteIntPtr(object ptr, int ofs, nint val)
	{
		WriteInt64(ptr, ofs, val);
	}

	public static void WriteIntPtr(nint ptr, nint val)
	{
		WriteIntPtr(ptr, 0, val);
	}

	public unsafe static void WriteInt64(nint ptr, int ofs, long val)
	{
		try
		{
			byte* ptr2 = (byte*)(ptr + ofs);
			if (((int)ptr2 & 7) == 0)
			{
				*(long*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt64(nint ptr, long val)
	{
		WriteInt64(ptr, 0, val);
	}

	public static void Prelink(MethodInfo m)
	{
		ArgumentNullException.ThrowIfNull(m, "m");
		PrelinkCore(m);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "This only needs to prelink methods that are actually used")]
	public static void PrelinkAll(Type c)
	{
		ArgumentNullException.ThrowIfNull(c, "c");
		MethodInfo[] methods = c.GetMethods();
		for (int i = 0; i < methods.Length; i++)
		{
			Prelink(methods[i]);
		}
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "AOT compilers can see the T.")]
	public static void StructureToPtr<T>([DisallowNull] T structure, nint ptr, bool fDeleteOld)
	{
		StructureToPtr((object)structure, ptr, fDeleteOld);
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static object? PtrToStructure(nint ptr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type structureType)
	{
		ArgumentNullException.ThrowIfNull(structureType, "structureType");
		if (ptr == IntPtr.Zero)
		{
			return null;
		}
		if (structureType.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "structureType");
		}
		if (!(structureType is RuntimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "structureType");
		}
		object obj = Activator.CreateInstance(structureType, nonPublic: true);
		PtrToStructureHelper(ptr, obj, allowValueClasses: true);
		return obj;
	}

	[RequiresDynamicCode("Marshalling code for the object might not be available")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void PtrToStructure(nint ptr, object structure)
	{
		PtrToStructureHelper(ptr, structure, allowValueClasses: false);
	}

	public static void PtrToStructure<T>(nint ptr, [DisallowNull] T structure)
	{
		PtrToStructureHelper(ptr, structure, allowValueClasses: false);
	}

	public static T? PtrToStructure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(nint ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			return (T)(object)null;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "T");
		}
		object obj = Activator.CreateInstance(typeFromHandle, nonPublic: true);
		PtrToStructureHelper(ptr, obj, allowValueClasses: true);
		return (T)obj;
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "AOT compilers can see the T.")]
	public static void DestroyStructure<T>(nint ptr)
	{
		DestroyStructure(ptr, typeof(T));
	}

	public static Exception? GetExceptionForHR(int errorCode)
	{
		return GetExceptionForHR(errorCode, IntPtr.Zero);
	}

	public static Exception? GetExceptionForHR(int errorCode, nint errorInfo)
	{
		if (errorCode >= 0)
		{
			return null;
		}
		return GetExceptionForHRInternal(errorCode, errorInfo);
	}

	public static void ThrowExceptionForHR(int errorCode)
	{
		if (errorCode < 0)
		{
			throw GetExceptionForHR(errorCode);
		}
	}

	public static void ThrowExceptionForHR(int errorCode, nint errorInfo)
	{
		if (errorCode < 0)
		{
			throw GetExceptionForHR(errorCode, errorInfo);
		}
	}

	public static nint SecureStringToBSTR(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToBSTR();
	}

	public static nint SecureStringToCoTaskMemAnsi(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: false, unicode: false);
	}

	public static nint SecureStringToCoTaskMemUnicode(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: false, unicode: true);
	}

	public static nint SecureStringToGlobalAllocAnsi(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: true, unicode: false);
	}

	public static nint SecureStringToGlobalAllocUnicode(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: true, unicode: true);
	}

	public unsafe static nint StringToHGlobalAnsi(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		long num = (long)(s.Length + 1) * (long)SystemMaxDBCSCharSize;
		int num2 = (int)num;
		if (num2 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		nint num3 = AllocHGlobal((nint)num2);
		StringToAnsiString(s, (byte*)num3, num2);
		return num3;
	}

	public unsafe static nint StringToHGlobalUni(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int num = (s.Length + 1) * 2;
		if (num < s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		nint num2 = AllocHGlobal((nint)num);
		s.CopyTo(new Span<char>((void*)num2, s.Length));
		*(short*)(num2 + (nint)s.Length * (nint)2) = 0;
		return num2;
	}

	public unsafe static nint StringToCoTaskMemUni(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int num = (s.Length + 1) * 2;
		if (num < s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		nint num2 = AllocCoTaskMem(num);
		s.CopyTo(new Span<char>((void*)num2, s.Length));
		*(short*)(num2 + (nint)s.Length * (nint)2) = 0;
		return num2;
	}

	public unsafe static nint StringToCoTaskMemUTF8(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int maxByteCount = Encoding.UTF8.GetMaxByteCount(s.Length);
		nint num = AllocCoTaskMem(checked(maxByteCount + 1));
		byte* ptr = (byte*)num;
		int bytes = Encoding.UTF8.GetBytes(s, new Span<byte>(ptr, maxByteCount));
		ptr[bytes] = 0;
		return num;
	}

	public unsafe static nint StringToCoTaskMemAnsi(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		long num = (long)(s.Length + 1) * (long)SystemMaxDBCSCharSize;
		int num2 = (int)num;
		if (num2 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		nint num3 = AllocCoTaskMem(num2);
		StringToAnsiString(s, (byte*)num3, num2);
		return num3;
	}

	public static Guid GenerateGuidForType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if (!(type is RuntimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		return type.GUID;
	}

	public static string? GenerateProgIdForType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if (type.IsImport)
		{
			throw new ArgumentException(SR.Argument_TypeMustNotBeComImport, "type");
		}
		if (type.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		ProgIdAttribute customAttribute = type.GetCustomAttribute<ProgIdAttribute>();
		if (customAttribute != null)
		{
			return customAttribute.Value ?? string.Empty;
		}
		return type.FullName;
	}

	[RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetDelegateForFunctionPointer<TDelegate> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Delegate GetDelegateForFunctionPointer(nint ptr, Type t)
	{
		ArgumentNullException.ThrowIfNull(t, "t");
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		if (!(t is RuntimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		if (t.BaseType != typeof(MulticastDelegate) && t != typeof(MulticastDelegate))
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "t");
		}
		return GetDelegateForFunctionPointerInternal(ptr, t);
	}

	public static TDelegate GetDelegateForFunctionPointer<TDelegate>(nint ptr)
	{
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		Type typeFromHandle = typeof(TDelegate);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "TDelegate");
		}
		if (typeFromHandle.BaseType != typeof(MulticastDelegate) && typeFromHandle != typeof(MulticastDelegate))
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "TDelegate");
		}
		return (TDelegate)(object)GetDelegateForFunctionPointerInternal(ptr, typeFromHandle);
	}

	[RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetFunctionPointerForDelegate<TDelegate> overload instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static nint GetFunctionPointerForDelegate(Delegate d)
	{
		ArgumentNullException.ThrowIfNull(d, "d");
		return GetFunctionPointerForDelegateInternal(d);
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi", Justification = "AOT compilers can see the T.")]
	public static nint GetFunctionPointerForDelegate<TDelegate>(TDelegate d) where TDelegate : notnull
	{
		return GetFunctionPointerForDelegate((Delegate)(object)d);
	}

	public static int GetHRForLastWin32Error()
	{
		int lastPInvokeError = GetLastPInvokeError();
		if ((lastPInvokeError & 0x80000000u) == 2147483648u)
		{
			return lastPInvokeError;
		}
		return (lastPInvokeError & 0xFFFF) | -2147024896;
	}

	public unsafe static void ZeroFreeBSTR(nint s)
	{
		if (s != IntPtr.Zero)
		{
			NativeMemory.Clear((void*)s, SysStringByteLen(s));
			FreeBSTR(s);
		}
	}

	public static void ZeroFreeCoTaskMemAnsi(nint s)
	{
		ZeroFreeCoTaskMemUTF8(s);
	}

	public unsafe static void ZeroFreeCoTaskMemUnicode(nint s)
	{
		if (s != IntPtr.Zero)
		{
			NativeMemory.Clear((void*)s, (nuint)string.wcslen((char*)s) * (nuint)2u);
			FreeCoTaskMem(s);
		}
	}

	public unsafe static void ZeroFreeCoTaskMemUTF8(nint s)
	{
		if (s != IntPtr.Zero)
		{
			NativeMemory.Clear((void*)s, (nuint)string.strlen((byte*)s));
			FreeCoTaskMem(s);
		}
	}

	public unsafe static void ZeroFreeGlobalAllocAnsi(nint s)
	{
		if (s != IntPtr.Zero)
		{
			NativeMemory.Clear((void*)s, (nuint)string.strlen((byte*)s));
			FreeHGlobal(s);
		}
	}

	public unsafe static void ZeroFreeGlobalAllocUnicode(nint s)
	{
		if (s != IntPtr.Zero)
		{
			NativeMemory.Clear((void*)s, (nuint)string.wcslen((char*)s) * (nuint)2u);
			FreeHGlobal(s);
		}
	}

	public unsafe static nint StringToBSTR(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		nint num = AllocBSTR(s.Length);
		s.CopyTo(new Span<char>((void*)num, s.Length));
		return num;
	}

	public static string PtrToStringBSTR(nint ptr)
	{
		ArgumentNullException.ThrowIfNull(ptr, "ptr");
		return PtrToStringUni(ptr, (int)(SysStringByteLen(ptr) / 2));
	}

	internal unsafe static uint SysStringByteLen(nint s)
	{
		return *(uint*)(s - 4);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid)
	{
		return GetTypeFromCLSID(clsid, null, throwOnError: false);
	}

	public static void InitHandle(SafeHandle safeHandle, nint handle)
	{
		safeHandle.SetHandle(handle);
	}

	public static int GetLastWin32Error()
	{
		return GetLastPInvokeError();
	}

	public static string GetLastPInvokeErrorMessage()
	{
		return GetPInvokeErrorMessage(GetLastPInvokeError());
	}

	public static string? PtrToStringAuto(nint ptr, int len)
	{
		return PtrToStringUni(ptr, len);
	}

	public static string? PtrToStringAuto(nint ptr)
	{
		return PtrToStringUni(ptr);
	}

	public static nint StringToHGlobalAuto(string? s)
	{
		return StringToHGlobalUni(s);
	}

	public static nint StringToCoTaskMemAuto(string? s)
	{
		return StringToCoTaskMemUni(s);
	}

	private unsafe static int GetSystemMaxDBCSCharSize()
	{
		Interop.Kernel32.CPINFO cPINFO = default(Interop.Kernel32.CPINFO);
		if (Interop.Kernel32.GetCPInfo(0u, &cPINFO) == Interop.BOOL.FALSE)
		{
			return 2;
		}
		return cPINFO.MaxCharSize;
	}

	private static bool IsNullOrWin32Atom(nint ptr)
	{
		long num = ptr;
		return (num & -65536) == 0;
	}

	internal unsafe static int StringToAnsiString(string s, byte* buffer, int bufferLength, bool bestFit = false, bool throwOnUnmappableChar = false)
	{
		uint dwFlags = ((!bestFit) ? 1024u : 0u);
		Interop.BOOL bOOL = Interop.BOOL.FALSE;
		int num;
		fixed (char* lpWideCharStr = s)
		{
			num = Interop.Kernel32.WideCharToMultiByte(0u, dwFlags, lpWideCharStr, s.Length, buffer, bufferLength, null, throwOnUnmappableChar ? (&bOOL) : null);
		}
		if (bOOL != 0)
		{
			throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
		}
		buffer[num] = 0;
		return num;
	}

	internal unsafe static int GetAnsiStringByteCount(ReadOnlySpan<char> chars)
	{
		int num;
		if (chars.Length == 0)
		{
			num = 0;
		}
		else
		{
			fixed (char* lpWideCharStr = chars)
			{
				num = Interop.Kernel32.WideCharToMultiByte(0u, 1024u, lpWideCharStr, chars.Length, null, 0, null, null);
				if (num <= 0)
				{
					throw new ArgumentException();
				}
			}
		}
		return checked(num + 1);
	}

	internal unsafe static void GetAnsiStringBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		int num;
		if (chars.Length == 0)
		{
			num = 0;
		}
		else
		{
			fixed (char* lpWideCharStr = chars)
			{
				fixed (byte* lpMultiByteStr = bytes)
				{
					num = Interop.Kernel32.WideCharToMultiByte(0u, 1024u, lpWideCharStr, chars.Length, lpMultiByteStr, bytes.Length, null, null);
					if (num <= 0)
					{
						throw new ArgumentException();
					}
				}
			}
		}
		bytes[num] = 0;
	}

	public unsafe static nint AllocHGlobal(nint cb)
	{
		void* ptr = Interop.Kernel32.LocalAlloc((nuint)cb);
		if (ptr == null)
		{
			throw new OutOfMemoryException();
		}
		return (nint)ptr;
	}

	public unsafe static void FreeHGlobal(nint hglobal)
	{
		if (!IsNullOrWin32Atom(hglobal))
		{
			Interop.Kernel32.LocalFree((void*)hglobal);
		}
	}

	public unsafe static nint ReAllocHGlobal(nint pv, nint cb)
	{
		if (pv == IntPtr.Zero)
		{
			return AllocHGlobal(cb);
		}
		void* ptr = Interop.Kernel32.LocalReAlloc((void*)pv, (nuint)cb);
		if (ptr == null)
		{
			throw new OutOfMemoryException();
		}
		return (nint)ptr;
	}

	public static nint AllocCoTaskMem(int cb)
	{
		nint num = Interop.Ole32.CoTaskMemAlloc((uint)cb);
		if (num == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	public static void FreeCoTaskMem(nint ptr)
	{
		if (!IsNullOrWin32Atom(ptr))
		{
			Interop.Ole32.CoTaskMemFree(ptr);
		}
	}

	public static nint ReAllocCoTaskMem(nint pv, int cb)
	{
		nint num = Interop.Ole32.CoTaskMemRealloc(pv, (uint)cb);
		if (num == IntPtr.Zero && cb != 0)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	internal static nint AllocBSTR(int length)
	{
		nint num = Interop.OleAut32.SysAllocStringLen(IntPtr.Zero, (uint)length);
		if (num == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	internal static nint AllocBSTRByteLen(uint length)
	{
		nint num = Interop.OleAut32.SysAllocStringByteLen(null, length);
		if (num == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	public static void FreeBSTR(nint ptr)
	{
		if (!IsNullOrWin32Atom(ptr))
		{
			Interop.OleAut32.SysFreeString(ptr);
		}
	}

	internal static Type GetTypeFromProgID(string progID, string server, bool throwOnError)
	{
		ArgumentNullException.ThrowIfNull(progID, "progID");
		Guid lpclsid;
		int num = Interop.Ole32.CLSIDFromProgID(progID, out lpclsid);
		if (num < 0)
		{
			if (throwOnError)
			{
				throw GetExceptionForHR(num, new IntPtr(-1));
			}
			return null;
		}
		return GetTypeFromCLSID(lpclsid, server, throwOnError);
	}

	public static int GetLastSystemError()
	{
		return Interop.Kernel32.GetLastError();
	}

	public static void SetLastSystemError(int error)
	{
		Interop.Kernel32.SetLastError(error);
	}

	public static string GetPInvokeErrorMessage(int error)
	{
		return Interop.Kernel32.GetMessage(error);
	}
}
