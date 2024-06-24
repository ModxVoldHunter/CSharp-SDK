namespace System.Runtime.Serialization.DataContracts;

internal sealed class LanguageDataContract : StringDataContract
{
	internal LanguageDataContract()
		: base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
