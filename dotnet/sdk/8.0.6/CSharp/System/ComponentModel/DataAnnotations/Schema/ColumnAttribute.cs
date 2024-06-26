using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
	private int _order = -1;

	private string _typeName;

	public string? Name { get; }

	public int Order
	{
		get
		{
			return _order;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			_order = value;
		}
	}

	public string? TypeName
	{
		get
		{
			return _typeName;
		}
		[param: DisallowNull]
		set
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(value, "value");
			_typeName = value;
		}
	}

	public ColumnAttribute()
	{
	}

	public ColumnAttribute(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name, "name");
		Name = name;
	}
}
