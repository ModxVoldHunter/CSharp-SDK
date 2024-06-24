using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IListConverter<TCollection> : JsonCollectionConverter<TCollection, object> where TCollection : IList
{
	internal override bool CanPopulate => true;

	protected override void Add(in object value, ref ReadStack state)
	{
		TCollection val = (TCollection)state.Current.ReturnValue;
		val.Add(value);
		if (base.IsValueType)
		{
			state.Current.ReturnValue = val;
		}
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
	{
		base.CreateCollection(ref reader, ref state, options);
		if (((TCollection)state.Current.ReturnValue).IsReadOnly)
		{
			state.Current.ReturnValue = null;
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
		}
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		IList list = value;
		int i = state.Current.EnumeratorIndex;
		JsonConverter<object> elementConverter = JsonCollectionConverter<TCollection, object>.GetElementConverter(ref state);
		if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			for (; i < list.Count; i++)
			{
				elementConverter.Write(writer, list[i], options);
			}
		}
		else
		{
			for (; i < list.Count; i++)
			{
				object value2 = list[i];
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

	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		if (jsonTypeInfo.CreateObject == null && Type.IsAssignableFrom(typeof(List<object>)))
		{
			jsonTypeInfo.CreateObject = () => new List<object>();
		}
	}
}
