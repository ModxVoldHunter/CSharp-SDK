using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates;

internal interface IStorePal : IDisposable
{
	SafeHandle SafeHandle { get; }

	void CloneTo(X509Certificate2Collection collection);

	void Add(ICertificatePal cert);

	void Remove(ICertificatePal cert);
}
