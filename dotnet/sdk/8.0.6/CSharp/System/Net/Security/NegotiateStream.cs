using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

public class NegotiateStream : AuthenticatedStream
{
	private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException("NegotiateStream", (string?)null));

	private static readonly byte[] s_emptyMessage = new byte[0];

	private readonly byte[] _writeHeader;

	private readonly byte[] _readHeader;

	private byte[] _readBuffer;

	private int _readBufferOffset;

	private int _readBufferCount;

	private ArrayBufferWriter<byte> _writeBuffer;

	private volatile int _writeInProgress;

	private volatile int _readInProgress;

	private volatile int _authInProgress;

	private ExceptionDispatchInfo _exception;

	private StreamFramer _framer;

	private NegotiateAuthentication _context;

	private bool _canRetryAuthentication;

	private ProtectionLevel _expectedProtectionLevel;

	private TokenImpersonationLevel _expectedImpersonationLevel;

	private ExtendedProtectionPolicy _extendedProtectionPolicy;

	private bool isNtlm;

	private bool _remoteOk;

	public override bool IsAuthenticated => IsAuthenticatedCore;

	[MemberNotNullWhen(true, "_context")]
	private bool IsAuthenticatedCore
	{
		[MemberNotNullWhen(true, "_context")]
		get
		{
			if (_context != null && HandshakeComplete && _exception == null)
			{
				return _remoteOk;
			}
			return false;
		}
	}

	public override bool IsMutuallyAuthenticated
	{
		get
		{
			if (IsAuthenticatedCore && !string.Equals(_context.Package, "NTLM"))
			{
				return _context.IsMutuallyAuthenticated;
			}
			return false;
		}
	}

	public override bool IsEncrypted
	{
		get
		{
			if (IsAuthenticatedCore)
			{
				return _context.IsEncrypted;
			}
			return false;
		}
	}

	public override bool IsSigned
	{
		get
		{
			if (IsAuthenticatedCore)
			{
				if (!_context.IsSigned)
				{
					return _context.IsEncrypted;
				}
				return true;
			}
			return false;
		}
	}

	public override bool IsServer
	{
		get
		{
			if (_context != null)
			{
				return _context.IsServer;
			}
			return false;
		}
	}

	public virtual TokenImpersonationLevel ImpersonationLevel
	{
		get
		{
			ThrowIfFailed(authSuccessCheck: true);
			return PrivateImpersonationLevel;
		}
	}

	private TokenImpersonationLevel PrivateImpersonationLevel => _context.ImpersonationLevel;

	private bool HandshakeComplete => _context.IsAuthenticated;

	private bool CanGetSecureStream
	{
		get
		{
			if (!_context.IsEncrypted)
			{
				return _context.IsSigned;
			}
			return true;
		}
	}

	public virtual IIdentity RemoteIdentity
	{
		get
		{
			ThrowIfFailed(authSuccessCheck: true);
			return _context.RemoteIdentity;
		}
	}

	public override bool CanSeek => false;

	public override bool CanRead
	{
		get
		{
			if (IsAuthenticated)
			{
				return base.InnerStream.CanRead;
			}
			return false;
		}
	}

	public override bool CanTimeout => base.InnerStream.CanTimeout;

	public override bool CanWrite
	{
		get
		{
			if (IsAuthenticated)
			{
				return base.InnerStream.CanWrite;
			}
			return false;
		}
	}

	public override int ReadTimeout
	{
		get
		{
			return base.InnerStream.ReadTimeout;
		}
		set
		{
			base.InnerStream.ReadTimeout = value;
		}
	}

	public override int WriteTimeout
	{
		get
		{
			return base.InnerStream.WriteTimeout;
		}
		set
		{
			base.InnerStream.WriteTimeout = value;
		}
	}

	public override long Length => base.InnerStream.Length;

	public override long Position
	{
		get
		{
			return base.InnerStream.Position;
		}
		set
		{
			throw new NotSupportedException(System.SR.net_noseek);
		}
	}

	public NegotiateStream(Stream innerStream)
		: this(innerStream, leaveInnerStreamOpen: false)
	{
	}

	public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen)
		: base(innerStream, leaveInnerStreamOpen)
	{
		_writeHeader = new byte[4];
		_readHeader = new byte[4];
		_readBuffer = Array.Empty<byte>();
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			_exception = s_disposedSentinel;
			_context?.Dispose();
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override async ValueTask DisposeAsync()
	{
		try
		{
			_exception = s_disposedSentinel;
			_context?.Dispose();
		}
		finally
		{
			await base.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return TaskToAsyncResult.Begin(AuthenticateAsClientAsync(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public virtual void AuthenticateAsServer()
	{
		AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsServer(ExtendedProtectionPolicy? policy)
	{
		AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		AuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel);
	}

	public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
		AuthenticateAsync<SyncReadWriteAdapter>(default(CancellationToken)).GetAwaiter().GetResult();
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(ExtendedProtectionPolicy? policy, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return TaskToAsyncResult.Begin(AuthenticateAsServerAsync(credential, policy, requiredProtectionLevel, requiredImpersonationLevel), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	public virtual void AuthenticateAsClient()
	{
		AuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName)
	{
		AuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName)
	{
		AuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		AuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
		AuthenticateAsync<SyncReadWriteAdapter>(default(CancellationToken)).GetAwaiter().GetResult();
	}

	public virtual Task AuthenticateAsClientAsync()
	{
		return AuthenticateAsClientAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName)
	{
		return AuthenticateAsClientAsync(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		return AuthenticateAsClientAsync(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName)
	{
		return AuthenticateAsClientAsync(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
		return AuthenticateAsync<AsyncReadWriteAdapter>(default(CancellationToken));
	}

	public virtual Task AuthenticateAsServerAsync()
	{
		return AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy? policy)
	{
		return AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		return AuthenticateAsServerAsync(credential, null, requiredProtectionLevel, requiredImpersonationLevel);
	}

	public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
		return AuthenticateAsync<AsyncReadWriteAdapter>(default(CancellationToken));
	}

	public override void SetLength(long value)
	{
		base.InnerStream.SetLength(value);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public override void Flush()
	{
		base.InnerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return base.InnerStream.FlushAsync(cancellationToken);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.Read(buffer, offset, count);
		}
		return ReadAsync<SyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), default(CancellationToken)).GetAwaiter().GetResult();
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
		}
		return ReadAsync<AsyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.ReadAsync(buffer, cancellationToken);
		}
		return ReadAsync<AsyncReadWriteAdapter>(buffer, cancellationToken);
	}

	private async ValueTask<int> ReadAsync<TIOAdapter>(Memory<byte> buffer, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _readInProgress, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "read"));
		}
		try
		{
			ThrowIfFailed(authSuccessCheck: true);
			if (_readBufferCount != 0)
			{
				int num = Math.Min(_readBufferCount, buffer.Length);
				if (num != 0)
				{
					_readBuffer.AsMemory(_readBufferOffset, num).CopyTo(buffer);
					_readBufferOffset += num;
					_readBufferCount -= num;
				}
				return num;
			}
			do
			{
				if (await ReadAllAsync(base.InnerStream, _readHeader, allowZeroRead: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) == 0)
				{
					return 0;
				}
				int num2 = BinaryPrimitives.ReadInt32LittleEndian(_readHeader);
				if (num2 <= 4 || num2 > 65536)
				{
					throw new IOException(System.SR.net_frame_read_size);
				}
				_readBufferCount = num2;
				_readBufferOffset = 0;
				if (_readBuffer.Length < num2)
				{
					_readBuffer = new byte[num2];
				}
				num2 = await ReadAllAsync(base.InnerStream, new Memory<byte>(_readBuffer, 0, num2), allowZeroRead: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				NegotiateAuthenticationStatusCode negotiateAuthenticationStatusCode;
				if (isNtlm && !_context.IsEncrypted)
				{
					if (num2 < 16 || !_context.VerifyMIC(_readBuffer.AsSpan(16, num2 - 16), _readBuffer.AsSpan(0, 16)))
					{
						negotiateAuthenticationStatusCode = NegotiateAuthenticationStatusCode.InvalidToken;
					}
					else
					{
						_readBufferOffset = 16;
						_readBufferCount = num2 - 16;
						negotiateAuthenticationStatusCode = NegotiateAuthenticationStatusCode.Completed;
					}
				}
				else
				{
					negotiateAuthenticationStatusCode = _context.UnwrapInPlace(_readBuffer.AsSpan(0, num2), out _readBufferOffset, out _readBufferCount, out var _);
				}
				if (negotiateAuthenticationStatusCode != 0)
				{
					throw new IOException(System.SR.net_io_read);
				}
			}
			while (_readBufferCount == 0 && buffer.Length != 0);
			int num3 = Math.Min(_readBufferCount, buffer.Length);
			_readBuffer.AsMemory(_readBufferOffset, num3).CopyTo(buffer);
			_readBufferOffset += num3;
			_readBufferCount -= num3;
			return num3;
		}
		catch (Exception ex) when (!(ex is IOException) && !(ex is OperationCanceledException))
		{
			throw new IOException(System.SR.net_io_read, ex);
		}
		finally
		{
			_readInProgress = 0;
		}
		static async ValueTask<int> ReadAllAsync(Stream stream, Memory<byte> buffer, bool allowZeroRead, CancellationToken cancellationToken)
		{
			int num4 = await TIOAdapter.ReadAtLeastAsync(stream, buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (num4 < buffer.Length && (num4 != 0 || !allowZeroRead))
			{
				throw new IOException(System.SR.net_io_eof);
			}
			return num4;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			base.InnerStream.Write(buffer, offset, count);
		}
		else
		{
			WriteAsync<SyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), default(CancellationToken)).GetAwaiter().GetResult();
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
		}
		return WriteAsync<AsyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.WriteAsync(buffer, cancellationToken);
		}
		return new ValueTask(WriteAsync<AsyncReadWriteAdapter>(buffer, cancellationToken));
	}

	private async Task WriteAsync<TIOAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _writeInProgress, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "write"));
		}
		try
		{
			ThrowIfFailed(authSuccessCheck: true);
			while (!buffer.IsEmpty)
			{
				int chunkBytes = Math.Min(buffer.Length, 64512);
				bool isEncrypted = _context.IsEncrypted;
				ReadOnlyMemory<byte> readOnlyMemory = buffer.Slice(0, chunkBytes);
				NegotiateAuthenticationStatusCode negotiateAuthenticationStatusCode;
				if (isNtlm && !isEncrypted)
				{
					_context.GetMIC(readOnlyMemory.Span, _writeBuffer);
					_writeBuffer.Write(readOnlyMemory.Span);
					negotiateAuthenticationStatusCode = NegotiateAuthenticationStatusCode.Completed;
				}
				else
				{
					negotiateAuthenticationStatusCode = _context.Wrap(readOnlyMemory.Span, _writeBuffer, isEncrypted, out var _);
				}
				if (negotiateAuthenticationStatusCode != 0)
				{
					throw new IOException(System.SR.net_io_encrypt);
				}
				BinaryPrimitives.WriteInt32LittleEndian(_writeHeader, _writeBuffer.WrittenCount);
				await TIOAdapter.WriteAsync(base.InnerStream, _writeHeader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await TIOAdapter.WriteAsync(base.InnerStream, _writeBuffer.WrittenMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				buffer = buffer.Slice(chunkBytes);
				_writeBuffer.Clear();
			}
		}
		catch (Exception ex) when (!(ex is IOException) && !(ex is OperationCanceledException))
		{
			throw new IOException(System.SR.net_io_write, ex);
		}
		finally
		{
			_writeBuffer.Clear();
			_writeInProgress = 0;
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToAsyncResult.End<int>(asyncResult);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count), asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToAsyncResult.End(asyncResult);
	}

	private void ThrowIfExceptional()
	{
		ExceptionDispatchInfo exception = _exception;
		if (exception != null)
		{
			ThrowExceptional(exception);
		}
		void ThrowExceptional(ExceptionDispatchInfo e)
		{
			ObjectDisposedException.ThrowIf(e == s_disposedSentinel, this);
			e.Throw();
		}
	}

	private void ValidateCreateContext(string package, NetworkCredential credential, string servicePrincipalName, ExtendedProtectionPolicy policy, ProtectionLevel protectionLevel, TokenImpersonationLevel impersonationLevel)
	{
		if (policy != null)
		{
			if (policy.CustomChannelBinding == null && policy.CustomServiceNames == null)
			{
				throw new ArgumentException(System.SR.net_auth_must_specify_extended_protection_scheme, "policy");
			}
			_extendedProtectionPolicy = policy;
		}
		else
		{
			_extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
		}
		ValidateCreateContext(package, isServer: true, credential, servicePrincipalName, _extendedProtectionPolicy.CustomChannelBinding, protectionLevel, impersonationLevel);
	}

	private void ValidateCreateContext(string package, bool isServer, NetworkCredential credential, string servicePrincipalName, ChannelBinding channelBinding, ProtectionLevel protectionLevel, TokenImpersonationLevel impersonationLevel)
	{
		if (!_canRetryAuthentication)
		{
			ThrowIfExceptional();
		}
		if (_context != null)
		{
			throw new InvalidOperationException(System.SR.net_auth_reauth);
		}
		ArgumentNullException.ThrowIfNull(credential, "credential");
		ArgumentNullException.ThrowIfNull(servicePrincipalName, "servicePrincipalName");
		if (impersonationLevel != TokenImpersonationLevel.Identification && impersonationLevel != TokenImpersonationLevel.Impersonation && impersonationLevel != TokenImpersonationLevel.Delegation)
		{
			throw new ArgumentOutOfRangeException("impersonationLevel", impersonationLevel.ToString(), System.SR.net_auth_supported_impl_levels);
		}
		if (_context != null && IsServer != isServer)
		{
			throw new InvalidOperationException(System.SR.net_auth_client_server);
		}
		_exception = null;
		_remoteOk = false;
		_framer = new StreamFramer();
		_framer.WriteHeader.MessageId = 22;
		_canRetryAuthentication = false;
		if (protectionLevel == ProtectionLevel.None && !isServer)
		{
			package = "NTLM";
		}
		if (isServer)
		{
			_expectedProtectionLevel = protectionLevel;
			_expectedImpersonationLevel = impersonationLevel;
			_context = new NegotiateAuthentication(new NegotiateAuthenticationServerOptions
			{
				Package = package,
				Credential = credential,
				Binding = channelBinding,
				RequiredProtectionLevel = protectionLevel,
				RequiredImpersonationLevel = impersonationLevel,
				Policy = _extendedProtectionPolicy
			});
		}
		else
		{
			_expectedProtectionLevel = protectionLevel;
			_expectedImpersonationLevel = TokenImpersonationLevel.None;
			_context = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
			{
				Package = package,
				Credential = credential,
				TargetName = servicePrincipalName,
				Binding = channelBinding,
				RequiredProtectionLevel = protectionLevel,
				AllowedImpersonationLevel = impersonationLevel,
				RequireMutualAuthentication = (protectionLevel != ProtectionLevel.None)
			});
		}
	}

	private void SetFailed(Exception e)
	{
		if (_exception == null || !(_exception.SourceException is ObjectDisposedException))
		{
			_exception = ExceptionDispatchInfo.Capture(e);
		}
		_context?.Dispose();
	}

	private void ThrowIfFailed(bool authSuccessCheck)
	{
		ThrowIfExceptional();
		if (authSuccessCheck && !IsAuthenticatedCore)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
	}

	private async Task AuthenticateAsync<TIOAdapter>(CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		ThrowIfFailed(authSuccessCheck: false);
		if (Interlocked.Exchange(ref _authInProgress, 1) == 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, "authenticate"));
		}
		try
		{
			await (_context.IsServer ? ReceiveBlobAsync<TIOAdapter>(cancellationToken) : SendBlobAsync<TIOAdapter>(null, cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception failed)
		{
			SetFailed(failed);
			throw;
		}
		finally
		{
			_authInProgress = 0;
		}
	}

	private async Task SendBlobAsync<TIOAdapter>(byte[] message, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		NegotiateAuthenticationStatusCode statusCode = NegotiateAuthenticationStatusCode.Completed;
		if (message != s_emptyMessage)
		{
			message = _context.GetOutgoingBlob(message, out statusCode);
		}
		if ((statusCode == NegotiateAuthenticationStatusCode.BadBinding || (uint)(statusCode - 13) <= 2u) ? true : false)
		{
			Exception exception = statusCode switch
			{
				NegotiateAuthenticationStatusCode.BadBinding => new AuthenticationException(System.SR.net_auth_bad_client_creds_or_target_mismatch), 
				NegotiateAuthenticationStatusCode.TargetUnknown => new AuthenticationException(System.SR.net_auth_bad_client_creds_or_target_mismatch), 
				NegotiateAuthenticationStatusCode.ImpersonationValidationFailed => new AuthenticationException(System.SR.Format(System.SR.net_auth_context_expectation, _expectedImpersonationLevel.ToString(), PrivateImpersonationLevel.ToString())), 
				_ => new AuthenticationException(System.SR.Format(System.SR.net_auth_context_expectation, _context.ProtectionLevel.ToString(), _expectedProtectionLevel.ToString())), 
			};
			message = new byte[8];
			BinaryPrimitives.WriteInt64LittleEndian(message, 1790L);
			await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			int num;
			int num2;
			Win32Exception innerException;
			AuthenticationException ex;
			Exception exception2;
			switch (statusCode)
			{
			case NegotiateAuthenticationStatusCode.Completed:
				_writeBuffer = new ArrayBufferWriter<byte>();
				isNtlm = string.Equals(_context.Package, "NTLM");
				_framer.WriteHeader.MessageId = 20;
				if (_context.IsServer)
				{
					_remoteOk = true;
					if (message == null)
					{
						message = s_emptyMessage;
					}
				}
				if (message != null)
				{
					await _framer.WriteMessageAsync<TIOAdapter>(base.InnerStream, message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (_remoteOk)
				{
					return;
				}
				break;
			case NegotiateAuthenticationStatusCode.BadBinding:
				num = -2146892986;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.Unsupported:
				num = -2146893054;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.MessageAltered:
				num = -2146893041;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.ContextExpired:
				num = 590615;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.CredentialsExpired:
				num = -2146893016;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.InvalidCredentials:
				num = -2146893044;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.InvalidToken:
				num = -2146893048;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.UnknownCredentials:
				num = -2146893043;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.QopNotSupported:
				num = -2146893046;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.OutOfSequence:
				num = -2146893040;
				goto IL_0347;
			default:
				num = -2146893052;
				goto IL_0347;
			case NegotiateAuthenticationStatusCode.ContinueNeeded:
				{
					if (message == null || message == s_emptyMessage)
					{
						throw new InternalException();
					}
					await _framer.WriteMessageAsync<TIOAdapter>(base.InnerStream, message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					break;
				}
				IL_0347:
				num2 = num;
				innerException = new Win32Exception(num2);
				ex = ((statusCode != NegotiateAuthenticationStatusCode.InvalidCredentials) ? new AuthenticationException(System.SR.net_auth_SSPI, innerException) : new InvalidCredentialException(IsServer ? System.SR.net_auth_bad_client_creds : System.SR.net_auth_bad_client_creds_or_target_mismatch, innerException));
				exception2 = ex;
				message = new byte[8];
				BinaryPrimitives.WriteInt64LittleEndian(message, num2);
				await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
		}
		await ReceiveBlobAsync<TIOAdapter>(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task ReceiveBlobAsync<TIOAdapter>(CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		byte[] array = await _framer.ReadMessageAsync<TIOAdapter>(base.InnerStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (array == null)
		{
			throw new AuthenticationException(System.SR.net_auth_eof);
		}
		if (_framer.ReadHeader.MessageId == 21)
		{
			if (array.Length >= 8)
			{
				long error = BinaryPrimitives.ReadInt64LittleEndian(array);
				ThrowCredentialException(error);
			}
			throw new AuthenticationException(System.SR.net_auth_alert);
		}
		if (_framer.ReadHeader.MessageId == 20)
		{
			_remoteOk = true;
		}
		else if (_framer.ReadHeader.MessageId != 22)
		{
			throw new AuthenticationException(System.SR.Format(System.SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, 22));
		}
		if (HandshakeComplete)
		{
			if (!_remoteOk)
			{
				throw new AuthenticationException(System.SR.Format(System.SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, 20));
			}
		}
		else
		{
			await SendBlobAsync<TIOAdapter>(array, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task SendAuthResetSignalAndThrowAsync<TIOAdapter>(byte[] message, Exception exception, CancellationToken cancellationToken) where TIOAdapter : IReadWriteAdapter
	{
		_framer.WriteHeader.MessageId = 21;
		await _framer.WriteMessageAsync<TIOAdapter>(base.InnerStream, message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_canRetryAuthentication = true;
		ExceptionDispatchInfo.Throw(exception);
	}

	private static void ThrowCredentialException(long error)
	{
		Win32Exception ex = new Win32Exception((int)error);
		throw ex.NativeErrorCode switch
		{
			22 => new InvalidCredentialException(System.SR.net_auth_bad_client_creds, ex), 
			-2146893044 => new InvalidCredentialException(System.SR.net_auth_bad_client_creds, ex), 
			1790 => new AuthenticationException(System.SR.net_auth_context_expectation_remote, ex), 
			_ => new AuthenticationException(System.SR.net_auth_alert, ex), 
		};
	}
}
