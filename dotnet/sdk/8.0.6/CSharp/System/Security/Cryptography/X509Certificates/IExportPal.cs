using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal interface IExportPal : IDisposable
{
	byte[] Export(X509ContentType contentType, SafePasswordHandle password);
}
