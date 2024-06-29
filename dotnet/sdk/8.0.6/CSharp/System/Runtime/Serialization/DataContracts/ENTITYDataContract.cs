namespace System.Runtime.Serialization.DataContracts;

internal sealed class ENTITYDataContract : StringDataContract
{
	internal ENTITYDataContract()
		: base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
