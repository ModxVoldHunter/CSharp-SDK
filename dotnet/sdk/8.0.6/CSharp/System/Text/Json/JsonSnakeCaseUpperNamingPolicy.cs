namespace System.Text.Json;

internal sealed class JsonSnakeCaseUpperNamingPolicy : JsonSeparatorNamingPolicy
{
	public JsonSnakeCaseUpperNamingPolicy()
		: base(lowercase: false, '_')
	{
	}
}
