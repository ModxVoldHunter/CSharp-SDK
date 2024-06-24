using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionManagerCommunicationException : TransactionException
{
	internal new static TransactionManagerCommunicationException Create(string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.TransactionManagerCommunicationException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new TransactionManagerCommunicationException(message, innerException);
	}

	public TransactionManagerCommunicationException()
		: base(System.SR.TransactionManagerCommunicationException)
	{
	}

	public TransactionManagerCommunicationException(string? message)
		: base(message)
	{
	}

	public TransactionManagerCommunicationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TransactionManagerCommunicationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
