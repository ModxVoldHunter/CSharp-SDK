namespace System.Runtime.Serialization.DataContracts;

internal sealed class GYearDataContract : StringDataContract
{
	internal GYearDataContract()
		: base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
