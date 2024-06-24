using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingMethodException : MissingMemberException
{
	public override string Message
	{
		get
		{
			if (ClassName != null)
			{
				return SR.Format(SR.MissingMethod_Name, ClassName, MemberName);
			}
			return base.Message;
		}
	}

	public MissingMethodException()
		: base(SR.Arg_MissingMethodException)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? message)
		: base(message)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233069;
	}

	public MissingMethodException(string? className, string? methodName)
	{
		ClassName = className;
		MemberName = methodName;
		base.HResult = -2146233069;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected MissingMethodException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
