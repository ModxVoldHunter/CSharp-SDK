using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ReadOnlyMemoryConverter<T> : JsonCollectionConverter<ReadOnlyMemory<T>, T>
{
	internal override bool CanHaveMetadata => false;

	public override bool HandleNull => true;

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out ReadOnlyMemory<T> value)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			value = default(ReadOnlyMemory<T>);
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
		ReadOnlyMemory<T> readOnlyMemory = ((List<T>)state.Current.ReturnValue).ToArray().AsMemory();
		state.Current.ReturnValue = readOnlyMemory;
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, ReadOnlyMemory<T> value, JsonSerializerOptions options, ref WriteStack state)
	{
		return OnWriteResume(writer, value.Span, options, ref state);
	}

	internal static bool OnWriteResume(Utf8JsonWriter writer, ReadOnlySpan<T> value, JsonSerializerOptions options, ref WriteStack state)
	{
		int i = state.Current.EnumeratorIndex;
		JsonConverter<T> elementConverter = JsonCollectionConverter<ReadOnlyMemory<T>, T>.GetElementConverter(ref state);
		if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			for (; i < value.Length; i++)
			{
				elementConverter.Write(writer, value[i], options);
			}
		}
		else
		{
			for (; i < value.Length; i++)
			{
				if (!elementConverter.TryWrite(writer, in value[i], options, ref state))
				{
					state.Current.EnumeratorIndex = i;
					return false;
				}
				state.Current.EndCollectionElement();
				if (JsonConverter.ShouldFlush(writer, ref state))
				{
					i = (state.Current.EnumeratorIndex = i + 1);
					return false;
				}
			}
		}
		return true;
	}
}
