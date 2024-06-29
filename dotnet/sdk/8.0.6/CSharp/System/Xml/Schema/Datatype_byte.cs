namespace System.Xml.Schema;

internal sealed class Datatype_byte : Datatype_short
{
	private static readonly Numeric10FacetsChecker s_numeric10FacetsChecker = new Numeric10FacetsChecker(-128m, 127m);

	internal override FacetsChecker FacetsChecker => s_numeric10FacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.Byte;

	public override Type ValueType => typeof(sbyte);

	internal override Type ListValueType => typeof(sbyte[]);

	internal override int Compare(object value1, object value2)
	{
		return ((sbyte)value1).CompareTo((sbyte)value2);
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = s_numeric10FacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			ex = XmlConvert.TryToSByte(s, out var result);
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
