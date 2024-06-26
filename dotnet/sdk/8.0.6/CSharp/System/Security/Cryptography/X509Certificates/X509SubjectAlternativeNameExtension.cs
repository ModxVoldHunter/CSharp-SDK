using System.Collections.Generic;
using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509SubjectAlternativeNameExtension : X509Extension
{
	private List<GeneralNameAsn> _decoded;

	public X509SubjectAlternativeNameExtension()
		: base(Oids.SubjectAltNameOid)
	{
		_decoded = new List<GeneralNameAsn>(0);
	}

	public X509SubjectAlternativeNameExtension(byte[] rawData, bool critical = false)
		: base(Oids.SubjectAltNameOid, rawData, critical)
	{
		_decoded = Decode(base.RawData);
	}

	public X509SubjectAlternativeNameExtension(ReadOnlySpan<byte> rawData, bool critical = false)
		: base(Oids.SubjectAltNameOid, rawData, critical)
	{
		_decoded = Decode(base.RawData);
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = null;
	}

	public IEnumerable<string> EnumerateDnsNames()
	{
		List<GeneralNameAsn> decoded = _decoded ?? (_decoded = Decode(base.RawData));
		return EnumerateDnsNames(decoded);
	}

	private static IEnumerable<string> EnumerateDnsNames(List<GeneralNameAsn> decoded)
	{
		foreach (GeneralNameAsn item in decoded)
		{
			if (item.DnsName != null)
			{
				yield return item.DnsName;
			}
		}
	}

	public IEnumerable<IPAddress> EnumerateIPAddresses()
	{
		List<GeneralNameAsn> decoded = _decoded ?? (_decoded = Decode(base.RawData));
		return EnumerateIPAddresses(decoded);
	}

	private static IEnumerable<IPAddress> EnumerateIPAddresses(List<GeneralNameAsn> decoded)
	{
		foreach (GeneralNameAsn item in decoded)
		{
			if (item.IPAddress.HasValue)
			{
				ReadOnlySpan<byte> span = item.IPAddress.GetValueOrDefault().Span;
				yield return new IPAddress(span);
			}
		}
	}

	private static List<GeneralNameAsn> Decode(ReadOnlySpan<byte> rawData)
	{
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(rawData, AsnEncodingRules.DER);
			AsnValueReader reader = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
			List<GeneralNameAsn> list = new List<GeneralNameAsn>();
			while (reader.HasData)
			{
				GeneralNameAsn.Decode(ref reader, default(ReadOnlyMemory<byte>), out var decoded);
				if (decoded.IPAddress.HasValue)
				{
					int length = decoded.IPAddress.GetValueOrDefault().Length;
					if (length != 4 && length != 16)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_SAN_UnknownIPAddressSize);
					}
				}
				list.Add(decoded);
			}
			return list;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}
}
