using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class VariantWrapper
{
	public object? WrappedObject { get; }

	public VariantWrapper(object? obj)
	{
		WrappedObject = obj;
	}
}
