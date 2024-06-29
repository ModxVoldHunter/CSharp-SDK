using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class SlimObjectConverter : ObjectConverter
{
	private readonly IJsonTypeInfoResolver _originatingResolver;

	public SlimObjectConverter(IJsonTypeInfoResolver originatingResolver)
	{
		_originatingResolver = originatingResolver;
	}

	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ThrowHelper.ThrowNotSupportedException_NoMetadataForType(typeToConvert, _originatingResolver);
		return null;
	}
}
