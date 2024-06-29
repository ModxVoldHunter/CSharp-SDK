using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct STATDATA
{
	public FORMATETC formatetc;

	public ADVF advf;

	public IAdviseSink advSink;

	public int connection;
}
