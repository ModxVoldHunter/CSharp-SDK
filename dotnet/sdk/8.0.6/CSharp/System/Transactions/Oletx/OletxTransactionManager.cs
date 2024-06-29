using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxTransactionManager
{
	private readonly IsolationLevel _isolationLevelProperty;

	private readonly TimeSpan _timeoutProperty;

	private TransactionOptions _configuredTransactionOptions;

	private static object _classSyncObject;

	internal static Hashtable _resourceManagerHashTable;

	public static ReaderWriterLock ResourceManagerHashTableLock;

	internal static volatile bool ProcessingTmDown;

	internal ReaderWriterLock DtcTransactionManagerLock;

	private readonly DtcTransactionManager _dtcTransactionManager;

	internal OletxInternalResourceManager InternalResourceManager;

	internal static DtcProxyShimFactory ProxyShimFactory;

	internal static volatile EventWaitHandle _shimWaitHandle;

	private readonly string _nodeNameField;

	internal static EventWaitHandle ShimWaitHandle
	{
		get
		{
			if (_shimWaitHandle == null)
			{
				lock (ClassSyncObject)
				{
					if (_shimWaitHandle == null)
					{
						_shimWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.AutoReset);
					}
				}
			}
			return _shimWaitHandle;
		}
	}

	internal string CreationNodeName => _nodeNameField;

	internal DtcTransactionManager DtcTransactionManager
	{
		get
		{
			if (DtcTransactionManagerLock.IsReaderLockHeld || DtcTransactionManagerLock.IsWriterLockHeld)
			{
				if (_dtcTransactionManager == null)
				{
					throw TransactionException.Create(System.SR.DtcTransactionManagerUnavailable, null);
				}
				return _dtcTransactionManager;
			}
			throw TransactionException.Create(System.SR.InternalError, null);
		}
	}

	internal static object ClassSyncObject
	{
		get
		{
			if (_classSyncObject == null)
			{
				object value = new object();
				Interlocked.CompareExchange(ref _classSyncObject, value, null);
			}
			return _classSyncObject;
		}
	}

	internal static void ShimNotificationCallback(object state, bool timeout)
	{
		object managedIdentifier = null;
		ShimNotificationType shimNotificationType = ShimNotificationType.None;
		byte[] prepareInfo = null;
		bool releaseLock = false;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, "OletxTransactionManager.ShimNotificationCallback");
		}
		Thread.BeginCriticalRegion();
		try
		{
			do
			{
				DtcProxyShimFactory proxyShimFactory = ProxyShimFactory;
				try
				{
					Thread.BeginThreadAffinity();
					bool isSinglePhase;
					bool abortingHint;
					try
					{
						proxyShimFactory.GetNotification(out managedIdentifier, out shimNotificationType, out isSinglePhase, out abortingHint, out releaseLock, out prepareInfo);
					}
					finally
					{
						if (releaseLock)
						{
							if (managedIdentifier is OletxInternalResourceManager)
							{
								ProcessingTmDown = true;
								Monitor.Enter(ProxyShimFactory);
							}
							else
							{
								releaseLock = false;
							}
							proxyShimFactory.ReleaseNotificationLock();
						}
						Thread.EndThreadAffinity();
					}
					if (ProcessingTmDown)
					{
						lock (ProxyShimFactory)
						{
						}
					}
					switch (shimNotificationType)
					{
					case ShimNotificationType.Phase0RequestNotify:
						if (managedIdentifier is OletxPhase0VolatileEnlistmentContainer oletxPhase0VolatileEnlistmentContainer)
						{
							oletxPhase0VolatileEnlistmentContainer.Phase0Request(abortingHint);
						}
						else if (managedIdentifier is OletxEnlistment oletxEnlistment)
						{
							oletxEnlistment.Phase0Request(abortingHint);
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.VoteRequestNotify:
						if (managedIdentifier is OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer2)
						{
							oletxPhase1VolatileEnlistmentContainer2.VoteRequest();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.CommittedNotify:
						if (managedIdentifier is OutcomeEnlistment outcomeEnlistment2)
						{
							outcomeEnlistment2.Committed();
						}
						else if (managedIdentifier is OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer3)
						{
							oletxPhase1VolatileEnlistmentContainer3.Committed();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.AbortedNotify:
						if (managedIdentifier is OutcomeEnlistment outcomeEnlistment)
						{
							outcomeEnlistment.Aborted();
						}
						else if (managedIdentifier is OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer)
						{
							oletxPhase1VolatileEnlistmentContainer.Aborted();
						}
						break;
					case ShimNotificationType.InDoubtNotify:
						if (managedIdentifier is OutcomeEnlistment outcomeEnlistment3)
						{
							outcomeEnlistment3.InDoubt();
						}
						else if (managedIdentifier is OletxPhase1VolatileEnlistmentContainer oletxPhase1VolatileEnlistmentContainer4)
						{
							oletxPhase1VolatileEnlistmentContainer4.InDoubt();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.PrepareRequestNotify:
					{
						bool flag = true;
						if (managedIdentifier is OletxEnlistment oletxEnlistment4)
						{
							flag = oletxEnlistment4.PrepareRequest(isSinglePhase, prepareInfo);
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					}
					case ShimNotificationType.CommitRequestNotify:
						if (managedIdentifier is OletxEnlistment oletxEnlistment2)
						{
							oletxEnlistment2.CommitRequest();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.AbortRequestNotify:
						if (managedIdentifier is OletxEnlistment oletxEnlistment5)
						{
							oletxEnlistment5.AbortRequest();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.EnlistmentTmDownNotify:
						if (managedIdentifier is OletxEnlistment oletxEnlistment3)
						{
							oletxEnlistment3.TMDown();
						}
						else
						{
							Environment.FailFast(System.SR.InternalError);
						}
						break;
					case ShimNotificationType.ResourceManagerTmDownNotify:
						if (!(managedIdentifier is OletxResourceManager oletxResourceManager))
						{
							if (managedIdentifier is OletxInternalResourceManager oletxInternalResourceManager)
							{
								oletxInternalResourceManager.TMDown();
							}
							else
							{
								Environment.FailFast(System.SR.InternalError);
							}
						}
						else
						{
							oletxResourceManager.TMDown();
						}
						break;
					default:
						Environment.FailFast(System.SR.InternalError);
						break;
					case ShimNotificationType.None:
						break;
					}
				}
				finally
				{
					if (releaseLock)
					{
						releaseLock = false;
						ProcessingTmDown = false;
						Monitor.Exit(ProxyShimFactory);
					}
				}
			}
			while (shimNotificationType != 0);
		}
		finally
		{
			if (releaseLock)
			{
				releaseLock = false;
				ProcessingTmDown = false;
				Monitor.Exit(ProxyShimFactory);
			}
			Thread.EndCriticalRegion();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, "OletxTransactionManager.ShimNotificationCallback");
		}
	}

	internal OletxTransactionManager(string nodeName)
	{
		lock (ClassSyncObject)
		{
			if (ProxyShimFactory == null)
			{
				ProxyShimFactory = new DtcProxyShimFactory(ShimWaitHandle);
				ThreadPool.UnsafeRegisterWaitForSingleObject(ShimWaitHandle, ShimNotificationCallback, null, -1, executeOnlyOnce: false);
			}
		}
		DtcTransactionManagerLock = new ReaderWriterLock();
		_nodeNameField = nodeName;
		string nodeNameField = _nodeNameField;
		if (nodeNameField != null && nodeNameField.Length == 0)
		{
			_nodeNameField = null;
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.OleTxTransactionManagerCreate(GetType(), _nodeNameField);
		}
		_configuredTransactionOptions.IsolationLevel = (_isolationLevelProperty = TransactionManager.DefaultIsolationLevel);
		_configuredTransactionOptions.Timeout = (_timeoutProperty = TransactionManager.DefaultTimeout);
		InternalResourceManager = new OletxInternalResourceManager(this);
		DtcTransactionManagerLock.AcquireWriterLock(-1);
		try
		{
			_dtcTransactionManager = new DtcTransactionManager(_nodeNameField, this);
		}
		finally
		{
			DtcTransactionManagerLock.ReleaseWriterLock();
		}
		if (_resourceManagerHashTable == null)
		{
			_resourceManagerHashTable = new Hashtable(2);
			ResourceManagerHashTableLock = new ReaderWriterLock();
		}
	}

	internal OletxCommittableTransaction CreateTransaction(TransactionOptions properties)
	{
		TransactionShim transactionShim = null;
		Guid transactionIdentifier = Guid.Empty;
		TransactionManager.ValidateIsolationLevel(properties.IsolationLevel);
		if (IsolationLevel.Unspecified == properties.IsolationLevel)
		{
			properties.IsolationLevel = _configuredTransactionOptions.IsolationLevel;
		}
		properties.Timeout = TransactionManager.ValidateTimeout(properties.Timeout);
		DtcTransactionManagerLock.AcquireReaderLock(-1);
		OletxCommittableTransaction oletxCommittableTransaction;
		try
		{
			OletxTransactionIsolationLevel oletxTransactionIsolationLevel = ConvertIsolationLevel(properties.IsolationLevel);
			uint timeout = DtcTransactionManager.AdjustTimeout(properties.Timeout);
			OutcomeEnlistment outcomeEnlistment = new OutcomeEnlistment();
			try
			{
				_dtcTransactionManager.ProxyShimFactory.BeginTransaction(timeout, oletxTransactionIsolationLevel, outcomeEnlistment, out transactionIdentifier, out transactionShim);
			}
			catch (COMException comException)
			{
				ProxyException(comException);
				throw;
			}
			RealOletxTransaction realOletxTransaction = new RealOletxTransaction(this, transactionShim, outcomeEnlistment, transactionIdentifier, oletxTransactionIsolationLevel);
			oletxCommittableTransaction = new OletxCommittableTransaction(realOletxTransaction);
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.TransactionCreated(TraceSourceType.TraceSourceOleTx, oletxCommittableTransaction.TransactionTraceId, "OletxTransaction");
			}
		}
		finally
		{
			DtcTransactionManagerLock.ReleaseReaderLock();
		}
		return oletxCommittableTransaction;
	}

	internal OletxEnlistment ReenlistTransaction(Guid resourceManagerIdentifier, byte[] recoveryInformation, IEnlistmentNotificationInternal enlistmentNotification)
	{
		ArgumentNullException.ThrowIfNull(recoveryInformation, "recoveryInformation");
		ArgumentNullException.ThrowIfNull(enlistmentNotification, "enlistmentNotification");
		OletxResourceManager oletxResourceManager = RegisterResourceManager(resourceManagerIdentifier);
		if (oletxResourceManager == null)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "resourceManagerIdentifier");
		}
		if (oletxResourceManager.RecoveryCompleteCalledByApplication)
		{
			throw new InvalidOperationException(System.SR.ReenlistAfterRecoveryComplete);
		}
		return oletxResourceManager.Reenlist(recoveryInformation, enlistmentNotification);
	}

	internal void ResourceManagerRecoveryComplete(Guid resourceManagerIdentifier)
	{
		OletxResourceManager oletxResourceManager = RegisterResourceManager(resourceManagerIdentifier);
		if (oletxResourceManager.RecoveryCompleteCalledByApplication)
		{
			throw new InvalidOperationException(System.SR.DuplicateRecoveryComplete);
		}
		oletxResourceManager.RecoveryComplete();
	}

	internal OletxResourceManager RegisterResourceManager(Guid resourceManagerIdentifier)
	{
		ResourceManagerHashTableLock.AcquireWriterLock(-1);
		try
		{
			if (_resourceManagerHashTable[resourceManagerIdentifier] is OletxResourceManager result)
			{
				return result;
			}
			OletxResourceManager oletxResourceManager = new OletxResourceManager(this, resourceManagerIdentifier);
			_resourceManagerHashTable.Add(resourceManagerIdentifier, oletxResourceManager);
			return oletxResourceManager;
		}
		finally
		{
			ResourceManagerHashTableLock.ReleaseWriterLock();
		}
	}

	internal OletxResourceManager FindOrRegisterResourceManager(Guid resourceManagerIdentifier)
	{
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		ResourceManagerHashTableLock.AcquireReaderLock(-1);
		OletxResourceManager oletxResourceManager;
		try
		{
			oletxResourceManager = _resourceManagerHashTable[resourceManagerIdentifier] as OletxResourceManager;
		}
		finally
		{
			ResourceManagerHashTableLock.ReleaseReaderLock();
		}
		if (oletxResourceManager == null)
		{
			return RegisterResourceManager(resourceManagerIdentifier);
		}
		return oletxResourceManager;
	}

	internal static void ProxyException(COMException comException)
	{
		if (comException.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || comException.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
		{
			throw TransactionManagerCommunicationException.Create(System.SR.TransactionManagerCommunicationException, comException);
		}
		if (comException.ErrorCode == OletxHelper.XACT_E_NETWORK_TX_DISABLED)
		{
			throw TransactionManagerCommunicationException.Create(System.SR.NetworkTransactionsDisabled, comException);
		}
		if (comException.ErrorCode >= OletxHelper.XACT_E_FIRST && comException.ErrorCode <= OletxHelper.XACT_E_LAST)
		{
			throw TransactionException.Create((OletxHelper.XACT_E_NOTRANSACTION == comException.ErrorCode) ? System.SR.TransactionAlreadyOver : comException.Message, comException);
		}
	}

	internal void ReinitializeProxy()
	{
		DtcTransactionManagerLock.AcquireWriterLock(-1);
		try
		{
			_dtcTransactionManager?.ReleaseProxy();
		}
		finally
		{
			DtcTransactionManagerLock.ReleaseWriterLock();
		}
	}

	internal static OletxTransactionIsolationLevel ConvertIsolationLevel(IsolationLevel isolationLevel)
	{
		return isolationLevel switch
		{
			IsolationLevel.Serializable => OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE, 
			IsolationLevel.RepeatableRead => OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD, 
			IsolationLevel.ReadCommitted => OletxTransactionIsolationLevel.ISOLATIONLEVEL_CURSORSTABILITY, 
			IsolationLevel.ReadUncommitted => OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED, 
			IsolationLevel.Chaos => OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS, 
			IsolationLevel.Unspecified => OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED, 
			_ => OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE, 
		};
	}

	internal static IsolationLevel ConvertIsolationLevelFromProxyValue(OletxTransactionIsolationLevel proxyIsolationLevel)
	{
		return proxyIsolationLevel switch
		{
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE => IsolationLevel.Serializable, 
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD => IsolationLevel.RepeatableRead, 
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_CURSORSTABILITY => IsolationLevel.ReadCommitted, 
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED => IsolationLevel.ReadUncommitted, 
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED => IsolationLevel.Unspecified, 
			OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS => IsolationLevel.Chaos, 
			_ => IsolationLevel.Serializable, 
		};
	}
}
