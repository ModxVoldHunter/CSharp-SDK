namespace System.Runtime.Serialization.DataContracts;

internal sealed class XsDurationDataContract : TimeSpanDataContract
{
	public XsDurationDataContract()
		: base(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
