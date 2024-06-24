namespace System.Xml.Schema;

internal class Datatype_short : Datatype_int
{
	private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(-32768m, 32767m);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.Short;

	public override Type ValueType => typeof(short);

	internal override Type ListValueType => typeof(short[]);

	internal override int Compare(object value1, object value2)
	{
		return ((short)value1).CompareTo((short)value2);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XmlConvert.TryToInt16(s, out var result);
			if (ex == null)
			{
				ex = s_numeric10FacetsChecker.CheckValueFacets(result, this);
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
