using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail;

internal sealed class SmtpNtlmAuthenticationModule : ISmtpAuthenticationModule
{
	private readonly Dictionary<object, NegotiateAuthentication> _sessions = new Dictionary<object, NegotiateAuthentication>();

	public string AuthenticationType => "ntlm";

	internal SmtpNtlmAuthenticationModule()
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
				value = (_sessions[sessionCookie] = new NegotiateAuthentication(new NegotiateAuthenticationClientOptions
				{
					Credential = credential,
					TargetName = spn,
					Binding = channelBindingToken
				}));
			}
			NegotiateAuthenticationStatusCode statusCode;
			string outgoingBlob = value.GetOutgoingBlob(challenge, out statusCode);
			if (statusCode != 0 && statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded)
			{
				return null;
			}
			if (!value.IsAuthenticated)
			{
				return new Authorization(outgoingBlob, finished: false);
			}
			_sessions.Remove(sessionCookie);
			value.Dispose();
			return new Authorization(outgoingBlob, finished: true);
		}
	}

	public void CloseContext(object sessionCookie)
	{
	}
}
