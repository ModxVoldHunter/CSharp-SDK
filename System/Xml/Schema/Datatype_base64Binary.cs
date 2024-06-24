namespace System.Xml.Schema;

internal sealed class Datatype_base64Binary : Datatype_anySimpleType
{
	internal override FacetsChecker FacetsChecker => DatatypeImplementation.binaryFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.Base64Binary;

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
				array = Convert.FromBase64String(s);
			}
			catch (ArgumentException ex2)
			{
				ex = ex2;
				goto IL_003f;
			}
			catch (FormatException ex3)
			{
				ex = ex3;
				goto IL_003f;
			}
			ex = DatatypeImplementation.binaryFacetsChecker.CheckValueFacets(array, this);
			if (ex == null)
			{
				typedValue = array;
				return null;
			}
		}
		goto IL_003f;
		IL_003f:
		return ex;
	}
}
