namespace System.Runtime.Serialization.DataContracts;

internal sealed class TokenDataContract : StringDataContract
{
	internal TokenDataContract()
		: base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
