using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System;

internal sealed class ComAwareWeakReference
{
	internal sealed class ComInfo
	{
		private nint _pComWeakRef;

		private readonly long _wrapperId;

		internal object ResolveTarget()
		{
			return ComWeakRefToObject(_pComWeakRef, _wrapperId);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ComInfo FromObject(object target)
		{
			if (target == null || !PossiblyComObject(target))
			{
				return null;
			}
			return FromObjectSlow(target);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static ComInfo FromObjectSlow(object target)
		{
			long wrapperId;
			nint num = ObjectToComWeakRef(target, out wrapperId);
			if (num == 0)
			{
				return null;
			}
			try
			{
				return new ComInfo(num, wrapperId);
			}
			catch (OutOfMemoryException)
			{
				Marshal.Release(num);
				throw;
			}
		}

		private ComInfo(nint pComWeakRef, long wrapperId)
		{
			_pComWeakRef = pComWeakRef;
			_wrapperId = wrapperId;
		}

		~ComInfo()
		{
			Marshal.Release(_pComWeakRef);
			_pComWeakRef = 0;
		}
	}

	private nint _weakHandle;

	private ComInfo _comInfo;

	internal object Target => GCHandle.InternalGet(_weakHandle) ?? RehydrateTarget();

	[DllImport("QCall", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ComWeakRefToObject")]
	private static extern void ComWeakRefToObject(nint pComWeakRef, long wrapperId, ObjectHandleOnStack retRcw);

	internal static object ComWeakRefToObject(nint pComWeakRef, long wrapperId)
	{
		object o = null;
		ComWeakRefToObject(pComWeakRef, wrapperId, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool PossiblyComObject(object target)
	{
		fixed (byte* ptr = &target.GetRawData())
		{
			int num = *(int*)(ptr - 8 - 4);
			return (num & 0xC000000) == 134217728;
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool HasInteropInfo(object target);

	[LibraryImport("QCall", EntryPoint = "ObjectToComWeakRef")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static nint ObjectToComWeakRef(ObjectHandleOnStack retRcw, out long wrapperId)
	{
		Unsafe.SkipInit<long>(out wrapperId);
		nint result;
		fixed (long* _wrapperId_native = &wrapperId)
		{
			result = __PInvoke(retRcw, _wrapperId_native);
		}
		return result;
		[DllImport("QCall", EntryPoint = "ObjectToComWeakRef", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(ObjectHandleOnStack __retRcw_native, long* __wrapperId_native);
	}

	internal static nint ObjectToComWeakRef(object target, out long wrapperId)
	{
		if (HasInteropInfo(target))
		{
			return ObjectToComWeakRef(ObjectHandleOnStack.Create(ref target), out wrapperId);
		}
		wrapperId = 0L;
		return IntPtr.Zero;
	}

	private ComAwareWeakReference(nint weakHandle)
	{
		_weakHandle = weakHandle;
	}

	~ComAwareWeakReference()
	{
		GCHandle.InternalFree(_weakHandle);
		_weakHandle = 0;
	}

	private void SetTarget(object target, ComInfo comInfo)
	{
		lock (this)
		{
			GCHandle.InternalSet(_weakHandle, target);
			_comInfo = comInfo;
		}
	}

	private object RehydrateTarget()
	{
		object obj = null;
		lock (this)
		{
			if (_comInfo != null)
			{
				obj = GCHandle.InternalGet(_weakHandle);
				if (obj == null)
				{
					obj = _comInfo.ResolveTarget();
					if (obj != null)
					{
						GCHandle.InternalSet(_weakHandle, obj);
					}
				}
			}
		}
		return obj;
	}

	private static ComAwareWeakReference EnsureComAwareReference(ref nint taggedHandle)
	{
		nint num = taggedHandle;
		if ((num & 2) == 0)
		{
			ComAwareWeakReference comAwareWeakReference = new ComAwareWeakReference(taggedHandle & ~(nint)3);
			nint num2 = GCHandle.InternalAlloc(comAwareWeakReference, GCHandleType.Normal);
			nint value = num2 | 2 | (taggedHandle & 1);
			if (Interlocked.CompareExchange(ref taggedHandle, value, num) == num)
			{
				return comAwareWeakReference;
			}
			GCHandle.InternalFree(num2);
			GC.SuppressFinalize(comAwareWeakReference);
		}
		return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~(nint)3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static object GetTarget(nint taggedHandle)
	{
		return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~(nint)3)).Target;
	}

	internal static nint GetWeakHandle(nint taggedHandle)
	{
		return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~(nint)3))._weakHandle;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void SetTarget(ref nint taggedHandle, object target, ComInfo comInfo)
	{
		ComAwareWeakReference comAwareWeakReference = ((comInfo != null) ? EnsureComAwareReference(ref taggedHandle) : Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~(nint)3)));
		comAwareWeakReference.SetTarget(target, comInfo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void SetComInfoInConstructor(ref nint taggedHandle, ComInfo comInfo)
	{
		ComAwareWeakReference comAwareWeakReference = new ComAwareWeakReference(taggedHandle & ~(nint)3);
		nint num = GCHandle.InternalAlloc(comAwareWeakReference, GCHandleType.Normal);
		taggedHandle = num | 2 | (taggedHandle & 1);
		comAwareWeakReference._comInfo = comInfo;
	}
}
