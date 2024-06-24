using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ISetOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : ISet<TElement>
{
	internal override bool CanPopulate => true;

	protected override void Add(in TElement value, ref ReadStack state)
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

	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		if (jsonTypeInfo.CreateObject == null && Type.IsAssignableFrom(typeof(HashSet<TElement>)))
		{
			jsonTypeInfo.CreateObject = () => new HashSet<TElement>();
		}
	}
}
