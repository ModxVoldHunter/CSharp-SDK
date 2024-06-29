namespace System.Security.Cryptography.X509Certificates;

public sealed class X509ChainPolicy
{
	private X509RevocationMode _revocationMode;

	private X509RevocationFlag _revocationFlag;

	private X509VerificationFlags _verificationFlags;

	private X509ChainTrustMode _trustMode;

	private DateTime _verificationTime;

	internal OidCollection _applicationPolicy;

	internal OidCollection _certificatePolicy;

	internal X509Certificate2Collection _extraStore;

	internal X509Certificate2Collection _customTrustStore;

	public bool DisableCertificateDownloads { get; set; }

	public bool VerificationTimeIgnored { get; set; }

	public OidCollection ApplicationPolicy => _applicationPolicy ?? (_applicationPolicy = new OidCollection());

	public OidCollection CertificatePolicy => _certificatePolicy ?? (_certificatePolicy = new OidCollection());

	public X509Certificate2Collection ExtraStore => _extraStore ?? (_extraStore = new X509Certificate2Collection());

	public X509Certificate2Collection CustomTrustStore => _customTrustStore ?? (_customTrustStore = new X509Certificate2Collection());

	public X509RevocationMode RevocationMode
	{
		get
		{
			return _revocationMode;
		}
		set
		{
			if (value < X509RevocationMode.NoCheck || value > X509RevocationMode.Offline)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "value"));
			}
			_revocationMode = value;
		}
	}

	public X509RevocationFlag RevocationFlag
	{
		get
		{
			return _revocationFlag;
		}
		set
		{
			if (value < X509RevocationFlag.EndCertificateOnly || value > X509RevocationFlag.ExcludeRoot)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "value"));
			}
			_revocationFlag = value;
		}
	}

	public X509VerificationFlags VerificationFlags
	{
		get
		{
			return _verificationFlags;
		}
		set
		{
			if (value < X509VerificationFlags.NoFlag || value > X509VerificationFlags.AllFlags)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "value"));
			}
			_verificationFlags = value;
		}
	}

	public X509ChainTrustMode TrustMode
	{
		get
		{
			return _trustMode;
		}
		set
		{
			if (value < X509ChainTrustMode.System || value > X509ChainTrustMode.CustomRootTrust)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "value"));
			}
			_trustMode = value;
		}
	}

	public DateTime VerificationTime
	{
		get
		{
			return _verificationTime;
		}
		set
		{
			_verificationTime = value;
			VerificationTimeIgnored = false;
		}
	}

	public TimeSpan UrlRetrievalTimeout { get; set; }

	public X509ChainPolicy()
	{
		Reset();
	}

	public void Reset()
	{
		_applicationPolicy = null;
		_certificatePolicy = null;
		_extraStore = null;
		_customTrustStore = null;
		DisableCertificateDownloads = false;
		_revocationMode = X509RevocationMode.Online;
		_revocationFlag = X509RevocationFlag.ExcludeRoot;
		_verificationFlags = X509VerificationFlags.NoFlag;
		_trustMode = X509ChainTrustMode.System;
		_verificationTime = DateTime.Now;
		VerificationTimeIgnored = true;
		UrlRetrievalTimeout = TimeSpan.Zero;
	}

	public X509ChainPolicy Clone()
	{
		X509ChainPolicy x509ChainPolicy = new X509ChainPolicy
		{
			DisableCertificateDownloads = DisableCertificateDownloads,
			_revocationMode = _revocationMode,
			_revocationFlag = _revocationFlag,
			_verificationFlags = _verificationFlags,
			_trustMode = _trustMode,
			_verificationTime = _verificationTime,
			VerificationTimeIgnored = VerificationTimeIgnored,
			UrlRetrievalTimeout = UrlRetrievalTimeout
		};
		OidCollection applicationPolicy = _applicationPolicy;
		if (applicationPolicy != null && applicationPolicy.Count > 0)
		{
			OidEnumerator enumerator = _applicationPolicy.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Oid current = enumerator.Current;
				x509ChainPolicy.ApplicationPolicy.Add(current);
			}
		}
		OidCollection certificatePolicy = _certificatePolicy;
		if (certificatePolicy != null && certificatePolicy.Count > 0)
		{
			OidEnumerator enumerator2 = _certificatePolicy.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				Oid current2 = enumerator2.Current;
				x509ChainPolicy.CertificatePolicy.Add(current2);
			}
		}
		X509Certificate2Collection customTrustStore = _customTrustStore;
		if (customTrustStore != null && customTrustStore.Count > 0)
		{
			x509ChainPolicy.CustomTrustStore.AddRange(_customTrustStore);
		}
		X509Certificate2Collection extraStore = _extraStore;
		if (extraStore != null && extraStore.Count > 0)
		{
			x509ChainPolicy.ExtraStore.AddRange(_extraStore);
		}
		return x509ChainPolicy;
	}
}
