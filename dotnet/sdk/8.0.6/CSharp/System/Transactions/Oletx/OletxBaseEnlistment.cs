using System.Threading;

namespace System.Transactions.Oletx;

internal abstract class OletxBaseEnlistment
{
	protected Guid EnlistmentGuid;

	protected OletxResourceManager OletxResourceManager;

	protected OletxTransaction oletxTransaction;

	protected string TransactionGuidString;

	protected int EnlistmentId;

	internal EnlistmentTraceIdentifier TraceIdentifier;

	protected InternalEnlistment InternalEnlistment;

	internal OletxTransaction OletxTransaction => oletxTransaction;

	internal Guid DistributedTxId
	{
		get
		{
			Guid result = Guid.Empty;
			if (OletxTransaction != null)
			{
				result = OletxTransaction.DistributedTxId;
			}
			return result;
		}
	}

	protected EnlistmentTraceIdentifier InternalTraceIdentifier
	{
		get
		{
			if (EnlistmentTraceIdentifier.Empty == TraceIdentifier)
			{
				lock (this)
				{
					if (EnlistmentTraceIdentifier.Empty == TraceIdentifier)
					{
						Guid resourceManagerIdentifier = Guid.Empty;
						if (OletxResourceManager != null)
						{
							resourceManagerIdentifier = OletxResourceManager.ResourceManagerIdentifier;
						}
						EnlistmentTraceIdentifier traceIdentifier;
						if (oletxTransaction != null)
						{
							traceIdentifier = new EnlistmentTraceIdentifier(resourceManagerIdentifier, oletxTransaction.TransactionTraceId, EnlistmentId);
						}
						else
						{
							TransactionTraceIdentifier transactionTraceId = new TransactionTraceIdentifier(TransactionGuidString, 0);
							traceIdentifier = new EnlistmentTraceIdentifier(resourceManagerIdentifier, transactionTraceId, EnlistmentId);
						}
						Thread.MemoryBarrier();
						TraceIdentifier = traceIdentifier;
					}
				}
			}
			return TraceIdentifier;
		}
	}

	public OletxBaseEnlistment(OletxResourceManager oletxResourceManager, OletxTransaction oletxTransaction)
	{
		Guid empty = Guid.Empty;
		EnlistmentGuid = Guid.NewGuid();
		OletxResourceManager = oletxResourceManager;
		this.oletxTransaction = oletxTransaction;
		if (oletxTransaction != null)
		{
			EnlistmentId = oletxTransaction.RealOletxTransaction._enlistmentCount++;
			TransactionGuidString = oletxTransaction.RealOletxTransaction.TxGuid.ToString();
		}
		else
		{
			TransactionGuidString = Guid.Empty.ToString();
		}
		TraceIdentifier = EnlistmentTraceIdentifier.Empty;
	}

	protected void AddToEnlistmentTable()
	{
		lock (OletxResourceManager.EnlistmentHashtable.SyncRoot)
		{
			OletxResourceManager.EnlistmentHashtable.Add(EnlistmentGuid, this);
		}
	}

	protected void RemoveFromEnlistmentTable()
	{
		lock (OletxResourceManager.EnlistmentHashtable.SyncRoot)
		{
			OletxResourceManager.EnlistmentHashtable.Remove(EnlistmentGuid);
		}
	}
}
