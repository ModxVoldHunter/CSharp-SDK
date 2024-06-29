using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Net.Security;

[UnsupportedOSPlatform("windows")]
[UnsupportedOSPlatform("android")]
public sealed class CipherSuitesPolicy
{
	internal CipherSuitesPolicyPal Pal { get; private set; }

	[CLSCompliant(false)]
	public IEnumerable<TlsCipherSuite> AllowedCipherSuites
	{
		get
		{
			foreach (TlsCipherSuite cipherSuite in Pal.GetCipherSuites())
			{
				yield return cipherSuite;
			}
		}
	}

	[CLSCompliant(false)]
	public CipherSuitesPolicy(IEnumerable<TlsCipherSuite> allowedCipherSuites)
	{
		ArgumentNullException.ThrowIfNull(allowedCipherSuites, "allowedCipherSuites");
		Pal = new CipherSuitesPolicyPal(allowedCipherSuites);
	}
}
