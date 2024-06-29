namespace System.Xml.Schema;

internal sealed class Datatype_QNameXdr : Datatype_anySimpleType
{
	public override XmlTokenizedType TokenizedType => XmlTokenizedType.QName;

	public override Type ValueType => typeof(XmlQualifiedName);

	internal override Type ListValueType => typeof(XmlQualifiedName[]);

	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		if (string.IsNullOrEmpty(s))
		{
			throw new XmlSchemaException(System.SR.Sch_EmptyAttributeValue, string.Empty);
		}
		ArgumentNullException.ThrowIfNull(nsmgr, "nsmgr");
		try
		{
			string prefix;
			return XmlQualifiedName.Parse(s.Trim(), nsmgr, out prefix);
		}
		catch (XmlSchemaException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new XmlSchemaException(System.SR.Format(System.SR.Sch_InvalidValue, s), innerException);
		}
	}
}
