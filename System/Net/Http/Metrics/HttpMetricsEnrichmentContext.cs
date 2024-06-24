using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Http.Metrics;

public sealed class HttpMetricsEnrichmentContext
{
	private static readonly HttpRequestOptionsKey<HttpMetricsEnrichmentContext> s_optionsKeyForContext = new HttpRequestOptionsKey<HttpMetricsEnrichmentContext>("HttpMetricsEnrichmentContext");

	private static readonly ConcurrentQueue<HttpMetricsEnrichmentContext> s_pool = new ConcurrentQueue<HttpMetricsEnrichmentContext>();

	private static int s_poolItemCount;

	private readonly List<Action<HttpMetricsEnrichmentContext>> _callbacks = new List<Action<HttpMetricsEnrichmentContext>>();

	private HttpRequestMessage _request;

	private HttpResponseMessage _response;

	private Exception _exception;

	private List<KeyValuePair<string, object>> _tags = new List<KeyValuePair<string, object>>(16);

	public HttpRequestMessage Request => _request;

	public HttpResponseMessage? Response => _response;

	public Exception? Exception => _exception;

	internal HttpMetricsEnrichmentContext()
	{
	}

	public void AddCustomTag(string name, object? value)
	{
		_tags.Add(new KeyValuePair<string, object>(name, value));
	}

	public static void AddCallback(HttpRequestMessage request, Action<HttpMetricsEnrichmentContext> callback)
	{
		HttpRequestOptions options = request.Options;
		if (!options.TryGetValue(s_optionsKeyForContext, out var value))
		{
			if (s_pool.TryDequeue(out value))
			{
				Interlocked.Decrement(ref s_poolItemCount);
			}
			else
			{
				value = new HttpMetricsEnrichmentContext();
			}
			options.Set(s_optionsKeyForContext, value);
		}
		value._callbacks.Add(callback);
	}

	internal static HttpMetricsEnrichmentContext GetEnrichmentContextForRequest(HttpRequestMessage request)
	{
		if (request._options == null)
		{
			return null;
		}
		request._options.TryGetValue(s_optionsKeyForContext, out var value);
		return value;
	}

	internal void RecordDurationWithEnrichment(HttpRequestMessage request, HttpResponseMessage response, Exception exception, TimeSpan durationTime, in TagList commonTags, Histogram<double> requestDuration)
	{
		_request = request;
		_response = response;
		_exception = exception;
		for (int i = 0; i < commonTags.Count; i++)
		{
			_tags.Add(commonTags[i]);
		}
		try
		{
			foreach (Action<HttpMetricsEnrichmentContext> callback in _callbacks)
			{
				callback(this);
			}
			requestDuration.Record(durationTime.TotalSeconds, CollectionsMarshal.AsSpan(_tags));
		}
		finally
		{
			_request = null;
			_response = null;
			_exception = null;
			_callbacks.Clear();
			_tags.Clear();
			if (Interlocked.Increment(ref s_poolItemCount) <= 1024)
			{
				s_pool.Enqueue(this);
			}
			else
			{
				Interlocked.Decrement(ref s_poolItemCount);
			}
		}
	}
}
