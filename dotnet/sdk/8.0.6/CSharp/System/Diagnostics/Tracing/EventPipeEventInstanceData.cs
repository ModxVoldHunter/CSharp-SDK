namespace System.Diagnostics.Tracing;

internal struct EventPipeEventInstanceData
{
	internal nint ProviderID;

	internal uint EventID;

	internal uint ThreadID;

	internal long TimeStamp;

	internal Guid ActivityId;

	internal Guid ChildActivityId;

	internal nint Payload;

	internal uint PayloadLength;
}
