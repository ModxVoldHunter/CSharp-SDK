using System.Runtime.Serialization;
using System.Threading;

namespace System.Transactions.Oletx;

[Serializable]
internal class OletxTransaction : ISerializable, IObjectReference
{
	internal RealOletxTransaction RealOletxTransaction;

	private readonly byte[] _propagationTokenForDeserialize;

	protected int Disposed;

	internal Transaction SavedLtmPromotedTransaction;

	private TransactionTraceIdentifier _traceIdentifier = TransactionTraceIdentifier.Empty;

	internal RealOletxTransaction RealTransaction => RealOletxTransaction;

	internal Guid Identifier
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Identifier");
			}
			Guid identifier = RealOletxTransaction.Identifier;
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Identifier");
			}
			return identifier;
		}
	}

	internal Guid DistributedTxId
	{
		get
		{
			Guid result = Guid.Empty;
			if (RealOletxTransaction != null && RealOletxTransaction.InternalTransaction != null)
			{
				result = RealOletxTransaction.InternalTransaction.DistributedTxId;
			}
			return result;
		}
	}

	internal TransactionStatus Status
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Status");
			}
			TransactionStatus status = RealOletxTransaction.Status;
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Status");
			}
			return status;
		}
	}

	internal Exception InnerException => RealOletxTransaction.InnerException;

	internal TransactionTraceIdentifier TransactionTraceId
	{
		get
		{
			if (_traceIdentifier == TransactionTraceIdentifier.Empty)
			{
				lock (RealOletxTransaction)
				{
					if (_traceIdentifier == TransactionTraceIdentifier.Empty)
					{
						try
						{
							TransactionTraceIdentifier traceIdentifier = new TransactionTraceIdentifier(RealOletxTransaction.Identifier.ToString(), 0);
							Thread.MemoryBarrier();
							_traceIdentifier = traceIdentifier;
						}
						catch (TransactionException exception)
						{
							TransactionsEtwProvider log = TransactionsEtwProvider.Log;
							if (log.IsEnabled())
							{
								log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
							}
						}
					}
				}
			}
			return _traceIdentifier;
		}
	}

	public virtual IsolationLevel IsolationLevel => RealOletxTransaction.TransactionIsolationLevel;

	internal OletxTransaction(RealOletxTransaction realOletxTransaction)
	{
		RealOletxTransaction = realOletxTransaction;
		RealOletxTransaction.OletxTransactionCreated();
	}

	protected OletxTransaction(SerializationInfo serializationInfo, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(serializationInfo, "serializationInfo");
		_propagationTokenForDeserialize = (byte[])serializationInfo.GetValue("OletxTransactionPropagationToken", typeof(byte[]));
		if (_propagationTokenForDeserialize.Length < 24)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "serializationInfo");
		}
		RealOletxTransaction = null;
	}

	public object GetRealObject(StreamingContext context)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "IObjectReference.GetRealObject");
		}
		if (_propagationTokenForDeserialize == null)
		{
			if (log.IsEnabled())
			{
				log.InternalError(System.SR.UnableToDeserializeTransaction);
			}
			throw TransactionException.Create(System.SR.UnableToDeserializeTransactionInternalError, null);
		}
		if (SavedLtmPromotedTransaction != null)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "IObjectReference.GetRealObject");
			}
			return SavedLtmPromotedTransaction;
		}
		Transaction transaction = (SavedLtmPromotedTransaction = TransactionInterop.GetTransactionFromTransmitterPropagationToken(_propagationTokenForDeserialize));
		if (log.IsEnabled())
		{
			log.TransactionDeserialized(transaction._internalTransaction.PromotedTransaction.TransactionTraceId);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "IObjectReference.GetRealObject");
		}
		return transaction;
	}

	internal void Dispose()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "IDisposable.Dispose");
		}
		if (Interlocked.CompareExchange(ref Disposed, 1, 0) == 0)
		{
			RealOletxTransaction.OletxTransactionDisposed();
		}
		GC.SuppressFinalize(this);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "IDisposable.Dispose");
		}
	}

	internal void Rollback()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Rollback");
			log.TransactionRollback(TraceSourceType.TraceSourceOleTx, TransactionTraceId, "Transaction");
		}
		RealOletxTransaction.Rollback();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.Rollback");
		}
	}

	internal IPromotedEnlistment EnlistVolatile(ISinglePhaseNotificationInternal singlePhaseNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistVolatile((ISinglePhaseNotificationInternal");
		}
		if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments)
		{
			throw TransactionException.Create(System.SR.TooLate, null, DistributedTxId);
		}
		IPromotedEnlistment result = RealOletxTransaction.EnlistVolatile(singlePhaseNotification, enlistmentOptions, this);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistVolatile((ISinglePhaseNotificationInternal");
		}
		return result;
	}

	internal IPromotedEnlistment EnlistVolatile(IEnlistmentNotificationInternal enlistmentNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.EnlistVolatile(IEnlistmentNotificationInternal");
		}
		if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments)
		{
			throw TransactionException.Create(System.SR.TooLate, null, DistributedTxId);
		}
		IPromotedEnlistment result = RealOletxTransaction.EnlistVolatile(enlistmentNotification, enlistmentOptions, this);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.EnlistVolatile(IEnlistmentNotificationInternal");
		}
		return result;
	}

	internal IPromotedEnlistment EnlistDurable(Guid resourceManagerIdentifier, ISinglePhaseNotificationInternal singlePhaseNotification, bool canDoSinglePhase, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.EnlistDurable(ISinglePhaseNotificationInternal)");
		}
		if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments)
		{
			throw TransactionException.Create(System.SR.TooLate, null, DistributedTxId);
		}
		OletxTransactionManager oletxTransactionManagerInstance = RealOletxTransaction.OletxTransactionManagerInstance;
		OletxResourceManager oletxResourceManager = oletxTransactionManagerInstance.FindOrRegisterResourceManager(resourceManagerIdentifier);
		OletxEnlistment result = oletxResourceManager.EnlistDurable(this, canDoSinglePhase, singlePhaseNotification, enlistmentOptions);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.EnlistDurable(ISinglePhaseNotificationInternal)");
		}
		return result;
	}

	internal OletxDependentTransaction DependentClone(bool delayCommit)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.DependentClone");
		}
		if (TransactionStatus.Aborted == Status)
		{
			throw TransactionAbortedException.Create(System.SR.TransactionAborted, RealOletxTransaction.InnerException, DistributedTxId);
		}
		if (TransactionStatus.InDoubt == Status)
		{
			throw TransactionException.Create(System.SR.TransactionIndoubt, RealOletxTransaction.InnerException, DistributedTxId);
		}
		if (Status != 0)
		{
			throw TransactionException.Create(System.SR.TransactionAlreadyOver, null, DistributedTxId);
		}
		OletxDependentTransaction result = new OletxDependentTransaction(RealOletxTransaction, delayCommit);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.DependentClone");
		}
		return result;
	}

	public void GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
	{
		ArgumentNullException.ThrowIfNull(serializationInfo, "serializationInfo");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.GetObjectData");
		}
		byte[] transmitterPropagationToken = TransactionInterop.GetTransmitterPropagationToken(this);
		serializationInfo.SetType(typeof(OletxTransaction));
		serializationInfo.AddValue("OletxTransactionPropagationToken", transmitterPropagationToken);
		if (log.IsEnabled())
		{
			log.TransactionSerialized(TransactionTraceId);
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxTransaction.GetObjectData");
		}
	}
}
