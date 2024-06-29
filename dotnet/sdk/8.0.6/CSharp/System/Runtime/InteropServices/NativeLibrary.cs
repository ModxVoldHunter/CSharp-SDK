using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace System.Runtime.InteropServices;

public static class NativeLibrary
{
	private static ConditionalWeakTable<Assembly, DllImportResolver> s_nativeDllResolveMap;

	internal static nint LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
	{
		RuntimeAssembly assembly2 = (RuntimeAssembly)assembly;
		return LoadByName(libraryName, new QCallAssembly(ref assembly2), searchPath.HasValue, (uint)searchPath.GetValueOrDefault(), throwOnError);
	}

	[LibraryImport("QCall", EntryPoint = "NativeLibrary_LoadFromPath", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static nint LoadFromPath(string libraryName, [MarshalAs(UnmanagedType.Bool)] bool throwOnError)
	{
		int _throwOnError_native = (throwOnError ? 1 : 0);
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(libraryName))
		{
			void* _libraryName_native = ptr;
			result = __PInvoke((ushort*)_libraryName_native, _throwOnError_native);
		}
		return result;
		[DllImport("QCall", EntryPoint = "NativeLibrary_LoadFromPath", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(ushort* __libraryName_native, int __throwOnError_native);
	}

	[LibraryImport("QCall", EntryPoint = "NativeLibrary_LoadByName", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static nint LoadByName(string libraryName, QCallAssembly callingAssembly, [MarshalAs(UnmanagedType.Bool)] bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, [MarshalAs(UnmanagedType.Bool)] bool throwOnError)
	{
		int _throwOnError_native = (throwOnError ? 1 : 0);
		int _hasDllImportSearchPathFlag_native = (hasDllImportSearchPathFlag ? 1 : 0);
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(libraryName))
		{
			void* _libraryName_native = ptr;
			result = __PInvoke((ushort*)_libraryName_native, callingAssembly, _hasDllImportSearchPathFlag_native, dllImportSearchPathFlag, _throwOnError_native);
		}
		return result;
		[DllImport("QCall", EntryPoint = "NativeLibrary_LoadByName", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(ushort* __libraryName_native, QCallAssembly __callingAssembly_native, int __hasDllImportSearchPathFlag_native, uint __dllImportSearchPathFlag_native, int __throwOnError_native);
	}

	[DllImport("QCall", EntryPoint = "NativeLibrary_FreeLib", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "NativeLibrary_FreeLib")]
	internal static extern void FreeLib(nint handle);

	[LibraryImport("QCall", EntryPoint = "NativeLibrary_GetSymbol", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	internal unsafe static nint GetSymbol(nint handle, string symbolName, [MarshalAs(UnmanagedType.Bool)] bool throwOnError)
	{
		int _throwOnError_native = (throwOnError ? 1 : 0);
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(symbolName))
		{
			void* _symbolName_native = ptr;
			result = __PInvoke(handle, (ushort*)_symbolName_native, _throwOnError_native);
		}
		return result;
		[DllImport("QCall", EntryPoint = "NativeLibrary_GetSymbol", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(nint __handle_native, ushort* __symbolName_native, int __throwOnError_native);
	}

	public static nint Load(string libraryPath)
	{
		ArgumentNullException.ThrowIfNull(libraryPath, "libraryPath");
		return LoadFromPath(libraryPath, throwOnError: true);
	}

	public static bool TryLoad(string libraryPath, out nint handle)
	{
		ArgumentNullException.ThrowIfNull(libraryPath, "libraryPath");
		handle = LoadFromPath(libraryPath, throwOnError: false);
		return handle != IntPtr.Zero;
	}

	public static nint Load(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		ArgumentNullException.ThrowIfNull(libraryName, "libraryName");
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		if (!(assembly is RuntimeAssembly))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		return LoadLibraryByName(libraryName, assembly, searchPath, throwOnError: true);
	}

	public static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out nint handle)
	{
		ArgumentNullException.ThrowIfNull(libraryName, "libraryName");
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		if (!(assembly is RuntimeAssembly))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		handle = LoadLibraryByName(libraryName, assembly, searchPath, throwOnError: false);
		return handle != IntPtr.Zero;
	}

	public static void Free(nint handle)
	{
		if (handle != IntPtr.Zero)
		{
			FreeLib(handle);
		}
	}

	public static nint GetExport(nint handle, string name)
	{
		ArgumentNullException.ThrowIfNull(handle, "handle");
		ArgumentNullException.ThrowIfNull(name, "name");
		return GetSymbol(handle, name, throwOnError: true);
	}

	public static bool TryGetExport(nint handle, string name, out nint address)
	{
		ArgumentNullException.ThrowIfNull(handle, "handle");
		ArgumentNullException.ThrowIfNull(name, "name");
		address = GetSymbol(handle, name, throwOnError: false);
		return address != IntPtr.Zero;
	}

	public static void SetDllImportResolver(Assembly assembly, DllImportResolver resolver)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		ArgumentNullException.ThrowIfNull(resolver, "resolver");
		if (!(assembly is RuntimeAssembly))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		if (s_nativeDllResolveMap == null)
		{
			Interlocked.CompareExchange(ref s_nativeDllResolveMap, new ConditionalWeakTable<Assembly, DllImportResolver>(), null);
		}
		if (!s_nativeDllResolveMap.TryAdd(assembly, resolver))
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondResolver);
		}
	}

	internal static nint LoadLibraryCallbackStub(string libraryName, Assembly assembly, bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags)
	{
		if (s_nativeDllResolveMap == null)
		{
			return IntPtr.Zero;
		}
		if (!s_nativeDllResolveMap.TryGetValue(assembly, out var value))
		{
			return IntPtr.Zero;
		}
		return value(libraryName, assembly, hasDllImportSearchPathFlags ? new DllImportSearchPath?((DllImportSearchPath)dllImportSearchPathFlags) : null);
	}

	public static nint GetMainProgramHandle()
	{
		nint zero = IntPtr.Zero;
		zero = Interop.Kernel32.GetModuleHandle(null);
		if (zero == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastPInvokeError());
		}
		return zero;
	}
}
