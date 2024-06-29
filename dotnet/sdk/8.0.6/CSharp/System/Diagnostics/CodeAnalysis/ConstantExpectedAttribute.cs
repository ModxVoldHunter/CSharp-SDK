namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class ConstantExpectedAttribute : Attribute
{
	public object? Min { get; set; }

	public object? Max { get; set; }
}
