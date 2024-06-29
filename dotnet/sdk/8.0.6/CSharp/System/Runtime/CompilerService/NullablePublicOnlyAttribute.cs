using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Module, Inherited = false)]
public sealed class NullablePublicOnlyAttribute : Attribute
{
	public readonly bool IncludesInternals;

	public NullablePublicOnlyAttribute(bool value)
	{
		IncludesInternals = value;
	}
}
