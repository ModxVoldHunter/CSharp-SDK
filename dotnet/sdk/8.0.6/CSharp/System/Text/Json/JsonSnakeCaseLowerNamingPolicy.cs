namespace System.Text.Json;

internal sealed class JsonSnakeCaseLowerNamingPolicy : JsonSeparatorNamingPolicy
{
	public JsonSnakeCaseLowerNamingPolicy()
		: base(lowercase: true, '_')
	{
	}
}
