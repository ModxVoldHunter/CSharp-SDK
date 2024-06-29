using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class UnknownWrapper
{
	public object? WrappedObject { get; }

	public UnknownWrapper(object? obj)
	{
		WrappedObject = obj;
	}
}
