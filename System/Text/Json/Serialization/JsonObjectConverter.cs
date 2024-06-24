namespace System.Text.Json.Serialization;

internal abstract class JsonObjectConverter<T> : JsonResumableConverter<T>
{
	internal override bool CanPopulate => true;

	internal sealed override Type ElementType => null;

	private protected sealed override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.Object;
	}
}
