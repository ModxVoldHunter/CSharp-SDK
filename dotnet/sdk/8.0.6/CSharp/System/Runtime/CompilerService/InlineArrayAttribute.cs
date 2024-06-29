namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public sealed class InlineArrayAttribute : Attribute
{
	public int Length { get; }

	public InlineArrayAttribute(int length)
	{
		Length = length;
	}
}
