namespace System.Runtime.Serialization.DataContracts;

internal sealed class NegativeIntegerDataContract : LongDataContract
{
	internal NegativeIntegerDataContract()
		: base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
