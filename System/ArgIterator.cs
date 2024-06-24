using System.Runtime.CompilerServices;

namespace System;

public ref struct ArgIterator
{
	private nint ArgCookie;

	private nint sigPtr;

	private nint sigPtrLen;

	private nint ArgPtr;

	private int RemainingArgs;

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern ArgIterator(nint arglist);

	public ArgIterator(RuntimeArgumentHandle arglist)
		: this(arglist.Value)
	{
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe extern ArgIterator(nint arglist, void* ptr);

	[CLSCompliant(false)]
	public unsafe ArgIterator(RuntimeArgumentHandle arglist, void* ptr)
		: this(arglist.Value, ptr)
	{
	}

	[CLSCompliant(false)]
	public unsafe TypedReference GetNextArg()
	{
		TypedReference result = default(TypedReference);
		FCallGetNextArg(&result);
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe extern void FCallGetNextArg(void* result);

	[CLSCompliant(false)]
	public unsafe TypedReference GetNextArg(RuntimeTypeHandle rth)
	{
		if (sigPtr != IntPtr.Zero)
		{
			return GetNextArg();
		}
		if (ArgPtr == IntPtr.Zero)
		{
			throw new ArgumentNullException();
		}
		TypedReference result = default(TypedReference);
		InternalGetNextArg(&result, rth.GetRuntimeType());
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe extern void InternalGetNextArg(void* result, RuntimeType rt);

	public void End()
	{
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern int GetRemainingCount();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe extern void* _GetNextArgType();

	public unsafe RuntimeTypeHandle GetNextArgType()
	{
		return new RuntimeTypeHandle(Type.GetTypeFromHandleUnsafe((nint)_GetNextArgType()));
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ArgCookie);
	}

	public override bool Equals(object? o)
	{
		throw new NotSupportedException(SR.NotSupported_NYI);
	}
}
