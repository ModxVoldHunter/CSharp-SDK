using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections;

internal static class ThrowHelper
{
	public static void ThrowIfNull(object arg, [CallerArgumentExpression("arg")] string? paramName = null)
	{
		if (arg == null)
		{
			ThrowArgumentNullException(paramName);
		}
	}

	[DoesNotReturn]
	public static void ThrowIfDestinationTooSmall()
	{
		throw new ArgumentException(System.SR.CapacityMustBeGreaterThanOrEqualToCount, "destination");
	}

	[DoesNotReturn]
	public static void ThrowArgumentNullException(string? paramName)
	{
		throw new ArgumentNullException(paramName);
	}

	[DoesNotReturn]
	public static void ThrowKeyNotFoundException()
	{
		throw new KeyNotFoundException();
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException()
	{
		throw new InvalidOperationException();
	}
}
