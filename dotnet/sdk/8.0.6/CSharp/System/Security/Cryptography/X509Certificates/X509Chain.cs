using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Chain : IDisposable
{
	private X509ChainPolicy _chainPolicy;

	private volatile X509ChainStatus[] _lazyChainStatus;

	private X509ChainElementCollection _chainElements;

	private IChainPal _pal;

	private bool _useMachineContext;

	private readonly object _syncRoot = new object();

	public X509ChainElementCollection ChainElements => _chainElements ?? (_chainElements = new X509ChainElementCollection());

	public X509ChainPolicy ChainPolicy
	{
		get
		{
			return _chainPolicy ?? (_chainPolicy = new X509ChainPolicy());
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_chainPolicy = value;
		}
	}

	public X509ChainStatus[] ChainStatus => _lazyChainStatus ?? (_lazyChainStatus = ((_pal == null) ? Array.Empty<X509ChainStatus>() : _pal.ChainStatus));

	public nint ChainContext => SafeHandle?.DangerousGetHandle() ?? IntPtr.Zero;

	public SafeX509ChainHandle? SafeHandle
	{
		get
		{
			if (_pal == null)
			{
				return SafeX509ChainHandle.InvalidHandle;
			}
			return _pal.SafeHandle;
		}
	}

	public X509Chain()
	{
	}

	public X509Chain(bool useMachineContext)
	{
		_useMachineContext = useMachineContext;
	}

	[SupportedOSPlatform("windows")]
	public X509Chain(nint chainContext)
	{
		_pal = ChainPal.FromHandle(chainContext);
		_chainElements = new X509ChainElementCollection(_pal.ChainElements);
	}

	public static X509Chain Create()
	{
		return new X509Chain();
	}

	[UnsupportedOSPlatform("browser")]
	public bool Build(X509Certificate2 certificate)
	{
		return Build(certificate, throwOnException: true);
	}

	internal bool Build(X509Certificate2 certificate, bool throwOnException)
	{
		lock (_syncRoot)
		{
			if (certificate == null || certificate.Pal == null)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidContextHandle, "certificate");
			}
			if (_chainPolicy != null)
			{
				if (_chainPolicy._customTrustStore != null)
				{
					if (_chainPolicy.TrustMode == X509ChainTrustMode.System && _chainPolicy.CustomTrustStore.Count > 0)
					{
						throw new CryptographicException(System.SR.Cryptography_CustomTrustCertsInSystemMode);
					}
					foreach (X509Certificate2 item in _chainPolicy.CustomTrustStore)
					{
						if (item == null || item.Handle == IntPtr.Zero)
						{
							throw new CryptographicException(System.SR.Cryptography_InvalidTrustCertificate);
						}
					}
				}
				if (_chainPolicy.TrustMode == X509ChainTrustMode.CustomRootTrust && _chainPolicy._customTrustStore == null)
				{
					_chainPolicy._customTrustStore = new X509Certificate2Collection();
				}
			}
			Reset();
			X509ChainPolicy chainPolicy = ChainPolicy;
			_pal = ChainPal.BuildChain(_useMachineContext, certificate.Pal, chainPolicy._extraStore, chainPolicy._applicationPolicy, chainPolicy._certificatePolicy, chainPolicy.RevocationMode, chainPolicy.RevocationFlag, chainPolicy._customTrustStore, chainPolicy.TrustMode, chainPolicy.VerificationTimeIgnored ? DateTime.Now : chainPolicy.VerificationTime, chainPolicy.UrlRetrievalTimeout, chainPolicy.DisableCertificateDownloads);
			bool flag = false;
			if (_pal != null)
			{
				_chainElements = new X509ChainElementCollection(_pal.ChainElements);
				Exception exception;
				bool? flag2 = _pal.Verify(chainPolicy.VerificationFlags, out exception);
				if (!flag2.HasValue)
				{
					if (throwOnException)
					{
						throw exception;
					}
					flag2 = false;
				}
				flag = flag2.Value;
			}
			if (!flag && throwOnException)
			{
				X509ChainStatus[] array = _pal?.ChainStatus;
				if ((array == null || array.Length <= 0) && System.LocalAppContextSwitches.X509ChainBuildThrowOnInternalError)
				{
					throw new CryptographicException(System.SR.Cryptography_X509_ChainBuildingFailed);
				}
			}
			return flag;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Reset();
		}
	}

	public void Reset()
	{
		_lazyChainStatus = null;
		_chainElements = null;
		_useMachineContext = false;
		IChainPal pal = _pal;
		if (pal != null)
		{
			_pal = null;
			pal.Dispose();
		}
	}
}
