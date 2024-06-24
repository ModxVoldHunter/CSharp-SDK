using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.CompilerServices;

internal ref struct QCallModule
{
	private unsafe void* _ptr;

	private nint _module;

	internal unsafe QCallModule(ref RuntimeModule module)
	{
		_ptr = Unsafe.AsPointer(ref module);
		_module = module.GetUnderlyingNativeHandle();
	}

	internal unsafe QCallModule(ref RuntimeModuleBuilder module)
	{
		_ptr = Unsafe.AsPointer(ref module);
		_module = module.InternalModule.GetUnderlyingNativeHandle();
	}
}
