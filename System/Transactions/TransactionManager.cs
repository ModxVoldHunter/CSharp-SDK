using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Transactions.Configuration;
using System.Transactions.DtcProxyShim;
using System.Transactions.Oletx;

namespace System.Transactions;

public static class TransactionManager
{
	private static Hashtable s_promotedTransactionTable;

	private static TransactionTable s_transactionTable;

	private static TransactionStartedEventHandler s_distributedTransactionStartedDelegate;

	internal static HostCurrentTransactionCallback s_currentDelegate;

	internal static bool s_currentDelegateSet;

	private static object s_classSyncObject;

	private static bool s_defaultTimeoutValidated;

	private static long s_defaultTimeoutTicks;

	private static bool s_cachedMaxTimeout;

	private static TimeSpan s_maximumTimeout;

	internal static bool? s_implicitDistributedTransactions;

	internal static object s_implicitDistributedTransactionsLock = new object();

	internal static OletxTransactionManager distributedTransactionManager;

	public static HostCurrentTransactionCallback? HostCurrentCallback
	{
		get
		{
			return s_currentDelegate;
		}
		[param: DisallowNull]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			lock (ClassSyncObject)
			{
				if (s_currentDelegateSet)
				{
					throw new InvalidOperationException(System.SR.CurrentDelegateSet);
				}
				s_currentDelegateSet = true;
			}
			s_currentDelegate = value;
		}
	}

	private static object ClassSyncObject => LazyInitializer.EnsureInitialized(ref s_classSyncObject);

	internal static IsolationLevel DefaultIsolationLevel
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultIsolationLevel");
			}
			return IsolationLevel.Serializable;
		}
	}

	public static TimeSpan DefaultTimeout
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
			}
			if (!s_defaultTimeoutValidated)
			{
				LazyInitializer.EnsureInitialized(ref s_defaultTimeoutTicks, ref s_defaultTimeoutValidated, ref s_classSyncObject, () => ValidateTimeout(DefaultSettingsSection.Timeout).Ticks);
				if (Interlocked.Read(ref s_defaultTimeoutTicks) != DefaultSettingsSection.Timeout.Ticks && log.IsEnabled())
				{
					log.ConfiguredDefaultTimeoutAdjusted();
				}
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultTimeout");
			}
			return new TimeSpan(Interlocked.Read(ref s_defaultTimeoutTicks));
		}
		set
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultTimeout");
			}
			Interlocked.Exchange(ref s_defaultTimeoutTicks, ValidateTimeout(value).Ticks);
			if (Interlocked.Read(ref s_defaultTimeoutTicks) != value.Ticks && log.IsEnabled())
			{
				log.ConfiguredDefaultTimeoutAdjusted();
			}
			s_defaultTimeoutValidated = true;
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultTimeout");
			}
		}
	}

	public static TimeSpan MaximumTimeout
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
			}
			LazyInitializer.EnsureInitialized(ref s_maximumTimeout, ref s_cachedMaxTimeout, ref s_classSyncObject, () => MachineSettingsSection.MaxTimeout);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.get_DefaultMaximumTimeout");
			}
			return s_maximumTimeout;
		}
		set
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultMaximumTimeout");
			}
			ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero, "value");
			s_cachedMaxTimeout = true;
			s_maximumTimeout = value;
			LazyInitializer.EnsureInitialized(ref s_defaultTimeoutTicks, ref s_defaultTimeoutValidated, ref s_classSyncObject, () => DefaultSettingsSection.Timeout.Ticks);
			long num = Interlocked.Read(ref s_defaultTimeoutTicks);
			Interlocked.Exchange(ref s_defaultTimeoutTicks, ValidateTimeout(new TimeSpan(num)).Ticks);
			if (Interlocked.Read(ref s_defaultTimeoutTicks) != num && log.IsEnabled())
			{
				log.ConfiguredDefaultTimeoutAdjusted();
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.set_DefaultMaximumTimeout");
			}
		}
	}

	public static bool ImplicitDistributedTransactions
	{
		get
		{
			return DtcProxyShimFactory.s_transactionConnector != null;
		}
		[SupportedOSPlatform("windows")]
		[RequiresUnreferencedCode("Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.")]
		set
		{
			lock (s_implicitDistributedTransactionsLock)
			{
				bool? flag = s_implicitDistributedTransactions;
				if (!flag.HasValue)
				{
					s_implicitDistributedTransactions = value;
					if (value && DtcProxyShimFactory.s_transactionConnector == null)
					{
						DtcProxyShimFactory.s_transactionConnector = new DtcProxyShimFactory.DtcTransactionConnector();
					}
				}
				else if (value != s_implicitDistributedTransactions)
				{
					throw new InvalidOperationException(System.SR.ImplicitDistributedTransactionsCannotBeChanged);
				}
			}
		}
	}

	internal static Hashtable PromotedTransactionTable => LazyInitializer.EnsureInitialized(ref s_promotedTransactionTable, ref s_classSyncObject, () => new Hashtable(100));

	internal static TransactionTable TransactionTable => LazyInitializer.EnsureInitialized(ref s_transactionTable, ref s_classSyncObject, () => new TransactionTable());

	internal static OletxTransactionManager DistributedTransactionManager => LazyInitializer.EnsureInitialized(ref distributedTransactionManager, ref s_classSyncObject, () => new OletxTransactionManager(DefaultSettingsSection.DistributedTransactionManagerName));

	public static event TransactionStartedEventHandler? DistributedTransactionStarted
	{
		add
		{
			lock (ClassSyncObject)
			{
				s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler)Delegate.Combine(s_distributedTransactionStartedDelegate, value);
				if (value != null)
				{
					ProcessExistingTransactions(value);
				}
			}
		}
		remove
		{
			lock (ClassSyncObject)
			{
				s_distributedTransactionStartedDelegate = (TransactionStartedEventHandler)Delegate.Remove(s_distributedTransactionStartedDelegate, value);
			}
		}
	}

	internal static void ProcessExistingTransactions(TransactionStartedEventHandler eventHandler)
	{
		lock (PromotedTransactionTable)
		{
			IDictionaryEnumerator enumerator = PromotedTransactionTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				WeakReference weakReference = (WeakReference)enumerator.Value;
				if (weakReference.Target is Transaction transaction)
				{
					TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
					transactionEventArgs._transaction = transaction.InternalClone();
					eventHandler(transactionEventArgs._transaction, transactionEventArgs);
				}
			}
		}
	}

	internal static void FireDistributedTransactionStarted(Transaction transaction)
	{
		TransactionStartedEventHandler transactionStartedEventHandler = null;
		lock (ClassSyncObject)
		{
			transactionStartedEventHandler = s_distributedTransactionStartedDelegate;
		}
		if (transactionStartedEventHandler != null)
		{
			TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
			transactionEventArgs._transaction = transaction.InternalClone();
			transactionStartedEventHandler(transactionEventArgs._transaction, transactionEventArgs);
		}
	}

	public static Enlistment Reenlist(Guid resourceManagerIdentifier, byte[] recoveryInformation, IEnlistmentNotification enlistmentNotification)
	{
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		ArgumentNullException.ThrowIfNull(recoveryInformation, "recoveryInformation");
		ArgumentNullException.ThrowIfNull(enlistmentNotification, "enlistmentNotification");
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
			log.TransactionManagerReenlist(resourceManagerIdentifier);
		}
		MemoryStream memoryStream = new MemoryStream(recoveryInformation);
		byte[] recoveryInformation2 = null;
		try
		{
			BinaryReader binaryReader = new BinaryReader(memoryStream);
			int num = binaryReader.ReadInt32();
			if (num != 1)
			{
				if (log.IsEnabled())
				{
					log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", string.Empty);
				}
				throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation");
			}
			string text = binaryReader.ReadString();
			recoveryInformation2 = binaryReader.ReadBytes(recoveryInformation.Length - checked((int)memoryStream.Position));
		}
		catch (EndOfStreamException ex)
		{
			if (log.IsEnabled())
			{
				log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", ex.ToString());
			}
			throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation", ex);
		}
		catch (FormatException ex2)
		{
			if (log.IsEnabled())
			{
				log.TransactionExceptionTrace(TraceSourceType.TraceSourceBase, TransactionExceptionType.UnrecognizedRecoveryInformation, "recoveryInformation", ex2.ToString());
			}
			throw new ArgumentException(System.SR.UnrecognizedRecoveryInformation, "recoveryInformation", ex2);
		}
		finally
		{
			memoryStream.Dispose();
		}
		object syncRoot = new object();
		Enlistment enlistment = new Enlistment(enlistmentNotification, syncRoot);
		EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
		enlistment.InternalEnlistment.PromotedEnlistment = DistributedTransactionManager.ReenlistTransaction(resourceManagerIdentifier, recoveryInformation2, (RecoveringInternalEnlistment)enlistment.InternalEnlistment);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.Reenlist");
		}
		return enlistment;
	}

	public static void RecoveryComplete(Guid resourceManagerIdentifier)
	{
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
			log.TransactionManagerRecoveryComplete(resourceManagerIdentifier);
		}
		DistributedTransactionManager.ResourceManagerRecoveryComplete(resourceManagerIdentifier);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, "TransactionManager.RecoveryComplete");
		}
	}

	internal static byte[] GetRecoveryInformation(string startupInfo, byte[] resourceManagerRecoveryInformation)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "TransactionManager.GetRecoveryInformation");
		}
		MemoryStream memoryStream = new MemoryStream();
		byte[] result = null;
		try
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(1);
			if (startupInfo != null)
			{
				binaryWriter.Write(startupInfo);
			}
			else
			{
				binaryWriter.Write("");
			}
			binaryWriter.Write(resourceManagerRecoveryInformation);
			binaryWriter.Flush();
			result = memoryStream.ToArray();
		}
		finally
		{
			memoryStream.Close();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "TransactionManager.GetRecoveryInformation");
		}
		return result;
	}

	internal static void ValidateIsolationLevel(IsolationLevel transactionIsolationLevel)
	{
		if ((uint)transactionIsolationLevel > 6u)
		{
			throw new ArgumentOutOfRangeException("transactionIsolationLevel");
		}
	}

	internal static TimeSpan ValidateTimeout(TimeSpan transactionTimeout)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(transactionTimeout, TimeSpan.Zero, "transactionTimeout");
		if (MaximumTimeout != TimeSpan.Zero && (transactionTimeout > MaximumTimeout || transactionTimeout == TimeSpan.Zero))
		{
			return MaximumTimeout;
		}
		return transactionTimeout;
	}

	internal static Transaction FindPromotedTransaction(Guid transactionIdentifier)
	{
		Hashtable promotedTransactionTable = PromotedTransactionTable;
		WeakReference weakReference = (WeakReference)promotedTransactionTable[transactionIdentifier];
		if (weakReference != null)
		{
			if (weakReference.Target is Transaction transaction)
			{
				return transaction.InternalClone();
			}
			lock (promotedTransactionTable)
			{
				promotedTransactionTable.Remove(transactionIdentifier);
			}
		}
		return null;
	}

	internal static Transaction FindOrCreatePromotedTransaction(Guid transactionIdentifier, OletxTransaction dtx)
	{
		Transaction transaction = null;
		Hashtable promotedTransactionTable = PromotedTransactionTable;
		lock (promotedTransactionTable)
		{
			WeakReference weakReference = (WeakReference)promotedTransactionTable[transactionIdentifier];
			if (weakReference != null)
			{
				transaction = weakReference.Target as Transaction;
				if (null != transaction)
				{
					return transaction.InternalClone();
				}
				lock (promotedTransactionTable)
				{
					promotedTransactionTable.Remove(transactionIdentifier);
				}
			}
			transaction = new Transaction(dtx);
			transaction._internalTransaction._finalizedObject = new FinalizedObject(transaction._internalTransaction, dtx.Identifier);
			weakReference = new WeakReference(transaction, trackResurrection: false);
			promotedTransactionTable[dtx.Identifier] = weakReference;
		}
		dtx.SavedLtmPromotedTransaction = transaction;
		FireDistributedTransactionStarted(transaction);
		return transaction;
	}
}
