namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class JsonPolymorphicAttribute : JsonAttribute
{
	public string? TypeDiscriminatorPropertyName { get; set; }

	public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }

	public bool IgnoreUnrecognizedTypeDiscriminators { get; set; }
}
