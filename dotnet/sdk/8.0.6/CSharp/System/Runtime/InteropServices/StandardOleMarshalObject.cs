namespace System.Runtime.InteropServices;

public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
{
	private static readonly Guid CLSID_StdMarshal = new Guid("00000017-0000-0000-c000-000000000046");

	protected StandardOleMarshalObject()
	{
	}

	private nint GetStdMarshaler(ref Guid riid, int dwDestContext, int mshlflags)
	{
		nint iUnknownForObject = Marshal.GetIUnknownForObject(this);
		if (iUnknownForObject != IntPtr.Zero)
		{
			try
			{
				nint ppMarshal = IntPtr.Zero;
				if (Interop.Ole32.CoGetStandardMarshal(ref riid, iUnknownForObject, dwDestContext, IntPtr.Zero, mshlflags, out ppMarshal) == 0)
				{
					return ppMarshal;
				}
			}
			finally
			{
				Marshal.Release(iUnknownForObject);
			}
		}
		throw new InvalidOperationException(SR.Format(SR.StandardOleMarshalObjectGetMarshalerFailed, riid));
	}

	int IMarshal.GetUnmarshalClass(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out Guid pCid)
	{
		pCid = CLSID_StdMarshal;
		return 0;
	}

	unsafe int IMarshal.GetMarshalSizeMax(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out int pSize)
	{
		nint stdMarshaler = GetStdMarshaler(ref riid, dwDestContext, mshlflags);
		try
		{
			fixed (Guid* ptr = &riid)
			{
				fixed (int* ptr2 = &pSize)
				{
					return ((delegate* unmanaged[Stdcall]<nint, Guid*, nint, int, nint, int, int*, int>)(*(IntPtr*)((nint)(*(IntPtr*)stdMarshaler) + (nint)4 * (nint)8)))(stdMarshaler, ptr, pv, dwDestContext, pvDestContext, mshlflags, ptr2);
				}
			}
		}
		finally
		{
			Marshal.Release(stdMarshaler);
		}
	}

	unsafe int IMarshal.MarshalInterface(nint pStm, ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags)
	{
		nint stdMarshaler = GetStdMarshaler(ref riid, dwDestContext, mshlflags);
		try
		{
			fixed (Guid* ptr = &riid)
			{
				return ((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint, int, nint, int, int>)(*(IntPtr*)((nint)(*(IntPtr*)stdMarshaler) + (nint)5 * (nint)8)))(stdMarshaler, pStm, ptr, pv, dwDestContext, pvDestContext, mshlflags);
			}
		}
		finally
		{
			Marshal.Release(stdMarshaler);
		}
	}

	int IMarshal.UnmarshalInterface(nint pStm, ref Guid riid, out nint ppv)
	{
		ppv = IntPtr.Zero;
		return -2147467263;
	}

	int IMarshal.ReleaseMarshalData(nint pStm)
	{
		return -2147467263;
	}

	int IMarshal.DisconnectObject(int dwReserved)
	{
		return -2147467263;
	}
}
