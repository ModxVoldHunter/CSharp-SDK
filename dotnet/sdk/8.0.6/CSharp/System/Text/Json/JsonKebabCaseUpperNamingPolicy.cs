namespace System.Text.Json;

internal sealed class JsonKebabCaseUpperNamingPolicy : JsonSeparatorNamingPolicy
{
	public JsonKebabCaseUpperNamingPolicy()
		: base(lowercase: false, '-')
	{
	}
}
