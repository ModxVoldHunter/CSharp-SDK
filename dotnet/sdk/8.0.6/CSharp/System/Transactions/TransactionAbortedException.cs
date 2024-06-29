using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionAbortedException : TransactionException
{
	internal new static TransactionAbortedException Create(string message, Exception innerException, Guid distributedTxId)
	{
		string text = message;
		if (TransactionException.IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		return Create(text, innerException);
	}

	internal new static TransactionAbortedException Create(string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.TransactionAbortedException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new TransactionAbortedException(message, innerException);
	}

	public TransactionAbortedException()
		: base(System.SR.TransactionAborted)
	{
	}

	public TransactionAbortedException(string? message)
		: base(message)
	{
	}

	public TransactionAbortedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	internal TransactionAbortedException(Exception innerException, Guid distributedTxId)
		: base(TransactionException.IncludeDistributedTxId(distributedTxId) ? System.SR.Format(System.SR.DistributedTxIDInTransactionException, System.SR.TransactionAborted, distributedTxId) : System.SR.TransactionAborted, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected TransactionAbortedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
