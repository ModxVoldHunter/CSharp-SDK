using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security;

public class NegotiateAuthenticationClientOptions
{
	public string Package { get; set; } = "Negotiate";


	public NetworkCredential Credential { get; set; } = CredentialCache.DefaultNetworkCredentials;


	public string? TargetName { get; set; }

	public ChannelBinding? Binding { get; set; }

	public ProtectionLevel RequiredProtectionLevel { get; set; }

	public bool RequireMutualAuthentication { get; set; }

	public TokenImpersonationLevel AllowedImpersonationLevel { get; set; }
}
