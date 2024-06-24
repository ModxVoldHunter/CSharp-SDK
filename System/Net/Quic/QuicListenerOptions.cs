using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

public sealed class QuicListenerOptions
{
	public IPEndPoint ListenEndPoint { get; set; }

	public List<SslApplicationProtocol> ApplicationProtocols { get; set; }

	public int ListenBacklog { get; set; }

	public Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> ConnectionOptionsCallback { get; set; }

	internal void Validate(string argumentName)
	{
		if (ListenEndPoint == null)
		{
			throw new ArgumentNullException(System.SR.Format(System.SR.net_quic_not_null_listener, "ListenEndPoint"), argumentName);
		}
		if (ApplicationProtocols == null || ApplicationProtocols.Count <= 0)
		{
			throw new ArgumentNullException(System.SR.Format(System.SR.net_quic_not_null_not_empty_listener, "ApplicationProtocols"), argumentName);
		}
		if (ListenBacklog == 0)
		{
			ListenBacklog = 512;
		}
		if (ConnectionOptionsCallback == null)
		{
			throw new ArgumentNullException(System.SR.Format(System.SR.net_quic_not_null_listener, "ConnectionOptionsCallback"), argumentName);
		}
	}
}
