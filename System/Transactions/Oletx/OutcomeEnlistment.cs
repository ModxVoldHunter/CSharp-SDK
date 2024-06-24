using System.Runtime.CompilerServices;

namespace System.Transactions.Oletx;

internal sealed class OutcomeEnlistment
{
	private WeakReference _weakRealTransaction;

	[CompilerGenerated]
	private Guid _003CTransactionIdentifier_003Ek__BackingField;

	private bool _haveIssuedOutcome;

	private TransactionStatus _savedStatus;

	private Guid TransactionIdentifier
	{
		[CompilerGenerated]
		set
		{
			_003CTransactionIdentifier_003Ek__BackingField = value;
		}
	}

	internal OutcomeEnlistment()
	{
		_haveIssuedOutcome = false;
		_savedStatus = TransactionStatus.InDoubt;
	}

	internal void SetRealTransaction(RealOletxTransaction realTx)
	{
		bool flag = false;
		TransactionStatus transactionStatus = TransactionStatus.InDoubt;
		lock (this)
		{
			flag = _haveIssuedOutcome;
			transactionStatus = _savedStatus;
			if (!flag)
			{
				_weakRealTransaction = new WeakReference(realTx);
				TransactionIdentifier = realTx.TxGuid;
			}
		}
		if (flag)
		{
			realTx.FireOutcome(transactionStatus);
			bool flag2 = (uint)(transactionStatus - 2) <= 1u;
			if (flag2 && realTx.Phase1EnlistVolatilementContainer != null)
			{
				realTx.Phase1EnlistVolatilementContainer.OutcomeFromTransaction(transactionStatus);
			}
		}
	}

	internal void UnregisterOutcomeCallback()
	{
		_weakRealTransaction = null;
	}

	private void InvokeOutcomeFunction(TransactionStatus status)
	{
		WeakReference weakRealTransaction;
		lock (this)
		{
			if (_haveIssuedOutcome)
			{
				return;
			}
			_haveIssuedOutcome = true;
			_savedStatus = status;
			weakRealTransaction = _weakRealTransaction;
		}
		if (weakRealTransaction == null)
		{
			return;
		}
		if (weakRealTransaction.Target is RealOletxTransaction realOletxTransaction)
		{
			realOletxTransaction.FireOutcome(status);
			if (realOletxTransaction.Phase0EnlistVolatilementContainerList != null)
			{
				foreach (OletxPhase0VolatileEnlistmentContainer phase0EnlistVolatilementContainer in realOletxTransaction.Phase0EnlistVolatilementContainerList)
				{
					phase0EnlistVolatilementContainer.OutcomeFromTransaction(status);
				}
			}
			bool flag = (uint)(status - 2) <= 1u;
			if (flag && realOletxTransaction.Phase1EnlistVolatilementContainer != null)
			{
				realOletxTransaction.Phase1EnlistVolatilementContainer.OutcomeFromTransaction(status);
			}
		}
		weakRealTransaction.Target = null;
	}

	internal static bool TransactionIsInDoubt(RealOletxTransaction realTx)
	{
		OletxCommittableTransaction committableTransaction = realTx.CommittableTransaction;
		if (committableTransaction != null && !committableTransaction.CommitCalled)
		{
			return false;
		}
		return realTx.UndecidedEnlistments == 0;
	}

	internal void TMDown()
	{
		bool flag = true;
		RealOletxTransaction realOletxTransaction = null;
		lock (this)
		{
			if (_weakRealTransaction != null)
			{
				realOletxTransaction = _weakRealTransaction.Target as RealOletxTransaction;
			}
		}
		if (realOletxTransaction != null)
		{
			lock (realOletxTransaction)
			{
				flag = TransactionIsInDoubt(realOletxTransaction);
			}
		}
		if (flag)
		{
			InDoubt();
		}
		else
		{
			Aborted();
		}
	}

	public void Committed()
	{
		InvokeOutcomeFunction(TransactionStatus.Committed);
	}

	public void Aborted()
	{
		InvokeOutcomeFunction(TransactionStatus.Aborted);
	}

	public void InDoubt()
	{
		InvokeOutcomeFunction(TransactionStatus.InDoubt);
	}
}
