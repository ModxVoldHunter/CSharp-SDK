using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates;

internal sealed class Pkcs9ExtensionRequest : X501Attribute
{
	internal Pkcs9ExtensionRequest(IEnumerable<X509Extension> extensions)
		: base(Oids.Pkcs9ExtensionRequestOid, EncodeAttribute(extensions))
	{
	}

	private static byte[] EncodeAttribute(IEnumerable<X509Extension> extensions)
	{
		ArgumentNullException.ThrowIfNull(extensions, "extensions");
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		using (asnWriter.PushSequence())
		{
			foreach (X509Extension extension in extensions)
			{
				new X509ExtensionAsn(extension).Encode(asnWriter);
			}
		}
		return asnWriter.Encode();
	}
}
