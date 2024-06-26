using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal interface IChainPal : IDisposable
{
	X509ChainElement[] ChainElements { get; }

	X509ChainStatus[] ChainStatus { get; }

	SafeX509ChainHandle SafeHandle { get; }

	bool? Verify(X509VerificationFlags flags, out Exception exception);
}
