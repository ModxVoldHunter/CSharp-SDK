using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

public static class HttpClientJsonExtensions
{
	private static readonly Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> s_deleteAsync = (HttpClient client, Uri uri, CancellationToken cancellation) => client.DeleteAsync(uri, cancellation);

	private static readonly Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> s_getAsync = (HttpClient client, Uri uri, CancellationToken cancellation) => client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation);

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsAsyncEnumerable<TValue>(CreateUri(requestUri), options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonStreamAsyncCore<TValue>(client, requestUri, options, cancellationToken);
	}

	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsAsyncEnumerable(CreateUri(requestUri), jsonTypeInfo, cancellationToken);
	}

	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonStreamAsyncCore(client, requestUri, jsonTypeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsAsyncEnumerable<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsAsyncEnumerable<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static IAsyncEnumerable<TValue> FromJsonStreamAsyncCore<TValue>(HttpClient client, Uri requestUri, JsonSerializerOptions options, CancellationToken cancellationToken)
	{
		JsonTypeInfo<TValue> jsonTypeInfo = (JsonTypeInfo<TValue>)JsonHelpers.GetJsonTypeInfo(typeof(TValue), options);
		return FromJsonStreamAsyncCore(client, requestUri, jsonTypeInfo, cancellationToken);
	}

	private static IAsyncEnumerable<TValue> FromJsonStreamAsyncCore<TValue>(HttpClient client, Uri requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken)
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		return Core(client, requestUri, jsonTypeInfo, cancellationToken);
		static async IAsyncEnumerable<TValue> Core(HttpClient client, Uri requestUri, JsonTypeInfo<TValue> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			response.EnsureSuccessStatusCode();
			using Stream readStream = await GetHttpResponseStreamAsync(client, response, usingResponseHeadersRead: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await foreach (TValue item in JsonSerializer.DeserializeAsyncEnumerable(readStream, jsonTypeInfo, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
			{
				yield return item;
			}
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static Task<object> FromJsonAsyncCore(Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri requestUri, Type type, JsonSerializerOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(getMethod, client, requestUri, (Stream stream, (Type type, JsonSerializerOptions options) options, CancellationToken cancellation) => JsonSerializer.DeserializeAsync(stream, options.type, options.options ?? JsonHelpers.s_defaultSerializerOptions, cancellation), (type, options), cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	private static Task<TValue> FromJsonAsyncCore<TValue>(Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri requestUri, JsonSerializerOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(getMethod, client, requestUri, (Stream stream, JsonSerializerOptions options, CancellationToken cancellation) => JsonSerializer.DeserializeAsync<TValue>(stream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellation), options, cancellationToken);
	}

	private static Task<object> FromJsonAsyncCore(Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(getMethod, client, requestUri, (Stream stream, (Type type, JsonSerializerContext context) options, CancellationToken cancellation) => JsonSerializer.DeserializeAsync(stream, options.type, options.context, cancellation), (type, context), cancellationToken);
	}

	private static Task<TValue> FromJsonAsyncCore<TValue>(Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken)
	{
		return FromJsonAsyncCore(getMethod, client, requestUri, (Stream stream, JsonTypeInfo<TValue> options, CancellationToken cancellation) => JsonSerializer.DeserializeAsync(stream, options, cancellation), jsonTypeInfo, cancellationToken);
	}

	private static Task<TValue> FromJsonAsyncCore<TValue, TJsonOptions>(Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri requestUri, Func<Stream, TJsonOptions, CancellationToken, ValueTask<TValue>> deserializeMethod, TJsonOptions jsonOptions, CancellationToken cancellationToken)
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		TimeSpan timeout = client.Timeout;
		CancellationTokenSource cancellationTokenSource = null;
		if (timeout != Timeout.InfiniteTimeSpan)
		{
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationTokenSource.CancelAfter(timeout);
		}
		Task<HttpResponseMessage> responseTask2;
		try
		{
			responseTask2 = getMethod(client, requestUri, cancellationToken);
		}
		catch
		{
			cancellationTokenSource?.Dispose();
			throw;
		}
		bool usingResponseHeadersRead2 = (object)getMethod != s_deleteAsync;
		return Core(client, responseTask2, usingResponseHeadersRead2, cancellationTokenSource, deserializeMethod, jsonOptions, cancellationToken);
		static async Task<TValue> Core(HttpClient client, Task<HttpResponseMessage> responseTask, bool usingResponseHeadersRead, CancellationTokenSource linkedCTS, Func<Stream, TJsonOptions, CancellationToken, ValueTask<TValue>> deserializeMethod, TJsonOptions jsonOptions, CancellationToken cancellationToken)
		{
			_ = 2;
			try
			{
				using HttpResponseMessage response = await responseTask.ConfigureAwait(continueOnCapturedContext: false);
				response.EnsureSuccessStatusCode();
				try
				{
					using Stream readStream = await GetHttpResponseStreamAsync(client, response, usingResponseHeadersRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					return await deserializeMethod(readStream, jsonOptions, linkedCTS?.Token ?? cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException ex) when ((linkedCTS?.Token.IsCancellationRequested ?? false) && !cancellationToken.IsCancellationRequested)
				{
					string message = System.SR.Format(System.SR.net_http_request_timedout, client.Timeout.TotalSeconds);
					throw new TaskCanceledException(message, new TimeoutException(ex.Message, ex), ex.CancellationToken);
				}
			}
			finally
			{
				linkedCTS?.Dispose();
			}
		}
	}

	private static Uri CreateUri(string uri)
	{
		if (!string.IsNullOrEmpty(uri))
		{
			return new Uri(uri, UriKind.RelativeOrAbsolute);
		}
		return null;
	}

	private static async Task<Stream> GetHttpResponseStreamAsync(HttpClient client, HttpResponseMessage response, bool usingResponseHeadersRead, CancellationToken cancellationToken)
	{
		int num = (int)client.MaxResponseContentBufferSize;
		long? contentLength = response.Content.Headers.ContentLength;
		if (contentLength.HasValue)
		{
			long valueOrDefault = contentLength.GetValueOrDefault();
			if (valueOrDefault > num)
			{
				LengthLimitReadStream.ThrowExceededBufferLimit(num);
			}
		}
		Stream stream = await HttpContentJsonExtensions.GetContentStreamAsync(response.Content, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return usingResponseHeadersRead ? new LengthLimitReadStream(stream, (int)client.MaxResponseContentBufferSize) : stream;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync(CreateUri(requestUri), type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(s_deleteAsync, client, requestUri, type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync<TValue>(CreateUri(requestUri), options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore<TValue>(s_deleteAsync, client, requestUri, options, cancellationToken);
	}

	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync(CreateUri(requestUri), type, context, cancellationToken);
	}

	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(s_deleteAsync, client, requestUri, type, context, cancellationToken);
	}

	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync(CreateUri(requestUri), jsonTypeInfo, cancellationToken);
	}

	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(s_deleteAsync, client, requestUri, jsonTypeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.DeleteFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(CreateUri(requestUri), type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore((HttpClient client, Uri uri, CancellationToken cancellation) => client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation), client, requestUri, type, options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync<TValue>(CreateUri(requestUri), options, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore<TValue>(s_getAsync, client, requestUri, options, cancellationToken);
	}

	public static Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(CreateUri(requestUri), type, context, cancellationToken);
	}

	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(s_getAsync, client, requestUri, type, context, cancellationToken);
	}

	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(CreateUri(requestUri), jsonTypeInfo, cancellationToken);
	}

	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		return FromJsonAsyncCore(s_getAsync, client, requestUri, jsonTypeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync(requestUri, type, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default(CancellationToken))
	{
		return client.GetFromJsonAsync<TValue>(requestUri, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PostAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PostAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	public static Task<HttpResponseMessage> PostAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PostAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PutAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PutAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	public static Task<HttpResponseMessage> PutAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PutAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PatchAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, null, options);
		return client.PatchAsync(requestUri, content, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PatchAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.")]
	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
	{
		return client.PatchAsJsonAsync(requestUri, value, (JsonSerializerOptions?)null, cancellationToken);
	}

	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PatchAsync(requestUri, content, cancellationToken);
	}

	public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		JsonContent content = JsonContent.Create(value, jsonTypeInfo);
		return client.PatchAsync(requestUri, content, cancellationToken);
	}
}
