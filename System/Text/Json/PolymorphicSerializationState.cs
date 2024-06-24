namespace System.Text.Json;

internal enum PolymorphicSerializationState : byte
{
	None,
	PolymorphicReEntryStarted,
	PolymorphicReEntrySuspended,
	PolymorphicReEntryNotFound
}
