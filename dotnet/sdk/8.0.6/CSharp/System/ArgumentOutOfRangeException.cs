using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentOutOfRangeException : ArgumentException
{
	private readonly object _actualValue;

	public override string Message
	{
		get
		{
			string message = base.Message;
			if (_actualValue != null)
			{
				string text = SR.Format(SR.ArgumentOutOfRange_ActualValue, _actualValue);
				if (message == null)
				{
					return text;
				}
				return message + "\r\n" + text;
			}
			return message;
		}
	}

	public virtual object? ActualValue => _actualValue;

	public ArgumentOutOfRangeException()
		: base(SR.Arg_ArgumentOutOfRangeException)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName)
		: base(SR.Arg_ArgumentOutOfRangeException, paramName)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName, string? message)
		: base(message, paramName)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233086;
	}

	public ArgumentOutOfRangeException(string? paramName, object? actualValue, string? message)
		: base(message, paramName)
	{
		_actualValue = actualValue;
		base.HResult = -2146233086;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_actualValue = info.GetValue("ActualValue", typeof(object));
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ActualValue", _actualValue, typeof(object));
	}

	[DoesNotReturn]
	private static void ThrowZero<T>(T value, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonZero, paramName, value));
	}

	[DoesNotReturn]
	private static void ThrowNegative<T>(T value, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegative, paramName, value));
	}

	[DoesNotReturn]
	private static void ThrowNegativeOrZero<T>(T value, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegativeNonZero, paramName, value));
	}

	[DoesNotReturn]
	private static void ThrowGreater<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLessOrEqual, paramName, value, other));
	}

	[DoesNotReturn]
	private static void ThrowGreaterEqual<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLess, paramName, value, other));
	}

	[DoesNotReturn]
	private static void ThrowLess<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, paramName, value, other));
	}

	[DoesNotReturn]
	private static void ThrowLessEqual<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreater, paramName, value, other));
	}

	[DoesNotReturn]
	private static void ThrowEqual<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNotEqual, paramName, ((object)value) ?? "null", ((object)other) ?? "null"));
	}

	[DoesNotReturn]
	private static void ThrowNotEqual<T>(T value, T other, string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeEqual, paramName, ((object)value) ?? "null", ((object)other) ?? "null"));
	}

	public static void ThrowIfZero<T>(T value, [CallerArgumentExpression("value")] string? paramName = null) where T : INumberBase<T>
	{
		if (T.IsZero(value))
		{
			ThrowZero(value, paramName);
		}
	}

	public static void ThrowIfNegative<T>(T value, [CallerArgumentExpression("value")] string? paramName = null) where T : INumberBase<T>
	{
		if (T.IsNegative(value))
		{
			ThrowNegative(value, paramName);
		}
	}

	public static void ThrowIfNegativeOrZero<T>(T value, [CallerArgumentExpression("value")] string? paramName = null) where T : INumberBase<T>
	{
		if (T.IsNegative(value) || T.IsZero(value))
		{
			ThrowNegativeOrZero(value, paramName);
		}
	}

	public static void ThrowIfEqual<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IEquatable<T>?
	{
		if (EqualityComparer<T>.Default.Equals(value, other))
		{
			ThrowEqual<T>(value, other, paramName);
		}
	}

	public static void ThrowIfNotEqual<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IEquatable<T>?
	{
		if (!EqualityComparer<T>.Default.Equals(value, other))
		{
			ThrowNotEqual<T>(value, other, paramName);
		}
	}

	public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
	{
		if (value.CompareTo(other) > 0)
		{
			ThrowGreater<T>(value, other, paramName);
		}
	}

	public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
	{
		if (value.CompareTo(other) >= 0)
		{
			ThrowGreaterEqual<T>(value, other, paramName);
		}
	}

	public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
	{
		if (value.CompareTo(other) < 0)
		{
			ThrowLess<T>(value, other, paramName);
		}
	}

	public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null) where T : IComparable<T>
	{
		if (value.CompareTo(other) <= 0)
		{
			ThrowLessEqual<T>(value, other, paramName);
		}
	}
}
