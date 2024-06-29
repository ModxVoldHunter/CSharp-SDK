namespace System.Diagnostics.SymbolStore;

public interface ISymbolBinder1
{
	ISymbolReader? GetReader(nint importer, string filename, string searchPath);
}
