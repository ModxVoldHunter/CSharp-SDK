using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class RealOletxTransaction
{
	private readonly TransactionShim _transactionShim;

	internal Exception InnerException;

	private int _undisposedOletxTransactionCount;

	internal ArrayList Phase0EnlistVolatilementContainerList;

	internal OletxPhase1VolatileEnlistmentContainer Phase1EnlistVolatilementContainer;

	private readonly OutcomeEnlistment _outcomeEnlistment;

	private int _undecidedEnlistmentCount;

	internal int _enlistmentCount;

	private readonly DateTime _creationTime;

	private readonly DateTime _lastStateChangeTime;

	private TransactionTraceIdentifier _traceIdentifier = TransactionTraceIdentifier.Empty;

	internal OletxCommittableTransaction CommittableTransaction;

	internal OletxTransaction InternalClone;

	internal OletxTransactionManager OletxTransactionManagerInstance { get; }

	internal Guid TxGuid { get; private set; }

	internal IsolationLevel TransactionIsolationLevel { get; private set; }

	internal TransactionStatus Status { get; private set; }

	internal bool Doomed { get; private set; }

	internal bool TooLateForEnlistments { get; set; }

	internal InternalTransaction InternalTransaction { get; set; }

	internal Guid Identifier
	{
		get
		{
			if (TxGuid.Equals(Guid.Empty))
			{
				throw TransactionException.Create(System.SR.CannotGetTransactionIdentifier, null);
			}
			return TxGuid;
		}
	}

	internal Guid DistributedTxId
	{
		get
		{
			Guid result = Guid.Empty;
			if (InternalTransaction != null)
			{
				result = InternalTransaction.DistributedTxId;
			}
			return result;
		}
	}

	internal int UndecidedEnlistments => _undecidedEnlistmentCount;

	internal TransactionShim TransactionShim
	{
		get
		{
			TransactionShim transactionShim = _transactionShim;
			if (transactionShim == null)
			{
				throw TransactionException.Create(System.SR.TransactionIndoubt, null, DistributedTxId);
			}
			return transactionShim;
		}
	}

	internal TransactionTraceIdentifier TransactionTraceId
	{
		get
		{
			if (TransactionTraceIdentifier.Empty == _traceIdentifier)
			{
				lock (this)
				{
					if (_traceIdentifier == TransactionTraceIdentifier.Empty && TxGuid != Guid.Empty)
					{
						TransactionTraceIdentifier traceIdentifier = new TransactionTraceIdentifier(TxGuid.ToString(), 0);
						Thread.MemoryBarrier();
						_traceIdentifier = traceIdentifier;
					}
				}
			}
			return _traceIdentifier;
		}
	}

	internal void IncrementUndecidedEnlistments()
	{
		Interlocked.Increment(ref _undecidedEnlistmentCount);
	}

	internal void DecrementUndecidedEnlistments()
	{
		Interlocked.Decrement(ref _undecidedEnlistmentCount);
	}

	internal RealOletxTransaction(OletxTransactionManager transactionManager, TransactionShim transactionShim, OutcomeEnlistment outcomeEnlistment, Guid identifier, OletxTransactionIsolationLevel oletxIsoLevel)
	{
		bool flag = false;
		try
		{
			OletxTransactionManagerInstance = transactionManager;
			_transactionShim = transactionShim;
			_outcomeEnlistment = outcomeEnlistment;
			TxGuid = identifier;
			TransactionIsolationLevel = OletxTransactionManager.ConvertIsolationLevelFromProxyValue(oletxIsoLevel);
			Status = TransactionStatus.Active;
			_undisposedOletxTransactionCount = 0;
			Phase0EnlistVolatilementContainerList = null;
			Phase1EnlistVolatilementContainer = null;
			TooLateForEnlistments = false;
			InternalTransaction = null;
			_creationTime = DateTime.UtcNow;
			_lastStateChangeTime = _creationTime;
			InternalClone = new OletxTransaction(this);
			if (_outcomeEnlistment != null)
			{
				_outcomeEnlistment.SetRealTransaction(this);
			}
			else
			{
				Status = TransactionStatus.InDoubt;
			}
			flag = true;
		}
		finally
		{
			if (!flag && _outcomeEnlistment != null)
			{
				_outcomeEnlistment.UnregisterOutcomeCallback();
				_outcomeEnlistment = null;
			}
		}
	}

	internal OletxVolatileEnlistmentContainer AddDependentClone(bool delayCommit)
	{
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		VoterBallotShim voterBallotShim = null;
		bool flag = false;
		bool flag2 = false;
		OletxVolatileEnlistmentContainer result = null;
		OletxPhase0VolatileEnlistmentContainer oletxPhase0VolatileEnlistmentContainer = null;
		OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer = null;
		bool phase0ContainerLockAcquired = false;
		try
		{
			lock (this)
			{
				if (delayCommit)
				{
					if (Phase0EnlistVolatilementContainerList == null)
					{
						Phase0EnlistVolatilementContainerList = new ArrayList(1);
					}
					if (Phase0EnlistVolatilementContainerList.Count == 0)
					{
						oletxPhase0VolatileEnlistmentContainer = new OletxPhase0VolatileEnlistmentContainer(this);
						flag2 = true;
					}
					else
					{
						ArrayList phase0EnlistVolatilementContainerList = Phase0EnlistVolatilementContainerList;
						oletxPhase0VolatileEnlistmentContainer = phase0EnlistVolatilementContainerList[phase0EnlistVolatilementContainerList.Count - 1] as OletxPhase0VolatileEnlistmentContainer;
						if (oletxPhase0VolatileEnlistmentContainer != null)
						{
							TakeContainerLock(oletxPhase0VolatileEnlistmentContainer, ref phase0ContainerLockAcquired);
						}
						if (!oletxPhase0VolatileEnlistmentContainer.NewEnlistmentsAllowed)
						{
							ReleaseContainerLock(oletxPhase0VolatileEnlistmentContainer, ref phase0ContainerLockAcquired);
							oletxPhase0VolatileEnlistmentContainer = new OletxPhase0VolatileEnlistmentContainer(this);
							flag2 = true;
						}
						else
						{
							flag2 = false;
						}
					}
				}
				else if (Phase1EnlistVolatilementContainer == null)
				{
					oletxPhase1VolatileEnlistmentContainer = new OletxPhase1VolatileEnlistmentContainer(this);
					flag = true;
				}
				else
				{
					flag = false;
					oletxPhase1VolatileEnlistmentContainer = Phase1EnlistVolatilementContainer;
				}
				try
				{
					if (oletxPhase0VolatileEnlistmentContainer != null)
					{
						TakeContainerLock(oletxPhase0VolatileEnlistmentContainer, ref phase0ContainerLockAcquired);
					}
					if (flag2)
					{
						_transactionShim.Phase0Enlist(oletxPhase0VolatileEnlistmentContainer, out phase0EnlistmentShim);
						oletxPhase0VolatileEnlistmentContainer.Phase0EnlistmentShim = phase0EnlistmentShim;
					}
					if (flag)
					{
						OletxTransactionManagerInstance.DtcTransactionManagerLock.AcquireReaderLock(-1);
						try
						{
							_transactionShim.CreateVoter(oletxPhase1VolatileEnlistmentContainer, out voterBallotShim);
						}
						finally
						{
							OletxTransactionManagerInstance.DtcTransactionManagerLock.ReleaseReaderLock();
						}
						oletxPhase1VolatileEnlistmentContainer.VoterBallotShim = voterBallotShim;
					}
					if (delayCommit)
					{
						if (flag2)
						{
							Phase0EnlistVolatilementContainerList.Add(oletxPhase0VolatileEnlistmentContainer);
						}
						oletxPhase0VolatileEnlistmentContainer.AddDependentClone();
						result = oletxPhase0VolatileEnlistmentContainer;
					}
					else
					{
						if (flag)
						{
							Phase1EnlistVolatilementContainer = oletxPhase1VolatileEnlistmentContainer;
						}
						oletxPhase1VolatileEnlistmentContainer.AddDependentClone();
						result = oletxPhase1VolatileEnlistmentContainer;
					}
				}
				catch (COMException comException)
				{
					OletxTransactionManager.ProxyException(comException);
					throw;
				}
			}
		}
		finally
		{
			if (oletxPhase0VolatileEnlistmentContainer != null)
			{
				ReleaseContainerLock(oletxPhase0VolatileEnlistmentContainer, ref phase0ContainerLockAcquired);
			}
		}
		return result;
	}

	private static void ReleaseContainerLock(OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer, ref bool phase0ContainerLockAcquired)
	{
		if (phase0ContainerLockAcquired)
		{
			Monitor.Exit(localPhase0VolatileContainer);
			phase0ContainerLockAcquired = false;
		}
	}

	private static void TakeContainerLock(OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer, ref bool phase0ContainerLockAcquired)
	{
		if (!phase0ContainerLockAcquired)
		{
			Monitor.Enter(localPhase0VolatileContainer);
			phase0ContainerLockAcquired = true;
		}
	}

	internal IPromotedEnlistment CommonEnlistVolatile(IEnlistmentNotificationInternal enlistmentNotification, EnlistmentOptions enlistmentOptions, OletxTransaction oletxTransaction)
	{
		OletxVolatileEnlistment oletxVolatileEnlistment = null;
		bool flag = false;
		bool flag2 = false;
		OletxPhase0VolatileEnlistmentContainer oletxPhase0VolatileEnlistmentContainer = null;
		OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer = null;
		VoterBallotShim voterBallotShim = null;
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		lock (this)
		{
			oletxVolatileEnlistment = new OletxVolatileEnlistment(enlistmentNotification, enlistmentOptions, oletxTransaction);
			if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
			{
				if (Phase0EnlistVolatilementContainerList == null)
				{
					Phase0EnlistVolatilementContainerList = new ArrayList(1);
				}
				if (Phase0EnlistVolatilementContainerList.Count == 0)
				{
					oletxPhase0VolatileEnlistmentContainer = new OletxPhase0VolatileEnlistmentContainer(this);
					flag2 = true;
				}
				else
				{
					ArrayList phase0EnlistVolatilementContainerList = Phase0EnlistVolatilementContainerList;
					oletxPhase0VolatileEnlistmentContainer = phase0EnlistVolatilementContainerList[phase0EnlistVolatilementContainerList.Count - 1] as OletxPhase0VolatileEnlistmentContainer;
					if (!oletxPhase0VolatileEnlistmentContainer.NewEnlistmentsAllowed)
					{
						oletxPhase0VolatileEnlistmentContainer = new OletxPhase0VolatileEnlistmentContainer(this);
						flag2 = true;
					}
					else
					{
						flag2 = false;
					}
				}
			}
			else if (Phase1EnlistVolatilementContainer == null)
			{
				flag = true;
				oletxPhase1VolatileEnlistmentContainer = new OletxPhase1VolatileEnlistmentContainer(this);
			}
			else
			{
				flag = false;
				oletxPhase1VolatileEnlistmentContainer = Phase1EnlistVolatilementContainer;
			}
			try
			{
				if (flag2)
				{
					lock (oletxPhase0VolatileEnlistmentContainer)
					{
						_transactionShim.Phase0Enlist(oletxPhase0VolatileEnlistmentContainer, out phase0EnlistmentShim);
						oletxPhase0VolatileEnlistmentContainer.Phase0EnlistmentShim = phase0EnlistmentShim;
					}
				}
				if (flag)
				{
					_transactionShim.CreateVoter(oletxPhase1VolatileEnlistmentContainer, out voterBallotShim);
					oletxPhase1VolatileEnlistmentContainer.VoterBallotShim = voterBallotShim;
				}
				if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
				{
					oletxPhase0VolatileEnlistmentContainer.AddEnlistment(oletxVolatileEnlistment);
					if (flag2)
					{
						Phase0EnlistVolatilementContainerList.Add(oletxPhase0VolatileEnlistmentContainer);
					}
				}
				else
				{
					oletxPhase1VolatileEnlistmentContainer.AddEnlistment(oletxVolatileEnlistment);
					if (flag)
					{
						Phase1EnlistVolatilementContainer = oletxPhase1VolatileEnlistmentContainer;
					}
				}
			}
			catch (COMException comException)
			{
				OletxTransactionManager.ProxyException(comException);
				throw;
			}
		}
		return oletxVolatileEnlistment;
	}

	internal IPromotedEnlistment EnlistVolatile(ISinglePhaseNotificationInternal enlistmentNotification, EnlistmentOptions enlistmentOptions, OletxTransaction oletxTransaction)
	{
		return CommonEnlistVolatile(enlistmentNotification, enlistmentOptions, oletxTransaction);
	}

	internal IPromotedEnlistment EnlistVolatile(IEnlistmentNotificationInternal enlistmentNotification, EnlistmentOptions enlistmentOptions, OletxTransaction oletxTransaction)
	{
		return CommonEnlistVolatile(enlistmentNotification, enlistmentOptions, oletxTransaction);
	}

	internal void Commit()
	{
		try
		{
			_transactionShim.Commit();
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode == OletxHelper.XACT_E_ABORTED || ex.ErrorCode == OletxHelper.XACT_E_INDOUBT)
			{
				Interlocked.CompareExchange(ref InnerException, ex, null);
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
				}
				return;
			}
			if (ex.ErrorCode == OletxHelper.XACT_E_ALREADYINPROGRESS)
			{
				throw TransactionException.Create(System.SR.TransactionAlreadyOver, ex);
			}
			OletxTransactionManager.ProxyException(ex);
			throw;
		}
	}

	internal void Rollback()
	{
		Guid empty = Guid.Empty;
		lock (this)
		{
			if (TransactionStatus.Aborted != Status && Status != 0)
			{
				throw TransactionException.Create(System.SR.TransactionAlreadyOver, null, DistributedTxId);
			}
			if (TransactionStatus.Aborted == Status)
			{
				return;
			}
			if (_undecidedEnlistmentCount > 0)
			{
				Doomed = true;
			}
			else if (TooLateForEnlistments)
			{
				throw TransactionException.Create(System.SR.TransactionAlreadyOver, null, DistributedTxId);
			}
			if (Phase0EnlistVolatilementContainerList != null)
			{
				foreach (OletxPhase0VolatileEnlistmentContainer phase0EnlistVolatilementContainer in Phase0EnlistVolatilementContainerList)
				{
					phase0EnlistVolatilementContainer.RollbackFromTransaction();
				}
			}
			Phase1EnlistVolatilementContainer?.RollbackFromTransaction();
		}
		try
		{
			_transactionShim.Abort();
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode == OletxHelper.XACT_E_ALREADYINPROGRESS)
			{
				if (Doomed)
				{
					TransactionsEtwProvider log = TransactionsEtwProvider.Log;
					if (log.IsEnabled())
					{
						log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
					}
					return;
				}
				throw TransactionException.Create(System.SR.TransactionAlreadyOver, ex, DistributedTxId);
			}
			OletxTransactionManager.ProxyException(ex);
			throw;
		}
	}

	internal void OletxTransactionCreated()
	{
		Interlocked.Increment(ref _undisposedOletxTransactionCount);
	}

	internal void OletxTransactionDisposed()
	{
		int num = Interlocked.Decrement(ref _undisposedOletxTransactionCount);
	}

	internal void FireOutcome(TransactionStatus statusArg)
	{
		lock (this)
		{
			switch (statusArg)
			{
			case TransactionStatus.Committed:
			{
				TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
				if (log2.IsEnabled())
				{
					log2.TransactionCommitted(TraceSourceType.TraceSourceOleTx, TransactionTraceId);
				}
				Status = TransactionStatus.Committed;
				break;
			}
			case TransactionStatus.Aborted:
			{
				TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
				if (log3.IsEnabled())
				{
					log3.TransactionAborted(TraceSourceType.TraceSourceOleTx, TransactionTraceId);
				}
				Status = TransactionStatus.Aborted;
				break;
			}
			default:
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.TransactionInDoubt(TraceSourceType.TraceSourceOleTx, TransactionTraceId);
				}
				Status = TransactionStatus.InDoubt;
				break;
			}
			}
		}
		if (InternalTransaction != null)
		{
			InternalTransaction.DistributedTransactionOutcome(InternalTransaction, Status);
		}
	}

	internal void TMDown()
	{
		lock (this)
		{
			if (Phase0EnlistVolatilementContainerList != null)
			{
				foreach (OletxPhase0VolatileEnlistmentContainer phase0EnlistVolatilementContainer in Phase0EnlistVolatilementContainerList)
				{
					phase0EnlistVolatilementContainer.TMDown();
				}
			}
		}
		_outcomeEnlistment.TMDown();
	}
}
