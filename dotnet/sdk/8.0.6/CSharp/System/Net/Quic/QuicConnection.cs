using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;

namespace System.Net.Quic;

public sealed class QuicConnection : IAsyncDisposable
{
	private readonly struct SslConnectionOptions
	{
		private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", null);

		private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", null);

		private readonly QuicConnection _connection;

		private readonly bool _isClient;

		private readonly string _targetHost;

		private readonly bool _certificateRequired;

		private readonly X509RevocationMode _revocationMode;

		private readonly RemoteCertificateValidationCallback _validationCallback;

		private readonly X509ChainPolicy _certificateChainPolicy;

		internal string TargetHost => _targetHost;

		public SslConnectionOptions(QuicConnection connection, bool isClient, string targetHost, bool certificateRequired, X509RevocationMode revocationMode, RemoteCertificateValidationCallback validationCallback, X509ChainPolicy certificateChainPolicy)
		{
			_connection = connection;
			_isClient = isClient;
			_targetHost = targetHost;
			_certificateRequired = certificateRequired;
			_revocationMode = revocationMode;
			_validationCallback = validationCallback;
			_certificateChainPolicy = certificateChainPolicy;
		}

		public unsafe int ValidateCertificate(QUIC_BUFFER* certificatePtr, QUIC_BUFFER* chainPtr, out X509Certificate2 certificate)
		{
			SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
			nint certificateBuffer = 0;
			int bufferLength = 0;
			bool flag = false;
			X509Chain x509Chain = null;
			X509Certificate2 x509Certificate = null;
			try
			{
				if (certificatePtr != null)
				{
					x509Chain = new X509Chain();
					if (_certificateChainPolicy != null)
					{
						x509Chain.ChainPolicy = _certificateChainPolicy;
					}
					else
					{
						x509Chain.ChainPolicy.RevocationMode = _revocationMode;
						x509Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
					}
					if (x509Chain.ChainPolicy.ApplicationPolicy.Count == 0)
					{
						x509Chain.ChainPolicy.ApplicationPolicy.Add(_isClient ? s_serverAuthOid : s_clientAuthOid);
					}
					if (MsQuicApi.UsesSChannelBackend)
					{
						x509Certificate = new X509Certificate2((nint)certificatePtr);
					}
					else
					{
						if (certificatePtr->Length != 0)
						{
							certificateBuffer = (nint)certificatePtr->Buffer;
							bufferLength = (int)certificatePtr->Length;
							x509Certificate = new X509Certificate2(certificatePtr->Span);
						}
						if (chainPtr->Length != 0)
						{
							X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
							x509Certificate2Collection.Import(chainPtr->Span);
							x509Chain.ChainPolicy.ExtraStore.AddRange(x509Certificate2Collection);
						}
					}
				}
				if (x509Certificate != null)
				{
					bool checkCertName = !x509Chain.ChainPolicy.VerificationFlags.HasFlag(X509VerificationFlags.IgnoreInvalidName);
					sslPolicyErrors |= System.Net.CertificateValidation.BuildChainAndVerifyProperties(x509Chain, x509Certificate, checkCertName, !_isClient, System.Net.Security.TargetHostNameHelper.NormalizeHostName(_targetHost), certificateBuffer, bufferLength);
				}
				else if (_certificateRequired)
				{
					sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
				}
				int result = MsQuic.QUIC_STATUS_SUCCESS;
				if (_validationCallback != null)
				{
					flag = true;
					if (!_validationCallback(_connection, x509Certificate, x509Chain, sslPolicyErrors))
					{
						flag = false;
						if (_isClient)
						{
							throw new AuthenticationException(System.SR.net_quic_cert_custom_validation);
						}
						result = MsQuic.QUIC_STATUS_USER_CANCELED;
					}
				}
				else if (sslPolicyErrors != 0)
				{
					if (_isClient)
					{
						throw new AuthenticationException(System.SR.Format(System.SR.net_quic_cert_chain_validation, sslPolicyErrors));
					}
					result = MsQuic.QUIC_STATUS_HANDSHAKE_FAILURE;
				}
				certificate = x509Certificate;
				return result;
			}
			catch (Exception innerException)
			{
				x509Certificate?.Dispose();
				if (flag)
				{
					throw new QuicException(QuicError.CallbackError, null, System.SR.net_quic_callback_error, innerException);
				}
				throw;
			}
			finally
			{
				if (x509Chain != null)
				{
					X509ChainElementCollection chainElements = x509Chain.ChainElements;
					for (int i = 0; i < chainElements.Count; i++)
					{
						chainElements[i].Certificate.Dispose();
					}
					x509Chain.Dispose();
				}
			}
		}
	}

	private readonly MsQuicContextSafeHandle _handle;

	private int _disposed;

	private readonly ValueTaskSource _connectedTcs = new ValueTaskSource();

	private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

	private readonly Channel<QuicStream> _acceptQueue = Channel.CreateUnbounded<QuicStream>(new UnboundedChannelOptions
	{
		SingleWriter = true
	});

	private SslConnectionOptions _sslConnectionOptions;

	private MsQuicSafeHandle _configuration;

	private bool _canAccept;

	private long _defaultStreamErrorCode;

	private long _defaultCloseErrorCode;

	private IPEndPoint _remoteEndPoint;

	private IPEndPoint _localEndPoint;

	private bool _remoteCertificateExposed;

	private X509Certificate2 _remoteCertificate;

	private SslApplicationProtocol _negotiatedApplicationProtocol;

	public static bool IsSupported => MsQuicApi.IsQuicSupported;

	public IPEndPoint RemoteEndPoint => _remoteEndPoint;

	public IPEndPoint LocalEndPoint => _localEndPoint;

	public string TargetHostName => _sslConnectionOptions.TargetHost ?? string.Empty;

	public X509Certificate? RemoteCertificate
	{
		get
		{
			_remoteCertificateExposed = true;
			return _remoteCertificate;
		}
	}

	public SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedApplicationProtocol;

	public static ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!IsSupported)
		{
			throw new PlatformNotSupportedException(System.SR.Format(System.SR.SystemNetQuic_PlatformNotSupported, MsQuicApi.NotSupportedReason));
		}
		options.Validate("options");
		return StartConnectAsync(options, cancellationToken);
		static async ValueTask<QuicConnection> StartConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken)
		{
			QuicConnection connection = new QuicConnection();
			try
			{
				await connection.FinishConnectAsync(options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
			return connection;
		}
	}

	public override string ToString()
	{
		return _handle.ToString();
	}

	private unsafe QuicConnection()
	{
		GCHandle gCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		try
		{
			Unsafe.SkipInit(out QUIC_HANDLE* handle);
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionOpen(MsQuicApi.Api.Registration, (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>)(delegate*<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>)(&NativeCallback), (void*)GCHandle.ToIntPtr(gCHandle), &handle), "ConnectionOpen failed");
			_handle = new MsQuicContextSafeHandle(handle, gCHandle, SafeHandleType.Connection);
		}
		catch
		{
			gCHandle.Free();
			throw;
		}
	}

	internal unsafe QuicConnection(QUIC_HANDLE* handle, QUIC_NEW_CONNECTION_INFO* info)
	{
		GCHandle gCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		try
		{
			_handle = new MsQuicContextSafeHandle(handle, gCHandle, SafeHandleType.Connection);
			delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> callback = (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>)(delegate*<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>)(&NativeCallback);
			MsQuicApi.Api.SetCallbackHandler(_handle, callback, (void*)GCHandle.ToIntPtr(gCHandle));
		}
		catch
		{
			gCHandle.Free();
			throw;
		}
		_remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info->RemoteAddress);
		_localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info->LocalAddress);
	}

	private unsafe async ValueTask FinishConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_connectedTcs.TryInitialize(out var valueTask, this, cancellationToken))
		{
			_canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
			_defaultStreamErrorCode = options.DefaultStreamErrorCode;
			_defaultCloseErrorCode = options.DefaultCloseErrorCode;
			if (!options.RemoteEndPoint.TryParse(out var host, out var address, out var port))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_quic_unsupported_endpoint_type, options.RemoteEndPoint.GetType()), "options");
			}
			if (address == null)
			{
				IPAddress[] array = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				cancellationToken.ThrowIfCancellationRequested();
				if (array.Length == 0)
				{
					throw new SocketException(11001);
				}
				address = array[0];
			}
			QuicAddr value = new IPEndPoint(address, port).ToQuicAddr();
			MsQuicHelpers.SetMsQuicParameter(_handle, 83886082u, value);
			if (options.LocalEndPoint != null)
			{
				QuicAddr value2 = options.LocalEndPoint.ToQuicAddr();
				MsQuicHelpers.SetMsQuicParameter(_handle, 83886081u, value2);
			}
			_sslConnectionOptions = new SslConnectionOptions(this, isClient: true, options.ClientAuthenticationOptions.TargetHost ?? string.Empty, certificateRequired: true, options.ClientAuthenticationOptions.CertificateRevocationCheckMode, options.ClientAuthenticationOptions.RemoteCertificateValidationCallback, options.ClientAuthenticationOptions.CertificateChainPolicy?.Clone());
			_configuration = MsQuicConfiguration.Create(options);
			string s = (System.Net.Security.TargetHostNameHelper.IsValidAddress(options.ClientAuthenticationOptions.TargetHost) ? null : options.ClientAuthenticationOptions.TargetHost) ?? host ?? address?.ToString() ?? string.Empty;
			nint num = Marshal.StringToCoTaskMemUTF8(s);
			try
			{
				ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionStart(_handle, _configuration, (ushort)value.Family, (sbyte*)num, (ushort)port), "ConnectionStart failed");
			}
			finally
			{
				Marshal.FreeCoTaskMem(num);
			}
		}
		await valueTask.ConfigureAwait(continueOnCapturedContext: false);
	}

	internal ValueTask FinishHandshakeAsync(QuicServerConnectionOptions options, string targetHost, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_connectedTcs.TryInitialize(out var valueTask, this, cancellationToken))
		{
			_canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
			_defaultStreamErrorCode = options.DefaultStreamErrorCode;
			_defaultCloseErrorCode = options.DefaultCloseErrorCode;
			if (System.Net.Security.TargetHostNameHelper.IsValidAddress(targetHost))
			{
				targetHost = string.Empty;
			}
			_sslConnectionOptions = new SslConnectionOptions(this, isClient: false, targetHost, options.ServerAuthenticationOptions.ClientCertificateRequired, options.ServerAuthenticationOptions.CertificateRevocationCheckMode, options.ServerAuthenticationOptions.RemoteCertificateValidationCallback, options.ServerAuthenticationOptions.CertificateChainPolicy?.Clone());
			_configuration = MsQuicConfiguration.Create(options, targetHost);
			ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionSetConfiguration(_handle, _configuration), "ConnectionSetConfiguration failed");
		}
		return valueTask;
	}

	public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		QuicStream stream = null;
		try
		{
			stream = new QuicStream(_handle, type, _defaultStreamErrorCode);
			await stream.StartAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			if (stream != null)
			{
				await stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_acceptQueue.Reader.Completion.IsFaulted)
			{
				await _acceptQueue.Reader.Completion.ConfigureAwait(continueOnCapturedContext: false);
			}
			throw;
		}
		return stream;
	}

	public async ValueTask<QuicStream> AcceptInboundStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		if (!_canAccept)
		{
			throw new InvalidOperationException(System.SR.net_quic_accept_not_allowed);
		}
		GCHandle keepObject = GCHandle.Alloc(this);
		try
		{
			return await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (ChannelClosedException ex) when (ex.InnerException != null)
		{
			ExceptionDispatchInfo.Throw(ex.InnerException);
			throw;
		}
		finally
		{
			keepObject.Free();
		}
	}

	public ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed == 1, this);
		if (_shutdownTcs.TryInitialize(out var valueTask, this, cancellationToken))
		{
			MsQuicApi.Api.ConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, (ulong)errorCode);
		}
		return valueTask;
	}

	private unsafe int HandleEventConnected(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._CONNECTED_e__Struct data)
	{
		_negotiatedApplicationProtocol = new SslApplicationProtocol(new Span<byte>(data.NegotiatedAlpn, data.NegotiatedAlpnLength).ToArray());
		QuicAddr msQuicParameter = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, 83886082u);
		_remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&msQuicParameter);
		QuicAddr msQuicParameter2 = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, 83886081u);
		_localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&msQuicParameter2);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{this} Connection connected {LocalEndPoint} -> {RemoteEndPoint} for {_negotiatedApplicationProtocol} protocol", "HandleEventConnected");
		}
		_connectedTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventShutdownInitiatedByTransport(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct data)
	{
		Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetExceptionForMsQuicStatus(data.Status, (long)data.ErrorCode));
		_connectedTcs.TrySetException(ex);
		_acceptQueue.Writer.TryComplete(ex);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventShutdownInitiatedByPeer(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct data)
	{
		_acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetConnectionAbortedException((long)data.ErrorCode)));
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private int HandleEventShutdownComplete()
	{
		Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetOperationAbortedException());
		_acceptQueue.Writer.TryComplete(ex);
		_connectedTcs.TrySetException(ex);
		_shutdownTcs.TrySetResult();
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private unsafe int HandleEventLocalAddressChanged(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct data)
	{
		_localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private unsafe int HandleEventPeerAddressChanged(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct data)
	{
		_remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private unsafe int HandleEventPeerStreamStarted(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct data)
	{
		QuicStream quicStream = new QuicStream(_handle, data.Stream, data.Flags, _defaultStreamErrorCode);
		if (!_acceptQueue.Writer.TryWrite(quicStream))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"{this} Unable to enqueue incoming stream {quicStream}", "HandleEventPeerStreamStarted");
			}
			quicStream.Dispose();
			return MsQuic.QUIC_STATUS_SUCCESS;
		}
		data.Flags |= QUIC_STREAM_OPEN_FLAGS.DELAY_FC_UPDATES;
		return MsQuic.QUIC_STATUS_SUCCESS;
	}

	private unsafe int HandleEventPeerCertificateReceived(ref QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct data)
	{
		try
		{
			return _sslConnectionOptions.ValidateCertificate((QUIC_BUFFER*)data.Certificate, (QUIC_BUFFER*)data.Chain, out _remoteCertificate);
		}
		catch (Exception exception)
		{
			_connectedTcs.TrySetException(exception);
			return MsQuic.QUIC_STATUS_HANDSHAKE_FAILURE;
		}
	}

	private int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
	{
		return connectionEvent.Type switch
		{
			QUIC_CONNECTION_EVENT_TYPE.CONNECTED => HandleEventConnected(ref connectionEvent.CONNECTED), 
			QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT => HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT), 
			QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER => HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER), 
			QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(), 
			QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED => HandleEventLocalAddressChanged(ref connectionEvent.LOCAL_ADDRESS_CHANGED), 
			QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED => HandleEventPeerAddressChanged(ref connectionEvent.PEER_ADDRESS_CHANGED), 
			QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED => HandleEventPeerStreamStarted(ref connectionEvent.PEER_STREAM_STARTED), 
			QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED => HandleEventPeerCertificateReceived(ref connectionEvent.PEER_CERTIFICATE_RECEIVED), 
			_ => MsQuic.QUIC_STATUS_SUCCESS, 
		};
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
	private unsafe static int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_CONNECTION_EVENT* connectionEvent)
	{
		GCHandle gCHandle = GCHandle.FromIntPtr((nint)context);
		if (!gCHandle.IsAllocated || !(gCHandle.Target is QuicConnection quicConnection))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"Received event {connectionEvent->Type} while connection is already disposed", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INVALID_STATE;
		}
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(quicConnection, $"{quicConnection} Received event {connectionEvent->Type} {connectionEvent->ToString()}", "NativeCallback");
			}
			return quicConnection.HandleConnectionEvent(ref *connectionEvent);
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(quicConnection, $"{quicConnection} Exception while processing event {connectionEvent->Type}: {ex}", "NativeCallback");
			}
			return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			if (_shutdownTcs.TryInitialize(out var valueTask, this))
			{
				MsQuicApi.Api.ConnectionShutdown(_handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, (ulong)_defaultCloseErrorCode);
			}
			await valueTask.ConfigureAwait(continueOnCapturedContext: false);
			_handle.Dispose();
			_configuration?.Dispose();
			if (!_remoteCertificateExposed)
			{
				_remoteCertificate?.Dispose();
			}
			_acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetOperationAbortedException()));
			QuicStream item;
			while (_acceptQueue.Reader.TryRead(out item))
			{
				await item.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
