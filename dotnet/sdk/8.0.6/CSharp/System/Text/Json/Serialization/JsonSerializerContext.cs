using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

public abstract class JsonSerializerContext : IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
{
	private JsonSerializerOptions _options;

	public JsonSerializerOptions Options
	{
		get
		{
			JsonSerializerOptions jsonSerializerOptions = _options;
			if (jsonSerializerOptions == null)
			{
				jsonSerializerOptions = new JsonSerializerOptions
				{
					TypeInfoResolver = this
				};
				jsonSerializerOptions.MakeReadOnly();
				_options = jsonSerializerOptions;
			}
			return jsonSerializerOptions;
		}
	}

	protected abstract JsonSerializerOptions? GeneratedSerializerOptions { get; }

	internal void AssociateWithOptions(JsonSerializerOptions options)
	{
		options.TypeInfoResolver = this;
		options.MakeReadOnly();
		_options = options;
	}

	bool IBuiltInJsonTypeInfoResolver.IsCompatibleWithOptions(JsonSerializerOptions options)
	{
		JsonSerializerOptions generatedSerializerOptions = GeneratedSerializerOptions;
		if (generatedSerializerOptions != null && options.Converters.Count == 0 && options.Encoder == null && !JsonHelpers.RequiresSpecialNumberHandlingOnWrite(options.NumberHandling) && options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.None && !options.IgnoreNullValues && options.DefaultIgnoreCondition == generatedSerializerOptions.DefaultIgnoreCondition && options.IgnoreReadOnlyFields == generatedSerializerOptions.IgnoreReadOnlyFields && options.IgnoreReadOnlyProperties == generatedSerializerOptions.IgnoreReadOnlyProperties && options.IncludeFields == generatedSerializerOptions.IncludeFields && options.PropertyNamingPolicy == generatedSerializerOptions.PropertyNamingPolicy)
		{
			return options.DictionaryKeyPolicy == null;
		}
		return false;
	}

	protected JsonSerializerContext(JsonSerializerOptions? options)
	{
		if (options != null)
		{
			options.VerifyMutable();
			AssociateWithOptions(options);
		}
	}

	public abstract JsonTypeInfo? GetTypeInfo(Type type);

	JsonTypeInfo IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (options != null && options != _options)
		{
			ThrowHelper.ThrowInvalidOperationException_ResolverTypeInfoOptionsNotCompatible();
		}
		return GetTypeInfo(type);
	}
}
