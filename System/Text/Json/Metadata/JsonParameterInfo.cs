namespace System.Text.Json.Serialization.Metadata;

internal abstract class JsonParameterInfo
{
	public JsonConverter EffectiveConverter => MatchingProperty.EffectiveConverter;

	public object DefaultValue { get; private protected init; }

	public bool IgnoreNullTokensOnRead { get; }

	public JsonSerializerOptions Options { get; }

	public byte[] NameAsUtf8Bytes { get; }

	public JsonNumberHandling? NumberHandling { get; }

	public int Position { get; }

	public JsonTypeInfo JsonTypeInfo => MatchingProperty.JsonTypeInfo;

	public Type ParameterType { get; }

	public bool ShouldDeserialize { get; }

	public JsonPropertyInfo MatchingProperty { get; }

	public JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo matchingProperty)
	{
		MatchingProperty = matchingProperty;
		ShouldDeserialize = !matchingProperty.IsIgnored;
		Options = matchingProperty.Options;
		Position = parameterInfoValues.Position;
		ParameterType = matchingProperty.PropertyType;
		NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes;
		IgnoreNullTokensOnRead = matchingProperty.IgnoreNullTokensOnRead;
		NumberHandling = matchingProperty.EffectiveNumberHandling;
	}
}
internal sealed class JsonParameterInfo<T> : JsonParameterInfo
{
	public new JsonConverter<T> EffectiveConverter => MatchingProperty.EffectiveConverter;

	public new JsonPropertyInfo<T> MatchingProperty { get; }

	public new T DefaultValue { get; }

	public JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo<T> matchingPropertyInfo)
		: base(parameterInfoValues, matchingPropertyInfo)
	{
		MatchingProperty = matchingPropertyInfo;
		DefaultValue = ((parameterInfoValues.HasDefaultValue && parameterInfoValues.DefaultValue != null) ? ((T)parameterInfoValues.DefaultValue) : default(T));
		base.DefaultValue = DefaultValue;
	}
}
