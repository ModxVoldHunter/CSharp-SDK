using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509AuthorityInformationAccessExtension : X509Extension
{
	private AccessDescriptionAsn[] _decoded;

	public X509AuthorityInformationAccessExtension()
		: base(Oids.AuthorityInformationAccessOid)
	{
		_decoded = Array.Empty<AccessDescriptionAsn>();
	}

	public X509AuthorityInformationAccessExtension(byte[] rawData, bool critical = false)
		: base(Oids.AuthorityInformationAccessOid, rawData, critical)
	{
		_decoded = Decode(base.RawData);
	}

	public X509AuthorityInformationAccessExtension(ReadOnlySpan<byte> rawData, bool critical = false)
		: base(Oids.AuthorityInformationAccessOid, rawData, critical)
	{
		_decoded = Decode(base.RawData);
	}

	public X509AuthorityInformationAccessExtension(IEnumerable<string>? ocspUris, IEnumerable<string>? caIssuersUris, bool critical = false)
		: base(Oids.AuthorityInformationAccessOid, Encode(ocspUris, caIssuersUris), critical, skipCopy: true)
	{
		_decoded = Decode(base.RawData);
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = null;
	}

	public IEnumerable<string> EnumerateUris(string accessMethodOid)
	{
		ArgumentNullException.ThrowIfNull(accessMethodOid, "accessMethodOid");
		if (_decoded == null)
		{
			_decoded = Decode(base.RawData);
		}
		return EnumerateUrisCore(accessMethodOid);
	}

	public IEnumerable<string> EnumerateUris(Oid accessMethodOid)
	{
		ArgumentNullException.ThrowIfNull(accessMethodOid, "accessMethodOid");
		ArgumentException.ThrowIfNullOrEmpty(accessMethodOid.Value, "accessMethodOid.Value");
		return EnumerateUris(accessMethodOid.Value);
	}

	private IEnumerable<string> EnumerateUrisCore(string accessMethodOid)
	{
		for (int i = 0; i < _decoded.Length; i++)
		{
			string text = GetUri(accessMethodOid, ref _decoded[i]);
			if (text != null)
			{
				yield return text;
			}
		}
		static string GetUri(string accessMethodOid, ref AccessDescriptionAsn desc)
		{
			if (string.Equals(accessMethodOid, desc.AccessMethod))
			{
				return desc.AccessLocation.Uri;
			}
			return null;
		}
	}

	public IEnumerable<string> EnumerateCAIssuersUris()
	{
		return EnumerateUris("1.3.6.1.5.5.7.48.2");
	}

	public IEnumerable<string> EnumerateOcspUris()
	{
		return EnumerateUris("1.3.6.1.5.5.7.48.1");
	}

	private static AccessDescriptionAsn[] Decode(byte[] authorityInfoAccessSyntax)
	{
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(authorityInfoAccessSyntax, AsnEncodingRules.DER);
			AsnValueReader reader = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
			int num = 0;
			AsnValueReader asnValueReader2 = reader;
			while (asnValueReader2.HasData)
			{
				num++;
				asnValueReader2.ReadEncodedValue();
			}
			AccessDescriptionAsn[] array = new AccessDescriptionAsn[num];
			num = 0;
			while (reader.HasData)
			{
				AccessDescriptionAsn.Decode(ref reader, authorityInfoAccessSyntax, out array[num]);
				num++;
			}
			return array;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static byte[] Encode(IEnumerable<string> ocspUris, IEnumerable<string> caIssuersUris)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		bool flag = true;
		asnWriter.PushSequence();
		if (ocspUris != null)
		{
			foreach (string ocspUri in ocspUris)
			{
				WriteAccessMethod(asnWriter, "1.3.6.1.5.5.7.48.1", ocspUri);
				flag = false;
			}
		}
		if (caIssuersUris != null)
		{
			foreach (string caIssuersUri in caIssuersUris)
			{
				WriteAccessMethod(asnWriter, "1.3.6.1.5.5.7.48.2", caIssuersUri);
				flag = false;
			}
		}
		asnWriter.PopSequence();
		if (flag)
		{
			throw new ArgumentException(System.SR.Cryptography_X509_AIA_MustNotBuildEmpty);
		}
		return asnWriter.Encode();
		static void WriteAccessMethod(AsnWriter writer, string oid, string value)
		{
			if (value == null)
			{
				throw new ArgumentException(System.SR.Cryptography_X509_AIA_NullValue);
			}
			writer.PushSequence();
			writer.WriteObjectIdentifier(oid);
			try
			{
				writer.WriteCharacterString(UniversalTagNumber.IA5String, value, new Asn1Tag(TagClass.ContextSpecific, 6));
			}
			catch (EncoderFallbackException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Invalid_IA5String, inner);
			}
			writer.PopSequence();
		}
	}
}
