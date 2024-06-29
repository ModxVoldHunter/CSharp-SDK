using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionInDoubtException : TransactionException
{
	internal static TransactionInDoubtException Create(TraceSourceType traceSource, string message, Exception innerException, Guid distributedTxId)
	{
		string text = message;
		if (TransactionException.IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		return Create(traceSource, text, innerException);
	}

	internal static TransactionInDoubtException Create(TraceSourceType traceSource, string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(traceSource, TransactionExceptionType.TransactionInDoubtException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new TransactionInDoubtException(message, innerException);
	}

	public TransactionInDoubtException()
		: base(System.SR.TransactionIndoubt)
	{
	}

	public TransactionInDoubtException(string? message)
		: base(message)
	{
	}

	public TransactionInDoubtException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TransactionInDoubtException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
