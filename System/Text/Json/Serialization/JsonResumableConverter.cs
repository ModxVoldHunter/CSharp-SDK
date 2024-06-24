using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

internal abstract class JsonResumableConverter<T> : JsonConverter<T>
{
	public override bool HandleNull => false;

	public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		ReadStack state = default(ReadStack);
		JsonTypeInfo typeInfoInternal = options.GetTypeInfoInternal(typeToConvert, ensureConfigured: true, true);
		state.Initialize(typeInfoInternal);
		TryRead(ref reader, typeToConvert, options, ref state, out var value, out var _);
		return value;
	}

	public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		WriteStack state = default(WriteStack);
		JsonTypeInfo typeInfoInternal = options.GetTypeInfoInternal(typeof(T), ensureConfigured: true, true);
		state.Initialize(typeInfoInternal);
		try
		{
			TryWrite(writer, in value, options, ref state);
		}
		catch
		{
			state.DisposePendingDisposablesOnException();
			throw;
		}
	}
}
