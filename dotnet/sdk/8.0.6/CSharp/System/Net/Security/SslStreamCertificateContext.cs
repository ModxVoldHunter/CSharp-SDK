using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

public class SslStreamCertificateContext
{
	internal readonly SslCertificateTrust Trust;

	public X509Certificate2 TargetCertificate { get; }

	public ReadOnlyCollection<X509Certificate2> IntermediateCertificates { get; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline)
	{
		return Create(target, additionalCertificates, offline, null);
	}

	public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline = false, SslCertificateTrust? trust = null)
	{
		return Create(target, additionalCertificates, offline, trust, noOcspFetch: false);
	}

	internal static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection additionalCertificates, bool offline, SslCertificateTrust trust, bool noOcspFetch)
	{
		if (!target.HasPrivateKey)
		{
			throw new NotSupportedException(System.SR.net_ssl_io_no_server_cert);
		}
		X509Certificate2[] array = Array.Empty<X509Certificate2>();
		X509Certificate2 x509Certificate = null;
		using (X509Chain x509Chain = new X509Chain())
		{
			if (additionalCertificates != null)
			{
				foreach (X509Certificate2 additionalCertificate in additionalCertificates)
				{
					x509Chain.ChainPolicy.ExtraStore.Add((X509Certificate)additionalCertificate);
				}
			}
			x509Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
			x509Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
			x509Chain.ChainPolicy.DisableCertificateDownloads = offline;
			if (!x509Chain.Build(target) && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"Failed to build chain for {target.Subject}", "Create");
			}
			int num = x509Chain.ChainElements.Count - 1;
			if (num >= 0)
			{
				if (num > 0 && x509Chain.ChainElements.Count > 1)
				{
					array = new X509Certificate2[num];
					for (int i = 0; i < num; i++)
					{
						array[i] = x509Chain.ChainElements[i + 1].Certificate;
					}
				}
				x509Chain.ChainElements[0].Certificate.Dispose();
				int num2 = ((x509Certificate == null) ? x509Chain.ChainElements.Count : (x509Chain.ChainElements.Count - 1));
				for (int j = num + 1; j < num2; j++)
				{
					x509Chain.ChainElements[j].Certificate.Dispose();
				}
			}
		}
		SslStreamCertificateContext result = new SslStreamCertificateContext(target, new ReadOnlyCollection<X509Certificate2>(array), trust);
		if (0 == 0)
		{
			x509Certificate?.Dispose();
		}
		return result;
	}

	internal SslStreamCertificateContext Duplicate()
	{
		return new SslStreamCertificateContext(new X509Certificate2(TargetCertificate), IntermediateCertificates, Trust);
	}

	internal static SslStreamCertificateContext Create(X509Certificate2 target)
	{
		return new SslStreamCertificateContext(target, new ReadOnlyCollection<X509Certificate2>(Array.Empty<X509Certificate2>()), null);
	}

	private SslStreamCertificateContext(X509Certificate2 target, ReadOnlyCollection<X509Certificate2> intermediates, SslCertificateTrust trust)
	{
		if (intermediates.Count > 0)
		{
			using X509Chain x509Chain = new X509Chain();
			x509Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
			x509Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
			x509Chain.ChainPolicy.DisableCertificateDownloads = true;
			bool flag = x509Chain.Build(target);
			int num = 0;
			X509ChainStatus[] chainStatus = x509Chain.ChainStatus;
			for (int i = 0; i < chainStatus.Length; i++)
			{
				X509ChainStatus x509ChainStatus = chainStatus[i];
				if (x509ChainStatus.Status.HasFlag(X509ChainStatusFlags.PartialChain) || x509ChainStatus.Status.HasFlag(X509ChainStatusFlags.NotSignatureValid))
				{
					flag = false;
					break;
				}
				num++;
			}
			if (!flag)
			{
				X509Store x509Store = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
				try
				{
					x509Store.Open(OpenFlags.ReadWrite);
				}
				catch
				{
					x509Store.Dispose();
					x509Store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
					try
					{
						x509Store.Open(OpenFlags.ReadWrite);
					}
					catch
					{
						x509Store.Dispose();
						x509Store = null;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Error(this, $"Failed to open certificate store for intermediates.", ".ctor");
						}
					}
				}
				if (x509Store != null)
				{
					using (x509Store)
					{
						for (int j = num; j < intermediates.Count - 1; j++)
						{
							x509Store.Add(intermediates[j]);
						}
						flag = x509Chain.Build(target);
						X509ChainStatus[] chainStatus2 = x509Chain.ChainStatus;
						for (int k = 0; k < chainStatus2.Length; k++)
						{
							X509ChainStatus x509ChainStatus2 = chainStatus2[k];
							if (x509ChainStatus2.Status.HasFlag(X509ChainStatusFlags.PartialChain) || x509ChainStatus2.Status.HasFlag(X509ChainStatusFlags.NotSignatureValid))
							{
								flag = false;
								break;
							}
						}
						if (!flag)
						{
							x509Store.Add(intermediates[intermediates.Count - 1]);
						}
					}
				}
			}
		}
		IntermediateCertificates = intermediates;
		TargetCertificate = target;
		Trust = trust;
	}
}
