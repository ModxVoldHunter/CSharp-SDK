namespace System.Xml.Schema;

internal class Datatype_unsignedShort : Datatype_unsignedInt
{
	private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(0m, 65535m);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.UnsignedShort;

	public override Type ValueType => typeof(ushort);

	internal override Type ListValueType => typeof(ushort[]);

	internal override int Compare(object value1, object value2)
	{
		return ((ushort)value1).CompareTo((ushort)value2);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XmlConvert.TryToUInt16(s, out var result);
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
