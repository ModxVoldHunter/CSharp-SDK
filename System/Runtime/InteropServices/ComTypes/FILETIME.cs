using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct FILETIME
{
	public int dwLowDateTime;

	public int dwHighDateTime;
}
