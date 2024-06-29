using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics;

internal static class VectorMath
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> ConditionalSelectBitwise(Vector128<float> selector, Vector128<float> ifTrue, Vector128<float> ifFalse)
	{
		if (false)
		{
		}
		if (Sse.IsSupported)
		{
			return Sse.Or(Sse.And(ifTrue, selector), Sse.AndNot(selector, ifFalse));
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<double> ConditionalSelectBitwise(Vector128<double> selector, Vector128<double> ifTrue, Vector128<double> ifFalse)
	{
		if (false)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.Or(Sse2.And(ifTrue, selector), Sse2.AndNot(selector, ifFalse));
		}
		throw new PlatformNotSupportedException();
	}
}
