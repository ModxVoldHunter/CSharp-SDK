using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingFieldException : MissingMemberException, ISerializable
{
	public override string Message
	{
		get
		{
			if (ClassName == null)
			{
				return base.Message;
			}
			return SR.Format(SR.MissingField_Name, ClassName, MemberName);
		}
	}

	public MissingFieldException()
		: base(SR.Arg_MissingFieldException)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? message)
		: base(message)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233071;
	}

	public MissingFieldException(string? className, string? fieldName)
	{
		ClassName = className;
		MemberName = fieldName;
		base.HResult = -2146233071;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected MissingFieldException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
