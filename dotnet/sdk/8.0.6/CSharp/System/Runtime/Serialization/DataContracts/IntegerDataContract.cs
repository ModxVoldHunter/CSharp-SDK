namespace System.Runtime.Serialization.DataContracts;

internal sealed class IntegerDataContract : LongDataContract
{
	internal IntegerDataContract()
		: base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
