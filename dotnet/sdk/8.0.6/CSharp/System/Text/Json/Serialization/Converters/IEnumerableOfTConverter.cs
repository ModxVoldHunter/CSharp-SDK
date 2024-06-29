using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IEnumerableOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : IEnumerable<TElement>
{
	private readonly bool _isDeserializable = typeof(TCollection).IsAssignableFrom(typeof(List<TElement>));

	internal override bool SupportsCreateObjectDelegate => false;

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((List<TElement>)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
	{
		if (!_isDeserializable)
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
		}
		state.Current.ReturnValue = new List<TElement>();
	}
}
