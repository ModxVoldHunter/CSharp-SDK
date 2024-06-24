using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

public static class AssemblyExtensions
{
	[LibraryImport("QCall", EntryPoint = "AssemblyNative_InternalTryGetRawMetadata")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool InternalTryGetRawMetadata(QCallAssembly assembly, ref byte* blob, ref int length)
	{
		int num;
		fixed (int* _length_native = &length)
		{
			fixed (byte** _blob_native = &blob)
			{
				num = __PInvoke(assembly, _blob_native, _length_native);
			}
		}
		return num != 0;
		[DllImport("QCall", EntryPoint = "AssemblyNative_InternalTryGetRawMetadata", ExactSpelling = true)]
		static extern unsafe int __PInvoke(QCallAssembly __assembly_native, byte** __blob_native, int* __length_native);
	}

	[CLSCompliant(false)]
	public unsafe static bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		blob = null;
		length = 0;
		RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
		if (runtimeAssembly == null)
		{
			return false;
		}
		RuntimeAssembly assembly2 = runtimeAssembly;
		return InternalTryGetRawMetadata(new QCallAssembly(ref assembly2), ref blob, ref length);
	}
}
