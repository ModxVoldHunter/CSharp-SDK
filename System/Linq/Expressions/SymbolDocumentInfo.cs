namespace System.Linq.Expressions;

public class SymbolDocumentInfo
{
	internal static readonly Guid DocumentType_Text = new Guid(1518771467, 26129, 4563, 189, 42, 0, 0, 248, 8, 73, 189);

	public string FileName { get; }

	public virtual Guid Language => Guid.Empty;

	public virtual Guid LanguageVendor => Guid.Empty;

	public virtual Guid DocumentType => DocumentType_Text;

	internal SymbolDocumentInfo(string fileName)
	{
		ArgumentNullException.ThrowIfNull(fileName, "fileName");
		FileName = fileName;
	}
}
