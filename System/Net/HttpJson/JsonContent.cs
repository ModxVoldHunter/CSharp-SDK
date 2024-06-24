using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public sealed class JsonContent : HttpContent
{
	private readonly JsonTypeInfo _typeInfo;

	public Type ObjectType => _typeInfo.Type;

	public object? Value { get; }

	private JsonContent(object inputValue, JsonTypeInfo jsonTypeInfo, MediaTypeHeaderValue mediaType)
	{
		Value = inputValue;
		_typeInfo = jsonTypeInfo;
		base.Headers.ContentType = mediaType ?? JsonHelpers.GetDefaultMediaType();
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
	{
		return Create(inputValue, JsonHelpers.GetJsonTypeInfo(typeof(T), options), mediaType);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
	{
		System.ThrowHelper.ThrowIfNull(inputType, "inputType");
		EnsureTypeCompatibility(inputValue, inputType);
		return new JsonContent(inputValue, JsonHelpers.GetJsonTypeInfo(inputType, options), mediaType);
	}

	public static JsonContent Create<T>(T? inputValue, JsonTypeInfo<T> jsonTypeInfo, MediaTypeHeaderValue? mediaType = null)
	{
		System.ThrowHelper.ThrowIfNull(jsonTypeInfo, "jsonTypeInfo");
		return new JsonContent(inputValue, jsonTypeInfo, mediaType);
	}

	public static JsonContent Create(object? inputValue, JsonTypeInfo jsonTypeInfo, MediaTypeHeaderValue? mediaType = null)
	{
		System.ThrowHelper.ThrowIfNull(jsonTypeInfo, "jsonTypeInfo");
		EnsureTypeCompatibility(inputValue, jsonTypeInfo.Type);
		return new JsonContent(inputValue, jsonTypeInfo, mediaType);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		return SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);
	}

	protected override bool TryComputeLength(out long length)
	{
		length = 0L;
		return false;
	}

	private async Task SerializeToStreamAsyncCore(Stream targetStream, bool async, CancellationToken cancellationToken)
	{
		Encoding encoding = JsonHelpers.GetEncoding(this);
		if (encoding != null && encoding != Encoding.UTF8)
		{
			Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, encoding, Encoding.UTF8, leaveOpen: true);
			try
			{
				if (async)
				{
					await JsonSerializer.SerializeAsync(transcodingStream, Value, _typeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					JsonSerializer.Serialize(transcodingStream, Value, _typeInfo);
				}
			}
			finally
			{
				if (async)
				{
					await transcodingStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					transcodingStream.Dispose();
				}
			}
		}
		else if (async)
		{
			await JsonSerializer.SerializeAsync(targetStream, Value, _typeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			JsonSerializer.Serialize(targetStream, Value, _typeInfo);
		}
	}

	private static void EnsureTypeCompatibility(object inputValue, Type inputType)
	{
		if (inputValue != null && !inputType.IsAssignableFrom(inputValue.GetType()))
		{
			throw new ArgumentException(System.SR.Format(System.SR.SerializeWrongType, inputType, inputValue.GetType()));
		}
	}

	protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		SerializeToStreamAsyncCore(stream, async: false, cancellationToken).GetAwaiter().GetResult();
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		return SerializeToStreamAsyncCore(stream, async: true, cancellationToken);
	}
}
