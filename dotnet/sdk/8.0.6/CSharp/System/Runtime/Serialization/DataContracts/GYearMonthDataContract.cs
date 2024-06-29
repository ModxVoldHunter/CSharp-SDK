namespace System.Runtime.Serialization.DataContracts;

internal sealed class GYearMonthDataContract : StringDataContract
{
	internal GYearMonthDataContract()
		: base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
