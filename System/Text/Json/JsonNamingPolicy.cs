namespace System.Text.Json;

public abstract class JsonNamingPolicy
{
	public static JsonNamingPolicy CamelCase { get; } = new JsonCamelCaseNamingPolicy();


	public static JsonNamingPolicy SnakeCaseLower { get; } = new JsonSnakeCaseLowerNamingPolicy();


	public static JsonNamingPolicy SnakeCaseUpper { get; } = new JsonSnakeCaseUpperNamingPolicy();


	public static JsonNamingPolicy KebabCaseLower { get; } = new JsonKebabCaseLowerNamingPolicy();


	public static JsonNamingPolicy KebabCaseUpper { get; } = new JsonKebabCaseUpperNamingPolicy();


	public abstract string ConvertName(string name);
}
