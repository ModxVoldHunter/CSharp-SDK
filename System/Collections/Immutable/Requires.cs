using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

internal static class Requires
{
	[DebuggerStepThrough]
	public static void NotNull<T>([NotNull] T value, string? parameterName) where T : class
	{
		if (value == null)
		{
			FailArgumentNullException(parameterName);
		}
	}

	[DebuggerStepThrough]
	public static T NotNullPassthrough<T>([NotNull] T value, string? parameterName) where T : class
	{
		NotNull(value, parameterName);
		return value;
	}

	[DebuggerStepThrough]
	public static void NotNullAllowStructs<T>([NotNull] T value, string? parameterName)
	{
		if (value == null)
		{
			FailArgumentNullException(parameterName);
		}
	}

	[DoesNotReturn]
	[DebuggerStepThrough]
	public static void FailArgumentNullException(string? parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[DebuggerStepThrough]
	public static void Range([DoesNotReturnIf(false)] bool condition, string? parameterName, string? message = null)
	{
		if (!condition)
		{
			FailRange(parameterName, message);
		}
	}

	[DoesNotReturn]
	[DebuggerStepThrough]
	public static void FailRange(string? parameterName, string? message = null)
	{
		if (string.IsNullOrEmpty(message))
		{
			throw new ArgumentOutOfRangeException(parameterName);
		}
		throw new ArgumentOutOfRangeException(parameterName, message);
	}

	[DebuggerStepThrough]
	public static void Argument([DoesNotReturnIf(false)] bool condition, string? parameterName, string? message)
	{
		if (!condition)
		{
			throw new ArgumentException(message, parameterName);
		}
	}

	[DebuggerStepThrough]
	public static void Argument([DoesNotReturnIf(false)] bool condition)
	{
		if (!condition)
		{
			throw new ArgumentException();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	[DebuggerStepThrough]
	public static void FailObjectDisposed<TDisposed>(TDisposed disposed)
	{
		throw new ObjectDisposedException(disposed.GetType().FullName);
	}
}
