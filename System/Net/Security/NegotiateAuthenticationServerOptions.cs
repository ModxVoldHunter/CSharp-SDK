using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security;

public class NegotiateAuthenticationServerOptions
{
	public string Package { get; set; } = "Negotiate";


	public NetworkCredential Credential { get; set; } = CredentialCache.DefaultNetworkCredentials;


	public ChannelBinding? Binding { get; set; }

	public ProtectionLevel RequiredProtectionLevel { get; set; }

	public ExtendedProtectionPolicy? Policy { get; set; }

	public TokenImpersonationLevel RequiredImpersonationLevel { get; set; }
}
