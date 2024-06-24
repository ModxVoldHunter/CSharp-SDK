using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class QueueOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : Queue<TElement>
{
	internal override bool CanPopulate => true;

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((TCollection)state.Current.ReturnValue).Enqueue(value);
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
}
