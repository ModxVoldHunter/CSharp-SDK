using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices;

public static class RuntimeHelpers
{
	public delegate void TryCode(object? userData);

	public delegate void CleanupCode(object? userData, bool exceptionThrown);

	[Obsolete("OffsetToStringData has been deprecated. Use string.GetPinnableReference() instead.")]
	public static int OffsetToStringData
	{
		[NonVersionable]
		get
		{
			return 12;
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void* GetSpanDataFrom(RuntimeFieldHandle fldHandle, RuntimeTypeHandle targetTypeHandle, out int count);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[return: NotNullIfNotNull("obj")]
	public static extern object? GetObjectValue(object? obj);

	[DllImport("QCall", EntryPoint = "ReflectionInvocation_RunClassConstructor", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ReflectionInvocation_RunClassConstructor")]
	private static extern void RunClassConstructor(QCallTypeHandle type);

	[RequiresUnreferencedCode("Trimmer can't guarantee existence of class constructor")]
	public static void RunClassConstructor(RuntimeTypeHandle type)
	{
		RuntimeType type2 = type.GetRuntimeType();
		if ((object)type2 == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "type");
		}
		RunClassConstructor(new QCallTypeHandle(ref type2));
	}

	[DllImport("QCall", EntryPoint = "ReflectionInvocation_RunModuleConstructor", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ReflectionInvocation_RunModuleConstructor")]
	private static extern void RunModuleConstructor(QCallModule module);

	public static void RunModuleConstructor(ModuleHandle module)
	{
		RuntimeModule module2 = module.GetRuntimeModule();
		if ((object)module2 == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "module");
		}
		RunModuleConstructor(new QCallModule(ref module2));
	}

	[DllImport("QCall", EntryPoint = "ReflectionInvocation_CompileMethod", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ReflectionInvocation_CompileMethod")]
	internal static extern void CompileMethod(RuntimeMethodHandleInternal method);

	[DllImport("QCall", EntryPoint = "ReflectionInvocation_PrepareMethod", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ReflectionInvocation_PrepareMethod")]
	private unsafe static extern void PrepareMethod(RuntimeMethodHandleInternal method, nint* pInstantiation, int cInstantiation);

	public static void PrepareMethod(RuntimeMethodHandle method)
	{
		PrepareMethod(method, null);
	}

	public unsafe static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[]? instantiation)
	{
		IRuntimeMethodInfo methodInfo = method.GetMethodInfo();
		if (methodInfo == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "method");
		}
		instantiation = (RuntimeTypeHandle[])instantiation?.Clone();
		RuntimeTypeHandle[] inHandles = instantiation;
		Span<nint> stackScratch = new Span<nint>(stackalloc byte[(int)checked((nuint)8u * (nuint)8u)], 8);
		ReadOnlySpan<nint> readOnlySpan = RuntimeTypeHandle.CopyRuntimeTypeHandles(inHandles, stackScratch);
		fixed (nint* pInstantiation = readOnlySpan)
		{
			PrepareMethod(methodInfo.Value, pInstantiation, readOnlySpan.Length);
			GC.KeepAlive(instantiation);
			GC.KeepAlive(methodInfo);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void PrepareDelegate(Delegate d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetHashCode(object? o);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int TryGetHashCode(object o);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public new static extern bool Equals(object? o1, object? o2);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void EnsureSufficientExecutionStack();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool TryEnsureSufficientExecutionStack();

	public static object GetUninitializedObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		RuntimeType type2 = type as RuntimeType;
		if ((object)type2 == null)
		{
			ArgumentNullException.ThrowIfNull(type, "type");
			throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type));
		}
		object o = null;
		GetUninitializedObject(new QCallTypeHandle(ref type2), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "ReflectionSerialization_GetUninitializedObject", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ReflectionSerialization_GetUninitializedObject")]
	private static extern void GetUninitializedObject(QCallTypeHandle type, ObjectHandleOnStack retObject);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object AllocateUninitializedClone(object obj);

	[Intrinsic]
	public static bool IsReferenceOrContainsReferences<T>()
	{
		throw new InvalidOperationException();
	}

	[Intrinsic]
	internal static bool IsBitwiseEquatable<T>()
	{
		throw new InvalidOperationException();
	}

	[Intrinsic]
	internal static bool EnumEquals<T>(T x, T y) where T : struct, Enum
	{
		return x.Equals(y);
	}

	[Intrinsic]
	internal static int EnumCompareTo<T>(T x, T y) where T : struct, Enum
	{
		return x.CompareTo(y);
	}

	internal static ref byte GetRawData(this object obj)
	{
		return ref Unsafe.As<RawData>(obj).Data;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static nuint GetRawObjectDataSize(object obj)
	{
		MethodTable* methodTable = GetMethodTable(obj);
		nuint num = (nuint)methodTable->BaseSize - (nuint)16u;
		if (methodTable->HasComponentSize)
		{
			num += (nuint)((nint)Unsafe.As<RawArrayData>(obj).Length * (nint)methodTable->ComponentSize);
		}
		GC.KeepAlive(obj);
		return num;
	}

	internal unsafe static ushort GetElementSize(this Array array)
	{
		return GetMethodTable(array)->ComponentSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref int GetMultiDimensionalArrayBounds(Array array)
	{
		return ref Unsafe.As<byte, int>(ref Unsafe.As<RawArrayData>(array).Data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static int GetMultiDimensionalArrayRank(Array array)
	{
		int multiDimensionalArrayRank = GetMethodTable(array)->MultiDimensionalArrayRank;
		GC.KeepAlive(array);
		return multiDimensionalArrayRank;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool ObjectHasComponentSize(object obj)
	{
		return GetMethodTable(obj)->HasComponentSize;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern object Box(MethodTable* methodTable, ref byte data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal unsafe static MethodTable* GetMethodTable(object obj)
	{
		return (MethodTable*)Unsafe.Add(ref Unsafe.As<byte, nint>(ref obj.GetRawData()), -1);
	}

	[LibraryImport("QCall", EntryPoint = "MethodTable_AreTypesEquivalent")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool AreTypesEquivalent(MethodTable* pMTa, MethodTable* pMTb)
	{
		int num = __PInvoke(pMTa, pMTb);
		return num != 0;
		[DllImport("QCall", EntryPoint = "MethodTable_AreTypesEquivalent", ExactSpelling = true)]
		static extern unsafe int __PInvoke(MethodTable* __pMTa_native, MethodTable* __pMTb_native);
	}

	public static nint AllocateTypeAssociatedMemory(Type type, int size)
	{
		RuntimeType type2 = type as RuntimeType;
		if ((object)type2 == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "type");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(size, "size");
		return AllocateTypeAssociatedMemory(new QCallTypeHandle(ref type2), (uint)size);
	}

	[DllImport("QCall", EntryPoint = "RuntimeTypeHandle_AllocateTypeAssociatedMemory", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "RuntimeTypeHandle_AllocateTypeAssociatedMemory")]
	private static extern nint AllocateTypeAssociatedMemory(QCallTypeHandle type, uint size);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint AllocTailCallArgBuffer(int size, nint gcDesc);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern TailCallTls* GetTailCallInfo(nint retAddrSlot, nint* retAddr);

	[StackTraceHidden]
	private unsafe static void DispatchTailCalls(nint callersRetAddrSlot, delegate*<nint, ref byte, PortableTailCallFrame*, void> callTarget, ref byte retVal)
	{
		Unsafe.SkipInit(out nint num);
		TailCallTls* tailCallInfo = GetTailCallInfo(callersRetAddrSlot, &num);
		PortableTailCallFrame* frame = tailCallInfo->Frame;
		if (num == frame->TailCallAwareReturnAddress)
		{
			frame->NextCall = callTarget;
			return;
		}
		Unsafe.SkipInit(out PortableTailCallFrame portableTailCallFrame);
		portableTailCallFrame.NextCall = null;
		try
		{
			tailCallInfo->Frame = &portableTailCallFrame;
			do
			{
				callTarget(tailCallInfo->ArgBuffer, ref retVal, &portableTailCallFrame);
				callTarget = portableTailCallFrame.NextCall;
			}
			while (callTarget != (delegate*<nint, ref byte, PortableTailCallFrame*, void>)null);
		}
		finally
		{
			tailCallInfo->Frame = frame;
			if (tailCallInfo->ArgBuffer != IntPtr.Zero && *(int*)tailCallInfo->ArgBuffer == 1)
			{
				*(int*)tailCallInfo->ArgBuffer = 2;
			}
		}
	}

	public static T[] GetSubArray<T>(T[] array, Range range)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		var (elementOffset, num) = range.GetOffsetAndLength(array.Length);
		if (num == 0)
		{
			return Array.Empty<T>();
		}
		T[] array2 = new T[num];
		Buffer.Memmove(ref MemoryMarshal.GetArrayDataReference(array2), ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), elementOffset), (uint)num);
		return array2;
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData)
	{
		ArgumentNullException.ThrowIfNull(code, "code");
		ArgumentNullException.ThrowIfNull(backoutCode, "backoutCode");
		bool exceptionThrown = true;
		try
		{
			code(userData);
			exceptionThrown = false;
		}
		finally
		{
			backoutCode(userData, exceptionThrown);
		}
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareContractedDelegate(Delegate d)
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void ProbeForSufficientStack()
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareConstrainedRegions()
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareConstrainedRegionsNoOP()
	{
	}

	internal static bool IsPrimitiveType(this CorElementType et)
	{
		return ((1 << (int)et) & 0x3003FFC) != 0;
	}

	[Intrinsic]
	public unsafe static ReadOnlySpan<T> CreateSpan<T>(RuntimeFieldHandle fldHandle)
	{
		int count;
		return new ReadOnlySpan<T>(GetSpanDataFrom(fldHandle, typeof(T).TypeHandle, out count), count);
	}

	[Intrinsic]
	internal static bool IsKnownConstant(Type t)
	{
		return false;
	}

	[Intrinsic]
	internal static bool IsKnownConstant(char t)
	{
		return false;
	}

	[Intrinsic]
	internal static bool IsKnownConstant(int t)
	{
		return false;
	}
}
