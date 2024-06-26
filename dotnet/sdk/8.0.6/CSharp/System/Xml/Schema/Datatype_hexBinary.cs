namespace System.Xml.Schema;

internal sealed class Datatype_hexBinary : Datatype_anySimpleType
{
	internal override FacetsChecker FacetsChecker => DatatypeImplementation.binaryFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.HexBinary;

	public override Type ValueType => typeof(byte[]);

	internal override Type ListValueType => typeof(byte[][]);

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlMiscConverter.Create(schemaType);
	}

	internal override int Compare(object value1, object value2)
	{
		return DatatypeImplementation.Compare((byte[])value1, (byte[])value2);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = DatatypeImplementation.binaryFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			byte[] array;
			try
			{
				array = XmlConvert.FromBinHexString(s, allowOddCount: false);
			}
			catch (ArgumentException ex2)
			{
				ex = ex2;
				goto IL_0045;
			}
			catch (XmlException ex3)
			{
				ex = ex3;
				goto IL_0045;
			}
			ex = DatatypeImplementation.binaryFacetsChecker.CheckValueFacets(array, this);
			if (ex == null)
			{
				typedValue = array;
				return null;
			}
		}
		goto IL_0045;
		IL_0045:
		return ex;
	}
}
