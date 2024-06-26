namespace System.Text.Json;

internal enum StackFrameObjectState : byte
{
	None,
	StartToken,
	ReadMetadata,
	ConstructorArguments,
	CreatedObject,
	ReadElements,
	EndToken,
	EndTokenValidation
}
