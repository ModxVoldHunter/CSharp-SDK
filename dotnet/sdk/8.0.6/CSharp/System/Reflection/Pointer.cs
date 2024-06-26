using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Reflection;

[CLSCompliant(false)]
public sealed class Pointer : ISerializable
{
	private unsafe readonly void* _ptr;

	private readonly RuntimeType _ptrType;

	private unsafe Pointer(void* ptr, RuntimeType ptrType)
	{
		_ptr = ptr;
		_ptrType = ptrType;
	}

	public unsafe static object Box(void* ptr, Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		if (!type.IsPointer)
		{
			throw new ArgumentException(SR.Arg_MustBePointer, "ptr");
		}
		if (!(type is RuntimeType ptrType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "type");
		}
		return new Pointer(ptr, ptrType);
	}

	public unsafe static void* Unbox(object ptr)
	{
		if (!(ptr is Pointer))
		{
			throw new ArgumentException(SR.Arg_MustBePointer, "ptr");
		}
		return ((Pointer)ptr)._ptr;
	}

	public unsafe override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Pointer pointer)
		{
			return _ptr == pointer._ptr;
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		nuint ptr = (nuint)_ptr;
		return ((UIntPtr)ptr).GetHashCode();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal RuntimeType GetPointerType()
	{
		return _ptrType;
	}

	internal unsafe nint GetPointerValue()
	{
		return (nint)_ptr;
	}
}
