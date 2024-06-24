using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System;

[NonVersionable]
public struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
{
	private readonly IRuntimeFieldInfo m_ptr;

	public nint Value
	{
		get
		{
			if (m_ptr == null)
			{
				return IntPtr.Zero;
			}
			return m_ptr.Value.Value;
		}
	}

	internal RuntimeFieldHandle(IRuntimeFieldInfo fieldInfo)
	{
		m_ptr = fieldInfo;
	}

	internal IRuntimeFieldInfo GetRuntimeFieldInfo()
	{
		return m_ptr;
	}

	internal bool IsNullHandle()
	{
		return m_ptr == null;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value);
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is RuntimeFieldHandle runtimeFieldHandle))
		{
			return false;
		}
		return runtimeFieldHandle.Value == Value;
	}

	public bool Equals(RuntimeFieldHandle handle)
	{
		return handle.Value == Value;
	}

	public static RuntimeFieldHandle FromIntPtr(nint value)
	{
		RuntimeFieldHandleInternal runtimeFieldHandleInternal = new RuntimeFieldHandleInternal(value);
		RuntimeFieldInfoStub fieldInfo = new RuntimeFieldInfoStub(runtimeFieldHandleInternal, GetLoaderAllocator(runtimeFieldHandleInternal));
		return new RuntimeFieldHandle(fieldInfo);
	}

	public static nint ToIntPtr(RuntimeFieldHandle value)
	{
		return value.Value;
	}

	public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right)
	{
		return !left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern string GetName(RtFieldInfo field);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void* _GetUtf8Name(RuntimeFieldHandleInternal field);

	internal unsafe static MdUtf8String GetUtf8Name(RuntimeFieldHandleInternal field)
	{
		return new MdUtf8String(_GetUtf8Name(field));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern FieldAttributes GetAttributes(RuntimeFieldHandleInternal field);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetApproxDeclaringType(RuntimeFieldHandleInternal field);

	internal static RuntimeType GetApproxDeclaringType(IRuntimeFieldInfo field)
	{
		RuntimeType approxDeclaringType = GetApproxDeclaringType(field.Value);
		GC.KeepAlive(field);
		return approxDeclaringType;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RtFieldInfo field);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object GetValue(RtFieldInfo field, object instance, RuntimeType fieldType, RuntimeType declaringType, ref bool domainInitialized);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern object GetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType contextType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void SetValue(RtFieldInfo field, object obj, object value, RuntimeType fieldType, FieldAttributes fieldAttr, RuntimeType declaringType, ref bool domainInitialized);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, object value, RuntimeType contextType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern LoaderAllocator GetLoaderAllocator(RuntimeFieldHandleInternal method);

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}
}
