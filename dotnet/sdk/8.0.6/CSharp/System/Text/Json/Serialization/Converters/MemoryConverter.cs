using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class MemoryConverter<T> : JsonCollectionConverter<Memory<T>, T>
{
	internal override bool CanHaveMetadata => false;

	public override bool HandleNull => true;

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out Memory<T> value)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			value = default(Memory<T>);
			return true;
		}
		return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
	}

	protected override void Add(in T value, ref ReadStack state)
	{
		((List<T>)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = new List<T>();
	}

	protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		Memory<T> memory = ((List<T>)state.Current.ReturnValue).ToArray().AsMemory();
		state.Current.ReturnValue = memory;
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, Memory<T> value, JsonSerializerOptions options, ref WriteStack state)
	{
		return ReadOnlyMemoryConverter<T>.OnWriteResume(writer, value.Span, options, ref state);
	}
}
