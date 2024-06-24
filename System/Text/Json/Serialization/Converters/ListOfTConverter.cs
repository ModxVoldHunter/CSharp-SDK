using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ListOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : List<TElement>
{
	internal override bool CanPopulate => true;

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((TCollection)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
	{
		JsonPropertyInfo parentProperty = state.ParentProperty;
		if (parentProperty == null || !parentProperty.TryGetPrePopulatedValue(ref state))
		{
			if (state.Current.JsonTypeInfo.CreateObject == null)
			{
				ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonTypeInfo.Type);
			}
			state.Current.ReturnValue = state.Current.JsonTypeInfo.CreateObject();
		}
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		int i = state.Current.EnumeratorIndex;
		JsonConverter<TElement> elementConverter = JsonCollectionConverter<TCollection, TElement>.GetElementConverter(ref state);
		if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			for (; i < value.Count; i++)
			{
				elementConverter.Write(writer, value[i], options);
			}
		}
		else
		{
			for (; i < value.Count; i++)
			{
				TElement value2 = value[i];
				if (!elementConverter.TryWrite(writer, in value2, options, ref state))
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
