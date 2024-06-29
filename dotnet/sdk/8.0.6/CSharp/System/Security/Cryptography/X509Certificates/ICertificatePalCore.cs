using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

internal interface ICertificatePalCore : IDisposable
{
	bool HasPrivateKey { get; }

	nint Handle { get; }

	string Issuer { get; }

	string Subject { get; }

	string LegacyIssuer { get; }

	string LegacySubject { get; }

	byte[] Thumbprint { get; }

	string KeyAlgorithm { get; }

	byte[] KeyAlgorithmParameters { get; }

	byte[] PublicKeyValue { get; }

	byte[] SerialNumber { get; }

	string SignatureAlgorithm { get; }

	DateTime NotAfter { get; }

	DateTime NotBefore { get; }

	byte[] RawData { get; }

	byte[] Export(X509ContentType contentType, SafePasswordHandle password);
}
