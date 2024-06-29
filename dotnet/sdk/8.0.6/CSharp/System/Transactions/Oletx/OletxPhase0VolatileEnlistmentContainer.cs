using System.Globalization;
using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxPhase0VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
{
	private Phase0EnlistmentShim _phase0EnlistmentShim;

	private bool _aborting;

	private bool _tmWentDown;

	internal bool NewEnlistmentsAllowed => Phase == -1;

	internal Phase0EnlistmentShim Phase0EnlistmentShim
	{
		get
		{
			lock (this)
			{
				return _phase0EnlistmentShim;
			}
		}
		set
		{
			lock (this)
			{
				if (_aborting || _tmWentDown)
				{
					value.Phase0Done(voteYes: false);
				}
				_phase0EnlistmentShim = value;
			}
		}
	}

	internal OletxPhase0VolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
		: base(realOletxTransaction)
	{
		_phase0EnlistmentShim = null;
		Phase = -1;
		_aborting = false;
		_tmWentDown = false;
		OutstandingNotifications = 0;
		IncompleteDependentClones = 0;
		AlreadyVoted = false;
		CollectedVoteYes = true;
		realOletxTransaction.IncrementUndecidedEnlistments();
	}

	internal void TMDown()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxPhase0VolatileEnlistmentContainer.TMDown");
		}
		_tmWentDown = true;
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPhase0VolatileEnlistmentContainer.TMDown");
		}
	}

	internal void AddEnlistment(OletxVolatileEnlistment enlistment)
	{
		lock (this)
		{
			if (Phase != -1)
			{
				throw TransactionException.Create(System.SR.TooLate, null);
			}
			EnlistmentList.Add(enlistment);
		}
	}

	internal override void AddDependentClone()
	{
		lock (this)
		{
			if (Phase != -1)
			{
				throw TransactionException.CreateTransactionStateException(null);
			}
			IncompleteDependentClones++;
		}
	}

	internal override void DependentCloneCompleted()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		bool flag = false;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture) + ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			IncompleteDependentClones--;
			if (IncompleteDependentClones == 0 && Phase == 0)
			{
				OutstandingNotifications++;
				flag = true;
			}
		}
		if (flag)
		{
			DecrementOutstandingNotifications(voteYes: true);
		}
		if (log.IsEnabled())
		{
			string methodname2 = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted";
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}

	internal override void RollbackFromTransaction()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			if (Phase == 0 && (OutstandingNotifications > 0 || IncompleteDependentClones > 0))
			{
				AlreadyVoted = true;
				Phase0EnlistmentShim?.Phase0Done(voteYes: false);
			}
		}
		if (log.IsEnabled())
		{
			string methodname2 = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction";
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}

	internal override void DecrementOutstandingNotifications(bool voteYes)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		bool flag = false;
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			OutstandingNotifications--;
			CollectedVoteYes &= voteYes;
			if (OutstandingNotifications == 0 && IncompleteDependentClones == 0)
			{
				if (Phase == 0 && !AlreadyVoted)
				{
					flag = true;
					AlreadyVoted = true;
					phase0EnlistmentShim = _phase0EnlistmentShim;
				}
				RealOletxTransaction.DecrementUndecidedEnlistments();
			}
		}
		try
		{
			if (flag)
			{
				phase0EnlistmentShim?.Phase0Done(CollectedVoteYes && !RealOletxTransaction.Doomed);
			}
		}
		catch (COMException ex)
		{
			if ((ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
			}
			else
			{
				if (OletxHelper.XACT_E_PROTOCOL != ex.ErrorCode)
				{
					throw;
				}
				_phase0EnlistmentShim = null;
				if (log.IsEnabled())
				{
					log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
				}
			}
		}
		if (log.IsEnabled())
		{
			string methodname2 = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications";
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}

	internal override void OutcomeFromTransaction(TransactionStatus outcome)
	{
		switch (outcome)
		{
		case TransactionStatus.Committed:
			Committed();
			break;
		case TransactionStatus.Aborted:
			Aborted();
			break;
		case TransactionStatus.InDoubt:
			InDoubt();
			break;
		}
	}

	internal override void Committed()
	{
		int count;
		lock (this)
		{
			Phase = 2;
			count = EnlistmentList.Count;
		}
		for (int i = 0; i < count; i++)
		{
			if (!(EnlistmentList[i] is OletxVolatileEnlistment oletxVolatileEnlistment))
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			oletxVolatileEnlistment.Commit();
		}
	}

	internal override void Aborted()
	{
		int count;
		lock (this)
		{
			Phase = 2;
			count = EnlistmentList.Count;
		}
		for (int i = 0; i < count; i++)
		{
			if (!(EnlistmentList[i] is OletxVolatileEnlistment oletxVolatileEnlistment))
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			oletxVolatileEnlistment.Rollback();
		}
	}

	internal override void InDoubt()
	{
		int count;
		lock (this)
		{
			Phase = 2;
			count = EnlistmentList.Count;
		}
		for (int i = 0; i < count; i++)
		{
			if (!(EnlistmentList[i] is OletxVolatileEnlistment oletxVolatileEnlistment))
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.InternalError();
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			oletxVolatileEnlistment.InDoubt();
		}
	}

	internal void Phase0Request(bool abortHint)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		bool flag = false;
		int count;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " + abortHint.ToString(CultureInfo.CurrentCulture) + ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			_aborting = abortHint;
			OletxCommittableTransaction committableTransaction = RealOletxTransaction.CommittableTransaction;
			if (committableTransaction != null && !committableTransaction.CommitCalled)
			{
				flag = true;
				_aborting = true;
			}
			if (Phase != 2 && Phase != -1)
			{
				if (log.IsEnabled())
				{
					log.InternalError("OletxPhase0VolatileEnlistmentContainer.Phase0Request, phase != -1");
				}
				throw new InvalidOperationException(System.SR.InternalError);
			}
			if (Phase == -1)
			{
				Phase = 0;
			}
			if (_aborting || _tmWentDown || flag || Phase == 2)
			{
				if (_phase0EnlistmentShim == null)
				{
					return;
				}
				try
				{
					_phase0EnlistmentShim.Phase0Done(voteYes: false);
					AlreadyVoted = true;
					return;
				}
				catch (COMException exception)
				{
					if (log.IsEnabled())
					{
						log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
					}
					return;
				}
			}
			OutstandingNotifications = EnlistmentList.Count;
			count = EnlistmentList.Count;
			if (count == 0)
			{
				OutstandingNotifications = 1;
			}
		}
		if (count == 0)
		{
			DecrementOutstandingNotifications(voteYes: true);
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				if (!(EnlistmentList[i] is OletxVolatileEnlistment oletxVolatileEnlistment))
				{
					if (log.IsEnabled())
					{
						log.InternalError();
					}
					throw new InvalidOperationException(System.SR.InternalError);
				}
				oletxVolatileEnlistment.Prepare(this);
			}
		}
		if (log.IsEnabled())
		{
			string methodname2 = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " + abortHint.ToString(CultureInfo.CurrentCulture);
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}
}
