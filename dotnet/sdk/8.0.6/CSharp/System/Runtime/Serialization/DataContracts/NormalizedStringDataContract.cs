namespace System.Runtime.Serialization.DataContracts;

internal sealed class NormalizedStringDataContract : StringDataContract
{
	internal NormalizedStringDataContract()
		: base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
