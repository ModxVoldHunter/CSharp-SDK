using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class X86Base
{
	[Intrinsic]
	public abstract class X64
	{
		public static bool IsSupported => IsSupported;

		internal static ulong BitScanForward(ulong value)
		{
			return BitScanForward(value);
		}

		internal static ulong BitScanReverse(ulong value)
		{
			return BitScanReverse(value);
		}

		[RequiresPreviewFeatures("DivRem is in preview.")]
		public static (ulong Quotient, ulong Remainder) DivRem(ulong lower, ulong upper, ulong divisor)
		{
			return DivRem(lower, upper, divisor);
		}

		[RequiresPreviewFeatures("DivRem is in preview.")]
		public static (long Quotient, long Remainder) DivRem(ulong lower, long upper, long divisor)
		{
			return DivRem(lower, upper, divisor);
		}
	}

	public static bool IsSupported => IsSupported;

	[DllImport("QCall", EntryPoint = "X86BaseCpuId", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "X86BaseCpuId")]
	private unsafe static extern void __cpuidex(int* cpuInfo, int functionId, int subFunctionId);

	internal static uint BitScanForward(uint value)
	{
		return BitScanForward(value);
	}

	internal static uint BitScanReverse(uint value)
	{
		return BitScanReverse(value);
	}

	public unsafe static (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId)
	{
		int* ptr = stackalloc int[4];
		__cpuidex(ptr, functionId, subFunctionId);
		return (Eax: *ptr, Ebx: ptr[1], Ecx: ptr[2], Edx: ptr[3]);
	}

	[RequiresPreviewFeatures("DivRem is in preview.")]
	public static (uint Quotient, uint Remainder) DivRem(uint lower, uint upper, uint divisor)
	{
		return DivRem(lower, upper, divisor);
	}

	[RequiresPreviewFeatures("DivRem is in preview.")]
	public static (int Quotient, int Remainder) DivRem(uint lower, int upper, int divisor)
	{
		return DivRem(lower, upper, divisor);
	}

	[RequiresPreviewFeatures("DivRem is in preview.")]
	public static (nuint Quotient, nuint Remainder) DivRem(nuint lower, nuint upper, nuint divisor)
	{
		return DivRem(lower, upper, divisor);
	}

	[RequiresPreviewFeatures("DivRem is in preview.")]
	public static (nint Quotient, nint Remainder) DivRem(nuint lower, nint upper, nint divisor)
	{
		return DivRem(lower, upper, divisor);
	}

	public static void Pause()
	{
		Pause();
	}
}
