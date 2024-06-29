using System.Collections.Generic;
using System.Text;

namespace System.Security.Cryptography.X509Certificates;

internal interface ICertificatePal : ICertificatePalCore, IDisposable
{
	int Version { get; }

	bool Archived { get; set; }

	string FriendlyName { get; set; }

	X500DistinguishedName SubjectName { get; }

	X500DistinguishedName IssuerName { get; }

	IEnumerable<X509Extension> Extensions { get; }

	RSA GetRSAPrivateKey();

	DSA GetDSAPrivateKey();

	ECDsa GetECDsaPrivateKey();

	ECDiffieHellman GetECDiffieHellmanPrivateKey();

	string GetNameInfo(X509NameType nameType, bool forIssuer);

	void AppendPrivateKeyInfo(StringBuilder sb);

	ICertificatePal CopyWithPrivateKey(DSA privateKey);

	ICertificatePal CopyWithPrivateKey(ECDsa privateKey);

	ICertificatePal CopyWithPrivateKey(RSA privateKey);

	ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey);

	PolicyData GetPolicyData();
}
