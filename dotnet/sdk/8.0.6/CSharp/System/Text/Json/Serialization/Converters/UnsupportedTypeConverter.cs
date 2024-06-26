namespace System.Text.Json.Serialization.Converters;

internal sealed class UnsupportedTypeConverter<T> : JsonConverter<T>
{
	private readonly string _errorMessage;

	public string ErrorMessage => _errorMessage ?? System.SR.Format(System.SR.SerializeTypeInstanceNotSupported, typeof(T).FullName);

	public UnsupportedTypeConverter(string errorMessage = null)
	{
		_errorMessage = errorMessage;
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException(ErrorMessage);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		throw new NotSupportedException(ErrorMessage);
	}
}
