using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICustomAdapter
{
	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetUnderlyingObject();
}
