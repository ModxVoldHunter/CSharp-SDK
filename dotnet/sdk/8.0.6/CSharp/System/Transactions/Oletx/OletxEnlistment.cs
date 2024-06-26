using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxEnlistment : OletxBaseEnlistment, IPromotedEnlistment
{
	internal enum OletxEnlistmentState
	{
		Active,
		Phase0Preparing,
		Preparing,
		SinglePhaseCommitting,
		Prepared,
		Committing,
		Committed,
		Aborting,
		Aborted,
		InDoubt,
		Done
	}

	private Phase0EnlistmentShim _phase0Shim;

	private readonly bool _canDoSinglePhase;

	private IEnlistmentNotificationInternal _iEnlistmentNotification;

	private byte[] _proxyPrepareInfoByteArray;

	private bool _isSinglePhase;

	private readonly Guid _transactionGuid = Guid.Empty;

	private bool _fabricateRollback;

	private bool _tmWentDown;

	private bool _aborting;

	private byte[] _prepareInfoByteArray;

	internal IEnlistmentNotificationInternal EnlistmentNotification => _iEnlistmentNotification;

	internal EnlistmentShim EnlistmentShim { get; set; }

	internal Phase0EnlistmentShim Phase0EnlistmentShim
	{
		get
		{
			return _phase0Shim;
		}
		set
		{
			lock (this)
			{
				if (value != null && (_aborting || _tmWentDown))
				{
					value.Phase0Done(voteYes: false);
				}
				_phase0Shim = value;
			}
		}
	}

	internal OletxEnlistmentState State { get; set; }

	internal byte[] ProxyPrepareInfoByteArray => _proxyPrepareInfoByteArray;

	public EnlistmentTraceIdentifier EnlistmentTraceId
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentTraceId");
				log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentTraceId");
			}
			return base.InternalTraceIdentifier;
		}
	}

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

	internal OletxEnlistment(bool canDoSinglePhase, IEnlistmentNotificationInternal enlistmentNotification, Guid transactionGuid, EnlistmentOptions enlistmentOptions, OletxResourceManager oletxResourceManager, OletxTransaction oletxTransaction)
		: base(oletxResourceManager, oletxTransaction)
	{
		EnlistmentShim = null;
		_phase0Shim = null;
		_canDoSinglePhase = canDoSinglePhase;
		_iEnlistmentNotification = enlistmentNotification;
		State = OletxEnlistmentState.Active;
		_transactionGuid = transactionGuid;
		_proxyPrepareInfoByteArray = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, EnlistmentType.Durable, enlistmentOptions);
		}
		AddToEnlistmentTable();
	}

	internal OletxEnlistment(IEnlistmentNotificationInternal enlistmentNotification, OletxTransactionStatus xactStatus, byte[] prepareInfoByteArray, OletxResourceManager oletxResourceManager)
		: base(oletxResourceManager, null)
	{
		EnlistmentShim = null;
		_phase0Shim = null;
		_canDoSinglePhase = false;
		_iEnlistmentNotification = enlistmentNotification;
		State = OletxEnlistmentState.Active;
		int num = prepareInfoByteArray.Length;
		_proxyPrepareInfoByteArray = new byte[num];
		Array.Copy(prepareInfoByteArray, _proxyPrepareInfoByteArray, num);
		_transactionGuid = new Guid(_proxyPrepareInfoByteArray.AsSpan(0, 16));
		TransactionGuidString = _transactionGuid.ToString();
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		switch (xactStatus)
		{
		case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_ABORTED:
			State = OletxEnlistmentState.Aborting;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Rollback);
			}
			_iEnlistmentNotification.Rollback(this);
			break;
		case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_COMMITTED:
			State = OletxEnlistmentState.Committing;
			lock (oletxResourceManager.ReenlistList)
			{
				oletxResourceManager.ReenlistPendingList.Add(this);
			}
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Commit);
			}
			_iEnlistmentNotification.Commit(this);
			break;
		case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED:
			State = OletxEnlistmentState.Prepared;
			lock (oletxResourceManager.ReenlistList)
			{
				oletxResourceManager.ReenlistList.Add(this);
				oletxResourceManager.StartReenlistThread();
			}
			break;
		default:
			if (log.IsEnabled())
			{
				log.InternalError(System.SR.OletxEnlistmentUnexpectedTransactionStatus);
			}
			throw TransactionException.Create(System.SR.OletxEnlistmentUnexpectedTransactionStatus, null, base.DistributedTxId);
		}
		if (log.IsEnabled())
		{
			log.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, EnlistmentType.Durable, EnlistmentOptions.None);
		}
		AddToEnlistmentTable();
	}

	internal void FinishEnlistment()
	{
		lock (this)
		{
			if (EnlistmentShim == null)
			{
				OletxResourceManager.RemoveFromReenlistPending(this);
			}
			_iEnlistmentNotification = null;
			RemoveFromEnlistmentTable();
		}
	}

	internal void TMDownFromInternalRM(OletxTransactionManager oletxTm)
	{
		lock (this)
		{
			if (oletxTransaction == null || oletxTm == oletxTransaction.RealOletxTransaction.OletxTransactionManagerInstance)
			{
				_tmWentDown = true;
			}
		}
	}

	public bool PrepareRequest(bool singlePhase, byte[] prepareInfo)
	{
		OletxEnlistmentState oletxEnlistmentState = OletxEnlistmentState.Active;
		IEnlistmentNotificationInternal iEnlistmentNotification;
		EnlistmentShim enlistmentShim;
		lock (this)
		{
			if (State == OletxEnlistmentState.Active)
			{
				OletxEnlistmentState oletxEnlistmentState3 = (State = OletxEnlistmentState.Preparing);
				oletxEnlistmentState = oletxEnlistmentState3;
			}
			else
			{
				oletxEnlistmentState = State;
			}
			iEnlistmentNotification = _iEnlistmentNotification;
			enlistmentShim = EnlistmentShim;
			oletxTransaction.RealOletxTransaction.TooLateForEnlistments = true;
		}
		if (OletxEnlistmentState.Preparing == oletxEnlistmentState)
		{
			_isSinglePhase = singlePhase;
			long num = prepareInfo.Length;
			_proxyPrepareInfoByteArray = new byte[num];
			Array.Copy(prepareInfo, _proxyPrepareInfoByteArray, num);
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (_isSinglePhase && _canDoSinglePhase)
			{
				ISinglePhaseNotificationInternal singlePhaseNotificationInternal = (ISinglePhaseNotificationInternal)iEnlistmentNotification;
				State = OletxEnlistmentState.SinglePhaseCommitting;
				if (log.IsEnabled())
				{
					log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.SinglePhaseCommit);
				}
				singlePhaseNotificationInternal.SinglePhaseCommit(this);
				return true;
			}
			State = OletxEnlistmentState.Preparing;
			_prepareInfoByteArray = TransactionManager.GetRecoveryInformation(OletxResourceManager.OletxTransactionManager.CreationNodeName, prepareInfo);
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Prepare);
			}
			iEnlistmentNotification.Prepare(this);
			return false;
		}
		if (OletxEnlistmentState.Prepared == oletxEnlistmentState)
		{
			try
			{
				enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.Prepared);
				return false;
			}
			catch (COMException comException)
			{
				OletxTransactionManager.ProxyException(comException);
				throw;
			}
		}
		if (OletxEnlistmentState.Done == oletxEnlistmentState)
		{
			try
			{
				try
				{
					enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.ReadOnly);
					return true;
				}
				finally
				{
					FinishEnlistment();
				}
			}
			catch (COMException comException2)
			{
				OletxTransactionManager.ProxyException(comException2);
				throw;
			}
		}
		try
		{
			enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.Failed);
		}
		catch (COMException exception)
		{
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
			}
		}
		return true;
	}

	public void CommitRequest()
	{
		OletxEnlistmentState oletxEnlistmentState = OletxEnlistmentState.Active;
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		EnlistmentShim enlistmentShim = null;
		bool flag = false;
		lock (this)
		{
			if (OletxEnlistmentState.Prepared == State)
			{
				OletxEnlistmentState oletxEnlistmentState3 = (State = OletxEnlistmentState.Committing);
				oletxEnlistmentState = oletxEnlistmentState3;
				enlistmentNotificationInternal = _iEnlistmentNotification;
			}
			else
			{
				oletxEnlistmentState = State;
				enlistmentShim = EnlistmentShim;
				flag = true;
			}
		}
		if (enlistmentNotificationInternal != null)
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Commit);
			}
			enlistmentNotificationInternal.Commit(this);
		}
		else
		{
			if (enlistmentShim == null)
			{
				return;
			}
			try
			{
				enlistmentShim.CommitRequestDone();
			}
			catch (COMException ex)
			{
				if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
				{
					flag = true;
					TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
					if (log2.IsEnabled())
					{
						log2.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
					}
					return;
				}
				throw;
			}
			finally
			{
				if (flag)
				{
					FinishEnlistment();
				}
			}
		}
	}

	public void AbortRequest()
	{
		OletxEnlistmentState oletxEnlistmentState = OletxEnlistmentState.Active;
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		EnlistmentShim enlistmentShim = null;
		bool flag = false;
		lock (this)
		{
			OletxEnlistmentState state = State;
			if ((state == OletxEnlistmentState.Active || state == OletxEnlistmentState.Prepared) ? true : false)
			{
				state = (State = OletxEnlistmentState.Aborting);
				oletxEnlistmentState = state;
				enlistmentNotificationInternal = _iEnlistmentNotification;
			}
			else
			{
				oletxEnlistmentState = State;
				if (OletxEnlistmentState.Phase0Preparing == State)
				{
					_fabricateRollback = true;
				}
				else
				{
					flag = true;
				}
				enlistmentShim = EnlistmentShim;
			}
		}
		if (enlistmentNotificationInternal != null)
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Rollback);
			}
			enlistmentNotificationInternal.Rollback(this);
		}
		else
		{
			if (enlistmentShim == null)
			{
				return;
			}
			try
			{
				enlistmentShim.AbortRequestDone();
			}
			catch (COMException ex)
			{
				if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
				{
					flag = true;
					TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
					if (log2.IsEnabled())
					{
						log2.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
					}
					return;
				}
				throw;
			}
			finally
			{
				if (flag)
				{
					FinishEnlistment();
				}
			}
		}
	}

	public void TMDown()
	{
		lock (OletxResourceManager.ReenlistList)
		{
			lock (this)
			{
				_tmWentDown = true;
				OletxEnlistmentState state = State;
				if ((uint)(state - 4) <= 1u)
				{
					OletxResourceManager.ReenlistList.Add(this);
				}
			}
		}
	}

	public void Phase0Request(bool abortingHint)
	{
		IEnlistmentNotificationInternal enlistmentNotificationInternal = null;
		OletxEnlistmentState oletxEnlistmentState = OletxEnlistmentState.Active;
		bool flag = false;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.Phase0Request");
		}
		OletxCommittableTransaction committableTransaction = oletxTransaction.RealOletxTransaction.CommittableTransaction;
		if (committableTransaction != null && !committableTransaction.CommitCalled)
		{
			flag = true;
		}
		lock (this)
		{
			_aborting = abortingHint;
			if (State == OletxEnlistmentState.Active)
			{
				if (_aborting || flag || _tmWentDown)
				{
					if (_phase0Shim != null)
					{
						try
						{
							_phase0Shim.Phase0Done(voteYes: false);
						}
						catch (COMException exception)
						{
							if (log.IsEnabled())
							{
								log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
							}
						}
					}
				}
				else
				{
					OletxEnlistmentState oletxEnlistmentState3 = (State = OletxEnlistmentState.Phase0Preparing);
					oletxEnlistmentState = oletxEnlistmentState3;
					enlistmentNotificationInternal = _iEnlistmentNotification;
				}
			}
		}
		if (enlistmentNotificationInternal != null)
		{
			if (OletxEnlistmentState.Phase0Preparing != oletxEnlistmentState)
			{
				if (log.IsEnabled())
				{
					log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.Phase0Request");
				}
				return;
			}
			byte[] array = _transactionGuid.ToByteArray();
			byte[] array2 = OletxResourceManager.ResourceManagerIdentifier.ToByteArray();
			byte[] proxyPrepareInfoByteArray = new byte[array.Length + array2.Length];
			Thread.MemoryBarrier();
			_proxyPrepareInfoByteArray = proxyPrepareInfoByteArray;
			for (int i = 0; i < array.Length; i++)
			{
				_proxyPrepareInfoByteArray[i] = array[i];
			}
			for (int j = 0; j < array2.Length; j++)
			{
				_proxyPrepareInfoByteArray[array.Length + j] = array2[j];
			}
			_prepareInfoByteArray = TransactionManager.GetRecoveryInformation(OletxResourceManager.OletxTransactionManager.CreationNodeName, _proxyPrepareInfoByteArray);
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, base.InternalTraceIdentifier, NotificationCall.Prepare);
			}
			enlistmentNotificationInternal.Prepare(this);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.Phase0Request");
		}
	}

	public void EnlistmentDone()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentDone");
			log.EnlistmentCallbackPositive(base.InternalTraceIdentifier, EnlistmentCallback.Done);
		}
		EnlistmentShim enlistmentShim = null;
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		OletxEnlistmentState oletxEnlistmentState = OletxEnlistmentState.Active;
		bool flag;
		bool fabricateRollback;
		lock (this)
		{
			oletxEnlistmentState = State;
			if (State == OletxEnlistmentState.Active)
			{
				phase0EnlistmentShim = Phase0EnlistmentShim;
				if (phase0EnlistmentShim != null)
				{
					oletxTransaction.RealOletxTransaction.DecrementUndecidedEnlistments();
				}
				flag = false;
			}
			else if (OletxEnlistmentState.Preparing == State)
			{
				enlistmentShim = EnlistmentShim;
				flag = true;
			}
			else if (OletxEnlistmentState.Phase0Preparing == State)
			{
				phase0EnlistmentShim = Phase0EnlistmentShim;
				oletxTransaction.RealOletxTransaction.DecrementUndecidedEnlistments();
				flag = (_fabricateRollback ? true : false);
			}
			else
			{
				bool flag2;
				switch (State)
				{
				case OletxEnlistmentState.SinglePhaseCommitting:
				case OletxEnlistmentState.Committing:
				case OletxEnlistmentState.Aborting:
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				if (!flag2)
				{
					throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
				}
				enlistmentShim = EnlistmentShim;
				flag = true;
			}
			fabricateRollback = _fabricateRollback;
			State = OletxEnlistmentState.Done;
		}
		try
		{
			if (enlistmentShim != null)
			{
				if (OletxEnlistmentState.Preparing == oletxEnlistmentState)
				{
					enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.ReadOnly);
				}
				else if (OletxEnlistmentState.Committing == oletxEnlistmentState)
				{
					enlistmentShim.CommitRequestDone();
				}
				else if (OletxEnlistmentState.Aborting == oletxEnlistmentState)
				{
					if (!fabricateRollback)
					{
						enlistmentShim.AbortRequestDone();
					}
				}
				else
				{
					if (OletxEnlistmentState.SinglePhaseCommitting != oletxEnlistmentState)
					{
						throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
					}
					enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.SinglePhase);
				}
			}
			else if (phase0EnlistmentShim != null)
			{
				switch (oletxEnlistmentState)
				{
				case OletxEnlistmentState.Active:
					phase0EnlistmentShim.Unenlist();
					break;
				case OletxEnlistmentState.Phase0Preparing:
					phase0EnlistmentShim.Phase0Done(voteYes: true);
					break;
				default:
					throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
				}
			}
		}
		catch (COMException exception)
		{
			flag = true;
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
			}
		}
		finally
		{
			if (flag)
			{
				FinishEnlistment();
			}
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxEnlistment.EnlistmentDone");
		}
	}

	public void Prepared()
	{
		int s_OK = OletxHelper.S_OK;
		EnlistmentShim enlistmentShim = null;
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		bool flag = false;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.Prepared");
			log.EnlistmentCallbackPositive(base.InternalTraceIdentifier, EnlistmentCallback.Prepared);
		}
		lock (this)
		{
			if (State == OletxEnlistmentState.Preparing)
			{
				enlistmentShim = EnlistmentShim;
			}
			else
			{
				if (OletxEnlistmentState.Phase0Preparing != State)
				{
					throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
				}
				phase0EnlistmentShim = Phase0EnlistmentShim;
				if (oletxTransaction.RealOletxTransaction.Doomed || _fabricateRollback)
				{
					_fabricateRollback = true;
					flag = _fabricateRollback;
				}
			}
			State = OletxEnlistmentState.Prepared;
		}
		try
		{
			if (enlistmentShim != null)
			{
				enlistmentShim.PrepareRequestDone(OletxPrepareVoteType.Prepared);
			}
			else if (phase0EnlistmentShim != null)
			{
				oletxTransaction.RealOletxTransaction.DecrementUndecidedEnlistments();
				phase0EnlistmentShim.Phase0Done(!flag);
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				AbortRequest();
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
				if (ex.ErrorCode != OletxHelper.XACT_E_PROTOCOL)
				{
					throw;
				}
				Phase0EnlistmentShim = null;
				if (log.IsEnabled())
				{
					log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
				}
			}
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.Prepared");
		}
	}

	public void ForceRollback()
	{
		ForceRollback(null);
	}

	public void ForceRollback(Exception e)
	{
		EnlistmentShim enlistmentShim = null;
		Phase0EnlistmentShim phase0EnlistmentShim = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.ForceRollback");
			log.EnlistmentCallbackNegative(base.InternalTraceIdentifier, EnlistmentCallback.ForceRollback);
		}
		lock (this)
		{
			if (OletxEnlistmentState.Preparing == State)
			{
				enlistmentShim = EnlistmentShim;
			}
			else
			{
				if (OletxEnlistmentState.Phase0Preparing != State)
				{
					throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
				}
				phase0EnlistmentShim = Phase0EnlistmentShim;
				if (phase0EnlistmentShim != null)
				{
					oletxTransaction.RealOletxTransaction.DecrementUndecidedEnlistments();
				}
			}
			State = OletxEnlistmentState.Aborted;
		}
		Interlocked.CompareExchange(ref oletxTransaction.RealOletxTransaction.InnerException, e, null);
		try
		{
			enlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.Failed);
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode != OletxHelper.XACT_E_CONNECTION_DOWN && ex.ErrorCode != OletxHelper.XACT_E_TMNOTAVAILABLE)
			{
				throw;
			}
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
			}
		}
		finally
		{
			FinishEnlistment();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPreparingEnlistment.ForceRollback");
		}
	}

	public void Committed()
	{
		EnlistmentShim enlistmentShim = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.Committed");
			log.EnlistmentCallbackPositive(base.InternalTraceIdentifier, EnlistmentCallback.Committed);
		}
		lock (this)
		{
			if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			State = OletxEnlistmentState.Committed;
			enlistmentShim = EnlistmentShim;
		}
		try
		{
			enlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.SinglePhase);
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode != OletxHelper.XACT_E_CONNECTION_DOWN && ex.ErrorCode != OletxHelper.XACT_E_TMNOTAVAILABLE)
			{
				throw;
			}
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
			}
		}
		finally
		{
			FinishEnlistment();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.Committed");
		}
	}

	public void Aborted()
	{
		Aborted(null);
	}

	public void Aborted(Exception e)
	{
		EnlistmentShim enlistmentShim = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.Aborted");
			log.EnlistmentCallbackNegative(base.InternalTraceIdentifier, EnlistmentCallback.Aborted);
		}
		lock (this)
		{
			if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			State = OletxEnlistmentState.Aborted;
			enlistmentShim = EnlistmentShim;
		}
		Interlocked.CompareExchange(ref oletxTransaction.RealOletxTransaction.InnerException, e, null);
		try
		{
			enlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.Failed);
		}
		catch (COMException ex) when ((ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && log.IsEnabled())
		{
			log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
		}
		finally
		{
			FinishEnlistment();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.Aborted");
		}
	}

	public void InDoubt()
	{
		InDoubt(null);
	}

	public void InDoubt(Exception e)
	{
		EnlistmentShim enlistmentShim = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.InDoubt");
			log.EnlistmentCallbackNegative(base.InternalTraceIdentifier, EnlistmentCallback.InDoubt);
		}
		lock (this)
		{
			if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
			{
				throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
			}
			State = OletxEnlistmentState.InDoubt;
			enlistmentShim = EnlistmentShim;
		}
		lock (oletxTransaction.RealOletxTransaction)
		{
			RealOletxTransaction realOletxTransaction = oletxTransaction.RealOletxTransaction;
			if (realOletxTransaction.InnerException == null)
			{
				realOletxTransaction.InnerException = e;
			}
		}
		try
		{
			enlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.InDoubt);
		}
		catch (COMException ex) when ((ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && log.IsEnabled())
		{
			log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
		}
		finally
		{
			FinishEnlistment();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxSinglePhaseEnlistment.InDoubt");
		}
	}

	public byte[] GetRecoveryInformation()
	{
		if (_prepareInfoByteArray == null)
		{
			throw TransactionException.CreateEnlistmentStateException(null, base.DistributedTxId);
		}
		return _prepareInfoByteArray;
	}
}
