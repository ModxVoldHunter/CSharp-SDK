using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Data;

internal sealed class RelationshipConverter : ExpandableObjectConverter
{
	public override bool CanConvertTo(ITypeDescriptorContext context, [NotNullWhen(true)] Type destinationType)
	{
		if (destinationType == typeof(InstanceDescriptor))
		{
			return true;
		}
		return base.CanConvertTo(context, destinationType);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		if (destinationType == typeof(InstanceDescriptor) && value is DataRelation)
		{
			DataRelation dataRelation = (DataRelation)value;
			DataTable table = dataRelation.ParentKey.Table;
			DataTable table2 = dataRelation.ChildKey.Table;
			ConstructorInfo constructor;
			object[] arguments;
			if (string.IsNullOrEmpty(table.Namespace) && string.IsNullOrEmpty(table2.Namespace))
			{
				constructor = typeof(DataRelation).GetConstructor(new Type[6]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string[]),
					typeof(string[]),
					typeof(bool)
				});
				arguments = new object[6]
				{
					dataRelation.RelationName,
					dataRelation.ParentKey.Table.TableName,
					dataRelation.ChildKey.Table.TableName,
					dataRelation.ParentColumnNames,
					dataRelation.ChildColumnNames,
					dataRelation.Nested
				};
			}
			else
			{
				constructor = typeof(DataRelation).GetConstructor(new Type[8]
				{
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string),
					typeof(string[]),
					typeof(string[]),
					typeof(bool)
				});
				arguments = new object[8]
				{
					dataRelation.RelationName,
					dataRelation.ParentKey.Table.TableName,
					dataRelation.ParentKey.Table.Namespace,
					dataRelation.ChildKey.Table.TableName,
					dataRelation.ChildKey.Table.Namespace,
					dataRelation.ParentColumnNames,
					dataRelation.ChildColumnNames,
					dataRelation.Nested
				};
			}
			return new InstanceDescriptor(constructor, arguments);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
