using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public static class HttpContentJsonExtensions
{
	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(this HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
	{
		return content.ReadFromJsonAsAsyncEnumerable<TValue>((JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(this HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsAsyncEnumerableCore<TValue>(content, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static IAsyncEnumerable<TValue> ReadFromJsonAsAsyncEnumerableCore<TValue>(HttpContent content, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		JsonTypeInfo<TValue> jsonTypeInfo = (JsonTypeInfo<TValue>)JsonHelpers.GetJsonTypeInfo(typeof(TValue), options);
		return ReadFromJsonAsAsyncEnumerableCore(content, jsonTypeInfo, cancellationToken);
	}

	public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(this HttpContent content, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsAsyncEnumerableCore(content, jsonTypeInfo, cancellationToken);
	}

	private static async IAsyncEnumerable<TValue> ReadFromJsonAsAsyncEnumerableCore<TValue>(HttpContent content, JsonTypeInfo<TValue> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await foreach (TValue item in JsonSerializer.DeserializeAsyncEnumerable(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			yield return item;
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsyncCore(content, type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return content.ReadFromJsonAsync(type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsyncCore<T>(content, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
	{
		return content.ReadFromJsonAsync<T>((JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static async Task<object> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync(contentStream, type, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsyncCore(content, type, context, cancellationToken);
	}

	public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		return ReadFromJsonAsyncCore(content, jsonTypeInfo, cancellationToken);
	}

	private static async Task<object> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync(contentStream, type, context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
	{
		using Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await JsonSerializer.DeserializeAsync(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal static async ValueTask<Stream> GetContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
	{
		Stream stream = await ReadHttpContentStreamAsync(content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Encoding encoding = JsonHelpers.GetEncoding(content);
		if (encoding != null && encoding != Encoding.UTF8)
		{
			stream = GetTranscodingStream(stream, encoding);
		}
		return stream;
	}

	private static Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
	{
		return content.ReadAsStreamAsync(cancellationToken);
	}

	private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
	{
		return Encoding.CreateTranscodingStream(contentStream, sourceEncoding, Encoding.UTF8);
	}
}
