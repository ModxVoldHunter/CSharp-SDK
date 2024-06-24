namespace System.Runtime.Serialization.DataContracts;

internal sealed class GMonthDayDataContract : StringDataContract
{
	internal GMonthDayDataContract()
		: base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
