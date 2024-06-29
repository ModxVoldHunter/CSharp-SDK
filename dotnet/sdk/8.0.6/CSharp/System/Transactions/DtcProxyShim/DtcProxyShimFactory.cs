using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class DtcProxyShimFactory
{
	internal interface ITransactionConnector
	{
		void ConnectToProxyCore(DtcProxyShimFactory proxyShimFactory, string nodeName, Guid resourceManagerIdentifier, object managedIdentifier, out bool nodeNameMatches, out byte[] whereabouts, out ResourceManagerShim resourceManagerShim);
	}

	[RequiresUnreferencedCode("Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.")]
	internal sealed class DtcTransactionConnector : ITransactionConnector
	{
		public void ConnectToProxyCore(DtcProxyShimFactory proxyShimFactory, string nodeName, Guid resourceManagerIdentifier, object managedIdentifier, out bool nodeNameMatches, out byte[] whereabouts, out ResourceManagerShim resourceManagerShim)
		{
			proxyShimFactory.ConnectToProxyCore(nodeName, resourceManagerIdentifier, managedIdentifier, out nodeNameMatches, out whereabouts, out resourceManagerShim);
		}
	}

	private static readonly object _proxyInitLock = new object();

	internal static ITransactionConnector s_transactionConnector;

	private readonly object _notificationLock = new object();

	private readonly ConcurrentQueue<NotificationShimBase> _notifications = new ConcurrentQueue<NotificationShimBase>();

	private readonly ConcurrentQueue<ITransactionOptions> _cachedOptions = new ConcurrentQueue<ITransactionOptions>();

	private readonly ConcurrentQueue<ITransactionTransmitter> _cachedTransmitters = new ConcurrentQueue<ITransactionTransmitter>();

	private readonly ConcurrentQueue<ITransactionReceiver> _cachedReceivers = new ConcurrentQueue<ITransactionReceiver>();

	private static readonly int s_maxCachedInterfaces = Environment.ProcessorCount * 2;

	private readonly EventWaitHandle _eventHandle;

	private ITransactionDispenser _transactionDispenser;

	internal ITransactionExportFactory ExportFactory => (ITransactionExportFactory)_transactionDispenser;

	internal ITransactionVoterFactory2 VoterFactory => (ITransactionVoterFactory2)_transactionDispenser;

	internal DtcProxyShimFactory(EventWaitHandle notificationEventHandle)
	{
		_eventHandle = notificationEventHandle;
	}

	[LibraryImport("xolehlp.dll", StringMarshalling = StringMarshalling.Utf16)]
	[RequiresUnreferencedCode("Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "8.0.10.26715")]
	private unsafe static int DtcGetTransactionManagerExW([MarshalAs(UnmanagedType.LPWStr)] string pszHost, [MarshalAs(UnmanagedType.LPWStr)] string pszTmName, in Guid riid, int grfOptions, void* pvConfigPararms, [MarshalAs(UnmanagedType.Interface)] out ITransactionDispenser ppvObject)
	{
		bool flag = false;
		Unsafe.SkipInit<ITransactionDispenser>(out ppvObject);
		void* unmanaged = default(void*);
		int result = 0;
		try
		{
			fixed (Guid* _riid_native = &riid)
			{
				fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(pszTmName))
				{
					void* _pszTmName_native = ptr;
					fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(pszHost))
					{
						void* _pszHost_native = ptr2;
						result = __PInvoke((ushort*)_pszHost_native, (ushort*)_pszTmName_native, _riid_native, grfOptions, pvConfigPararms, &unmanaged);
					}
				}
			}
			flag = true;
			ppvObject = ComInterfaceMarshaller<ITransactionDispenser>.ConvertToManaged(unmanaged);
			return result;
		}
		finally
		{
			if (flag)
			{
				ComInterfaceMarshaller<ITransactionDispenser>.Free(unmanaged);
			}
		}
		[DllImport("xolehlp.dll", EntryPoint = "DtcGetTransactionManagerExW", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ushort* __pszHost_native, ushort* __pszTmName_native, Guid* __riid_native, int __grfOptions_native, void* __pvConfigPararms_native, void** __ppvObject_native);
	}

	[RequiresUnreferencedCode("Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.")]
	private unsafe static void DtcGetTransactionManager(string nodeName, out ITransactionDispenser localDispenser)
	{
		Marshal.ThrowExceptionForHR(DtcGetTransactionManagerExW(nodeName, null, in Guids.IID_ITransactionDispenser_Guid, 0, null, out localDispenser));
	}

	public void ConnectToProxy(string nodeName, Guid resourceManagerIdentifier, object managedIdentifier, out bool nodeNameMatches, out byte[] whereabouts, out ResourceManagerShim resourceManagerShim)
	{
		if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
		{
			throw new PlatformNotSupportedException(System.SR.DistributedNotSupportedOn32Bits);
		}
		lock (TransactionManager.s_implicitDistributedTransactionsLock)
		{
			if (s_transactionConnector == null)
			{
				TransactionManager.s_implicitDistributedTransactions = false;
				throw new NotSupportedException(System.SR.ImplicitDistributedTransactionsDisabled);
			}
		}
		s_transactionConnector.ConnectToProxyCore(this, nodeName, resourceManagerIdentifier, managedIdentifier, out nodeNameMatches, out whereabouts, out resourceManagerShim);
	}

	[RequiresUnreferencedCode("Distributed transactions support may not be compatible with trimming. If your program creates a distributed transaction via System.Transactions, the correctness of the application cannot be guaranteed after trimming.")]
	private void ConnectToProxyCore(string nodeName, Guid resourceManagerIdentifier, object managedIdentifier, out bool nodeNameMatches, out byte[] whereabouts, out ResourceManagerShim resourceManagerShim)
	{
		lock (_proxyInitLock)
		{
			DtcGetTransactionManager(nodeName, out var localDispenser);
			if (nodeName != null)
			{
				ITmNodeName tmNodeName = (ITmNodeName)localDispenser;
				tmNodeName.GetNodeNameSize(out var pcbNodeNameSize);
				tmNodeName.GetNodeName(pcbNodeNameSize, out var pcbNodeSize);
				nodeNameMatches = pcbNodeSize == nodeName;
			}
			else
			{
				nodeNameMatches = true;
			}
			ITransactionImportWhereabouts pImportWhereabouts = (ITransactionImportWhereabouts)localDispenser;
			uint whereaboutsSize = 0u;
			OletxHelper.Retry(delegate
			{
				pImportWhereabouts.GetWhereaboutsSize(out whereaboutsSize);
			});
			byte[] tmpWhereabouts = new byte[whereaboutsSize];
			OletxHelper.Retry(delegate
			{
				pImportWhereabouts.GetWhereabouts(whereaboutsSize, tmpWhereabouts, out var _);
			});
			IResourceManagerFactory2 rmFactory = (IResourceManagerFactory2)localDispenser;
			ResourceManagerNotifyShim rmNotifyShim = new ResourceManagerNotifyShim(this, managedIdentifier);
			ResourceManagerShim rmShim = new ResourceManagerShim(this);
			OletxHelper.Retry(delegate
			{
				rmFactory.CreateEx(in resourceManagerIdentifier, "System.Transactions.InternalRM", rmNotifyShim, in Guids.IID_IResourceManager_Guid, out var rm);
				rmShim.ResourceManager = (IResourceManager)rm;
			});
			resourceManagerShim = rmShim;
			_transactionDispenser = localDispenser;
			whereabouts = tmpWhereabouts;
		}
	}

	internal void NewNotification(NotificationShimBase notification)
	{
		lock (_notificationLock)
		{
			_notifications.Enqueue(notification);
		}
		_eventHandle.Set();
	}

	public void ReleaseNotificationLock()
	{
		Monitor.Exit(_notificationLock);
	}

	public void BeginTransaction(uint timeout, OletxTransactionIsolationLevel isolationLevel, object managedIdentifier, out Guid transactionIdentifier, out TransactionShim transactionShim)
	{
		ITransactionOptions cachedOptions = GetCachedOptions();
		try
		{
			Xactopt options = new Xactopt(timeout, string.Empty);
			cachedOptions.SetOptions(options);
			_transactionDispenser.BeginTransaction(IntPtr.Zero, isolationLevel, OletxTransactionIsoFlags.ISOFLAG_NONE, cachedOptions, out var ppTransaction);
			SetupTransaction(ppTransaction, managedIdentifier, out transactionIdentifier, out var _, out transactionShim);
		}
		finally
		{
			ReturnCachedOptions(cachedOptions);
		}
	}

	public void CreateResourceManager(Guid resourceManagerIdentifier, OletxResourceManager managedIdentifier, out ResourceManagerShim resourceManagerShim)
	{
		IResourceManagerFactory2 rmFactory = (IResourceManagerFactory2)_transactionDispenser;
		ResourceManagerNotifyShim rmNotifyShim = new ResourceManagerNotifyShim(this, managedIdentifier);
		ResourceManagerShim rmShim = new ResourceManagerShim(this);
		OletxHelper.Retry(delegate
		{
			rmFactory.CreateEx(in resourceManagerIdentifier, "System.Transactions.ResourceManager", rmNotifyShim, in Guids.IID_IResourceManager_Guid, out var rm);
			rmShim.ResourceManager = (IResourceManager)rm;
		});
		resourceManagerShim = rmShim;
	}

	public void Import(byte[] cookie, OutcomeEnlistment managedIdentifier, out Guid transactionIdentifier, out OletxTransactionIsolationLevel isolationLevel, out TransactionShim transactionShim)
	{
		ITransactionImport transactionImport = (ITransactionImport)_transactionDispenser;
		transactionImport.Import(Convert.ToUInt32(cookie.Length), cookie, in Guids.IID_ITransaction_Guid, out var ppvTransaction);
		SetupTransaction((ITransaction)ppvTransaction, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
	}

	public void ReceiveTransaction(byte[] propagationToken, OutcomeEnlistment managedIdentifier, out Guid transactionIdentifier, out OletxTransactionIsolationLevel isolationLevel, out TransactionShim transactionShim)
	{
		ITransactionReceiver cachedReceiver = GetCachedReceiver();
		try
		{
			cachedReceiver.UnmarshalPropagationToken(Convert.ToUInt32(propagationToken.Length), propagationToken, out var ppTransaction);
			SetupTransaction(ppTransaction, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
		}
		finally
		{
			ReturnCachedReceiver(cachedReceiver);
		}
	}

	public void CreateTransactionShim(IDtcTransaction transactionNative, OutcomeEnlistment managedIdentifier, out Guid transactionIdentifier, out OletxTransactionIsolationLevel isolationLevel, out TransactionShim transactionShim)
	{
		ITransactionCloner transactionCloner = (ITransactionCloner)TransactionInterop.GetITransactionFromIDtcTransaction(transactionNative);
		transactionCloner.CloneWithCommitDisabled(out var ppITransaction);
		SetupTransaction(ppITransaction, managedIdentifier, out transactionIdentifier, out isolationLevel, out transactionShim);
	}

	public void GetNotification(out object managedIdentifier, out ShimNotificationType shimNotificationType, out bool isSinglePhase, out bool abortingHint, out bool releaseLock, out byte[] prepareInfo)
	{
		managedIdentifier = null;
		shimNotificationType = ShimNotificationType.None;
		isSinglePhase = false;
		abortingHint = false;
		releaseLock = false;
		prepareInfo = null;
		Monitor.Enter(_notificationLock);
		NotificationShimBase result;
		bool flag = _notifications.TryDequeue(out result);
		if (flag)
		{
			managedIdentifier = result.EnlistmentIdentifier;
			shimNotificationType = result.NotificationType;
			isSinglePhase = result.IsSinglePhase;
			abortingHint = result.AbortingHint;
			prepareInfo = result.PrepareInfo;
		}
		if (!flag || shimNotificationType != ShimNotificationType.ResourceManagerTmDownNotify)
		{
			Monitor.Exit(_notificationLock);
		}
		else
		{
			releaseLock = true;
		}
	}

	private void SetupTransaction(ITransaction transaction, object managedIdentifier, out Guid pTransactionIdentifier, out OletxTransactionIsolationLevel pIsolationLevel, out TransactionShim ppTransactionShim)
	{
		TransactionNotifyShim transactionNotifyShim = new TransactionNotifyShim(this, managedIdentifier);
		transaction.GetTransactionInfo(out var xactInfo);
		IConnectionPointContainer connectionPointContainer = (IConnectionPointContainer)TransactionInterop.GetDtcTransaction(transaction);
		Guid riid = Guids.IID_ITransactionOutcomeEvents_Guid;
		connectionPointContainer.FindConnectionPoint(ref riid, out IConnectionPoint ppCP);
		ppCP.Advise(transactionNotifyShim, out var _);
		TransactionShim transactionShim = new TransactionShim(this, transactionNotifyShim, transaction);
		pTransactionIdentifier = xactInfo.Uow;
		pIsolationLevel = xactInfo.IsoLevel;
		ppTransactionShim = transactionShim;
	}

	private ITransactionOptions GetCachedOptions()
	{
		if (_cachedOptions.TryDequeue(out var result))
		{
			return result;
		}
		_transactionDispenser.GetOptionsObject(out var ppOptions);
		return ppOptions;
	}

	internal void ReturnCachedOptions(ITransactionOptions options)
	{
		_cachedOptions.Enqueue(options);
	}

	internal ITransactionTransmitter GetCachedTransmitter(ITransaction transaction)
	{
		if (!_cachedTransmitters.TryDequeue(out var result))
		{
			ITransactionTransmitterFactory transactionTransmitterFactory = (ITransactionTransmitterFactory)_transactionDispenser;
			transactionTransmitterFactory.Create(out result);
		}
		result.Set(transaction);
		return result;
	}

	internal void ReturnCachedTransmitter(ITransactionTransmitter transmitter)
	{
		if (_cachedTransmitters.Count < s_maxCachedInterfaces)
		{
			transmitter.Reset();
			_cachedTransmitters.Enqueue(transmitter);
		}
	}

	internal ITransactionReceiver GetCachedReceiver()
	{
		if (_cachedReceivers.TryDequeue(out var result))
		{
			return result;
		}
		ITransactionReceiverFactory transactionReceiverFactory = (ITransactionReceiverFactory)_transactionDispenser;
		transactionReceiverFactory.Create(out var pTxReceiver);
		return pTxReceiver;
	}

	internal void ReturnCachedReceiver(ITransactionReceiver receiver)
	{
		if (_cachedReceivers.Count < s_maxCachedInterfaces)
		{
			receiver.Reset();
			_cachedReceivers.Enqueue(receiver);
		}
	}
}
