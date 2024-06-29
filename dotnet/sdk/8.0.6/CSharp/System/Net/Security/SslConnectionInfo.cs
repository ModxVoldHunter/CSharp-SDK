namespace System.Net.Security;

internal struct SslConnectionInfo
{
	private static readonly byte[] s_http1 = SslApplicationProtocol.Http11.Protocol.ToArray();

	private static readonly byte[] s_http2 = SslApplicationProtocol.Http2.Protocol.ToArray();

	private static readonly byte[] s_http3 = SslApplicationProtocol.Http3.Protocol.ToArray();

	public int Protocol { get; private set; }

	public TlsCipherSuite TlsCipherSuite { get; private set; }

	public int DataCipherAlg { get; private set; }

	public int DataKeySize { get; private set; }

	public int DataHashAlg { get; private set; }

	public int DataHashKeySize { get; private set; }

	public int KeyExchangeAlg { get; private set; }

	public int KeyExchKeySize { get; private set; }

	public byte[] ApplicationProtocol { get; internal set; }

	private static byte[] GetNegotiatedApplicationProtocol(SafeDeleteContext context)
	{
		global::Interop.SecPkgContext_ApplicationProtocol attribute = default(global::Interop.SecPkgContext_ApplicationProtocol);
		if (SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, context, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_APPLICATION_PROTOCOL, ref attribute) && attribute.ProtoNegoExt == global::Interop.ApplicationProtocolNegotiationExt.ALPN && attribute.ProtoNegoStatus == global::Interop.ApplicationProtocolNegotiationStatus.Success)
		{
			if (attribute.Protocol.SequenceEqual(s_http1))
			{
				return s_http1;
			}
			if (attribute.Protocol.SequenceEqual(s_http2))
			{
				return s_http2;
			}
			if (attribute.Protocol.SequenceEqual(s_http3))
			{
				return s_http3;
			}
			return attribute.Protocol.ToArray();
		}
		return null;
	}

	public void UpdateSslConnectionInfo(SafeDeleteContext securityContext)
	{
		SecPkgContext_ConnectionInfo attribute = default(SecPkgContext_ConnectionInfo);
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO, ref attribute);
		TlsCipherSuite tlsCipherSuite = TlsCipherSuite.TLS_NULL_WITH_NULL_NULL;
		SecPkgContext_CipherInfo attribute2 = default(SecPkgContext_CipherInfo);
		if (SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CIPHER_INFO, ref attribute2))
		{
			tlsCipherSuite = (TlsCipherSuite)attribute2.dwCipherSuite;
		}
		Protocol = attribute.Protocol;
		DataCipherAlg = attribute.DataCipherAlg;
		DataKeySize = attribute.DataKeySize;
		DataHashAlg = attribute.DataHashAlg;
		DataHashKeySize = attribute.DataHashKeySize;
		KeyExchangeAlg = attribute.KeyExchangeAlg;
		KeyExchKeySize = attribute.KeyExchKeySize;
		TlsCipherSuite = tlsCipherSuite;
		ApplicationProtocol = GetNegotiatedApplicationProtocol(securityContext);
	}
}
