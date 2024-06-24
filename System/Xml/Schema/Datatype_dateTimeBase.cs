namespace System.Xml.Schema;

internal class Datatype_dateTimeBase : Datatype_anySimpleType
{
	private readonly XsdDateTimeFlags _dateTimeFlags;

	internal override FacetsChecker FacetsChecker => DatatypeImplementation.dateTimeFacetsChecker;

	public override XmlTypeCode TypeCode => XmlTypeCode.DateTime;

	public override Type ValueType => typeof(DateTime);

	internal override Type ListValueType => typeof(DateTime[]);

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Collapse;

	internal override RestrictionFlags ValidRestrictionFlags => RestrictionFlags.Pattern | RestrictionFlags.Enumeration | RestrictionFlags.WhiteSpace | RestrictionFlags.MaxInclusive | RestrictionFlags.MaxExclusive | RestrictionFlags.MinInclusive | RestrictionFlags.MinExclusive;

	internal override XmlValueConverter CreateValueConverter(XmlSchemaType schemaType)
	{
		return XmlDateTimeConverter.Create(schemaType);
	}

	internal Datatype_dateTimeBase(XsdDateTimeFlags dateTimeFlags)
	{
		_dateTimeFlags = dateTimeFlags;
	}

	internal override int Compare(object value1, object value2)
	{
		DateTime dateTime = (DateTime)value1;
		DateTime value3 = (DateTime)value2;
		if (dateTime.Kind == DateTimeKind.Unspecified || value3.Kind == DateTimeKind.Unspecified)
		{
			return dateTime.CompareTo(value3);
		}
		return dateTime.ToUniversalTime().CompareTo(value3.ToUniversalTime());
	}

	internal override Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue)
	{
		typedValue = null;
		Exception ex = DatatypeImplementation.dateTimeFacetsChecker.CheckLexicalFacets(ref s, this);
		if (ex == null)
		{
			if (!XsdDateTime.TryParse(s, _dateTimeFlags, out var result))
			{
				ex = new FormatException(System.SR.Format(System.SR.XmlConvert_BadFormat, s, _dateTimeFlags.ToString()));
			}
			else
			{
				DateTime dateTime;
				try
				{
					dateTime = result;
				}
				catch (ArgumentException ex2)
				{
					ex = ex2;
					goto IL_0078;
				}
				ex = DatatypeImplementation.dateTimeFacetsChecker.CheckValueFacets(dateTime, this);
				if (ex == null)
				{
					typedValue = dateTime;
					return null;
				}
			}
		}
		goto IL_0078;
		IL_0078:
		return ex;
	}
}
