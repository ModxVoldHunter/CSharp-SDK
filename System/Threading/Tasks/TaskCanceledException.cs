using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TaskCanceledException : OperationCanceledException
{
	[NonSerialized]
	private readonly Task _canceledTask;

	public Task? Task => _canceledTask;

	public TaskCanceledException()
		: base(SR.TaskCanceledException_ctor_DefaultMessage)
	{
	}

	public TaskCanceledException(string? message)
		: base(message)
	{
	}

	public TaskCanceledException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public TaskCanceledException(string? message, Exception? innerException, CancellationToken token)
		: base(message, innerException, token)
	{
	}

	public TaskCanceledException(Task? task)
		: base(SR.TaskCanceledException_ctor_DefaultMessage, task?.CancellationToken ?? CancellationToken.None)
	{
		_canceledTask = task;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TaskCanceledException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
