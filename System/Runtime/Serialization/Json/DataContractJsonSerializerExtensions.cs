namespace System.Runtime.Serialization.Json;

public static class DataContractJsonSerializerExtensions
{
	public static ISerializationSurrogateProvider? GetSerializationSurrogateProvider(this DataContractJsonSerializer serializer)
	{
		return serializer.SerializationSurrogateProvider;
	}

	public static void SetSerializationSurrogateProvider(this DataContractJsonSerializer serializer, ISerializationSurrogateProvider? provider)
	{
		serializer.SerializationSurrogateProvider = provider;
	}
}
