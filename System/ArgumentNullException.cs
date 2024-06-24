using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentNullException : ArgumentException
{
	public ArgumentNullException()
		: base(SR.ArgumentNull_Generic)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? paramName)
		: base(SR.ArgumentNull_Generic, paramName)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467261;
	}

	public ArgumentNullException(string? paramName, string? message)
		: base(message, paramName)
	{
		base.HResult = -2147467261;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ArgumentNullException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument == null)
		{
			Throw(paramName);
		}
	}

	[CLSCompliant(false)]
	public unsafe static void ThrowIfNull([NotNull] void* argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument == null)
		{
			Throw(paramName);
		}
	}

	internal static void ThrowIfNull(nint argument, [CallerArgumentExpression("argument")] string paramName = null)
	{
		if (argument == IntPtr.Zero)
		{
			Throw(paramName);
		}
	}

	[DoesNotReturn]
	internal static void Throw(string paramName)
	{
		throw new ArgumentNullException(paramName);
	}
}
