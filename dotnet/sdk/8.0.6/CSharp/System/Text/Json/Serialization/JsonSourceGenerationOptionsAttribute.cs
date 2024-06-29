namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class JsonSourceGenerationOptionsAttribute : JsonAttribute
{
	public bool AllowTrailingCommas { get; set; }

	public Type[]? Converters { get; set; }

	public int DefaultBufferSize { get; set; }

	public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

	public JsonKnownNamingPolicy DictionaryKeyPolicy { get; set; }

	public bool IgnoreReadOnlyFields { get; set; }

	public bool IgnoreReadOnlyProperties { get; set; }

	public bool IncludeFields { get; set; }

	public int MaxDepth { get; set; }

	public JsonNumberHandling NumberHandling { get; set; }

	public JsonObjectCreationHandling PreferredObjectCreationHandling { get; set; }

	public bool PropertyNameCaseInsensitive { get; set; }

	public JsonKnownNamingPolicy PropertyNamingPolicy { get; set; }

	public JsonCommentHandling ReadCommentHandling { get; set; }

	public JsonUnknownTypeHandling UnknownTypeHandling { get; set; }

	public JsonUnmappedMemberHandling UnmappedMemberHandling { get; set; }

	public bool WriteIndented { get; set; }

	public JsonSourceGenerationMode GenerationMode { get; set; }

	public bool UseStringEnumConverter { get; set; }

	public JsonSourceGenerationOptionsAttribute()
	{
	}

	public JsonSourceGenerationOptionsAttribute(JsonSerializerDefaults defaults)
	{
		switch (defaults)
		{
		case JsonSerializerDefaults.Web:
			PropertyNameCaseInsensitive = true;
			PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase;
			NumberHandling = JsonNumberHandling.AllowReadingFromString;
			break;
		default:
			throw new ArgumentOutOfRangeException("defaults");
		case JsonSerializerDefaults.General:
			break;
		}
	}
}
