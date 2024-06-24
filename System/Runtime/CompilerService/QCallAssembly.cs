using System.Reflection;

namespace System.Runtime.CompilerServices;

internal ref struct QCallAssembly
{
	private unsafe void* _ptr;

	private nint _assembly;

	internal unsafe QCallAssembly(ref RuntimeAssembly assembly)
	{
		_ptr = Unsafe.AsPointer(ref assembly);
		_assembly = assembly?.GetUnderlyingNativeHandle() ?? IntPtr.Zero;
	}
}
