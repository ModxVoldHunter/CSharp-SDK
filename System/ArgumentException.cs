using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentException : SystemException
{
	private readonly string _paramName;

	public override string Message
	{
		get
		{
			SetMessageField();
			string text = base.Message;
			if (!string.IsNullOrEmpty(_paramName))
			{
				text = text + " " + SR.Format(SR.Arg_ParamName_Name, _paramName);
			}
			return text;
		}
	}

	public virtual string? ParamName => _paramName;

	public ArgumentException()
		: base(SR.Arg_ArgumentException)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message)
		: base(message)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, string? paramName, Exception? innerException)
		: base(message, innerException)
	{
		_paramName = paramName;
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, string? paramName)
		: base(message)
	{
		_paramName = paramName;
		base.HResult = -2147024809;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ArgumentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_paramName = info.GetString("ParamName");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ParamName", _paramName, typeof(string));
	}

	private void SetMessageField()
	{
		if (_message == null && base.HResult == -2147024809)
		{
			_message = SR.Arg_ArgumentException;
		}
	}

	public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (string.IsNullOrEmpty(argument))
		{
			ThrowNullOrEmptyException(argument, paramName);
		}
	}

	public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (string.IsNullOrWhiteSpace(argument))
		{
			ThrowNullOrWhiteSpaceException(argument, paramName);
		}
	}

	[DoesNotReturn]
	private static void ThrowNullOrEmptyException(string argument, string paramName)
	{
		ArgumentNullException.ThrowIfNull(argument, paramName);
		throw new ArgumentException(SR.Argument_EmptyString, paramName);
	}

	[DoesNotReturn]
	private static void ThrowNullOrWhiteSpaceException(string argument, string paramName)
	{
		ArgumentNullException.ThrowIfNull(argument, paramName);
		throw new ArgumentException(SR.Argument_EmptyOrWhiteSpaceString, paramName);
	}
}
