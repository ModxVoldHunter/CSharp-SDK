namespace System.Diagnostics.Tracing;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class EventChannelAttribute : Attribute
{
	public bool Enabled { get; set; }

	public EventChannelType EventChannelType { get; set; }
}
