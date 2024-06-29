using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue> : DictionaryDefaultConverter<TDictionary, TKey, TValue> where TDictionary : IDictionary<TKey, TValue>
{
	internal override bool CanPopulate => true;

	protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		TDictionary val = (TDictionary)state.Current.ReturnValue;
		val[key] = value;
		if (base.IsValueType)
		{
			state.Current.ReturnValue = val;
		}
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		base.CreateCollection(ref reader, ref state);
		if (((TDictionary)state.Current.ReturnValue).IsReadOnly)
		{
			state.Current.ReturnValue = null;
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
		}
	}

	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		if (jsonTypeInfo.CreateObject == null && Type.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
		{
			jsonTypeInfo.CreateObject = () => new Dictionary<TKey, TValue>();
		}
	}
}
