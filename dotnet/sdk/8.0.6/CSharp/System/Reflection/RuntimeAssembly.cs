using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Reflection;

internal sealed class RuntimeAssembly : Assembly
{
	private sealed class ManifestResourceStream : UnmanagedMemoryStream
	{
		private readonly RuntimeAssembly _manifestAssembly;

		internal unsafe ManifestResourceStream(RuntimeAssembly manifestAssembly, byte* pointer, long length, long capacity, FileAccess access)
			: base(pointer, length, capacity, access)
		{
			_manifestAssembly = manifestAssembly;
		}

		public override int Read(Span<byte> buffer)
		{
			return ReadCore(buffer);
		}
	}

	private string m_fullname;

	private object m_syncRoot;

	private nint m_assembly;

	internal object SyncRoot
	{
		get
		{
			if (m_syncRoot == null)
			{
				Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), (object)null);
			}
			return m_syncRoot;
		}
	}

	[Obsolete("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override string CodeBase
	{
		get
		{
			if (IsDynamic)
			{
				throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
			}
			string codeBase = GetCodeBase();
			if (codeBase == null)
			{
				throw new NotSupportedException(SR.NotSupported_CodeBase);
			}
			if (codeBase.Length == 0)
			{
				codeBase = typeof(object).Assembly.CodeBase;
			}
			return codeBase;
		}
	}

	public override string FullName
	{
		get
		{
			if (m_fullname == null)
			{
				string s = null;
				RuntimeAssembly assembly = this;
				GetFullName(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
				Interlocked.CompareExchange(ref m_fullname, s, null);
			}
			return m_fullname;
		}
	}

	public override MethodInfo EntryPoint
	{
		get
		{
			IRuntimeMethodInfo o = null;
			RuntimeAssembly assembly = this;
			GetEntryPoint(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
			if (o == null)
			{
				return null;
			}
			return (MethodInfo)RuntimeType.GetMethodBase(o);
		}
	}

	public override IEnumerable<TypeInfo> DefinedTypes
	{
		[RequiresUnreferencedCode("Types might be removed")]
		get
		{
			RuntimeModule[] modulesInternal = GetModulesInternal(loadIfNotFound: true, getResourceModules: false);
			if (modulesInternal.Length == 1)
			{
				return modulesInternal[0].GetDefinedTypes();
			}
			List<RuntimeType> list = new List<RuntimeType>();
			for (int i = 0; i < modulesInternal.Length; i++)
			{
				list.AddRange(modulesInternal[i].GetDefinedTypes());
			}
			return list.ToArray();
		}
	}

	public override bool IsCollectible
	{
		get
		{
			RuntimeAssembly assembly = this;
			return GetIsCollectible(new QCallAssembly(ref assembly)) != Interop.BOOL.FALSE;
		}
	}

	public override Module ManifestModule => GetManifestModule(GetNativeHandle());

	public override bool ReflectionOnly => false;

	public override string Location
	{
		get
		{
			string s = null;
			RuntimeAssembly assembly = this;
			GetLocation(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
			return s;
		}
	}

	public override string ImageRuntimeVersion
	{
		get
		{
			string s = null;
			RuntimeAssembly assembly = this;
			GetImageRuntimeVersion(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
			return s;
		}
	}

	[Obsolete("The Global Assembly Cache is not supported.", DiagnosticId = "SYSLIB0005", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override bool GlobalAssemblyCache => false;

	public override long HostContext => 0L;

	public override bool IsDynamic => FCallIsDynamic(GetNativeHandle());

	private event ModuleResolveEventHandler _ModuleResolve;

	public override event ModuleResolveEventHandler ModuleResolve
	{
		add
		{
			_ModuleResolve += value;
		}
		remove
		{
			_ModuleResolve -= value;
		}
	}

	internal RuntimeAssembly()
	{
		throw new NotSupportedException();
	}

	internal nint GetUnderlyingNativeHandle()
	{
		return m_assembly;
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetCodeBase")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool GetCodeBase(QCallAssembly assembly, StringHandleOnStack retString)
	{
		int num = __PInvoke(assembly, retString);
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetCodeBase", ExactSpelling = true)]
		static extern int __PInvoke(QCallAssembly __assembly_native, StringHandleOnStack __retString_native);
	}

	internal string GetCodeBase()
	{
		string s = null;
		RuntimeAssembly assembly = this;
		if (GetCodeBase(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s)))
		{
			return s;
		}
		return null;
	}

	internal RuntimeAssembly GetNativeHandle()
	{
		return this;
	}

	public override AssemblyName GetName(bool copiedName)
	{
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.Name = GetSimpleName();
		assemblyName.Version = GetVersion();
		assemblyName.CultureInfo = GetLocale();
		assemblyName.SetPublicKey(GetPublicKey());
		assemblyName.RawFlags = GetFlags() | AssemblyNameFlags.PublicKey;
		assemblyName.CodeBase = GetCodeBase();
		assemblyName.HashAlgorithm = GetHashAlgorithm();
		Module manifestModule = ManifestModule;
		if (manifestModule.MDStreamVersion > 65536)
		{
			manifestModule.GetPEKind(out var peKind, out var machine);
			assemblyName.SetProcArchIndex(peKind, machine);
		}
		return assemblyName;
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetFullName", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetFullName")]
	private static extern void GetFullName(QCallAssembly assembly, StringHandleOnStack retString);

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetEntryPoint", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetEntryPoint")]
	private static extern void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack retMethod);

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetTypeCore", StringMarshalling = StringMarshalling.Utf8)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetTypeCore(QCallAssembly assembly, string typeName, ReadOnlySpan<string> nestedTypeNames, int nestedTypeNamesLength, ObjectHandleOnStack retType)
	{
		bool flag = false;
		byte* ptr = default(byte*);
		nint* ptr2 = default(nint*);
		ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn managedToUnmanagedIn = default(ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn);
		int num = 0;
		Unsafe.SkipInit<int>(out var _);
		Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn2 = default(Utf8StringMarshaller.ManagedToUnmanagedIn);
		try
		{
			int bufferSize = ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn.BufferSize;
			Span<nint> buffer = new Span<nint>(stackalloc byte[(int)checked(unchecked((nuint)(uint)bufferSize) * (nuint)8u)], bufferSize);
			managedToUnmanagedIn.FromManaged(nestedTypeNames, buffer);
			ReadOnlySpan<string> managedValuesSource = managedToUnmanagedIn.GetManagedValuesSource();
			Span<nint> unmanagedValuesDestination = managedToUnmanagedIn.GetUnmanagedValuesDestination();
			int num2 = 0;
			while (num2 < managedValuesSource.Length)
			{
				unmanagedValuesDestination[num2] = (nint)Utf8StringMarshaller.ConvertToUnmanaged(managedValuesSource[num2]);
				num2++;
				num++;
			}
			Span<byte> buffer2 = stackalloc byte[256];
			managedToUnmanagedIn2.FromManaged(typeName, buffer2);
			fixed (nint* ptr3 = managedToUnmanagedIn)
			{
				void* ptr4 = ptr3;
				ptr2 = managedToUnmanagedIn.ToUnmanaged();
				ptr = managedToUnmanagedIn2.ToUnmanaged();
				__PInvoke(assembly, ptr, ptr2, nestedTypeNamesLength, retType);
			}
			flag = true;
		}
		finally
		{
			ReadOnlySpan<nint> readOnlySpan = managedToUnmanagedIn.GetUnmanagedValuesDestination();
			for (int i = 0; i < num; i++)
			{
				Utf8StringMarshaller.Free((byte*)readOnlySpan[i]);
			}
			managedToUnmanagedIn.Free();
			managedToUnmanagedIn2.Free();
		}
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetTypeCore", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallAssembly __assembly_native, byte* __typeName_native, nint* __nestedTypeNames_native, int __nestedTypeNamesLength_native, ObjectHandleOnStack __retType_native);
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetTypeCoreIgnoreCase", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetTypeCoreIgnoreCase(QCallAssembly assembly, string typeName, ReadOnlySpan<string> nestedTypeNames, int nestedTypeNamesLength, ObjectHandleOnStack retType)
	{
		bool flag = false;
		nint* ptr = default(nint*);
		ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn managedToUnmanagedIn = default(ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn);
		int num = 0;
		Unsafe.SkipInit<int>(out var _);
		try
		{
			int bufferSize = ReadOnlySpanMarshaller<string, nint>.ManagedToUnmanagedIn.BufferSize;
			Span<nint> buffer = new Span<nint>(stackalloc byte[(int)checked(unchecked((nuint)(uint)bufferSize) * (nuint)8u)], bufferSize);
			managedToUnmanagedIn.FromManaged(nestedTypeNames, buffer);
			ReadOnlySpan<string> managedValuesSource = managedToUnmanagedIn.GetManagedValuesSource();
			Span<nint> unmanagedValuesDestination = managedToUnmanagedIn.GetUnmanagedValuesDestination();
			int num2 = 0;
			while (num2 < managedValuesSource.Length)
			{
				unmanagedValuesDestination[num2] = (nint)Utf16StringMarshaller.ConvertToUnmanaged(managedValuesSource[num2]);
				num2++;
				num++;
			}
			fixed (nint* ptr2 = managedToUnmanagedIn)
			{
				void* ptr3 = ptr2;
				fixed (char* ptr4 = &Utf16StringMarshaller.GetPinnableReference(typeName))
				{
					void* _typeName_native = ptr4;
					ptr = managedToUnmanagedIn.ToUnmanaged();
					__PInvoke(assembly, (ushort*)_typeName_native, ptr, nestedTypeNamesLength, retType);
				}
			}
			flag = true;
		}
		finally
		{
			ReadOnlySpan<nint> readOnlySpan = managedToUnmanagedIn.GetUnmanagedValuesDestination();
			for (int i = 0; i < num; i++)
			{
				Utf16StringMarshaller.Free((ushort*)readOnlySpan[i]);
			}
			managedToUnmanagedIn.Free();
		}
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetTypeCoreIgnoreCase", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallAssembly __assembly_native, ushort* __typeName_native, nint* __nestedTypeNames_native, int __nestedTypeNamesLength_native, ObjectHandleOnStack __retType_native);
	}

	internal Type GetTypeCore(string typeName, ReadOnlySpan<string> nestedTypeNames, bool throwOnError, bool ignoreCase)
	{
		RuntimeAssembly assembly = this;
		Type o = null;
		try
		{
			if (ignoreCase)
			{
				GetTypeCoreIgnoreCase(new QCallAssembly(ref assembly), typeName, nestedTypeNames, nestedTypeNames.Length, ObjectHandleOnStack.Create(ref o));
			}
			else
			{
				GetTypeCore(new QCallAssembly(ref assembly), typeName, nestedTypeNames, nestedTypeNames.Length, ObjectHandleOnStack.Create(ref o));
			}
		}
		catch (FileNotFoundException) when (!throwOnError)
		{
			return null;
		}
		if (o == null && throwOnError)
		{
			throw new TypeLoadException(SR.Format(SR.ClassLoad_General, typeName, FullName));
		}
		return o;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string name, bool throwOnError, bool ignoreCase)
	{
		ArgumentException.ThrowIfNullOrEmpty(name, "name");
		return TypeNameParser.GetType(name, throwOnError, ignoreCase, this);
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetExportedTypes", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetExportedTypes")]
	private static extern void GetExportedTypes(QCallAssembly assembly, ObjectHandleOnStack retTypes);

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetExportedTypes()
	{
		Type[] o = null;
		RuntimeAssembly assembly = this;
		GetExportedTypes(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetIsCollectible", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetIsCollectible")]
	internal static extern Interop.BOOL GetIsCollectible(QCallAssembly assembly);

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetResource", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static byte* GetResource(QCallAssembly assembly, string resourceName, out uint length)
	{
		Unsafe.SkipInit<uint>(out length);
		byte* result;
		fixed (uint* _length_native = &length)
		{
			fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(resourceName))
			{
				void* _resourceName_native = ptr;
				result = __PInvoke(assembly, (ushort*)_resourceName_native, _length_native);
			}
		}
		return result;
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetResource", ExactSpelling = true)]
		static extern unsafe byte* __PInvoke(QCallAssembly __assembly_native, ushort* __resourceName_native, uint* __length_native);
	}

	public override Stream GetManifestResourceStream(Type type, string name)
	{
		if (name == null)
		{
			ArgumentNullException.ThrowIfNull(type, "type");
		}
		string text = type?.Namespace;
		char reference = Type.Delimiter;
		string name2 = ((text != null && name != null) ? string.Concat(text, new ReadOnlySpan<char>(ref reference), name) : (text + name));
		return GetManifestResourceStream(name2);
	}

	public unsafe override Stream GetManifestResourceStream(string name)
	{
		RuntimeAssembly assembly = this;
		uint length;
		byte* resource = GetResource(new QCallAssembly(ref assembly), name, out length);
		if (resource != null)
		{
			return new ManifestResourceStream(this, resource, length, length, FileAccess.Read);
		}
		return null;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
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

	internal static RuntimeAssembly InternalLoad(string assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext = null)
	{
		return InternalLoad(new AssemblyName(assemblyName), ref stackMark, assemblyLoadContext);
	}

	internal unsafe static RuntimeAssembly InternalLoad(AssemblyName assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext = null, RuntimeAssembly requestingAssembly = null, bool throwOnFileNotFound = true)
	{
		//The blocks IL_0059, IL_005d, IL_005f, IL_0078, IL_00a7, IL_00aa, IL_00ad, IL_00ea are reachable both inside and outside the pinned region starting at IL_0054. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		RuntimeAssembly o = null;
		AssemblyNameFlags assemblyNameFlags = assemblyName.RawFlags;
		byte[] array;
		if ((array = assemblyName.RawPublicKeyToken) != null)
		{
			assemblyNameFlags &= ~AssemblyNameFlags.PublicKey;
		}
		else if ((array = assemblyName.RawPublicKey) != null)
		{
			assemblyNameFlags |= AssemblyNameFlags.PublicKey;
		}
		fixed (char* pName = assemblyName.Name)
		{
			string? cultureName = assemblyName.CultureName;
			char* intPtr;
			byte[] array2;
			NativeAssemblyNameParts nativeAssemblyNameParts;
			ref NativeAssemblyNameParts reference;
			int cbPublicKeyOrToken;
			if (cultureName == null)
			{
				char* pCultureName;
				intPtr = (pCultureName = null);
				array2 = array;
				fixed (byte* ptr = array2)
				{
					byte* pPublicKeyOrToken = ptr;
					nativeAssemblyNameParts = default(NativeAssemblyNameParts);
					nativeAssemblyNameParts._flags = assemblyNameFlags;
					nativeAssemblyNameParts._pName = pName;
					nativeAssemblyNameParts._pCultureName = pCultureName;
					nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
					reference = ref nativeAssemblyNameParts;
					cbPublicKeyOrToken = ((array != null) ? array.Length : 0);
					reference._cbPublicKeyOrToken = cbPublicKeyOrToken;
					nativeAssemblyNameParts.SetVersion(assemblyName.Version, ushort.MaxValue);
					InternalLoad(&nativeAssemblyNameParts, ObjectHandleOnStack.Create(ref requestingAssembly), new StackCrawlMarkHandle(ref stackMark), throwOnFileNotFound, ObjectHandleOnStack.Create(ref assemblyLoadContext), ObjectHandleOnStack.Create(ref o));
				}
			}
			else
			{
				fixed (char* ptr2 = &cultureName.GetPinnableReference())
				{
					char* pCultureName;
					intPtr = (pCultureName = ptr2);
					array2 = array;
					fixed (byte* ptr = array2)
					{
						byte* pPublicKeyOrToken = ptr;
						nativeAssemblyNameParts = default(NativeAssemblyNameParts);
						nativeAssemblyNameParts._flags = assemblyNameFlags;
						nativeAssemblyNameParts._pName = pName;
						nativeAssemblyNameParts._pCultureName = pCultureName;
						nativeAssemblyNameParts._pPublicKeyOrToken = pPublicKeyOrToken;
						reference = ref nativeAssemblyNameParts;
						cbPublicKeyOrToken = (reference._cbPublicKeyOrToken = ((array != null) ? array.Length : 0));
						nativeAssemblyNameParts.SetVersion(assemblyName.Version, ushort.MaxValue);
						InternalLoad(&nativeAssemblyNameParts, ObjectHandleOnStack.Create(ref requestingAssembly), new StackCrawlMarkHandle(ref stackMark), throwOnFileNotFound, ObjectHandleOnStack.Create(ref assemblyLoadContext), ObjectHandleOnStack.Create(ref o));
					}
				}
			}
		}
		return o;
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_InternalLoad")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void InternalLoad(NativeAssemblyNameParts* pAssemblyNameParts, ObjectHandleOnStack requestingAssembly, StackCrawlMarkHandle stackMark, [MarshalAs(UnmanagedType.Bool)] bool throwOnFileNotFound, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack retAssembly)
	{
		int _throwOnFileNotFound_native = (throwOnFileNotFound ? 1 : 0);
		__PInvoke(pAssemblyNameParts, requestingAssembly, stackMark, _throwOnFileNotFound_native, assemblyLoadContext, retAssembly);
		[DllImport("QCall", EntryPoint = "AssemblyNative_InternalLoad", ExactSpelling = true)]
		static extern unsafe void __PInvoke(NativeAssemblyNameParts* __pAssemblyNameParts_native, ObjectHandleOnStack __requestingAssembly_native, StackCrawlMarkHandle __stackMark_native, int __throwOnFileNotFound_native, ObjectHandleOnStack __assemblyLoadContext_native, ObjectHandleOnStack __retAssembly_native);
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetModule", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetModule(QCallAssembly assembly, string name, ObjectHandleOnStack retModule)
	{
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			__PInvoke(assembly, (ushort*)_name_native, retModule);
		}
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetModule", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallAssembly __assembly_native, ushort* __name_native, ObjectHandleOnStack __retModule_native);
	}

	public override Module GetModule(string name)
	{
		Module o = null;
		RuntimeAssembly assembly = this;
		GetModule(new QCallAssembly(ref assembly), name, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream GetFile(string name)
	{
		if (Location.Length == 0)
		{
			throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
		}
		RuntimeModule runtimeModule = (RuntimeModule)GetModule(name);
		if (runtimeModule == null)
		{
			return null;
		}
		return new FileStream(runtimeModule.GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream[] GetFiles(bool getResourceModules)
	{
		if (Location.Length == 0)
		{
			throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
		}
		Module[] modules = GetModules(getResourceModules);
		FileStream[] array = new FileStream[modules.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new FileStream(((RuntimeModule)modules[i]).GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern string[] GetManifestResourceNames(RuntimeAssembly assembly);

	public override string[] GetManifestResourceNames()
	{
		return GetManifestResourceNames(GetNativeHandle());
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

	[RequiresUnreferencedCode("Assembly references might be removed")]
	public override AssemblyName[] GetReferencedAssemblies()
	{
		return GetReferencedAssemblies(GetNativeHandle());
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetManifestResourceInfo", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int GetManifestResourceInfo(QCallAssembly assembly, string resourceName, ObjectHandleOnStack assemblyRef, StringHandleOnStack retFileName)
	{
		int result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(resourceName))
		{
			void* _resourceName_native = ptr;
			result = __PInvoke(assembly, (ushort*)_resourceName_native, assemblyRef, retFileName);
		}
		return result;
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetManifestResourceInfo", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallAssembly __assembly_native, ushort* __resourceName_native, ObjectHandleOnStack __assemblyRef_native, StringHandleOnStack __retFileName_native);
	}

	public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
	{
		RuntimeAssembly o = null;
		string s = null;
		RuntimeAssembly assembly = this;
		int manifestResourceInfo = GetManifestResourceInfo(new QCallAssembly(ref assembly), resourceName, ObjectHandleOnStack.Create(ref o), new StringHandleOnStack(ref s));
		if (manifestResourceInfo == -1)
		{
			return null;
		}
		return new ManifestResourceInfo(o, s, (ResourceLocation)manifestResourceInfo);
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetLocation", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetLocation")]
	private static extern void GetLocation(QCallAssembly assembly, StringHandleOnStack retString);

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetImageRuntimeVersion", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetImageRuntimeVersion")]
	private static extern void GetImageRuntimeVersion(QCallAssembly assembly, StringHandleOnStack retString);

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetVersion")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static void GetVersion(QCallAssembly assembly, out int majVer, out int minVer, out int buildNum, out int revNum)
	{
		Unsafe.SkipInit<int>(out majVer);
		Unsafe.SkipInit<int>(out minVer);
		Unsafe.SkipInit<int>(out buildNum);
		Unsafe.SkipInit<int>(out revNum);
		fixed (int* _revNum_native = &revNum)
		{
			fixed (int* _buildNum_native = &buildNum)
			{
				fixed (int* _minVer_native = &minVer)
				{
					fixed (int* _majVer_native = &majVer)
					{
						__PInvoke(assembly, _majVer_native, _minVer_native, _buildNum_native, _revNum_native);
					}
				}
			}
		}
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetVersion", ExactSpelling = true)]
		static extern unsafe void __PInvoke(QCallAssembly __assembly_native, int* __majVer_native, int* __minVer_native, int* __buildNum_native, int* __revNum_native);
	}

	private Version GetVersion()
	{
		RuntimeAssembly assembly = this;
		GetVersion(new QCallAssembly(ref assembly), out var majVer, out var minVer, out var buildNum, out var revNum);
		return new Version(majVer, minVer, buildNum, revNum);
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetLocale", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetLocale")]
	private static extern void GetLocale(QCallAssembly assembly, StringHandleOnStack retString);

	private CultureInfo GetLocale()
	{
		string s = null;
		RuntimeAssembly assembly = this;
		GetLocale(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
		if (s == null)
		{
			return CultureInfo.InvariantCulture;
		}
		return CultureInfo.GetCultureInfo(s);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetSimpleName", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetSimpleName")]
	private static extern void GetSimpleName(QCallAssembly assembly, StringHandleOnStack retSimpleName);

	internal string GetSimpleName()
	{
		RuntimeAssembly assembly = this;
		string s = null;
		GetSimpleName(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
		return s;
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetHashAlgorithm", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetHashAlgorithm")]
	private static extern AssemblyHashAlgorithm GetHashAlgorithm(QCallAssembly assembly);

	private AssemblyHashAlgorithm GetHashAlgorithm()
	{
		RuntimeAssembly assembly = this;
		return GetHashAlgorithm(new QCallAssembly(ref assembly));
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetFlags", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetFlags")]
	private static extern AssemblyNameFlags GetFlags(QCallAssembly assembly);

	private AssemblyNameFlags GetFlags()
	{
		RuntimeAssembly assembly = this;
		return GetFlags(new QCallAssembly(ref assembly));
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetPublicKey", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetPublicKey")]
	private static extern void GetPublicKey(QCallAssembly assembly, ObjectHandleOnStack retPublicKey);

	internal byte[] GetPublicKey()
	{
		byte[] o = null;
		RuntimeAssembly assembly = this;
		GetPublicKey(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture)
	{
		return GetSatelliteAssembly(culture, null);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
	{
		ArgumentNullException.ThrowIfNull(culture, "culture");
		return InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: true);
	}

	internal Assembly InternalGetSatelliteAssembly(CultureInfo culture, Version version, bool throwOnFileNotFound)
	{
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.SetPublicKey(GetPublicKey());
		assemblyName.Flags = GetFlags() | AssemblyNameFlags.PublicKey;
		assemblyName.Version = version ?? GetVersion();
		assemblyName.CultureInfo = culture;
		assemblyName.Name = GetSimpleName() + ".resources";
		StackCrawlMark stackMark = StackCrawlMark.LookForMe;
		RuntimeAssembly runtimeAssembly = InternalLoad(assemblyName, ref stackMark, null, this, throwOnFileNotFound);
		if (runtimeAssembly == this)
		{
			runtimeAssembly = null;
		}
		if (runtimeAssembly == null && throwOnFileNotFound)
		{
			throw new FileNotFoundException(SR.Format(culture, SR.IO_FileNotFound_FileName, assemblyName.Name));
		}
		return runtimeAssembly;
	}

	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetModules")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private static void GetModules(QCallAssembly assembly, [MarshalAs(UnmanagedType.Bool)] bool loadIfNotFound, [MarshalAs(UnmanagedType.Bool)] bool getResourceModules, ObjectHandleOnStack retModuleHandles)
	{
		int _getResourceModules_native = (getResourceModules ? 1 : 0);
		int _loadIfNotFound_native = (loadIfNotFound ? 1 : 0);
		__PInvoke(assembly, _loadIfNotFound_native, _getResourceModules_native, retModuleHandles);
		[DllImport("QCall", EntryPoint = "AssemblyNative_GetModules", ExactSpelling = true)]
		static extern void __PInvoke(QCallAssembly __assembly_native, int __loadIfNotFound_native, int __getResourceModules_native, ObjectHandleOnStack __retModuleHandles_native);
	}

	private RuntimeModule[] GetModulesInternal(bool loadIfNotFound, bool getResourceModules)
	{
		RuntimeModule[] o = null;
		RuntimeAssembly assembly = this;
		GetModules(new QCallAssembly(ref assembly), loadIfNotFound, getResourceModules, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	public override Module[] GetModules(bool getResourceModules)
	{
		return GetModulesInternal(loadIfNotFound: true, getResourceModules);
	}

	public override Module[] GetLoadedModules(bool getResourceModules)
	{
		return GetModulesInternal(loadIfNotFound: false, getResourceModules);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RuntimeAssembly assembly);

	[RequiresUnreferencedCode("Types might be removed")]
	public sealed override Type[] GetForwardedTypes()
	{
		List<Type> list = new List<Type>();
		List<Exception> list2 = new List<Exception>();
		GetManifestModule(GetNativeHandle()).MetadataImport.Enum(MetadataTokenType.ExportedType, 0, out var result);
		RuntimeAssembly assembly = this;
		QCallAssembly assembly2 = new QCallAssembly(ref assembly);
		for (int i = 0; i < result.Length; i++)
		{
			MetadataToken mdtExternalType = result[i];
			Type o = null;
			Exception item = null;
			ObjectHandleOnStack type = ObjectHandleOnStack.Create(ref o);
			try
			{
				GetForwardedType(assembly2, mdtExternalType, type);
				if (o == null)
				{
					continue;
				}
			}
			catch (Exception ex)
			{
				o = null;
				item = ex;
			}
			if (o != null)
			{
				list.Add(o);
				AddPublicNestedTypes(o, list, list2);
			}
			else
			{
				list2.Add(item);
			}
		}
		if (list2.Count != 0)
		{
			int count = list.Count;
			int count2 = list2.Count;
			list.AddRange(new Type[count2]);
			list2.InsertRange(0, new Exception[count]);
			throw new ReflectionTypeLoadException(list.ToArray(), list2.ToArray());
		}
		return list.ToArray();
	}

	[RequiresUnreferencedCode("Types might be removed because recursive nested types can't currently be annotated for dynamic access.")]
	private static void AddPublicNestedTypes(Type type, List<Type> types, List<Exception> exceptions)
	{
		Type[] nestedTypes;
		try
		{
			nestedTypes = type.GetNestedTypes(BindingFlags.Public);
		}
		catch (Exception item)
		{
			exceptions.Add(item);
			return;
		}
		Type[] array = nestedTypes;
		foreach (Type type2 in array)
		{
			types.Add(type2);
			AddPublicNestedTypes(type2, types, exceptions);
		}
	}

	[DllImport("QCall", EntryPoint = "AssemblyNative_GetForwardedType", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_GetForwardedType")]
	private static extern void GetForwardedType(QCallAssembly assembly, MetadataToken mdtExternalType, ObjectHandleOnStack type);
}
