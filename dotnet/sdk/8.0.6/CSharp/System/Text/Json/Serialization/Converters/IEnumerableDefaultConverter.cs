using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal abstract class IEnumerableDefaultConverter<TCollection, TElement> : JsonCollectionConverter<TCollection, TElement> where TCollection : IEnumerable<TElement>
{
	internal override bool CanHaveMetadata => true;

	protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		IEnumerator<TElement> enumerator;
		if (state.Current.CollectionEnumerator == null)
		{
			enumerator = value.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				enumerator.Dispose();
				return true;
			}
		}
		else
		{
			enumerator = (IEnumerator<TElement>)state.Current.CollectionEnumerator;
		}
		JsonConverter<TElement> elementConverter = JsonCollectionConverter<TCollection, TElement>.GetElementConverter(ref state);
		do
		{
			if (JsonConverter.ShouldFlush(writer, ref state))
			{
				state.Current.CollectionEnumerator = enumerator;
				return false;
			}
			TElement value2 = enumerator.Current;
			if (!elementConverter.TryWrite(writer, in value2, options, ref state))
			{
				state.Current.CollectionEnumerator = enumerator;
				return false;
			}
			state.Current.EndCollectionElement();
		}
		while (enumerator.MoveNext());
		enumerator.Dispose();
		return true;
	}
}
