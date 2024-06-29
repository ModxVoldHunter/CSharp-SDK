using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.StubHelpers;

internal struct AsAnyMarshaler
{
	private enum BackPropAction
	{
		None,
		Array,
		Layout,
		StringBuilderAnsi,
		StringBuilderUnicode
	}

	private readonly nint pvArrayMarshaler;

	private BackPropAction backPropAction;

	private Type layoutType;

	private CleanupWorkListElement cleanupWorkList;

	private static bool IsIn(int dwFlags)
	{
		return (dwFlags & 0x10000000) != 0;
	}

	private static bool IsOut(int dwFlags)
	{
		return (dwFlags & 0x20000000) != 0;
	}

	private static bool IsAnsi(int dwFlags)
	{
		return (dwFlags & 0xFF0000) != 0;
	}

	private static bool IsThrowOn(int dwFlags)
	{
		return (dwFlags & 0xFF00) != 0;
	}

	private static bool IsBestFit(int dwFlags)
	{
		return (dwFlags & 0xFF) != 0;
	}

	internal AsAnyMarshaler(nint pvArrayMarshaler)
	{
		this.pvArrayMarshaler = pvArrayMarshaler;
		backPropAction = BackPropAction.None;
		layoutType = null;
		cleanupWorkList = null;
	}

	private unsafe nint ConvertArrayToNative(object pManagedHome, int dwFlags)
	{
		Type elementType = pManagedHome.GetType().GetElementType();
		VarEnum varEnum;
		switch (Type.GetTypeCode(elementType))
		{
		case TypeCode.SByte:
			varEnum = VarEnum.VT_I1;
			break;
		case TypeCode.Byte:
			varEnum = VarEnum.VT_UI1;
			break;
		case TypeCode.Int16:
			varEnum = VarEnum.VT_I2;
			break;
		case TypeCode.UInt16:
			varEnum = VarEnum.VT_UI2;
			break;
		case TypeCode.Int32:
			varEnum = VarEnum.VT_I4;
			break;
		case TypeCode.UInt32:
			varEnum = VarEnum.VT_UI4;
			break;
		case TypeCode.Int64:
			varEnum = VarEnum.VT_I8;
			break;
		case TypeCode.UInt64:
			varEnum = VarEnum.VT_UI8;
			break;
		case TypeCode.Single:
			varEnum = VarEnum.VT_R4;
			break;
		case TypeCode.Double:
			varEnum = VarEnum.VT_R8;
			break;
		case TypeCode.Char:
			varEnum = (IsAnsi(dwFlags) ? ((VarEnum)253) : VarEnum.VT_UI2);
			break;
		case TypeCode.Boolean:
			varEnum = (VarEnum)254;
			break;
		case TypeCode.Object:
			if (elementType == typeof(nint))
			{
				_ = 8;
				varEnum = VarEnum.VT_I8;
				break;
			}
			if (elementType == typeof(nuint))
			{
				_ = 8;
				varEnum = VarEnum.VT_UI8;
				break;
			}
			goto default;
		default:
			throw new ArgumentException(SR.Arg_NDirectBadObject);
		}
		int num = (int)varEnum;
		if (IsBestFit(dwFlags))
		{
			num |= 0x10000;
		}
		if (IsThrowOn(dwFlags))
		{
			num |= 0x1000000;
		}
		MngdNativeArrayMarshaler.CreateMarshaler(pvArrayMarshaler, IntPtr.Zero, num, IntPtr.Zero);
		Unsafe.SkipInit(out nint result);
		nint pNativeHome = new IntPtr(&result);
		MngdNativeArrayMarshaler.ConvertSpaceToNative(pvArrayMarshaler, ref pManagedHome, pNativeHome);
		if (IsIn(dwFlags))
		{
			MngdNativeArrayMarshaler.ConvertContentsToNative(pvArrayMarshaler, ref pManagedHome, pNativeHome);
		}
		if (IsOut(dwFlags))
		{
			backPropAction = BackPropAction.Array;
		}
		return result;
	}

	private unsafe static nint ConvertStringToNative(string pManagedHome, int dwFlags)
	{
		nint num;
		if (IsAnsi(dwFlags))
		{
			num = CSTRMarshaler.ConvertToNative(dwFlags & 0xFFFF, pManagedHome, IntPtr.Zero);
		}
		else
		{
			int cb = (pManagedHome.Length + 1) * 2;
			num = Marshal.AllocCoTaskMem(cb);
			Buffer.Memmove(ref *(char*)num, ref pManagedHome.GetRawStringData(), (nuint)pManagedHome.Length + (nuint)1u);
		}
		return num;
	}

	private unsafe nint ConvertStringBuilderToNative(StringBuilder pManagedHome, int dwFlags)
	{
		int capacity = pManagedHome.Capacity;
		int length = pManagedHome.Length;
		if (length > capacity)
		{
			ThrowHelper.ThrowInvalidOperationException();
		}
		nint num2;
		if (IsAnsi(dwFlags))
		{
			StubHelpers.CheckStringLength(capacity);
			int num = checked(capacity * Marshal.SystemMaxDBCSCharSize + 4);
			num2 = Marshal.AllocCoTaskMem(num);
			byte* ptr = (byte*)num2;
			*(ptr + num - 3) = 0;
			*(ptr + num - 2) = 0;
			*(ptr + num - 1) = 0;
			if (IsIn(dwFlags))
			{
				int num3 = Marshal.StringToAnsiString(pManagedHome.ToString(), ptr, num, IsBestFit(dwFlags), IsThrowOn(dwFlags));
			}
			if (IsOut(dwFlags))
			{
				backPropAction = BackPropAction.StringBuilderAnsi;
			}
		}
		else
		{
			int num4 = checked(capacity * 2 + 4);
			num2 = Marshal.AllocCoTaskMem(num4);
			byte* ptr2 = (byte*)num2;
			*(ptr2 + num4 - 1) = 0;
			*(ptr2 + num4 - 2) = 0;
			if (IsIn(dwFlags))
			{
				pManagedHome.InternalCopy(num2, length);
				int num5 = length * 2;
				ptr2[num5] = 0;
				(ptr2 + num5)[1] = 0;
			}
			if (IsOut(dwFlags))
			{
				backPropAction = BackPropAction.StringBuilderUnicode;
			}
		}
		return num2;
	}

	private unsafe nint ConvertLayoutToNative(object pManagedHome, int dwFlags)
	{
		int cb = Marshal.SizeOfHelper(pManagedHome.GetType(), throwIfNotMarshalable: false);
		nint num = Marshal.AllocCoTaskMem(cb);
		if (IsIn(dwFlags))
		{
			StubHelpers.FmtClassUpdateNativeInternal(pManagedHome, (byte*)num, ref cleanupWorkList);
		}
		if (IsOut(dwFlags))
		{
			backPropAction = BackPropAction.Layout;
		}
		layoutType = pManagedHome.GetType();
		return num;
	}

	internal nint ConvertToNative(object pManagedHome, int dwFlags)
	{
		if (pManagedHome == null)
		{
			return IntPtr.Zero;
		}
		if (pManagedHome is ArrayWithOffset)
		{
			throw new ArgumentException(SR.Arg_MarshalAsAnyRestriction);
		}
		if (pManagedHome.GetType().IsArray)
		{
			return ConvertArrayToNative(pManagedHome, dwFlags);
		}
		if (pManagedHome is string pManagedHome2)
		{
			return ConvertStringToNative(pManagedHome2, dwFlags);
		}
		if (pManagedHome is StringBuilder pManagedHome3)
		{
			return ConvertStringBuilderToNative(pManagedHome3, dwFlags);
		}
		if (pManagedHome.GetType().IsLayoutSequential || pManagedHome.GetType().IsExplicitLayout)
		{
			return ConvertLayoutToNative(pManagedHome, dwFlags);
		}
		throw new ArgumentException(SR.Arg_NDirectBadObject);
	}

	internal unsafe void ConvertToManaged(object pManagedHome, nint pNativeHome)
	{
		switch (backPropAction)
		{
		case BackPropAction.Array:
			MngdNativeArrayMarshaler.ConvertContentsToManaged(pvArrayMarshaler, ref pManagedHome, new IntPtr(&pNativeHome));
			break;
		case BackPropAction.Layout:
			StubHelpers.FmtClassUpdateCLRInternal(pManagedHome, (byte*)pNativeHome);
			break;
		case BackPropAction.StringBuilderAnsi:
		{
			int newLength2 = ((pNativeHome != IntPtr.Zero) ? string.strlen((byte*)pNativeHome) : 0);
			((StringBuilder)pManagedHome).ReplaceBufferAnsiInternal((sbyte*)pNativeHome, newLength2);
			break;
		}
		case BackPropAction.StringBuilderUnicode:
		{
			int newLength = ((pNativeHome != IntPtr.Zero) ? string.wcslen((char*)pNativeHome) : 0);
			((StringBuilder)pManagedHome).ReplaceBufferInternal((char*)pNativeHome, newLength);
			break;
		}
		}
	}

	internal void ClearNative(nint pNativeHome)
	{
		if (pNativeHome != IntPtr.Zero)
		{
			if (layoutType != null)
			{
				Marshal.DestroyStructure(pNativeHome, layoutType);
			}
			Marshal.FreeCoTaskMem(pNativeHome);
		}
		StubHelpers.DestroyCleanupList(ref cleanupWorkList);
	}
}
