namespace System.Runtime.Serialization.DataContracts;

internal sealed class AsmxGuidDataContract : GuidDataContract
{
	internal AsmxGuidDataContract()
		: base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace)
	{
	}
}
