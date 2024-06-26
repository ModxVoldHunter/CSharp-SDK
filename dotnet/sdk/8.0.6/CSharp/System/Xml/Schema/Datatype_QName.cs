namespace System.Xml.Schema;

internal sealed class Datatype_QName : Datatype_anySimpleType
{
	internal override FacetsChecker FacetsChecker => DatatypeImplementation.qnameFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.QName;

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.QName;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace;

	public override Type ValueType => typeof(XmlQualifiedName);

	internal override Type ListValueType => typeof(XmlQualifiedName[]);

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlMiscConverter.Create(schemaType);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		if (string.IsNullOrEmpty(s))
		{
			return new XmlSchemaException(System.SR.Sch_EmptyAttributeValue, string.Empty);
		}
		Exception ex = DatatypeImplementation.qnameFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			XmlQualifiedName xmlQualifiedName;
			try
			{
				xmlQualifiedName = XmlQualifiedName.Parse(s, nsmgr, out var _);
			}
			catch (ArgumentException ex2)
			{
				ex = ex2;
				goto IL_005c;
			}
			catch (XmlException ex3)
			{
				ex = ex3;
				goto IL_005c;
			}
			ex = DatatypeImplementation.qnameFacetsChecker.CheckValueFacets(xmlQualifiedName, this);
			if (ex == null)
			{
				typedValue = xmlQualifiedName;
				return null;
			}
		}
		goto IL_005c;
		IL_005c:
		return ex;
	}
}
