using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Diagnostics.Contracts;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ContractException : Exception
{
	private readonly ContractFailureKind _kind;

	private readonly string _userMessage;

	private readonly string _condition;

	public ContractFailureKind Kind => _kind;

	public string Failure => Message;

	public string? UserMessage => _userMessage;

	public string? Condition => _condition;

	private ContractException()
	{
		base.HResult = -2146233022;
	}

	public ContractException(ContractFailureKind kind, string? failure, string? userMessage, string? condition, Exception? innerException)
		: base(failure, innerException)
	{
		base.HResult = -2146233022;
		_kind = kind;
		_userMessage = userMessage;
		_condition = condition;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private ContractException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_kind = (ContractFailureKind)info.GetInt32("Kind");
		_userMessage = info.GetString("UserMessage");
		_condition = info.GetString("Condition");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("Kind", _kind);
		info.AddValue("UserMessage", _userMessage);
		info.AddValue("Condition", _condition);
	}
}
