namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class JsonObjectCreationHandlingAttribute : JsonAttribute
{
	public JsonObjectCreationHandling Handling { get; }

	public JsonObjectCreationHandlingAttribute(JsonObjectCreationHandling handling)
	{
		if (!JsonSerializer.IsValidCreationHandlingValue(handling))
		{
			throw new ArgumentOutOfRangeException("handling");
		}
		Handling = handling;
	}
}
