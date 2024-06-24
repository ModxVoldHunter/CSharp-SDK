using System.Threading;

namespace System.Transactions.Oletx;

internal sealed class OletxVolatileEnlistment : OletxBaseEnlistment, IPromotedEnlistment
{
	private enum OletxVolatileEnlistmentState
	{
		Active,
		Preparing,
		Committing,
		Aborting,
		Prepared,
		Aborted,
		InDoubt,
		Done
	}

	private readonly IEnlistmentNotificationInternal _iEnlistmentNotification;

	private OletxVolatileEnlistmentState _state;

	private OletxVolatileEnlistmentContainer _container;

	internal bool EnlistDuringPrepareRequired;

	private TransactionStatus _pendingOutcome;

	InternalEnlistment IPromotedEnlistment.InternalEnlistment
	{
		get
		{
			return InternalEnlistment;
		}
		set
		{
			InternalEnlistment = value;
		}
	}

	internal OletxVolatileEnlistment(IEnlistmentNotificationInternal enlistmentNotification, EnlistmentOptions enlistmentOptions, OletxTransaction oletxTransaction)
		: base(null, oletxTransaction)
	{
		_iEnlistmentNotification = enlistmentNotification;
		EnlistDuringPrepareRequired = (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0;
		_container = null;
		_pendingOutcome = TransactionStatus.Active;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, EnlistmentType.Volatile, enlistmentOptions);
		}
	}

	internal void Prepare(OletxVolatileEnlistmentContainer container)
	{
		OletxVolatileEnlistmentState oletxVolatileEnlistmentState = OletxVolatileEnlistmentState.Active;
		IEnlistmentNotificationInternal iEnlistmentNotification;
		lock (this)
		{
			iEnlistmentNotification = _iEnlistmentNotification;
			oletxVolatileEnlistmentState = ((_state != 0) ? _state : (_state = OletxVolatileEnlistmentState.Preparing));
			_container = container;
		}
		switch (oletxVolatileEnlistmentState)
		{
		case OletxVolatileEnlistmentState.Preparing:
		{
			if (iEnlistmentNotification != null)
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Prepare);
				}
				iEnlistmentNotification.Prepare(this);
				return;
			}
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		}
		case OletxVolatileEnlistmentState.Done:
			container.DecrementOutstandingNotifications(voteYes: true);
			return;
		case OletxVolatileEnlistmentState.Prepared:
			if (EnlistDuringPrepareRequired)
			{
				container.DecrementOutstandingNotifications(voteYes: true);
				return;
			}
			break;
		}
		if ((oletxVolatileEnlistmentState == OletxVolatileEnlistmentState.Aborting || oletxVolatileEnlistmentState == OletxVolatileEnlistmentState.Aborted) ? true : false)
		{
			container.DecrementOutstandingNotifications(voteYes: false);
			return;
		}
		TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
		if (log3.IsEnabled())
		{
			log3.InternalError();
		}
		throw new InvalidOperationException(System.SR.InternalError);
	}

	internal void Commit()
	{
		OletxVolatileEnlistmentState oletxVolatileEnlistmentState = OletxVolatileEnlistmentState.Active;
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		lock (this)
		{
			if (_state == OletxVolatileEnlistmentState.Prepared)
			{
				oletxVolatileEnlistmentState = (_state = OletxVolatileEnlistmentState.Committing);
				enlistmentNotificationInternal = _iEnlistmentNotification;
			}
			else
			{
				oletxVolatileEnlistmentState = _state;
			}
		}
		if (OletxVolatileEnlistmentState.Committing == oletxVolatileEnlistmentState)
		{
			if (enlistmentNotificationInternal == null)
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Commit);
			}
			enlistmentNotificationInternal.Commit(this);
		}
		else if (oletxVolatileEnlistmentState != OletxVolatileEnlistmentState.Done)
		{
			TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
			if (log3.IsEnabled())
			{
				log3.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		}
	}

	internal void Rollback()
	{
		OletxVolatileEnlistmentState oletxVolatileEnlistmentState = OletxVolatileEnlistmentState.Active;
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		lock (this)
		{
			OletxVolatileEnlistmentState state = _state;
			if ((state == OletxVolatileEnlistmentState.Active || state == OletxVolatileEnlistmentState.Prepared) ? true : false)
			{
				oletxVolatileEnlistmentState = (_state = OletxVolatileEnlistmentState.Aborting);
				enlistmentNotificationInternal = _iEnlistmentNotification;
			}
			else
			{
				if (_state == OletxVolatileEnlistmentState.Preparing)
				{
					_pendingOutcome = TransactionStatus.Aborted;
				}
				oletxVolatileEnlistmentState = _state;
			}
		}
		switch (oletxVolatileEnlistmentState)
		{
		case OletxVolatileEnlistmentState.Aborting:
			if (enlistmentNotificationInternal != null)
			{
				TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
				if (log2.IsEnabled())
				{
					log2.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Rollback);
				}
				enlistmentNotificationInternal.Rollback(this);
			}
			break;
		default:
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		}
		case OletxVolatileEnlistmentState.Preparing:
		case OletxVolatileEnlistmentState.Done:
			break;
		}
	}

	internal void InDoubt()
	{
		OletxVolatileEnlistmentState oletxVolatileEnlistmentState = OletxVolatileEnlistmentState.Active;
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		lock (this)
		{
			if (_state == OletxVolatileEnlistmentState.Prepared)
			{
				oletxVolatileEnlistmentState = (_state = OletxVolatileEnlistmentState.InDoubt);
				enlistmentNotificationInternal = _iEnlistmentNotification;
			}
			else
			{
				if (_state == OletxVolatileEnlistmentState.Preparing)
				{
					_pendingOutcome = TransactionStatus.InDoubt;
				}
				oletxVolatileEnlistmentState = _state;
			}
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		switch (oletxVolatileEnlistmentState)
		{
		case OletxVolatileEnlistmentState.InDoubt:
			if (enlistmentNotificationInternal != null)
			{
				if (log.IsEnabled())
				{
					log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.InDoubt);
				}
				enlistmentNotificationInternal.InDoubt(this);
				break;
			}
			if (log.IsEnabled())
			{
				log.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		default:
			if (log.IsEnabled())
			{
				log.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		case OletxVolatileEnlistmentState.Preparing:
		case OletxVolatileEnlistmentState.Done:
			break;
		}
	}

	void IPromotedEnlistment.EnlistmentDone()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentDone");
			log.EnlistmentCallbackPositive(base.InternalTraceIdentifier, EnlistmentCallback.Done);
		}
		OletxVolatileEnlistmentState oletxVolatileEnlistmentState = OletxVolatileEnlistmentState.Active;
		OletxVolatileEnlistmentContainer container;
		lock (this)
		{
			oletxVolatileEnlistmentState = _state;
			container = _container;
			if (_state != 0 && _state != OletxVolatileEnlistmentState.Preparing && _state != OletxVolatileEnlistmentState.Aborting && _state != OletxVolatileEnlistmentState.Committing && _state != OletxVolatileEnlistmentState.InDoubt)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			_state = OletxVolatileEnlistmentState.Done;
		}
		if (oletxVolatileEnlistmentState == OletxVolatileEnlistmentState.Preparing)
		{
			container?.DecrementOutstandingNotifications(voteYes: true);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentDone");
		}
	}

	void IPromotedEnlistment.Prepared()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.Prepared");
			log.EnlistmentCallbackPositive(base.InternalTraceIdentifier, EnlistmentCallback.Prepared);
		}
		TransactionStatus transactionStatus = TransactionStatus.Active;
		OletxVolatileEnlistmentContainer container;
		lock (this)
		{
			if (_state != OletxVolatileEnlistmentState.Preparing)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			_state = OletxVolatileEnlistmentState.Prepared;
			transactionStatus = _pendingOutcome;
			if (_container == null)
			{
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			container = _container;
		}
		container.DecrementOutstandingNotifications(voteYes: true);
		switch (transactionStatus)
		{
		case TransactionStatus.Aborted:
			Rollback();
			break;
		case TransactionStatus.InDoubt:
			InDoubt();
			break;
		default:
			if (log.IsEnabled())
			{
				log.InternalError();
			}
			throw new InvalidOperationException(System.SR.InternalError);
		case TransactionStatus.Active:
			break;
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.Prepared");
		}
	}

	void IPromotedEnlistment.ForceRollback()
	{
		((IPromotedEnlistment)this).ForceRollback((Exception)null);
	}

	void IPromotedEnlistment.ForceRollback(Exception e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.ForceRollback");
			log.EnlistmentCallbackNegative(base.InternalTraceIdentifier, EnlistmentCallback.ForceRollback);
		}
		OletxVolatileEnlistmentContainer container;
		lock (this)
		{
			if (_state != OletxVolatileEnlistmentState.Preparing)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			_state = OletxVolatileEnlistmentState.Done;
			if (_container == null)
			{
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			container = _container;
		}
		Interlocked.CompareExchange(ref oletxTransaction.RealOletxTransaction.InnerException, e, null);
		container.DecrementOutstandingNotifications(voteYes: false);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.ForceRollback");
		}
	}

	void IPromotedEnlistment.Committed()
	{
		throw new InvalidOperationException();
	}

	void IPromotedEnlistment.Aborted()
	{
		throw new InvalidOperationException();
	}

	void IPromotedEnlistment.Aborted(Exception e)
	{
		throw new InvalidOperationException();
	}

	void IPromotedEnlistment.InDoubt()
	{
		throw new InvalidOperationException();
	}

	void IPromotedEnlistment.InDoubt(Exception e)
	{
		throw new InvalidOperationException();
	}

	byte[] IPromotedEnlistment.GetRecoveryInformation()
	{
		throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceOleTx, System.SR.VolEnlistNoRecoveryInfo, null, base.DistributedTxId);
	}
}
