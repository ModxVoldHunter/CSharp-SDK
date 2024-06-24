namespace System.Runtime.Serialization.DataContracts;

internal sealed class NonPositiveIntegerDataContract : LongDataContract
{
	internal NonPositiveIntegerDataContract()
		: base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
