using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IReadOnlyDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue> : DictionaryDefaultConverter<TDictionary, TKey, TValue> where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
	private readonly bool _isDeserializable = typeof(TDictionary).IsAssignableFrom(typeof(Dictionary<TKey, TValue>));

	internal override bool SupportsCreateObjectDelegate => false;

	protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		((Dictionary<TKey, TValue>)state.Current.ReturnValue)[key] = value;
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		if (!_isDeserializable)
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
		}
		state.Current.ReturnValue = new Dictionary<TKey, TValue>();
	}
}
