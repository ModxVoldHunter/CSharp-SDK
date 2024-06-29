using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IDictionaryConverter<TDictionary> : JsonDictionaryConverter<TDictionary, string, object> where TDictionary : IDictionary
{
	internal override bool CanPopulate => true;

	protected override void Add(string key, in object value, JsonSerializerOptions options, ref ReadStack state)
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

	protected internal override bool OnWriteResume(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options, ref WriteStack state)
	{
		IDictionaryEnumerator dictionaryEnumerator;
		if (state.Current.CollectionEnumerator == null)
		{
			dictionaryEnumerator = value.GetEnumerator();
			if (!dictionaryEnumerator.MoveNext())
			{
				return true;
			}
		}
		else
		{
			dictionaryEnumerator = (IDictionaryEnumerator)state.Current.CollectionEnumerator;
		}
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (_valueConverter == null)
		{
			_valueConverter = JsonDictionaryConverter<TDictionary, string, object>.GetConverter<object>(jsonTypeInfo.ElementTypeInfo);
		}
		do
		{
			if (JsonConverter.ShouldFlush(writer, ref state))
			{
				state.Current.CollectionEnumerator = dictionaryEnumerator;
				return false;
			}
			if ((int)state.Current.PropertyState < 2)
			{
				state.Current.PropertyState = StackFramePropertyState.Name;
				object key = dictionaryEnumerator.Key;
				if (key is string value2)
				{
					if (_keyConverter == null)
					{
						_keyConverter = JsonDictionaryConverter<TDictionary, string, object>.GetConverter<string>(jsonTypeInfo.KeyTypeInfo);
					}
					_keyConverter.WriteAsPropertyNameCore(writer, value2, options, state.Current.IsWritingExtensionDataProperty);
				}
				else
				{
					_valueConverter.WriteAsPropertyNameCore(writer, key, options, state.Current.IsWritingExtensionDataProperty);
				}
			}
			object value3 = dictionaryEnumerator.Value;
			if (!_valueConverter.TryWrite(writer, in value3, options, ref state))
			{
				state.Current.CollectionEnumerator = dictionaryEnumerator;
				return false;
			}
			state.Current.EndDictionaryEntry();
		}
		while (dictionaryEnumerator.MoveNext());
		return true;
	}

	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		if (jsonTypeInfo.CreateObject == null && Type.IsAssignableFrom(typeof(Dictionary<string, object>)))
		{
			jsonTypeInfo.CreateObject = () => new Dictionary<string, object>();
		}
	}
}
