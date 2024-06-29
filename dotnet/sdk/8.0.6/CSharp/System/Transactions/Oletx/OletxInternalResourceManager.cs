using System.Collections;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxInternalResourceManager
{
	private readonly OletxTransactionManager _oletxTm;

	internal ResourceManagerShim ResourceManagerShim;

	internal Guid Identifier { get; }

	internal OletxInternalResourceManager(OletxTransactionManager oletxTm)
	{
		_oletxTm = oletxTm;
		Identifier = Guid.NewGuid();
	}

	public void TMDown()
	{
		ResourceManagerShim = null;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "OletxInternalResourceManager.TMDown");
		}
		Hashtable hashtable;
		lock (TransactionManager.PromotedTransactionTable.SyncRoot)
		{
			hashtable = (Hashtable)TransactionManager.PromotedTransactionTable.Clone();
		}
		IDictionaryEnumerator enumerator = hashtable.GetEnumerator();
		while (enumerator.MoveNext())
		{
			WeakReference weakReference = (WeakReference)enumerator.Value;
			if (weakReference == null)
			{
				continue;
			}
			Transaction transaction = (Transaction)weakReference.Target;
			if (transaction != null)
			{
				RealOletxTransaction realOletxTransaction = transaction._internalTransaction.PromotedTransaction.RealOletxTransaction;
				if (realOletxTransaction.OletxTransactionManagerInstance == _oletxTm)
				{
					realOletxTransaction.TMDown();
				}
			}
		}
		Hashtable hashtable2 = null;
		if (OletxTransactionManager._resourceManagerHashTable != null)
		{
			OletxTransactionManager.ResourceManagerHashTableLock.AcquireReaderLock(-1);
			try
			{
				hashtable2 = (Hashtable)OletxTransactionManager._resourceManagerHashTable.Clone();
			}
			finally
			{
				OletxTransactionManager.ResourceManagerHashTableLock.ReleaseReaderLock();
			}
		}
		if (hashtable2 != null)
		{
			enumerator = hashtable2.GetEnumerator();
			while (enumerator.MoveNext())
			{
				((OletxResourceManager)enumerator.Value)?.TMDownFromInternalRM(_oletxTm);
			}
		}
		_oletxTm.DtcTransactionManagerLock.AcquireWriterLock(-1);
		try
		{
			_oletxTm.ReinitializeProxy();
		}
		finally
		{
			_oletxTm.DtcTransactionManagerLock.ReleaseWriterLock();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxInternalResourceManager.TMDown");
		}
	}

	internal void CallReenlistComplete()
	{
		ResourceManagerShim.ReenlistComplete();
	}
}
