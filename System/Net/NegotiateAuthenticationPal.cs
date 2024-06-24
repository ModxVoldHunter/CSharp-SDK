using System.Buffers;
using System.ComponentModel;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net;

internal abstract class NegotiateAuthenticationPal : IDisposable
{
	internal sealed class UnsupportedNegotiateAuthenticationPal : NegotiateAuthenticationPal
	{
		private string _package;

		private string _targetName;

		public override bool IsAuthenticated => false;

		public override bool IsSigned => false;

		public override bool IsEncrypted => false;

		public override bool IsMutuallyAuthenticated => false;

		public override string Package => _package;

		public override string TargetName => _targetName;

		public override IIdentity RemoteIdentity
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		public override TokenImpersonationLevel ImpersonationLevel => TokenImpersonationLevel.Impersonation;

		public UnsupportedNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions)
		{
			_package = clientOptions.Package;
			_targetName = clientOptions.TargetName;
		}

		public UnsupportedNegotiateAuthenticationPal(NegotiateAuthenticationServerOptions serverOptions)
		{
			_package = serverOptions.Package;
		}

		public override void Dispose()
		{
		}

		public override byte[] GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
		{
			statusCode = NegotiateAuthenticationStatusCode.Unsupported;
			return null;
		}

		public override NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
		{
			throw new InvalidOperationException();
		}

		public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
		{
			throw new InvalidOperationException();
		}

		public override NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
		{
			throw new InvalidOperationException();
		}

		public override void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
		{
			throw new InvalidOperationException();
		}

		public override bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
		{
			throw new InvalidOperationException();
		}
	}

	internal sealed class WindowsNegotiateAuthenticationPal : NegotiateAuthenticationPal
	{
		private bool _isServer;

		private bool _isAuthenticated;

		private int _tokenSize;

		private byte[] _tokenBuffer;

		private SafeFreeCredentials _credentialsHandle;

		private SafeDeleteContext _securityContext;

		private global::Interop.SspiCli.ContextFlags _requestedContextFlags;

		private global::Interop.SspiCli.ContextFlags _contextFlags;

		private string _package;

		private string _protocolName;

		private string _spn;

		private ChannelBinding _channelBinding;

		public override bool IsAuthenticated => _isAuthenticated;

		public override bool IsSigned => ((uint)_contextFlags & (uint)(_isServer ? 131072 : 65536)) != 0;

		public override bool IsEncrypted => (_contextFlags & global::Interop.SspiCli.ContextFlags.Confidentiality) != 0;

		public override bool IsMutuallyAuthenticated => (_contextFlags & global::Interop.SspiCli.ContextFlags.MutualAuth) != 0;

		public override string Package
		{
			get
			{
				if (_protocolName == null)
				{
					string text = null;
					if (_securityContext != null)
					{
						SecPkgContext_NegotiationInfoW attribute = default(SecPkgContext_NegotiationInfoW);
						SafeHandle sspiHandle;
						bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO, typeof(SafeFreeContextBuffer), out sspiHandle, ref attribute);
						using (sspiHandle)
						{
							text = (flag ? NegotiationInfoClass.GetAuthenticationPackageName(sspiHandle, (int)attribute.NegotiationState) : null);
						}
						if (_isAuthenticated)
						{
							_protocolName = text;
						}
					}
					return text ?? string.Empty;
				}
				return _protocolName;
			}
		}

		public override string TargetName
		{
			get
			{
				if (_isServer && _spn == null)
				{
					_spn = SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"The client specified SPN is [{_spn}]", "TargetName");
					}
				}
				return _spn;
			}
		}

		public override IIdentity RemoteIdentity
		{
			get
			{
				string text = (_isServer ? null : TargetName);
				string package = Package;
				if (_isServer)
				{
					SecurityContextTokenHandle token = null;
					try
					{
						text = SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"NTAuthentication: The context is associated with [{text}]", "RemoteIdentity");
						}
						global::Interop.SECURITY_STATUS sECURITY_STATUS = (global::Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(GlobalSSPI.SSPIAuth, _securityContext, out token);
						if (sECURITY_STATUS != 0)
						{
							throw new Win32Exception((int)sECURITY_STATUS);
						}
						return new WindowsIdentity(token.DangerousGetHandle(), package);
					}
					catch (SecurityException)
					{
					}
					finally
					{
						token?.Dispose();
					}
				}
				return new GenericIdentity(text ?? string.Empty, package);
			}
		}

		public override TokenImpersonationLevel ImpersonationLevel
		{
			get
			{
				if ((_contextFlags & global::Interop.SspiCli.ContextFlags.Delegate) == 0 || !(Package != "NTLM"))
				{
					if (((uint)_contextFlags & (uint)(_isServer ? 524288 : 131072)) == 0)
					{
						return TokenImpersonationLevel.Impersonation;
					}
					return TokenImpersonationLevel.Identification;
				}
				return TokenImpersonationLevel.Delegation;
			}
		}

		public WindowsNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions)
		{
			global::Interop.SspiCli.ContextFlags contextFlags = global::Interop.SspiCli.ContextFlags.Connection;
			global::Interop.SspiCli.ContextFlags contextFlags2 = contextFlags;
			contextFlags = contextFlags2 | (clientOptions.RequiredProtectionLevel switch
			{
				ProtectionLevel.Sign => global::Interop.SspiCli.ContextFlags.AcceptStream, 
				ProtectionLevel.EncryptAndSign => global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AcceptStream, 
				_ => global::Interop.SspiCli.ContextFlags.Zero, 
			}) | (clientOptions.RequireMutualAuthentication ? global::Interop.SspiCli.ContextFlags.MutualAuth : global::Interop.SspiCli.ContextFlags.Zero) | (clientOptions.AllowedImpersonationLevel switch
			{
				TokenImpersonationLevel.Identification => global::Interop.SspiCli.ContextFlags.AcceptIntegrity, 
				TokenImpersonationLevel.Delegation => global::Interop.SspiCli.ContextFlags.Delegate, 
				_ => global::Interop.SspiCli.ContextFlags.Zero, 
			});
			_isServer = false;
			_tokenSize = SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, clientOptions.Package, throwIfMissing: true).MaxToken;
			_spn = clientOptions.TargetName;
			_securityContext = null;
			_requestedContextFlags = contextFlags;
			_package = clientOptions.Package;
			_channelBinding = clientOptions.Binding;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Peer SPN-> '{_spn}'", ".ctor");
			}
			if (clientOptions.Credential == CredentialCache.DefaultCredentials)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "using DefaultCredentials", ".ctor");
				}
				_credentialsHandle = AcquireDefaultCredential(_package, _isServer);
			}
			else
			{
				_credentialsHandle = AcquireCredentialsHandle(_package, _isServer, clientOptions.Credential);
			}
		}

		public WindowsNegotiateAuthenticationPal(NegotiateAuthenticationServerOptions serverOptions)
		{
			global::Interop.SspiCli.ContextFlags contextFlags = (serverOptions.RequiredProtectionLevel switch
			{
				ProtectionLevel.Sign => global::Interop.SspiCli.ContextFlags.AcceptIntegrity, 
				ProtectionLevel.EncryptAndSign => global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AcceptIntegrity, 
				_ => global::Interop.SspiCli.ContextFlags.Zero, 
			}) | global::Interop.SspiCli.ContextFlags.Connection;
			if (serverOptions.Policy != null)
			{
				if (serverOptions.Policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
				{
					contextFlags |= global::Interop.SspiCli.ContextFlags.AllowMissingBindings;
				}
				if (serverOptions.Policy.PolicyEnforcement != 0 && serverOptions.Policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
				{
					contextFlags |= global::Interop.SspiCli.ContextFlags.ProxyBindings;
				}
			}
			_isServer = true;
			_tokenSize = SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, serverOptions.Package, throwIfMissing: true).MaxToken;
			_securityContext = null;
			_requestedContextFlags = contextFlags;
			_package = serverOptions.Package;
			_channelBinding = serverOptions.Binding;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Peer SPN-> '{_spn}'", ".ctor");
			}
			if (serverOptions.Credential == CredentialCache.DefaultCredentials)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "using DefaultCredentials", ".ctor");
				}
				_credentialsHandle = AcquireDefaultCredential(_package, _isServer);
			}
			else
			{
				_credentialsHandle = AcquireCredentialsHandle(_package, _isServer, serverOptions.Credential);
			}
		}

		public override void Dispose()
		{
			_securityContext?.Dispose();
		}

		public override byte[] GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
		{
			if (_tokenBuffer == null)
			{
				_tokenBuffer = ((_tokenSize == 0) ? Array.Empty<byte>() : new byte[_tokenSize]);
			}
			bool flag = _securityContext == null;
			SecurityStatusPal securityStatusPal;
			int resultBlobLength;
			try
			{
				if (!_isServer)
				{
					securityStatusPal = InitializeSecurityContext(ref _credentialsHandle, ref _securityContext, _spn, _requestedContextFlags, incomingBlob, _channelBinding, ref _tokenBuffer, out resultBlobLength, ref _contextFlags);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{(int)securityStatusPal.ErrorCode:x8} ({securityStatusPal})", "GetOutgoingBlob");
					}
					if (securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
					{
						securityStatusPal = CompleteAuthToken(ref _securityContext, _tokenBuffer.AsSpan(0, resultBlobLength));
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{(int)securityStatusPal.ErrorCode:x8} ({securityStatusPal})", "GetOutgoingBlob");
						}
						resultBlobLength = 0;
					}
				}
				else
				{
					securityStatusPal = AcceptSecurityContext(_credentialsHandle, ref _securityContext, _requestedContextFlags, incomingBlob, _channelBinding, ref _tokenBuffer, out resultBlobLength, ref _contextFlags);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"SSPIWrapper.AcceptSecurityContext() returns statusCode:0x{(int)securityStatusPal.ErrorCode:x8} ({securityStatusPal})", "GetOutgoingBlob");
					}
				}
			}
			finally
			{
				if (flag)
				{
					_credentialsHandle?.Dispose();
				}
			}
			statusCode = securityStatusPal.ErrorCode switch
			{
				SecurityStatusPalErrorCode.OK => NegotiateAuthenticationStatusCode.Completed, 
				SecurityStatusPalErrorCode.ContinueNeeded => NegotiateAuthenticationStatusCode.ContinueNeeded, 
				SecurityStatusPalErrorCode.CompleteNeeded => NegotiateAuthenticationStatusCode.Completed, 
				SecurityStatusPalErrorCode.CompAndContinue => NegotiateAuthenticationStatusCode.ContinueNeeded, 
				SecurityStatusPalErrorCode.ContextExpired => NegotiateAuthenticationStatusCode.ContextExpired, 
				SecurityStatusPalErrorCode.Unsupported => NegotiateAuthenticationStatusCode.Unsupported, 
				SecurityStatusPalErrorCode.PackageNotFound => NegotiateAuthenticationStatusCode.Unsupported, 
				SecurityStatusPalErrorCode.CannotInstall => NegotiateAuthenticationStatusCode.Unsupported, 
				SecurityStatusPalErrorCode.InvalidToken => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.QopNotSupported => NegotiateAuthenticationStatusCode.QopNotSupported, 
				SecurityStatusPalErrorCode.NoImpersonation => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.LogonDenied => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.UnknownCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.NoCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.MessageAltered => NegotiateAuthenticationStatusCode.MessageAltered, 
				SecurityStatusPalErrorCode.OutOfSequence => NegotiateAuthenticationStatusCode.OutOfSequence, 
				SecurityStatusPalErrorCode.NoAuthenticatingAuthority => NegotiateAuthenticationStatusCode.InvalidCredentials, 
				SecurityStatusPalErrorCode.IncompleteCredentials => NegotiateAuthenticationStatusCode.InvalidCredentials, 
				SecurityStatusPalErrorCode.IllegalMessage => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.CertExpired => NegotiateAuthenticationStatusCode.CredentialsExpired, 
				SecurityStatusPalErrorCode.SecurityQosFailed => NegotiateAuthenticationStatusCode.QopNotSupported, 
				SecurityStatusPalErrorCode.UnsupportedPreauth => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.BadBinding => NegotiateAuthenticationStatusCode.BadBinding, 
				SecurityStatusPalErrorCode.UntrustedRoot => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.SmartcardLogonRequired => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.WrongPrincipal => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.CannotPack => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.TimeSkew => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.AlgorithmMismatch => NegotiateAuthenticationStatusCode.InvalidToken, 
				SecurityStatusPalErrorCode.CertUnknown => NegotiateAuthenticationStatusCode.UnknownCredentials, 
				SecurityStatusPalErrorCode.IncompleteMessage => NegotiateAuthenticationStatusCode.InvalidToken, 
				_ => NegotiateAuthenticationStatusCode.GenericFailure, 
			};
			if (securityStatusPal.ErrorCode >= SecurityStatusPalErrorCode.OutOfMemory)
			{
				_securityContext?.Dispose();
				_isAuthenticated = true;
				_tokenBuffer = null;
				return null;
			}
			if (flag && _credentialsHandle != null)
			{
				SSPIHandleCache.CacheCredential(_credentialsHandle);
			}
			byte[] result = ((resultBlobLength == 0 || _tokenBuffer == null) ? null : ((_tokenBuffer.Length == resultBlobLength) ? _tokenBuffer : _tokenBuffer[0..resultBlobLength]));
			if (securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.OK || (_isServer && securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded))
			{
				_isAuthenticated = true;
				_tokenBuffer = null;
			}
			else if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"need continue statusCode:0x{(int)securityStatusPal.ErrorCode:x8} ({securityStatusPal}) _securityContext:{_securityContext}", "GetOutgoingBlob");
			}
			return result;
		}

		public unsafe override NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
		{
			SecPkgContext_Sizes attribute = default(SecPkgContext_Sizes);
			bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref attribute);
			int sizeHint = input.Length + attribute.cbSecurityTrailer + attribute.cbBlockSize;
			Span<byte> span = outputWriter.GetSpan(sizeHint);
			input.CopyTo(span.Slice(attribute.cbSecurityTrailer, input.Length));
			isEncrypted = requestEncryption;
			fixed (byte* ptr5 = span)
			{
				global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[3];
				global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
				global::Interop.SspiCli.SecBuffer* ptr3 = ptr + 1;
				global::Interop.SspiCli.SecBuffer* ptr4 = ptr + 2;
				ptr2->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
				ptr2->pvBuffer = (nint)ptr5;
				ptr2->cbBuffer = attribute.cbSecurityTrailer;
				ptr3->BufferType = SecurityBufferType.SECBUFFER_DATA;
				ptr3->pvBuffer = (nint)(ptr5 + attribute.cbSecurityTrailer);
				ptr3->cbBuffer = input.Length;
				ptr4->BufferType = SecurityBufferType.SECBUFFER_PADDING;
				ptr4->pvBuffer = (nint)(ptr5 + attribute.cbSecurityTrailer + input.Length);
				ptr4->cbBuffer = attribute.cbBlockSize;
				global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(3);
				secBufferDesc.pBuffers = ptr;
				global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
				uint qop = ((!requestEncryption) ? 2147483649u : 0u);
				switch (GlobalSSPI.SSPIAuth.EncryptMessage(_securityContext, ref inputOutput, qop))
				{
				case 590615:
					return NegotiateAuthenticationStatusCode.ContextExpired;
				case -2146893046:
					return NegotiateAuthenticationStatusCode.QopNotSupported;
				default:
					return NegotiateAuthenticationStatusCode.GenericFailure;
				case 0:
					if (ptr2->cbBuffer != attribute.cbSecurityTrailer)
					{
						span.Slice(attribute.cbSecurityTrailer, ptr3->cbBuffer).CopyTo(span.Slice(ptr2->cbBuffer, ptr3->cbBuffer));
					}
					if (ptr2->cbBuffer != attribute.cbSecurityTrailer || ptr4->cbBuffer != attribute.cbBlockSize)
					{
						span.Slice(attribute.cbSecurityTrailer + input.Length, ptr4->cbBuffer).CopyTo(span.Slice(ptr2->cbBuffer + ptr3->cbBuffer, ptr4->cbBuffer));
					}
					outputWriter.Advance(ptr2->cbBuffer + ptr3->cbBuffer + ptr4->cbBuffer);
					return NegotiateAuthenticationStatusCode.Completed;
				}
			}
		}

		public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
		{
			Span<byte> span = outputWriter.GetSpan(input.Length);
			Span<byte> span2 = span.Slice(0, input.Length);
			input.CopyTo(span2);
			int unwrappedOffset;
			int unwrappedLength;
			NegotiateAuthenticationStatusCode negotiateAuthenticationStatusCode = UnwrapInPlace(span2, out unwrappedOffset, out unwrappedLength, out wasEncrypted);
			if (negotiateAuthenticationStatusCode == NegotiateAuthenticationStatusCode.Completed)
			{
				if (unwrappedOffset > 0)
				{
					span = span2.Slice(unwrappedOffset, unwrappedLength);
					span.CopyTo(span2);
				}
				outputWriter.Advance(unwrappedLength);
			}
			return negotiateAuthenticationStatusCode;
		}

		public unsafe override NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
		{
			fixed (byte* ptr4 = input)
			{
				global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[2];
				global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
				global::Interop.SspiCli.SecBuffer* ptr3 = ptr + 1;
				ptr2->BufferType = SecurityBufferType.SECBUFFER_STREAM;
				ptr2->pvBuffer = (nint)ptr4;
				ptr2->cbBuffer = input.Length;
				ptr3->BufferType = SecurityBufferType.SECBUFFER_DATA;
				ptr3->pvBuffer = IntPtr.Zero;
				ptr3->cbBuffer = 0;
				global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(2);
				secBufferDesc.pBuffers = ptr;
				global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
				uint qop;
				int num = GlobalSSPI.SSPIAuth.DecryptMessage(_securityContext, ref inputOutput, out qop);
				if (num != 0)
				{
					unwrappedOffset = 0;
					unwrappedLength = 0;
					wasEncrypted = false;
					if (num == -2146893041)
					{
						return NegotiateAuthenticationStatusCode.MessageAltered;
					}
					return NegotiateAuthenticationStatusCode.InvalidToken;
				}
				if (ptr3->BufferType != SecurityBufferType.SECBUFFER_DATA)
				{
					throw new InternalException(ptr3->BufferType);
				}
				wasEncrypted = qop != 2147483649u;
				unwrappedOffset = (int)((byte*)ptr3->pvBuffer - ptr4);
				unwrappedLength = ptr3->cbBuffer;
				return NegotiateAuthenticationStatusCode.Completed;
			}
		}

		public unsafe override void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
		{
			bool success = false;
			try
			{
				_securityContext.DangerousAddRef(ref success);
				SecPkgContext_Sizes attribute = default(SecPkgContext_Sizes);
				bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref attribute);
				Span<byte> span = signature.GetSpan(attribute.cbMaxSignature);
				fixed (byte* pvBuffer2 = message)
				{
					fixed (byte* pvBuffer = span)
					{
						global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[2];
						global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
						global::Interop.SspiCli.SecBuffer* ptr3 = ptr + 1;
						ptr2->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
						ptr2->pvBuffer = (nint)pvBuffer;
						ptr2->cbBuffer = attribute.cbMaxSignature;
						ptr3->BufferType = SecurityBufferType.SECBUFFER_DATA;
						ptr3->pvBuffer = (nint)pvBuffer2;
						ptr3->cbBuffer = message.Length;
						global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(2);
						secBufferDesc.pBuffers = ptr;
						global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
						uint qualityOfProtection = ((!IsEncrypted) ? 2147483649u : 0u);
						int num = global::Interop.SspiCli.MakeSignature(ref _securityContext._handle, qualityOfProtection, ref inputOutput, 0u);
						if (num != 0)
						{
							Exception message2 = new Win32Exception(num);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Error(null, message2, "GetMIC");
							}
							throw new Win32Exception(num);
						}
						signature.Advance(ptr2->cbBuffer);
					}
				}
			}
			finally
			{
				if (success)
				{
					_securityContext.DangerousRelease();
				}
			}
		}

		public unsafe override bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
		{
			bool success = false;
			try
			{
				_securityContext.DangerousAddRef(ref success);
				fixed (byte* pvBuffer2 = message)
				{
					fixed (byte* pvBuffer = signature)
					{
						global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[2];
						global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
						global::Interop.SspiCli.SecBuffer* ptr3 = ptr + 1;
						ptr2->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
						ptr2->pvBuffer = (nint)pvBuffer;
						ptr2->cbBuffer = signature.Length;
						ptr3->BufferType = SecurityBufferType.SECBUFFER_DATA;
						ptr3->pvBuffer = (nint)pvBuffer2;
						ptr3->cbBuffer = message.Length;
						global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(2);
						secBufferDesc.pBuffers = ptr;
						global::Interop.SspiCli.SecBufferDesc input = secBufferDesc;
						Unsafe.SkipInit(out uint num);
						int num2 = global::Interop.SspiCli.VerifySignature(ref _securityContext._handle, in input, 0u, &num);
						if (num2 != 0)
						{
							Exception message2 = new Win32Exception(num2);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Error(null, message2, "VerifyMIC");
							}
							throw new Win32Exception(num2);
						}
						if (IsEncrypted && num == 2147483649u)
						{
							throw new InvalidOperationException(System.SR.net_auth_message_not_encrypted);
						}
						return true;
					}
				}
			}
			finally
			{
				if (success)
				{
					_securityContext.DangerousRelease();
				}
			}
		}

		private static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
		{
			return SSPIWrapper.AcquireDefaultCredential(GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND);
		}

		private static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
		{
			SafeSspiAuthDataHandle authData = null;
			try
			{
				global::Interop.SECURITY_STATUS sECURITY_STATUS = global::Interop.SspiCli.SspiEncodeStringsAsAuthIdentity(credential.UserName, credential.Domain, credential.Password, out authData);
				if (sECURITY_STATUS != 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_log_operation_failed_with_error, "SspiEncodeStringsAsAuthIdentity", $"0x{sECURITY_STATUS:X}"), "AcquireCredentialsHandle");
					}
					throw new Win32Exception((int)sECURITY_STATUS);
				}
				return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND, ref authData);
			}
			finally
			{
				authData?.Dispose();
			}
		}

		private static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteContext securityContext, string spn, global::Interop.SspiCli.ContextFlags requestedContextFlags, ReadOnlySpan<byte> incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, out int resultBlobLength, ref global::Interop.SspiCli.ContextFlags contextFlags)
		{
			InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
			if (!incomingBlob.IsEmpty)
			{
				inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
			}
			if (channelBinding != null)
			{
				inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
			}
			SecurityBuffer outputBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);
			contextFlags = global::Interop.SspiCli.ContextFlags.Zero;
			SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
			global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.InitializeSecurityContext(GlobalSSPI.SSPIAuth, ref credentialsHandle, ref context, spn, requestedContextFlags, global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref contextFlags);
			securityContext = context;
			resultBlob = outputBuffer.token;
			resultBlobLength = outputBuffer.size;
			return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
		}

		private static SecurityStatusPal CompleteAuthToken(ref SafeDeleteContext securityContext, ReadOnlySpan<byte> incomingBlob)
		{
			SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
			InputSecurityBuffer inputBuffer = new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN);
			global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.CompleteAuthToken(GlobalSSPI.SSPIAuth, ref context, in inputBuffer);
			securityContext = context;
			return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
		}

		private static SecurityStatusPal AcceptSecurityContext(SafeFreeCredentials credentialsHandle, ref SafeDeleteContext securityContext, global::Interop.SspiCli.ContextFlags requestedContextFlags, ReadOnlySpan<byte> incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, out int resultBlobLength, ref global::Interop.SspiCli.ContextFlags contextFlags)
		{
			InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
			if (!incomingBlob.IsEmpty)
			{
				inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
			}
			if (channelBinding != null)
			{
				inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
			}
			SecurityBuffer outputBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);
			contextFlags = global::Interop.SspiCli.ContextFlags.Zero;
			SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
			global::Interop.SECURITY_STATUS sECURITY_STATUS = (global::Interop.SECURITY_STATUS)SSPIWrapper.AcceptSecurityContext(GlobalSSPI.SSPIAuth, credentialsHandle, ref context, requestedContextFlags, global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref contextFlags);
			if (sECURITY_STATUS == global::Interop.SECURITY_STATUS.InvalidHandle && securityContext == null && !incomingBlob.IsEmpty)
			{
				sECURITY_STATUS = global::Interop.SECURITY_STATUS.InvalidToken;
			}
			resultBlob = outputBuffer.token;
			resultBlobLength = outputBuffer.size;
			securityContext = context;
			return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(sECURITY_STATUS);
		}
	}

	public abstract bool IsAuthenticated { get; }

	public abstract bool IsSigned { get; }

	public abstract bool IsEncrypted { get; }

	public abstract bool IsMutuallyAuthenticated { get; }

	public abstract string Package { get; }

	public abstract string TargetName { get; }

	public abstract IIdentity RemoteIdentity { get; }

	public abstract TokenImpersonationLevel ImpersonationLevel { get; }

	public abstract void Dispose();

	public abstract byte[] GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode);

	public abstract NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted);

	public abstract NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted);

	public abstract NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted);

	public abstract void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature);

	public abstract bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature);

	public static NegotiateAuthenticationPal Create(NegotiateAuthenticationClientOptions clientOptions)
	{
		try
		{
			return new WindowsNegotiateAuthenticationPal(clientOptions);
		}
		catch (NotSupportedException)
		{
			return new UnsupportedNegotiateAuthenticationPal(clientOptions);
		}
	}

	public static NegotiateAuthenticationPal Create(NegotiateAuthenticationServerOptions serverOptions)
	{
		try
		{
			return new WindowsNegotiateAuthenticationPal(serverOptions);
		}
		catch (NotSupportedException)
		{
			return new UnsupportedNegotiateAuthenticationPal(serverOptions);
		}
	}
}
