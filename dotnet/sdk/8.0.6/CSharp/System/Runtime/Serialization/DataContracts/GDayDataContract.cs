namespace System.Runtime.Serialization.DataContracts;

internal sealed class GDayDataContract : StringDataContract
{
	internal GDayDataContract()
		: base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
