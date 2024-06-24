using System.Net.Security;

namespace System.Net.Quic;

public sealed class QuicServerConnectionOptions : QuicConnectionOptions
{
	public SslServerAuthenticationOptions ServerAuthenticationOptions { get; set; }

	public QuicServerConnectionOptions()
	{
		base.MaxInboundBidirectionalStreams = 100;
		base.MaxInboundUnidirectionalStreams = 10;
	}

	internal override void Validate(string argumentName)
	{
		base.Validate(argumentName);
		if (ServerAuthenticationOptions == null)
		{
			throw new ArgumentNullException(System.SR.Format(System.SR.net_quic_not_null_accept_connection, "ServerAuthenticationOptions"), argumentName);
		}
	}
}
