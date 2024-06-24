using System.Collections;

namespace System.Xml.Schema;

internal sealed class ListFacetsChecker : FacetsChecker
{
	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		Array array = value as Array;
		RestrictionFacets restriction = datatype.Restriction;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		if ((restrictionFlags & (RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength)) != 0)
		{
			int length = array.Length;
			if ((restrictionFlags & RestrictionFlags.Length) != 0 && restriction.Length != length)
			{
				return new XmlSchemaException(System.SR.Sch_LengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MinLength) != 0 && length < restriction.MinLength)
			{
				return new XmlSchemaException(System.SR.Sch_MinLengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MaxLength) != 0 && restriction.MaxLength < length)
			{
				return new XmlSchemaException(System.SR.Sch_MaxLengthConstraintFailed, string.Empty);
			}
		}
		if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration, datatype))
		{
			return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
		}
		return null;
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		Array array = value as Array;
		Datatype_List datatype_List = datatype as Datatype_List;
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < enumeration.Count; j++)
			{
				Array array2 = enumeration[j] as Array;
				if (datatype_List.ItemType.Compare(array.GetValue(i), array2.GetValue(0)) == 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}
}
