namespace System.Runtime.Serialization.DataContracts;

internal sealed class NonNegativeIntegerDataContract : LongDataContract
{
	internal NonNegativeIntegerDataContract()
		: base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
