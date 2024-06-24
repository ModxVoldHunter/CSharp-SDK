using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices;

[NonVersionable]
internal sealed class RawArrayData
{
	public uint Length;

	public uint Padding;

	public byte Data;
}
