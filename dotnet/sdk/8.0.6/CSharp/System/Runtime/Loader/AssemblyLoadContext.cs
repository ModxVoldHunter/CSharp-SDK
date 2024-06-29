using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace System.Runtime.Loader;

public class AssemblyLoadContext
{
	private enum InternalState
	{
		Alive,
		Unloading
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct ContextualReflectionScope : IDisposable
	{
		private readonly AssemblyLoadContext _activated;

		private readonly AssemblyLoadContext _predecessor;

		private readonly bool _initialized;

		internal ContextualReflectionScope(AssemblyLoadContext activating)
		{
			_predecessor = CurrentContextualReflectionContext;
			SetCurrentContextualReflectionContext(activating);
			_activated = activating;
			_initialized = true;
		}

		public void Dispose()
		{
			if (_initialized)
			{
				SetCurrentContextualReflectionContext(_predecessor);
			}
		}
	}

	private const string AssemblyLoadName = "AssemblyLoad";

	private static volatile Dictionary<long, WeakReference<AssemblyLoadContext>> s_allContexts;

	private static long s_nextId;

	private readonly nint _nativeAssemblyLoadContext;

	private readonly object _unloadLock;

	private readonly string _name;

	private readonly long _id;

	private InternalState _state;

	private readonly bool _isCollectible;

	private static AsyncLocal<AssemblyLoadContext> s_asyncLocalCurrent;

	[MemberNotNull("s_allContexts")]
	private static Dictionary<long, WeakReference<AssemblyLoadContext>> AllContexts
	{
		[MemberNotNull("s_allContexts")]
		get
		{
			return s_allContexts ?? Interlocked.CompareExchange(ref s_allContexts, new Dictionary<long, WeakReference<AssemblyLoadContext>>(), null) ?? s_allContexts;
		}
	}

	public IEnumerable<Assembly> Assemblies
	{
		get
		{
			Assembly[] loadedAssemblies = GetLoadedAssemblies();
			foreach (Assembly assembly in loadedAssemblies)
			{
				AssemblyLoadContext loadContext = GetLoadContext(assembly);
				if (loadContext == this)
				{
					yield return assembly;
				}
			}
		}
	}

	public static AssemblyLoadContext Default => DefaultAssemblyLoadContext.s_loadContext;

	public bool IsCollectible => _isCollectible;

	public string? Name => _name;

	public static IEnumerable<AssemblyLoadContext> All
	{
		get
		{
			_ = Default;
			Dictionary<long, WeakReference<AssemblyLoadContext>> dictionary = s_allContexts;
			WeakReference<AssemblyLoadContext>[] array;
			lock (dictionary)
			{
				array = new WeakReference<AssemblyLoadContext>[dictionary.Count];
				int num = 0;
				foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> item in dictionary)
				{
					array[num++] = item.Value;
				}
			}
			WeakReference<AssemblyLoadContext>[] array2 = array;
			foreach (WeakReference<AssemblyLoadContext> weakReference in array2)
			{
				if (weakReference.TryGetTarget(out var target))
				{
					yield return target;
				}
			}
		}
	}

	public static AssemblyLoadContext? CurrentContextualReflectionContext => s_asyncLocalCurrent?.Value;

	private event Func<Assembly, string, nint>? _resolvingUnmanagedDll;

	private event Func<AssemblyLoadContext, AssemblyName, Assembly>? _resolving;

	private event Action<AssemblyLoadContext>? _unloading;

	public event Func<Assembly, string, nint>? ResolvingUnmanagedDll
	{
		add
		{
			_resolvingUnmanagedDll += value;
		}
		remove
		{
			_resolvingUnmanagedDll -= value;
		}
	}

	public event Func<AssemblyLoadContext, AssemblyName, Assembly?>? Resolving
	{
		add
		{
			_resolving += value;
		}
		remove
		{
			_resolving -= value;
		}
	}

	public event Action<AssemblyLoadContext>? Unloading
	{
		add
		{
			_unloading += value;
		}
		remove
		{
			_unloading -= value;
		}
	}

	internal static event AssemblyLoadEventHandler? AssemblyLoad;

	internal static event ResolveEventHandler? TypeResolve;

	internal static event ResolveEventHandler? ResourceResolve;

	internal static event ResolveEventHandler? AssemblyResolve;

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_InitializeAssemblyLoadContext")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private static nint InitializeAssemblyLoadContext(nint ptrAssemblyLoadContext, [MarshalAs(UnmanagedType.Bool)] bool fRepresentsTPALoadContext, [MarshalAs(UnmanagedType.Bool)] bool isCollectible)
	{
		int _isCollectible_native = (isCollectible ? 1 : 0);
		int _fRepresentsTPALoadContext_native = (fRepresentsTPALoadContext ? 1 : 0);
		return __PInvoke(ptrAssemblyLoadContext, _fRepresentsTPALoadContext_native, _isCollectible_native);
		[DllImport("QCall", EntryPoint = "AssemblyNative_InitializeAssemblyLoadContext", ExactSpelling = true)]
		static extern nint __PInvoke(nint __ptrAssemblyLoadContext_native, int __fRepresentsTPALoadContext_native, int __isCollectible_native);
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_PrepareForAssemblyLoadContextRelease", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_PrepareForAssemblyLoadContextRelease")]
	private static extern void PrepareForAssemblyLoadContextRelease(nint ptrNativeAssemblyBinder, nint ptrAssemblyLoadContextStrong);

	[DllImport("QCall", EntryPoint = "AssemblyNative_LoadFromStream", ExactSpelling = true)]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_LoadFromStream")]
	private static extern void LoadFromStream(nint ptrNativeAssemblyBinder, nint ptrAssemblyArray, int iAssemblyArrayLen, nint ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);

	[LibraryImport("QCall", EntryPoint = "MultiCoreJIT_InternalSetProfileRoot", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static void InternalSetProfileRoot(string directoryPath)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(directoryPath))
		{
			void* _directoryPath_native = ptr;
			__PInvoke((ushort*)_directoryPath_native);
		}
		[DllImport("QCall", EntryPoint = "MultiCoreJIT_InternalSetProfileRoot", ExactSpelling = true)]
		static extern unsafe void __PInvoke(ushort* __directoryPath_native);
	}

	[LibraryImport("QCall", EntryPoint = "MultiCoreJIT_InternalStartProfile", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static void InternalStartProfile(string profile, nint ptrNativeAssemblyBinder)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(profile))
		{
			void* _profile_native = ptr;
			__PInvoke((ushort*)_profile_native, ptrNativeAssemblyBinder);
		}
		[DllImport("QCall", EntryPoint = "MultiCoreJIT_InternalStartProfile", ExactSpelling = true)]
		static extern unsafe void __PInvoke(ushort* __profile_native, nint __ptrNativeAssemblyBinder_native);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_LoadFromPath", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void LoadFromPath(nint ptrNativeAssemblyBinder, string ilPath, string niPath, ObjectHandleOnStack retAssembly)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(niPath))
		{
			void* _niPath_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(ilPath))
			{
				void* _ilPath_native = ptr2;
				__PInvoke(ptrNativeAssemblyBinder, (ushort*)_ilPath_native, (ushort*)_niPath_native, retAssembly);
			}
		}
		[DllImport("QCall", EntryPoint = "AssemblyNative_LoadFromPath", ExactSpelling = true)]
		static extern unsafe void __PInvoke(nint __ptrNativeAssemblyBinder_native, ushort* __ilPath_native, ushort* __niPath_native, ObjectHandleOnStack __retAssembly_native);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Assembly[] GetLoadedAssemblies();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsTracingEnabled();

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_TraceResolvingHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool TraceResolvingHandlerInvoked(string assemblyName, string handlerName, string alcName, string resultAssemblyName, string resultAssemblyPath)
	{
		int num;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(resultAssemblyPath))
		{
			void* _resultAssemblyPath_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(resultAssemblyName))
			{
				void* _resultAssemblyName_native = ptr2;
				fixed (char* ptr3 = &Utf16StringMarshaller.GetPinnableReference(alcName))
				{
					void* _alcName_native = ptr3;
					fixed (char* ptr4 = &Utf16StringMarshaller.GetPinnableReference(handlerName))
					{
						void* _handlerName_native = ptr4;
						fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(assemblyName))
						{
							void* _assemblyName_native = ptr5;
							num = __PInvoke((ushort*)_assemblyName_native, (ushort*)_handlerName_native, (ushort*)_alcName_native, (ushort*)_resultAssemblyName_native, (ushort*)_resultAssemblyPath_native);
						}
					}
				}
			}
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_TraceResolvingHandlerInvoked", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ushort* __assemblyName_native, ushort* __handlerName_native, ushort* __alcName_native, ushort* __resultAssemblyName_native, ushort* __resultAssemblyPath_native);
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_TraceAssemblyResolveHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool TraceAssemblyResolveHandlerInvoked(string assemblyName, string handlerName, string resultAssemblyName, string resultAssemblyPath)
	{
		int num;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(resultAssemblyPath))
		{
			void* _resultAssemblyPath_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(resultAssemblyName))
			{
				void* _resultAssemblyName_native = ptr2;
				fixed (char* ptr3 = &Utf16StringMarshaller.GetPinnableReference(handlerName))
				{
					void* _handlerName_native = ptr3;
					fixed (char* ptr4 = &Utf16StringMarshaller.GetPinnableReference(assemblyName))
					{
						void* _assemblyName_native = ptr4;
						num = __PInvoke((ushort*)_assemblyName_native, (ushort*)_handlerName_native, (ushort*)_resultAssemblyName_native, (ushort*)_resultAssemblyPath_native);
					}
				}
			}
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_TraceAssemblyResolveHandlerInvoked", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ushort* __assemblyName_native, ushort* __handlerName_native, ushort* __resultAssemblyName_native, ushort* __resultAssemblyPath_native);
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool TraceAssemblyLoadFromResolveHandlerInvoked(string assemblyName, [MarshalAs(UnmanagedType.Bool)] bool isTrackedAssembly, string requestingAssemblyPath, string requestedAssemblyPath)
	{
		int _isTrackedAssembly_native = (isTrackedAssembly ? 1 : 0);
		int num;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(requestedAssemblyPath))
		{
			void* _requestedAssemblyPath_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(requestingAssemblyPath))
			{
				void* _requestingAssemblyPath_native = ptr2;
				fixed (char* ptr3 = &Utf16StringMarshaller.GetPinnableReference(assemblyName))
				{
					void* _assemblyName_native = ptr3;
					num = __PInvoke((ushort*)_assemblyName_native, _isTrackedAssembly_native, (ushort*)_requestingAssemblyPath_native, (ushort*)_requestedAssemblyPath_native);
				}
			}
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ushort* __assemblyName_native, int __isTrackedAssembly_native, ushort* __requestingAssemblyPath_native, ushort* __requestedAssemblyPath_native);
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_TraceSatelliteSubdirectoryPathProbed", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool TraceSatelliteSubdirectoryPathProbed(string filePath, int hResult)
	{
		int num;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(filePath))
		{
			void* _filePath_native = ptr;
			num = __PInvoke((ushort*)_filePath_native, hResult);
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_TraceSatelliteSubdirectoryPathProbed", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ushort* __filePath_native, int __hResult_native);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	private RuntimeAssembly InternalLoadFromPath(string assemblyPath, string nativeImagePath)
	{
		RuntimeAssembly o = null;
		LoadFromPath(_nativeAssemblyLoadContext, assemblyPath, nativeImagePath, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	internal unsafe Assembly InternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols)
	{
		RuntimeAssembly o = null;
		fixed (byte* value = arrAssembly)
		{
			fixed (byte* value2 = arrSymbols)
			{
				LoadFromStream(_nativeAssemblyLoadContext, new IntPtr(value), arrAssembly.Length, new IntPtr(value2), arrSymbols.Length, ObjectHandleOnStack.Create(ref o));
			}
		}
		return o;
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_LoadFromInMemoryModule", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_LoadFromInMemoryModule")]
	private static extern nint LoadFromInMemoryModuleInternal(nint ptrNativeAssemblyBinder, nint hModule, ObjectHandleOnStack retAssembly);

	internal Assembly LoadFromInMemoryModule(nint moduleHandle)
	{
		ArgumentNullException.ThrowIfNull(moduleHandle, "moduleHandle");
		lock (_unloadLock)
		{
			VerifyIsAlive();
			RuntimeAssembly o = null;
			LoadFromInMemoryModuleInternal(_nativeAssemblyLoadContext, moduleHandle, ObjectHandleOnStack.Create(ref o));
			return o;
		}
	}

	private static Assembly ResolveSatelliteAssembly(nint gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveSatelliteAssembly(assemblyName);
	}

	private static nint ResolveUnmanagedDll(string unmanagedDllName, nint gchManagedAssemblyLoadContext)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.LoadUnmanagedDll(unmanagedDllName);
	}

	private static nint ResolveUnmanagedDllUsingEvent(string unmanagedDllName, Assembly assembly, nint gchManagedAssemblyLoadContext)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.GetResolvedUnmanagedDll(assembly, unmanagedDllName);
	}

	private static Assembly ResolveUsingResolvingEvent(nint gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveUsingEvent(assemblyName);
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetLoadContextForAssembly", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetLoadContextForAssembly")]
	private static extern nint GetLoadContextForAssembly(QCallAssembly assembly);

	public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly);
		AssemblyLoadContext result = null;
		if (runtimeAssembly != null)
		{
			RuntimeAssembly assembly2 = runtimeAssembly;
			nint loadContextForAssembly = GetLoadContextForAssembly(new QCallAssembly(ref assembly2));
			result = ((loadContextForAssembly != IntPtr.Zero) ? ((AssemblyLoadContext)GCHandle.FromIntPtr(loadContextForAssembly).Target) : Default);
		}
		return result;
	}

	public void SetProfileOptimizationRoot(string directoryPath)
	{
		InternalSetProfileRoot(directoryPath);
	}

	public void StartProfileOptimization(string? profile)
	{
		InternalStartProfile(profile, _nativeAssemblyLoadContext);
	}

	private static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
	{
		if (!(asm == null))
		{
			if (!(asm is RuntimeAssembly result))
			{
				if (!(asm is RuntimeAssemblyBuilder runtimeAssemblyBuilder))
				{
					return null;
				}
				return runtimeAssemblyBuilder.InternalAssembly;
			}
			return result;
		}
		return null;
	}

	private static void StartAssemblyLoad(ref Guid activityId, ref Guid relatedActivityId)
	{
		ActivityTracker.Instance.Enable();
		ActivityTracker.Instance.OnStart(NativeRuntimeEventSource.Log.Name, "AssemblyLoad", 0, ref activityId, ref relatedActivityId, EventActivityOptions.Recursive, useTplSource: false);
	}

	private static void StopAssemblyLoad(ref Guid activityId)
	{
		ActivityTracker.Instance.OnStop(NativeRuntimeEventSource.Log.Name, "AssemblyLoad", 0, ref activityId, useTplSource: false);
	}

	private static void InitializeDefaultContext()
	{
		_ = Default;
	}

	protected AssemblyLoadContext()
		: this(representsTPALoadContext: false, isCollectible: false, null)
	{
	}

	protected AssemblyLoadContext(bool isCollectible)
		: this(representsTPALoadContext: false, isCollectible, null)
	{
	}

	public AssemblyLoadContext(string? name, bool isCollectible = false)
		: this(representsTPALoadContext: false, isCollectible, name)
	{
	}

	private protected AssemblyLoadContext(bool representsTPALoadContext, bool isCollectible, string name)
	{
		_isCollectible = isCollectible;
		_name = name;
		_unloadLock = new object();
		if (!isCollectible)
		{
			GC.SuppressFinalize(this);
		}
		_nativeAssemblyLoadContext = InitializeAssemblyLoadContext(GCHandle.ToIntPtr(GCHandle.Alloc(this, IsCollectible ? GCHandleType.WeakTrackResurrection : GCHandleType.Normal)), representsTPALoadContext, isCollectible);
		Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
		lock (allContexts)
		{
			_id = s_nextId++;
			allContexts.Add(_id, new WeakReference<AssemblyLoadContext>(this, trackResurrection: true));
		}
	}

	~AssemblyLoadContext()
	{
		if (_unloadLock != null)
		{
			InitiateUnload();
		}
	}

	private void RaiseUnloadEvent()
	{
		Interlocked.Exchange(ref this._unloading, null)?.Invoke(this);
	}

	private void InitiateUnload()
	{
		RaiseUnloadEvent();
		InternalState state;
		lock (_unloadLock)
		{
			state = _state;
			if (state == InternalState.Alive)
			{
				GCHandle value = GCHandle.Alloc(this, GCHandleType.Normal);
				nint ptrAssemblyLoadContextStrong = GCHandle.ToIntPtr(value);
				PrepareForAssemblyLoadContextRelease(_nativeAssemblyLoadContext, ptrAssemblyLoadContextStrong);
				_state = InternalState.Unloading;
			}
		}
		if (state == InternalState.Alive)
		{
			Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
			lock (allContexts)
			{
				allContexts.Remove(_id);
			}
		}
	}

	public override string ToString()
	{
		return $"\"{Name}\" {GetType()} #{_id}";
	}

	public static AssemblyName GetAssemblyName(string assemblyPath)
	{
		ArgumentNullException.ThrowIfNull(assemblyPath, "assemblyPath");
		return AssemblyName.GetAssemblyName(assemblyPath);
	}

	protected virtual Assembly? Load(AssemblyName assemblyName)
	{
		return null;
	}

	public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
	{
		ArgumentNullException.ThrowIfNull(assemblyName, "assemblyName");
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, this);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromAssemblyPath(string assemblyPath)
	{
		ArgumentNullException.ThrowIfNull(assemblyPath, "assemblyPath");
		if (PathInternal.IsPartiallyQualified(assemblyPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), "assemblyPath");
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoadFromPath(assemblyPath, null);
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromNativeImagePath(string nativeImagePath, string? assemblyPath)
	{
		ArgumentNullException.ThrowIfNull(nativeImagePath, "nativeImagePath");
		if (PathInternal.IsPartiallyQualified(nativeImagePath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, nativeImagePath), "nativeImagePath");
		}
		if (assemblyPath != null && PathInternal.IsPartiallyQualified(assemblyPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), "assemblyPath");
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoadFromPath(assemblyPath, nativeImagePath);
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromStream(Stream assembly)
	{
		return LoadFromStream(assembly, null);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromStream(Stream assembly, Stream? assemblySymbols)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		ReadOnlySpan<byte> arrAssembly = ReadAllBytes(assembly);
		if (arrAssembly.IsEmpty)
		{
			throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
		}
		ReadOnlySpan<byte> arrSymbols = default(ReadOnlySpan<byte>);
		if (assemblySymbols != null)
		{
			arrSymbols = ReadAllBytes(assemblySymbols);
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoad(arrAssembly, arrSymbols);
		}
		static ReadOnlySpan<byte> ReadAllBytes(Stream stream)
		{
			if (stream.GetType() == typeof(MemoryStream) && ((MemoryStream)stream).TryGetBuffer(out var buffer))
			{
				int start = (int)stream.Position;
				stream.Seek(0L, SeekOrigin.End);
				return buffer.AsSpan(start);
			}
			long num = stream.Length - stream.Position;
			if (num == 0L)
			{
				return ReadOnlySpan<byte>.Empty;
			}
			if ((ulong)num > 2147483591uL)
			{
				throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
			}
			byte[] array = GC.AllocateUninitializedArray<byte>((int)num);
			stream.ReadExactly(array);
			return array;
		}
	}

	protected nint LoadUnmanagedDllFromPath(string unmanagedDllPath)
	{
		ArgumentException.ThrowIfNullOrEmpty(unmanagedDllPath, "unmanagedDllPath");
		if (PathInternal.IsPartiallyQualified(unmanagedDllPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, unmanagedDllPath), "unmanagedDllPath");
		}
		return NativeLibrary.Load(unmanagedDllPath);
	}

	protected virtual nint LoadUnmanagedDll(string unmanagedDllName)
	{
		return IntPtr.Zero;
	}

	public void Unload()
	{
		if (!IsCollectible)
		{
			throw new InvalidOperationException(SR.AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible);
		}
		GC.SuppressFinalize(this);
		InitiateUnload();
	}

	internal static void OnProcessExit()
	{
		Dictionary<long, WeakReference<AssemblyLoadContext>> dictionary = s_allContexts;
		if (dictionary == null)
		{
			return;
		}
		lock (dictionary)
		{
			foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> item in dictionary)
			{
				if (item.Value.TryGetTarget(out var target))
				{
					target.RaiseUnloadEvent();
				}
			}
		}
	}

	private void VerifyIsAlive()
	{
		if (_state != 0)
		{
			throw new InvalidOperationException(SR.AssemblyLoadContext_Verify_NotUnloading);
		}
	}

	private static void SetCurrentContextualReflectionContext(AssemblyLoadContext value)
	{
		if (s_asyncLocalCurrent == null)
		{
			Interlocked.CompareExchange(ref s_asyncLocalCurrent, new AsyncLocal<AssemblyLoadContext>(), null);
		}
		s_asyncLocalCurrent.Value = value;
	}

	public ContextualReflectionScope EnterContextualReflection()
	{
		return new ContextualReflectionScope(this);
	}

	public static ContextualReflectionScope EnterContextualReflection(Assembly? activating)
	{
		if (activating == null)
		{
			return new ContextualReflectionScope(null);
		}
		AssemblyLoadContext loadContext = GetLoadContext(activating);
		if (loadContext == null)
		{
			throw new ArgumentException(SR.Arg_MustBeRuntimeAssembly, "activating");
		}
		return loadContext.EnterContextualReflection();
	}

	private static Assembly Resolve(nint gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveUsingLoad(assemblyName);
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "The code handles the Assembly.Location equals null")]
	private Assembly GetFirstResolvedAssemblyFromResolvingEvent(AssemblyName assemblyName)
	{
		Assembly assembly = null;
		Func<AssemblyLoadContext, AssemblyName, Assembly> resolving = this._resolving;
		if (resolving != null)
		{
			Delegate[] invocationList = resolving.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Func<AssemblyLoadContext, AssemblyName, Assembly> func = (Func<AssemblyLoadContext, AssemblyName, Assembly>)invocationList[i];
				assembly = func(this, assemblyName);
				if (IsTracingEnabled())
				{
					TraceResolvingHandlerInvoked(assemblyName.FullName, func.Method.Name, (this != Default) ? ToString() : Name, assembly?.FullName, (assembly != null && !assembly.IsDynamic) ? assembly.Location : null);
				}
				if (assembly != null)
				{
					return assembly;
				}
			}
		}
		return null;
	}

	private static Assembly ValidateAssemblyNameWithSimpleName(Assembly assembly, string requestedSimpleName)
	{
		ArgumentException.ThrowIfNullOrEmpty(requestedSimpleName, "AssemblyName.Name");
		string value = null;
		RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly);
		if (runtimeAssembly != null)
		{
			value = runtimeAssembly.GetSimpleName();
		}
		if (string.IsNullOrEmpty(value) || !requestedSimpleName.Equals(value, StringComparison.InvariantCultureIgnoreCase))
		{
			throw new InvalidOperationException(SR.Argument_CustomAssemblyLoadContextRequestedNameMismatch);
		}
		return assembly;
	}

	private Assembly ResolveUsingLoad(AssemblyName assemblyName)
	{
		string name = assemblyName.Name;
		Assembly assembly = Load(assemblyName);
		if (assembly != null)
		{
			assembly = ValidateAssemblyNameWithSimpleName(assembly, name);
		}
		return assembly;
	}

	private Assembly ResolveUsingEvent(AssemblyName assemblyName)
	{
		string name = assemblyName.Name;
		Assembly assembly = GetFirstResolvedAssemblyFromResolvingEvent(assemblyName);
		if (assembly != null)
		{
			assembly = ValidateAssemblyNameWithSimpleName(assembly, name);
		}
		return assembly;
	}

	private static void OnAssemblyLoad(RuntimeAssembly assembly)
	{
		AssemblyLoadContext.AssemblyLoad?.Invoke(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(assembly));
	}

	internal static RuntimeAssembly OnResourceResolve(RuntimeAssembly assembly, string resourceName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.ResourceResolve, assembly, resourceName);
	}

	internal static RuntimeAssembly OnTypeResolve(RuntimeAssembly assembly, string typeName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.TypeResolve, assembly, typeName);
	}

	private static RuntimeAssembly OnAssemblyResolve(RuntimeAssembly assembly, string assemblyFullName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.AssemblyResolve, assembly, assemblyFullName);
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "The code handles the Assembly.Location equals null")]
	private static RuntimeAssembly InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, string name)
	{
		if (eventHandler == null)
		{
			return null;
		}
		ResolveEventArgs args = new ResolveEventArgs(name, assembly);
		Delegate[] invocationList = eventHandler.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			ResolveEventHandler resolveEventHandler = (ResolveEventHandler)invocationList[i];
			Assembly assembly2 = resolveEventHandler(AppDomain.CurrentDomain, args);
			if (eventHandler == AssemblyLoadContext.AssemblyResolve && IsTracingEnabled())
			{
				TraceAssemblyResolveHandlerInvoked(name, resolveEventHandler.Method.Name, assembly2?.FullName, (assembly2 != null && !assembly2.IsDynamic) ? assembly2.Location : null);
			}
			RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly2);
			if (runtimeAssembly != null)
			{
				return runtimeAssembly;
			}
		}
		return null;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Satellite assemblies have no code in them and loading is not a problem")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "This call is fine because native call runs before this and checks BindSatelliteResourceFromBundle")]
	private Assembly ResolveSatelliteAssembly(AssemblyName assemblyName)
	{
		if (assemblyName.Name == null || !assemblyName.Name.EndsWith(".resources", StringComparison.Ordinal))
		{
			return null;
		}
		string assemblyName2 = assemblyName.Name.Substring(0, assemblyName.Name.Length - ".resources".Length);
		Assembly assembly = LoadFromAssemblyName(new AssemblyName(assemblyName2));
		AssemblyLoadContext loadContext = GetLoadContext(assembly);
		string directoryName = Path.GetDirectoryName(assembly.Location);
		if (directoryName == null)
		{
			return null;
		}
		string text = Path.Combine(directoryName, assemblyName.CultureName, assemblyName.Name + ".dll");
		bool flag = FileSystem.FileExists(text);
		if (flag ? true : false)
		{
		}
		Assembly result = (flag ? loadContext.LoadFromAssemblyPath(text) : null);
		if (IsTracingEnabled())
		{
			TraceSatelliteSubdirectoryPathProbed(text, (!flag) ? (-2147024894) : 0);
		}
		return result;
	}

	internal nint GetResolvedUnmanagedDll(Assembly assembly, string unmanagedDllName)
	{
		nint zero = IntPtr.Zero;
		Func<Assembly, string, nint> resolvingUnmanagedDll = this._resolvingUnmanagedDll;
		if (resolvingUnmanagedDll != null)
		{
			Delegate[] invocationList = resolvingUnmanagedDll.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Func<Assembly, string, nint> func = (Func<Assembly, string, nint>)invocationList[i];
				zero = func(assembly, unmanagedDllName);
				if (zero != IntPtr.Zero)
				{
					return zero;
				}
			}
		}
		return IntPtr.Zero;
	}
}
