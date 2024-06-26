using System.Runtime.ConstrainedExecution;

namespace System.Net.Security;

internal sealed class SafeCredentialReference : CriticalFinalizerObject, IDisposable
{
	internal SafeFreeCredentials Target { get; private set; }

	internal static SafeCredentialReference CreateReference(SafeFreeCredentials target)
	{
		if (target.IsInvalid || target.IsClosed)
		{
			return null;
		}
		return new SafeCredentialReference(target);
	}

	private SafeCredentialReference(SafeFreeCredentials target)
	{
		bool success = false;
		target.DangerousAddRef(ref success);
		Target = target;
	}

	public void Dispose()
	{
		DisposeInternal();
		GC.SuppressFinalize(this);
	}

	private void DisposeInternal()
	{
		Target?.DangerousRelease();
		Target = null;
	}

	~SafeCredentialReference()
	{
		DisposeInternal();
	}
}
