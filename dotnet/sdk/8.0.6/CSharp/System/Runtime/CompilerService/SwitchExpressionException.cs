using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices;

[Serializable]
[TypeForwardedFrom("System.Runtime.Extensions, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public sealed class SwitchExpressionException : InvalidOperationException
{
	public object? UnmatchedValue { get; }

	public override string Message
	{
		get
		{
			if (UnmatchedValue == null)
			{
				return base.Message;
			}
			string text = SR.Format(SR.SwitchExpressionException_UnmatchedValue, UnmatchedValue);
			return base.Message + "\r\n" + text;
		}
	}

	public SwitchExpressionException()
		: base(SR.Arg_SwitchExpressionException)
	{
	}

	public SwitchExpressionException(Exception? innerException)
		: base(SR.Arg_SwitchExpressionException, innerException)
	{
	}

	public SwitchExpressionException(object? unmatchedValue)
		: this()
	{
		UnmatchedValue = unmatchedValue;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private SwitchExpressionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		UnmatchedValue = info.GetValue("UnmatchedValue", typeof(object));
	}

	public SwitchExpressionException(string? message)
		: base(message)
	{
	}

	public SwitchExpressionException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("UnmatchedValue", UnmatchedValue, typeof(object));
	}
}
