using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TaskSchedulerException : Exception
{
	public TaskSchedulerException()
		: base(SR.TaskSchedulerException_ctor_DefaultMessage)
	{
	}

	public TaskSchedulerException(string? message)
		: base(message)
	{
	}

	public TaskSchedulerException(Exception? innerException)
		: base(SR.TaskSchedulerException_ctor_DefaultMessage, innerException)
	{
	}

	public TaskSchedulerException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TaskSchedulerException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
