namespace System.Text.Json;

internal sealed class JsonKebabCaseLowerNamingPolicy : JsonSeparatorNamingPolicy
{
	public JsonKebabCaseLowerNamingPolicy()
		: base(lowercase: true, '-')
	{
	}
}
