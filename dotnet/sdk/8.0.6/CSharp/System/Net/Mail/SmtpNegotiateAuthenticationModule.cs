using System.Buffers;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail;

internal sealed class SmtpNegotiateAuthenticationModule : ISmtpAuthenticationModule
{
	private static readonly byte[] s_saslNoSecurtyLayerToken = new byte[4] { 1, 0, 0, 0 };

	private readonly Dictionary<object, NegotiateAuthentication> _sessions = new Dictionary<object, NegotiateAuthentication>();

	public string AuthenticationType => "gssapi";

	internal SmtpNegotiateAuthenticationModule()
	{
	}

	public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
	{
		lock (_sessions)
		{
			if (!_sessions.TryGetValue(sessionCookie, out var value))
			{
				if (credential == null)
				{
					return null;
				}
				ProtectionLevel requiredProtectionLevel = ProtectionLevel.Sign;
				if (OperatingSystem.IsLinux())
				{
					requiredProtectionLevel = ProtectionLevel.EncryptAndSign;
				}
				value = (_sessions[sessionCookie] = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
				{
					Credential = credential,
					TargetName = spn,
					RequiredProtectionLevel = requiredProtectionLevel,
					Binding = channelBindingToken
				}));
			}
			string text = null;
			if (!value.IsAuthenticated)
			{
				text = value.GetOutgoingBlob(challenge, out var statusCode);
				if (statusCode != 0 && statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded)
				{
					return null;
				}
				if (value.IsAuthenticated && text == null)
				{
					text = "\r\n";
				}
			}
			else
			{
				text = GetSecurityLayerOutgoingBlob(challenge, value);
			}
			return new Authorization(text, value.IsAuthenticated);
		}
	}

	public void CloseContext(object sessionCookie)
	{
		NegotiateAuthentication value = null;
		lock (_sessions)
		{
			if (_sessions.TryGetValue(sessionCookie, out value))
			{
				_sessions.Remove(sessionCookie);
			}
		}
		value?.Dispose();
	}

	private static string GetSecurityLayerOutgoingBlob(string challenge, NegotiateAuthentication clientContext)
	{
		if (challenge == null)
		{
			return null;
		}
		byte[] array = Convert.FromBase64String(challenge);
		if (clientContext.UnwrapInPlace(array, out var unwrappedOffset, out var unwrappedLength, out var wasEncrypted) != 0)
		{
			return null;
		}
		Span<byte> span = array.AsSpan(unwrappedOffset, unwrappedLength);
		if (span.Length != 4 || (span[0] & 1) != 1)
		{
			return null;
		}
		ArrayBufferWriter<byte> arrayBufferWriter = new ArrayBufferWriter<byte>();
		if (clientContext.Wrap(s_saslNoSecurtyLayerToken, arrayBufferWriter, requestEncryption: false, out wasEncrypted) != 0)
		{
			return null;
		}
		return Convert.ToBase64String(arrayBufferWriter.WrittenSpan);
	}
}
