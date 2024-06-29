namespace System.Runtime.Serialization.DataContracts;

internal sealed class PositiveIntegerDataContract : LongDataContract
{
	internal PositiveIntegerDataContract()
		: base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
