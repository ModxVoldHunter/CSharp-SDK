using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[NonVersionable]
[CLSCompliant(false)]
public ref struct TypedReference
{
	private readonly ref byte _value;

	private readonly nint _type;

	internal bool IsNull
	{
		get
		{
			if (Unsafe.IsNullRef(ref _value))
			{
				return _type == IntPtr.Zero;
			}
			return false;
		}
	}

	public unsafe static object? ToObject(TypedReference value)
	{
		TypeHandle typeHandle = new TypeHandle((void*)value._type);
		if (typeHandle.IsNull)
		{
			ThrowHelper.ThrowArgumentException_ArgumentNull_TypedRefType();
		}
		MethodTable* ptr = (typeHandle.IsTypeDesc ? TypeHandle.TypeHandleOf<nuint>().AsMethodTable() : typeHandle.AsMethodTable());
		if (ptr->IsValueType)
		{
			return RuntimeHelpers.Box(ptr, ref value._value);
		}
		return Unsafe.As<byte, object>(ref value._value);
	}

	public unsafe static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
	{
		ArgumentNullException.ThrowIfNull(target, "target");
		ArgumentNullException.ThrowIfNull(flds, "flds");
		if (flds.Length == 0)
		{
			throw new ArgumentException(SR.Arg_ArrayZeroError, "flds");
		}
		nint[] array = new nint[flds.Length];
		RuntimeType runtimeType = (RuntimeType)target.GetType();
		for (int i = 0; i < flds.Length; i++)
		{
			RuntimeFieldInfo runtimeFieldInfo = flds[i] as RuntimeFieldInfo;
			if (runtimeFieldInfo == null)
			{
				throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo);
			}
			if (runtimeFieldInfo.IsStatic)
			{
				throw new ArgumentException(SR.Format(SR.Argument_TypedReferenceInvalidField, runtimeFieldInfo.Name));
			}
			if (runtimeType != runtimeFieldInfo.GetDeclaringTypeInternal() && !runtimeType.IsSubclassOf(runtimeFieldInfo.GetDeclaringTypeInternal()))
			{
				throw new MissingMemberException(SR.MissingMemberTypeRef);
			}
			RuntimeType runtimeType2 = (RuntimeType)runtimeFieldInfo.FieldType;
			if (runtimeType2.IsPrimitive)
			{
				throw new ArgumentException(SR.Format(SR.Arg_TypeRefPrimitive, runtimeFieldInfo.Name));
			}
			if (i < flds.Length - 1 && !runtimeType2.IsValueType)
			{
				throw new MissingMemberException(SR.MissingMemberNestErr);
			}
			array[i] = runtimeFieldInfo.FieldHandle.Value;
			runtimeType = runtimeType2;
		}
		TypedReference result = default(TypedReference);
		InternalMakeTypedReference(&result, target, array, runtimeType);
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void InternalMakeTypedReference(void* result, object target, nint[] flds, RuntimeType lastFieldType);

	public override int GetHashCode()
	{
		if (_type == IntPtr.Zero)
		{
			return 0;
		}
		return __reftype(this).GetHashCode();
	}

	public override bool Equals(object? o)
	{
		throw new NotSupportedException(SR.NotSupported_NYI);
	}

	public static Type GetTargetType(TypedReference value)
	{
		return __reftype(value);
	}

	public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
	{
		return __reftype(value).TypeHandle;
	}

	public static void SetTypedReference(TypedReference target, object? value)
	{
		throw new NotSupportedException();
	}
}
