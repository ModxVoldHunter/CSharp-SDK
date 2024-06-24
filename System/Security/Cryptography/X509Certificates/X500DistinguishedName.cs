using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X500DistinguishedName : AsnEncodedData
{
	private volatile string _lazyDistinguishedName;

	private List<X500RelativeDistinguishedName> _parsedAttributes;

	public string Name => _lazyDistinguishedName ?? (_lazyDistinguishedName = Decode(X500DistinguishedNameFlags.Reversed));

	public X500DistinguishedName(byte[] encodedDistinguishedName)
		: base(new Oid(null, null), encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(ReadOnlySpan<byte> encodedDistinguishedName)
		: base(new Oid(null, null), encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(AsnEncodedData encodedDistinguishedName)
		: base(encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(X500DistinguishedName distinguishedName)
		: base(distinguishedName)
	{
		_lazyDistinguishedName = distinguishedName.Name;
	}

	public X500DistinguishedName(string distinguishedName)
		: this(distinguishedName, X500DistinguishedNameFlags.Reversed)
	{
	}

	public X500DistinguishedName(string distinguishedName, X500DistinguishedNameFlags flag)
		: base(new Oid(null, null), Encode(distinguishedName, flag))
	{
		_lazyDistinguishedName = distinguishedName;
	}

	public string Decode(X500DistinguishedNameFlags flag)
	{
		ThrowIfInvalid(flag);
		return X509Pal.Instance.X500DistinguishedNameDecode(base.RawData, flag);
	}

	public override string Format(bool multiLine)
	{
		return X509Pal.Instance.X500DistinguishedNameFormat(base.RawData, multiLine);
	}

	public IEnumerable<X500RelativeDistinguishedName> EnumerateRelativeDistinguishedNames(bool reversed = true)
	{
		List<X500RelativeDistinguishedName> parsedAttributes = _parsedAttributes ?? (_parsedAttributes = ParseAttributes(base.RawData));
		return EnumerateRelativeDistinguishedNames(parsedAttributes, reversed);
	}

	private static byte[] Encode(string distinguishedName, X500DistinguishedNameFlags flags)
	{
		ArgumentNullException.ThrowIfNull(distinguishedName, "distinguishedName");
		ThrowIfInvalid(flags);
		return X509Pal.Instance.X500DistinguishedNameEncode(distinguishedName, flags);
	}

	private static void ThrowIfInvalid(X500DistinguishedNameFlags flags)
	{
		uint num = 29169u;
		if (((uint)flags & ~num) != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "flag"));
		}
	}

	private static IEnumerable<X500RelativeDistinguishedName> EnumerateRelativeDistinguishedNames(List<X500RelativeDistinguishedName> parsedAttributes, bool reversed)
	{
		if (reversed)
		{
			for (int i = parsedAttributes.Count - 1; i >= 0; i--)
			{
				yield return parsedAttributes[i];
			}
		}
		else
		{
			for (int i = 0; i < parsedAttributes.Count; i++)
			{
				yield return parsedAttributes[i];
			}
		}
	}

	private static List<X500RelativeDistinguishedName> ParseAttributes(byte[] rawData)
	{
		List<X500RelativeDistinguishedName> list = null;
		ReadOnlyMemory<byte> readOnlyMemory = rawData;
		ReadOnlySpan<byte> span = rawData;
		try
		{
			AsnValueReader asnValueReader = new AsnValueReader(span, AsnEncodingRules.DER);
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence();
			asnValueReader.ThrowIfNotEmpty();
			while (asnValueReader2.HasData)
			{
				ReadOnlySpan<byte> other = asnValueReader2.PeekEncodedValue();
				if (!span.Overlaps(other, out var elementOffset))
				{
					throw new UnreachableException();
				}
				X500RelativeDistinguishedName item = new X500RelativeDistinguishedName(readOnlyMemory.Slice(elementOffset, other.Length));
				asnValueReader2.ReadEncodedValue();
				(list ?? (list = new List<X500RelativeDistinguishedName>())).Add(item);
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		return list ?? new List<X500RelativeDistinguishedName>();
	}
}
