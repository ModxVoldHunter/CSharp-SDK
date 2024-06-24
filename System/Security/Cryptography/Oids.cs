using System.Formats.Asn1;

namespace System.Security.Cryptography;

internal static class Oids
{
	private static volatile Oid s_rsaOid;

	private static volatile Oid s_ecPublicKeyOid;

	private static volatile Oid s_secp256R1Oid;

	private static volatile Oid s_secp384R1Oid;

	private static volatile Oid s_secp521R1Oid;

	private static volatile Oid s_pkcs9ExtensionRequestOid;

	private static volatile Oid s_basicConstraints2Oid;

	private static volatile Oid s_enhancedKeyUsageOid;

	private static volatile Oid s_keyUsageOid;

	private static volatile Oid s_subjectAltNameOid;

	private static volatile Oid s_subjectKeyIdentifierOid;

	private static volatile Oid s_authorityKeyIdentifierOid;

	private static volatile Oid s_authorityInformationAccessOid;

	private static volatile Oid s_crlNumberOid;

	private static volatile Oid s_crlDistributionPointOid;

	private static volatile Oid s_commonNameOid;

	private static volatile Oid s_countryOrRegionOid;

	private static volatile Oid s_localityNameOid;

	private static volatile Oid s_stateOrProvinceNameOid;

	private static volatile Oid s_organizationOid;

	private static volatile Oid s_organizationalUnitOid;

	private static volatile Oid s_emailAddressOid;

	internal static Oid StateOrProvinceNameOid = s_stateOrProvinceNameOid ?? (s_stateOrProvinceNameOid = InitializeOid("2.5.4.8"));

	internal static Oid OrganizationOid = s_organizationOid ?? (s_organizationOid = InitializeOid("2.5.4.10"));

	internal static Oid OrganizationalUnitOid = s_organizationalUnitOid ?? (s_organizationalUnitOid = InitializeOid("2.5.4.11"));

	internal static Oid EmailAddressOid = s_emailAddressOid ?? (s_emailAddressOid = InitializeOid("1.2.840.113549.1.9.1"));

	internal static Oid RsaOid => s_rsaOid ?? (s_rsaOid = InitializeOid("1.2.840.113549.1.1.1"));

	internal static Oid EcPublicKeyOid => s_ecPublicKeyOid ?? (s_ecPublicKeyOid = InitializeOid("1.2.840.10045.2.1"));

	internal static Oid secp256r1Oid => s_secp256R1Oid ?? (s_secp256R1Oid = new Oid("1.2.840.10045.3.1.7", "nistP256"));

	internal static Oid secp384r1Oid => s_secp384R1Oid ?? (s_secp384R1Oid = new Oid("1.3.132.0.34", "nistP384"));

	internal static Oid secp521r1Oid => s_secp521R1Oid ?? (s_secp521R1Oid = new Oid("1.3.132.0.35", "nistP521"));

	internal static Oid Pkcs9ExtensionRequestOid => s_pkcs9ExtensionRequestOid ?? (s_pkcs9ExtensionRequestOid = InitializeOid("1.2.840.113549.1.9.14"));

	internal static Oid BasicConstraints2Oid => s_basicConstraints2Oid ?? (s_basicConstraints2Oid = InitializeOid("2.5.29.19"));

	internal static Oid EnhancedKeyUsageOid => s_enhancedKeyUsageOid ?? (s_enhancedKeyUsageOid = InitializeOid("2.5.29.37"));

	internal static Oid KeyUsageOid => s_keyUsageOid ?? (s_keyUsageOid = InitializeOid("2.5.29.15"));

	internal static Oid AuthorityKeyIdentifierOid => s_authorityKeyIdentifierOid ?? (s_authorityKeyIdentifierOid = InitializeOid("2.5.29.35"));

	internal static Oid SubjectKeyIdentifierOid => s_subjectKeyIdentifierOid ?? (s_subjectKeyIdentifierOid = InitializeOid("2.5.29.14"));

	internal static Oid SubjectAltNameOid => s_subjectAltNameOid ?? (s_subjectAltNameOid = InitializeOid("2.5.29.17"));

	internal static Oid AuthorityInformationAccessOid => s_authorityInformationAccessOid ?? (s_authorityInformationAccessOid = InitializeOid("1.3.6.1.5.5.7.1.1"));

	internal static Oid CrlNumberOid => s_crlNumberOid ?? (s_crlNumberOid = InitializeOid("2.5.29.20"));

	internal static Oid CrlDistributionPointsOid => s_crlDistributionPointOid ?? (s_crlDistributionPointOid = InitializeOid("2.5.29.31"));

	internal static Oid CommonNameOid => s_commonNameOid ?? (s_commonNameOid = InitializeOid("2.5.4.3"));

	internal static Oid CountryOrRegionNameOid => s_countryOrRegionOid ?? (s_countryOrRegionOid = InitializeOid("2.5.4.6"));

	internal static Oid LocalityNameOid => s_localityNameOid ?? (s_localityNameOid = InitializeOid("2.5.4.7"));

	private static Oid InitializeOid(string oidValue)
	{
		Oid oid = new Oid(oidValue, null);
		_ = oid.FriendlyName;
		return oid;
	}

	internal static Oid GetSharedOrNewOid(ref AsnValueReader asnValueReader)
	{
		Oid sharedOrNullOid = GetSharedOrNullOid(ref asnValueReader);
		if (sharedOrNullOid != null)
		{
			return sharedOrNullOid;
		}
		string value = asnValueReader.ReadObjectIdentifier();
		return new Oid(value, null);
	}

	internal static Oid GetSharedOrNullOid(ref AsnValueReader asnValueReader, Asn1Tag? expectedTag = null)
	{
		Asn1Tag asn1Tag = asnValueReader.PeekTag();
		if (asn1Tag.IsConstructed)
		{
			return null;
		}
		Asn1Tag valueOrDefault = expectedTag.GetValueOrDefault(Asn1Tag.ObjectIdentifier);
		if (!asn1Tag.HasSameClassAndValue(valueOrDefault))
		{
			return null;
		}
		ReadOnlySpan<byte> readOnlySpan = asnValueReader.PeekContentBytes();
		int length = readOnlySpan.Length;
		Oid oid;
		if (length != 3)
		{
			if (length == 9)
			{
				byte b = readOnlySpan[0];
				if (b == 42)
				{
					byte b2 = readOnlySpan[1];
					if (b2 == 134)
					{
						byte b3 = readOnlySpan[2];
						if (b3 == 72)
						{
							byte b4 = readOnlySpan[3];
							if (b4 == 134)
							{
								byte b5 = readOnlySpan[4];
								if (b5 == 247)
								{
									byte b6 = readOnlySpan[5];
									if (b6 == 13)
									{
										byte b7 = readOnlySpan[6];
										if (b7 == 1)
										{
											byte b8 = readOnlySpan[7];
											if (b8 == 9)
											{
												byte b9 = readOnlySpan[8];
												if (b9 == 1)
												{
													oid = EmailAddressOid;
													goto IL_01d3;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		else
		{
			byte b = readOnlySpan[0];
			if (b == 85)
			{
				byte b2 = readOnlySpan[1];
				if (b2 == 4)
				{
					switch (readOnlySpan[2])
					{
					case 3:
						break;
					case 6:
						goto IL_019a;
					case 7:
						goto IL_01a3;
					case 8:
						goto IL_01ac;
					case 10:
						goto IL_01b5;
					case 11:
						goto IL_01be;
					default:
						goto IL_01d0;
					}
					oid = CommonNameOid;
					goto IL_01d3;
				}
				if (b2 == 29)
				{
					byte b3 = readOnlySpan[2];
					if (b3 == 20)
					{
						oid = CrlNumberOid;
						goto IL_01d3;
					}
				}
			}
		}
		goto IL_01d0;
		IL_01ac:
		oid = StateOrProvinceNameOid;
		goto IL_01d3;
		IL_01be:
		oid = OrganizationalUnitOid;
		goto IL_01d3;
		IL_01d3:
		Oid oid2 = oid;
		if (oid2 != null)
		{
			asnValueReader.ReadEncodedValue();
		}
		return oid2;
		IL_01a3:
		oid = LocalityNameOid;
		goto IL_01d3;
		IL_01d0:
		oid = null;
		goto IL_01d3;
		IL_01b5:
		oid = OrganizationOid;
		goto IL_01d3;
		IL_019a:
		oid = CountryOrRegionNameOid;
		goto IL_01d3;
	}

	internal static bool ValueEquals(this Oid oid, Oid other)
	{
		if (oid == other)
		{
			return true;
		}
		if (other == null)
		{
			return false;
		}
		if (oid.Value != null)
		{
			return oid.Value.Equals(other.Value);
		}
		return false;
	}
}
