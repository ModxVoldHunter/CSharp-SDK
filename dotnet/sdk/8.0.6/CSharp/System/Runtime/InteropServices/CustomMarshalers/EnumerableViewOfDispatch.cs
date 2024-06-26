using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class EnumerableViewOfDispatch : ICustomAdapter, System.Collections.IEnumerable
{
	private const int DISPID_NEWENUM = -4;

	private const int LCID_DEFAULT = 1;

	private readonly object _dispatch;

	private IDispatch Dispatch => (IDispatch)_dispatch;

	public EnumerableViewOfDispatch(object dispatch)
	{
		_dispatch = dispatch;
	}

	public unsafe System.Collections.IEnumerator GetEnumerator()
	{
		Unsafe.SkipInit(out Variant variant);
		void* value = &variant;
		DISPPARAMS pDispParams = default(DISPPARAMS);
		Guid riid = Guid.Empty;
		Dispatch.Invoke(-4, ref riid, 1, InvokeFlags.DISPATCH_METHOD | InvokeFlags.DISPATCH_PROPERTYGET, ref pDispParams, new IntPtr(value), IntPtr.Zero, IntPtr.Zero);
		nint num = IntPtr.Zero;
		try
		{
			object obj = variant.ToObject();
			if (!(obj is IEnumVARIANT o))
			{
				throw new InvalidOperationException(SR.InvalidOp_InvalidNewEnumVariant);
			}
			num = Marshal.GetIUnknownForObject(o);
			return (System.Collections.IEnumerator)EnumeratorToEnumVariantMarshaler.GetInstance(null).MarshalNativeToManaged(num);
		}
		finally
		{
			variant.Clear();
			if (num != IntPtr.Zero)
			{
				Marshal.Release(num);
			}
		}
	}

	public object GetUnderlyingObject()
	{
		return _dispatch;
	}
}
