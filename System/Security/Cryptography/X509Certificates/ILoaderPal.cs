namespace System.Security.Cryptography.X509Certificates;

internal interface ILoaderPal : IDisposable
{
	void MoveTo(X509Certificate2Collection collection);
}
