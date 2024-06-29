namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class InversePropertyAttribute : Attribute
{
	public string Property { get; }

	public InversePropertyAttribute(string property)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(property, "property");
		Property = property;
	}
}
