using System.Globalization;
using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxPhase1VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
{
	private VoterBallotShim _voterBallotShim;

	internal VoterBallotShim VoterBallotShim
	{
		get
		{
			lock (this)
			{
				return _voterBallotShim;
			}
		}
		set
		{
			lock (this)
			{
				_voterBallotShim = value;
			}
		}
	}

	internal OletxPhase1VolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
		: base(realOletxTransaction)
	{
		_voterBallotShim = null;
		Phase = -1;
		OutstandingNotifications = 0;
		IncompleteDependentClones = 0;
		AlreadyVoted = false;
		CollectedVoteYes = true;
		realOletxTransaction.IncrementUndecidedEnlistments();
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
				throw TransactionException.CreateTransactionStateException(null, Guid.Empty);
			}
			IncompleteDependentClones++;
		}
	}

	internal override void DependentCloneCompleted()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			string methodname = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture) + ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
		}
		lock (this)
		{
			IncompleteDependentClones--;
		}
		if (log.IsEnabled())
		{
			string methodname2 = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted";
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}

	internal override void RollbackFromTransaction()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		bool flag = false;
		VoterBallotShim voterBallotShim = null;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			if (Phase == 1 && OutstandingNotifications > 0)
			{
				AlreadyVoted = true;
				flag = true;
				voterBallotShim = _voterBallotShim;
			}
		}
		if (flag)
		{
			try
			{
				voterBallotShim?.Vote(voteYes: false);
				Aborted();
			}
			catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
			{
				lock (this)
				{
					if (Phase == 1)
					{
						InDoubt();
					}
				}
				if (log.IsEnabled())
				{
					log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
				}
			}
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction");
		}
	}

	internal override void DecrementOutstandingNotifications(bool voteYes)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		bool flag = false;
		VoterBallotShim voterBallotShim = null;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " + OutstandingNotifications.ToString(CultureInfo.CurrentCulture) + ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			OutstandingNotifications--;
			CollectedVoteYes &= voteYes;
			if (OutstandingNotifications == 0)
			{
				if (Phase == 1 && !AlreadyVoted)
				{
					flag = true;
					AlreadyVoted = true;
					voterBallotShim = VoterBallotShim;
				}
				RealOletxTransaction.DecrementUndecidedEnlistments();
			}
		}
		try
		{
			if (flag)
			{
				if (CollectedVoteYes && !RealOletxTransaction.Doomed)
				{
					voterBallotShim?.Vote(voteYes: true);
				}
				else
				{
					voterBallotShim?.Vote(voteYes: false);
					Aborted();
				}
			}
		}
		catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
		{
			lock (this)
			{
				if (Phase == 1)
				{
					InDoubt();
				}
			}
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
			}
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications");
		}
	}

	internal override void OutcomeFromTransaction(TransactionStatus outcome)
	{
		bool flag = false;
		bool flag2 = false;
		lock (this)
		{
			if (Phase == 1 && OutstandingNotifications > 0)
			{
				switch (outcome)
				{
				case TransactionStatus.Aborted:
					flag = true;
					break;
				case TransactionStatus.InDoubt:
					flag2 = true;
					break;
				}
			}
		}
		if (flag)
		{
			Aborted();
		}
		if (flag2)
		{
			InDoubt();
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

	internal void VoteRequest()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		int num = 0;
		bool flag = false;
		lock (this)
		{
			if (log.IsEnabled())
			{
				string methodname = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, methodname);
			}
			Phase = 1;
			if (IncompleteDependentClones > 0)
			{
				flag = true;
				OutstandingNotifications = 1;
			}
			else
			{
				OutstandingNotifications = EnlistmentList.Count;
				num = EnlistmentList.Count;
				if (num == 0)
				{
					OutstandingNotifications = 1;
				}
			}
			RealOletxTransaction.TooLateForEnlistments = true;
		}
		if (flag)
		{
			DecrementOutstandingNotifications(voteYes: false);
		}
		else if (num == 0)
		{
			DecrementOutstandingNotifications(voteYes: true);
		}
		else
		{
			for (int i = 0; i < num; i++)
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
			string methodname2 = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, methodname2);
		}
	}
}
