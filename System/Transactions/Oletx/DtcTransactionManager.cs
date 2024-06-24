using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class DtcTransactionManager
{
	private readonly string _nodeName;

	private readonly OletxTransactionManager _oletxTm;

	private readonly DtcProxyShimFactory _proxyShimFactory;

	private byte[] _whereabouts;

	internal DtcProxyShimFactory ProxyShimFactory
	{
		get
		{
			if (_whereabouts == null)
			{
				lock (this)
				{
					Initialize();
				}
			}
			return _proxyShimFactory;
		}
	}

	internal byte[] Whereabouts
	{
		get
		{
			if (_whereabouts == null)
			{
				lock (this)
				{
					Initialize();
				}
			}
			return _whereabouts;
		}
	}

	internal DtcTransactionManager(string nodeName, OletxTransactionManager oletxTm)
	{
		_nodeName = nodeName;
		_oletxTm = oletxTm;
		_proxyShimFactory = OletxTransactionManager.ProxyShimFactory;
	}

	[MemberNotNull("_whereabouts")]
	private void Initialize()
	{
		if (_whereabouts != null)
		{
			return;
		}
		OletxInternalResourceManager internalResourceManager = _oletxTm.InternalResourceManager;
		try
		{
			_proxyShimFactory.ConnectToProxy(_nodeName, internalResourceManager.Identifier, internalResourceManager, out var nodeNameMatches, out _whereabouts, out var resourceManagerShim);
			if (!nodeNameMatches)
			{
				throw new NotSupportedException(System.SR.ProxyCannotSupportMultipleNodeNames);
			}
			internalResourceManager.ResourceManagerShim = resourceManagerShim;
			internalResourceManager.CallReenlistComplete();
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode == OletxHelper.XACT_E_NOTSUPPORTED)
			{
				throw new NotSupportedException(System.SR.CannotSupportNodeNameSpecification);
			}
			OletxTransactionManager.ProxyException(ex);
			throw TransactionManagerCommunicationException.Create(System.SR.TransactionManagerCommunicationException, ex);
		}
	}

	internal void ReleaseProxy()
	{
		lock (this)
		{
			_whereabouts = null;
		}
	}

	internal static uint AdjustTimeout(TimeSpan timeout)
	{
		uint num = 0u;
		try
		{
			return Convert.ToUInt32(timeout.TotalMilliseconds, CultureInfo.CurrentCulture);
		}
		catch (OverflowException exception)
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, exception);
			}
			return uint.MaxValue;
		}
	}
}
