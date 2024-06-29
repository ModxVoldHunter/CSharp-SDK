namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public interface IIUnknownCacheStrategy
{
	public readonly struct TableInfo
	{
		public unsafe void* ThisPtr { get; init; }

		public unsafe void** Table { get; init; }

		public RuntimeTypeHandle ManagedType { get; init; }
	}

	unsafe TableInfo ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails interfaceDetails, void* ptr);

	bool TryGetTableInfo(RuntimeTypeHandle handle, out TableInfo info);

	bool TrySetTableInfo(RuntimeTypeHandle handle, TableInfo info);

	void Clear(IIUnknownStrategy unknownStrategy);
}
