using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions;

public static class TransactionInterop
{
	public static readonly Guid PromoterTypeDtc = new Guid("14229753-FFE1-428D-82B7-DF73045CB8DA");

	internal static OletxTransaction ConvertToOletxTransaction(Transaction transaction)
	{
		ArgumentNullException.ThrowIfNull(transaction, "transaction");
		ObjectDisposedException.ThrowIf(transaction.Disposed, transaction);
		if (transaction._complete)
		{
			throw TransactionException.CreateTransactionCompletedException(transaction.DistributedTxId);
		}
		return transaction.Promote();
	}

	public static byte[] GetExportCookie(Transaction transaction, byte[] whereabouts)
	{
		ArgumentNullException.ThrowIfNull(transaction, "transaction");
		ArgumentNullException.ThrowIfNull(whereabouts, "whereabouts");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetExportCookie");
		}
		byte[] dst = new byte[whereabouts.Length];
		Buffer.BlockCopy(whereabouts, 0, dst, 0, whereabouts.Length);
		OletxTransaction oletxTransaction = ConvertToOletxTransaction(transaction);
		byte[] cookieBuffer;
		try
		{
			oletxTransaction.RealOletxTransaction.TransactionShim.Export(whereabouts, out cookieBuffer);
		}
		catch (COMException ex)
		{
			OletxTransactionManager.ProxyException(ex);
			throw TransactionManagerCommunicationException.Create(null, ex);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetExportCookie");
		}
		return cookieBuffer;
	}

	public static Transaction GetTransactionFromExportCookie(byte[] cookie)
	{
		ArgumentNullException.ThrowIfNull(cookie, "cookie");
		if (cookie.Length < 32)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "cookie");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
		}
		byte[] array = new byte[cookie.Length];
		Buffer.BlockCopy(cookie, 0, array, 0, cookie.Length);
		cookie = array;
		TransactionShim transactionShim = null;
		Guid transactionIdentifier = Guid.Empty;
		OletxTransactionIsolationLevel isolationLevel = OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE;
		Guid transactionIdentifier2 = new Guid(cookie.AsSpan(16, 16));
		Transaction transaction = TransactionManager.FindPromotedTransaction(transactionIdentifier2);
		if (transaction != null)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
			}
			return transaction;
		}
		RealOletxTransaction realOletxTransaction = null;
		OletxTransactionManager distributedTransactionManager = TransactionManager.DistributedTransactionManager;
		distributedTransactionManager.DtcTransactionManagerLock.AcquireReaderLock(-1);
		OutcomeEnlistment outcomeEnlistment;
		try
		{
			outcomeEnlistment = new OutcomeEnlistment();
			distributedTransactionManager.DtcTransactionManager.ProxyShimFactory.Import(cookie, outcomeEnlistment, out transactionIdentifier, out isolationLevel, out transactionShim);
		}
		catch (COMException ex)
		{
			OletxTransactionManager.ProxyException(ex);
			throw TransactionManagerCommunicationException.Create(System.SR.TraceSourceOletx, ex);
		}
		finally
		{
			distributedTransactionManager.DtcTransactionManagerLock.ReleaseReaderLock();
		}
		realOletxTransaction = new RealOletxTransaction(distributedTransactionManager, transactionShim, outcomeEnlistment, transactionIdentifier, isolationLevel);
		OletxTransaction dtx = new OletxTransaction(realOletxTransaction);
		transaction = TransactionManager.FindOrCreatePromotedTransaction(transactionIdentifier2, dtx);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromExportCookie");
		}
		return transaction;
	}

	public static byte[] GetTransmitterPropagationToken(Transaction transaction)
	{
		ArgumentNullException.ThrowIfNull(transaction, "transaction");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransmitterPropagationToken");
		}
		OletxTransaction oletxTx = ConvertToOletxTransaction(transaction);
		byte[] transmitterPropagationToken = GetTransmitterPropagationToken(oletxTx);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransmitterPropagationToken");
		}
		return transmitterPropagationToken;
	}

	internal static byte[] GetTransmitterPropagationToken(OletxTransaction oletxTx)
	{
		byte[] array = null;
		try
		{
			return oletxTx.RealOletxTransaction.TransactionShim.GetPropagationToken();
		}
		catch (COMException comException)
		{
			OletxTransactionManager.ProxyException(comException);
			throw;
		}
	}

	public static Transaction GetTransactionFromTransmitterPropagationToken(byte[] propagationToken)
	{
		ArgumentNullException.ThrowIfNull(propagationToken, "propagationToken");
		if (propagationToken.Length < 24)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "propagationToken");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
		}
		Guid transactionIdentifier = new Guid(propagationToken.AsSpan(8, 16));
		Transaction transaction = TransactionManager.FindPromotedTransaction(transactionIdentifier);
		if (null != transaction)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
			}
			return transaction;
		}
		OletxTransaction oletxTransactionFromTransmitterPropagationToken = GetOletxTransactionFromTransmitterPropagationToken(propagationToken);
		Transaction result = TransactionManager.FindOrCreatePromotedTransaction(transactionIdentifier, oletxTransactionFromTransmitterPropagationToken);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
		}
		return result;
	}

	public static IDtcTransaction GetDtcTransaction(Transaction transaction)
	{
		ArgumentNullException.ThrowIfNull(transaction, "transaction");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetDtcTransaction");
		}
		OletxTransaction oletxTransaction = ConvertToOletxTransaction(transaction);
		IDtcTransaction dtcTransaction;
		try
		{
			oletxTransaction.RealOletxTransaction.TransactionShim.GetITransactionNative(out var transactionNative);
			ComWrappers.TryGetComInstance(transactionNative, out var unknown);
			dtcTransaction = (IDtcTransaction)Marshal.GetObjectForIUnknown(unknown);
			Marshal.SetComObjectData(dtcTransaction, typeof(ITransaction), transactionNative);
			Marshal.Release(unknown);
		}
		catch (COMException comException)
		{
			OletxTransactionManager.ProxyException(comException);
			throw;
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetDtcTransaction");
		}
		return dtcTransaction;
	}

	internal static IDtcTransaction GetDtcTransaction(ITransaction transaction)
	{
		ComWrappers.TryGetComInstance(transaction, out var unknown);
		IDtcTransaction dtcTransaction = (IDtcTransaction)Marshal.GetObjectForIUnknown(unknown);
		Marshal.SetComObjectData(dtcTransaction, typeof(ITransaction), transaction);
		Marshal.Release(unknown);
		return dtcTransaction;
	}

	public static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
	{
		ArgumentNullException.ThrowIfNull(transactionNative, "transactionNative");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromDtcTransaction");
		}
		Transaction transaction = null;
		bool flag = false;
		TransactionShim transactionShim = null;
		Guid transactionIdentifier = Guid.Empty;
		OletxTransactionIsolationLevel isolationLevel = OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE;
		OutcomeEnlistment outcomeEnlistment = null;
		RealOletxTransaction realOletxTransaction = null;
		OletxTransaction oletxTransaction = null;
		ITransaction iTransactionFromIDtcTransaction = GetITransactionFromIDtcTransaction(transactionNative);
		Unsafe.SkipInit(out OletxXactTransInfo xactInfo);
		try
		{
			iTransactionFromIDtcTransaction.GetTransactionInfo(out xactInfo);
		}
		catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_NOTRANSACTION)
		{
			flag = true;
			xactInfo.Uow = Guid.Empty;
		}
		OletxTransactionManager distributedTransactionManager = TransactionManager.DistributedTransactionManager;
		if (!flag)
		{
			transaction = TransactionManager.FindPromotedTransaction(xactInfo.Uow);
			if (transaction != null)
			{
				if (log.IsEnabled())
				{
					log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromDtcTransaction");
				}
				return transaction;
			}
			distributedTransactionManager.DtcTransactionManagerLock.AcquireReaderLock(-1);
			try
			{
				outcomeEnlistment = new OutcomeEnlistment();
				distributedTransactionManager.DtcTransactionManager.ProxyShimFactory.CreateTransactionShim(transactionNative, outcomeEnlistment, out transactionIdentifier, out isolationLevel, out transactionShim);
			}
			catch (COMException comException)
			{
				OletxTransactionManager.ProxyException(comException);
				throw;
			}
			finally
			{
				distributedTransactionManager.DtcTransactionManagerLock.ReleaseReaderLock();
			}
			realOletxTransaction = new RealOletxTransaction(distributedTransactionManager, transactionShim, outcomeEnlistment, transactionIdentifier, isolationLevel);
			oletxTransaction = new OletxTransaction(realOletxTransaction);
			transaction = TransactionManager.FindOrCreatePromotedTransaction(xactInfo.Uow, oletxTransaction);
		}
		else
		{
			realOletxTransaction = new RealOletxTransaction(distributedTransactionManager, null, null, transactionIdentifier, OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE);
			oletxTransaction = new OletxTransaction(realOletxTransaction);
			transaction = new Transaction(oletxTransaction);
			TransactionManager.FireDistributedTransactionStarted(transaction);
			oletxTransaction.SavedLtmPromotedTransaction = transaction;
			InternalTransaction.DistributedTransactionOutcome(transaction._internalTransaction, TransactionStatus.InDoubt);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.GetTransactionFromDtcTransaction");
		}
		return transaction;
	}

	internal unsafe static ITransaction GetITransactionFromIDtcTransaction(IDtcTransaction transactionNative)
	{
		ITransaction transaction = Marshal.GetComObjectData(transactionNative, typeof(ITransaction)) as ITransaction;
		if (transaction == null)
		{
			nint iUnknownForObject = Marshal.GetIUnknownForObject(transactionNative);
			if (Marshal.QueryInterface(iUnknownForObject, ref Guids.IID_ITransaction_Guid, out var ppv) != 0)
			{
				Marshal.Release(iUnknownForObject);
				throw new ArgumentException(System.SR.InvalidArgument, "transactionNative");
			}
			Marshal.Release(iUnknownForObject);
			transaction = ComInterfaceMarshaller<ITransaction>.ConvertToManaged((void*)ppv);
			Marshal.SetComObjectData(transactionNative, typeof(ITransaction), transaction);
			Marshal.Release(ppv);
		}
		return transaction;
	}

	public static byte[] GetWhereabouts()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionInterop.$GetWhereabouts");
		}
		OletxTransactionManager distributedTransactionManager = TransactionManager.DistributedTransactionManager;
		if (distributedTransactionManager == null)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "transactionManager");
		}
		distributedTransactionManager.DtcTransactionManagerLock.AcquireReaderLock(-1);
		byte[] whereabouts;
		try
		{
			whereabouts = distributedTransactionManager.DtcTransactionManager.Whereabouts;
		}
		finally
		{
			distributedTransactionManager.DtcTransactionManagerLock.ReleaseReaderLock();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionInterop.$GetWhereabouts");
		}
		return whereabouts;
	}

	internal static OletxTransaction GetOletxTransactionFromTransmitterPropagationToken(byte[] propagationToken)
	{
		ArgumentNullException.ThrowIfNull(propagationToken, "propagationToken");
		if (propagationToken.Length < 24)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "propagationToken");
		}
		TransactionShim transactionShim = null;
		byte[] array = new byte[propagationToken.Length];
		Array.Copy(propagationToken, array, propagationToken.Length);
		propagationToken = array;
		OletxTransactionManager distributedTransactionManager = TransactionManager.DistributedTransactionManager;
		distributedTransactionManager.DtcTransactionManagerLock.AcquireReaderLock(-1);
		OutcomeEnlistment outcomeEnlistment;
		Guid transactionIdentifier;
		OletxTransactionIsolationLevel isolationLevel;
		try
		{
			outcomeEnlistment = new OutcomeEnlistment();
			distributedTransactionManager.DtcTransactionManager.ProxyShimFactory.ReceiveTransaction(propagationToken, outcomeEnlistment, out transactionIdentifier, out isolationLevel, out transactionShim);
		}
		catch (COMException ex)
		{
			OletxTransactionManager.ProxyException(ex);
			throw TransactionManagerCommunicationException.Create(System.SR.TraceSourceOletx, ex);
		}
		finally
		{
			distributedTransactionManager.DtcTransactionManagerLock.ReleaseReaderLock();
		}
		RealOletxTransaction realOletxTransaction = new RealOletxTransaction(distributedTransactionManager, transactionShim, outcomeEnlistment, transactionIdentifier, isolationLevel);
		return new OletxTransaction(realOletxTransaction);
	}
}
