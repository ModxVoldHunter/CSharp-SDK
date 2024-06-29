using System.ComponentModel;

namespace System.Runtime.InteropServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICustomQueryInterface
{
	CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out nint ppv);
}
