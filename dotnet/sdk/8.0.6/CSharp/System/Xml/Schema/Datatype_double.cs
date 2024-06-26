namespace System.Xml.Schema;

internal class Datatype_double : Datatype_anySimpleType
{
	internal override FacetsChecker FacetsChecker => DatatypeImplementation.numeric2FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.Double;

	public override Type ValueType => typeof(double);

	internal override Type ListValueType => typeof(double[]);

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace | RestrictionFlags.MaxInclusive | RestrictionFlags.MaxExclusive | RestrictionFlags.MinInclusive | RestrictionFlags.MinExclusive;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlNumeric2Converter.Create(schemaType);
	}

	internal override int Compare(object value1, object value2)
	{
		return ((double)value1).CompareTo((double)value2);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = DatatypeImplementation.numeric2FacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XmlConvert.TryToDouble(s, out var result);
			if (ex == null)
			{
				ex = DatatypeImplementation.numeric2FacetsChecker.CheckValueFacets(result, this);
				if (ex == null)
				{
					typedValue = result;
					return null;
				}
			}
		}
		return ex;
	}
}
