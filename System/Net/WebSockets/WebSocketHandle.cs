using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

internal sealed class WebSocketHandle
{
	internal sealed class DefaultWebProxy : IWebProxy
	{
		public static DefaultWebProxy Instance { get; } = new DefaultWebProxy();


		public ICredentials Credentials
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public Uri GetProxy(Uri destination)
		{
			throw new NotSupportedException();
		}

		public bool IsBypassed(Uri host)
		{
			throw new NotSupportedException();
		}
	}

	private static HttpMessageInvoker s_defaultInvokerDefaultProxy;

	private static HttpMessageInvoker s_defaultInvokerNoProxy;

	private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();

	private WebSocketState _state = WebSocketState.Connecting;

	private WebSocketDeflateOptions _negotiatedDeflateOptions;

	public WebSocket WebSocket { get; private set; }

	public WebSocketState State => WebSocket?.State ?? _state;

	public HttpStatusCode HttpStatusCode { get; private set; }

	public IReadOnlyDictionary<string, IEnumerable<string>> HttpResponseHeaders { get; set; }

	public static ClientWebSocketOptions CreateDefaultOptions()
	{
		return new ClientWebSocketOptions
		{
			Proxy = DefaultWebProxy.Instance
		};
	}

	public void Dispose()
	{
		_state = WebSocketState.Closed;
		WebSocket?.Dispose();
	}

	public void Abort()
	{
		_abortSource.Cancel();
		WebSocket?.Abort();
	}

	public async Task ConnectAsync(Uri uri, HttpMessageInvoker invoker, CancellationToken cancellationToken, ClientWebSocketOptions options)
	{
		bool disposeInvoker = false;
		if (invoker == null)
		{
			if (options.HttpVersion.Major >= 2 || options.HttpVersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
			{
				throw new ArgumentException(System.SR.net_WebSockets_CustomInvokerRequiredForHttp2, "options");
			}
			invoker = SetupInvoker(options, out disposeInvoker);
		}
		else if (!options.AreCompatibleWithCustomInvoker())
		{
			throw new ArgumentException(System.SR.net_WebSockets_OptionsIncompatibleWithCustomInvoker, "options");
		}
		HttpResponseMessage response = null;
		bool disposeResponse = false;
		bool tryDowngrade = uri.Scheme == "ws" && (options.HttpVersion == HttpVersion.Version11 || options.HttpVersionPolicy == HttpVersionPolicy.RequestVersionOrLower);
		try
		{
			while (true)
			{
				try
				{
					HttpRequestMessage httpRequestMessage;
					if ((!tryDowngrade && options.HttpVersion >= HttpVersion.Version20) || (options.HttpVersion == HttpVersion.Version11 && options.HttpVersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && uri.Scheme == "wss"))
					{
						if (options.HttpVersion > HttpVersion.Version20 && options.HttpVersionPolicy != 0)
						{
							throw new WebSocketException(WebSocketError.UnsupportedProtocol);
						}
						httpRequestMessage = new HttpRequestMessage(HttpMethod.Connect, uri)
						{
							Version = HttpVersion.Version20
						};
						tryDowngrade = true;
					}
					else
					{
						if (!tryDowngrade && !(options.HttpVersion == HttpVersion.Version11))
						{
							throw new WebSocketException(WebSocketError.UnsupportedProtocol);
						}
						httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
						{
							Version = HttpVersion.Version11
						};
						tryDowngrade = false;
					}
					WebHeaderCollection requestHeaders = options._requestHeaders;
					if (requestHeaders != null && requestHeaders.Count > 0)
					{
						foreach (string requestHeader in options.RequestHeaders)
						{
							httpRequestMessage.Headers.TryAddWithoutValidation(requestHeader, options.RequestHeaders[requestHeader]);
						}
					}
					string secValue = AddWebSocketHeaders(httpRequestMessage, options);
					CancellationTokenSource externalAndAbortCancellation;
					CancellationTokenSource cancellationTokenSource2;
					if (cancellationToken.CanBeCanceled)
					{
						CancellationTokenSource cancellationTokenSource;
						externalAndAbortCancellation = (cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abortSource.Token));
						cancellationTokenSource2 = cancellationTokenSource;
					}
					else
					{
						cancellationTokenSource2 = null;
						externalAndAbortCancellation = _abortSource;
					}
					using (cancellationTokenSource2)
					{
						Task<HttpResponseMessage> task = ((invoker is HttpClient httpClient) ? httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, externalAndAbortCancellation.Token) : invoker.SendAsync(httpRequestMessage, externalAndAbortCancellation.Token));
						response = await task.ConfigureAwait(continueOnCapturedContext: false);
						externalAndAbortCancellation.Token.ThrowIfCancellationRequested();
					}
					ValidateResponse(response, secValue);
				}
				catch (HttpRequestException ex) when ((ex.HttpRequestError == HttpRequestError.ExtendedConnectNotSupported || ex.Data.Contains("HTTP2_ENABLED")) && tryDowngrade && (options.HttpVersion == HttpVersion.Version11 || options.HttpVersionPolicy == HttpVersionPolicy.RequestVersionOrLower))
				{
					continue;
				}
				break;
			}
			string text = null;
			if (response.Headers.TryGetValues("Sec-WebSocket-Protocol", out IEnumerable<string> values))
			{
				string[] array = (string[])values;
				if (array.Length != 0 && !string.IsNullOrEmpty(array[0]))
				{
					if (options._requestedSubProtocols != null)
					{
						foreach (string requestedSubProtocol in options._requestedSubProtocols)
						{
							if (requestedSubProtocol.Equals(array[0], StringComparison.OrdinalIgnoreCase))
							{
								text = requestedSubProtocol;
								break;
							}
						}
					}
					if (text == null)
					{
						throw new WebSocketException(WebSocketError.UnsupportedProtocol, System.SR.Format(System.SR.net_WebSockets_AcceptUnsupportedProtocol, string.Join(", ", options.RequestedSubProtocols), string.Join(", ", array)));
					}
				}
			}
			WebSocketDeflateOptions webSocketDeflateOptions = null;
			if (options.DangerousDeflateOptions != null && response.Headers.TryGetValues("Sec-WebSocket-Extensions", out IEnumerable<string> values2))
			{
				foreach (string item in values2)
				{
					if (item.AsSpan().TrimStart().StartsWith("permessage-deflate"))
					{
						webSocketDeflateOptions = ParseDeflateOptions(item, options.DangerousDeflateOptions);
						break;
					}
				}
			}
			Stream stream = response.Content.ReadAsStream();
			WebSocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
			{
				IsServer = false,
				SubProtocol = text,
				KeepAliveInterval = options.KeepAliveInterval,
				DangerousDeflateOptions = webSocketDeflateOptions
			});
			_negotiatedDeflateOptions = webSocketDeflateOptions;
		}
		catch (Exception ex2)
		{
			if (_state < WebSocketState.Closed)
			{
				_state = WebSocketState.Closed;
			}
			Abort();
			disposeResponse = true;
			if (ex2 is WebSocketException || (ex2 is OperationCanceledException && cancellationToken.IsCancellationRequested))
			{
				throw;
			}
			throw new WebSocketException(WebSocketError.Faulted, System.SR.net_webstatus_ConnectFailure, ex2);
		}
		finally
		{
			if (response != null)
			{
				if (options.CollectHttpResponseDetails)
				{
					HttpStatusCode = response.StatusCode;
					HttpResponseHeaders = new HttpResponseHeadersReadOnlyCollection(response.Headers);
				}
				if (disposeResponse)
				{
					response.Dispose();
				}
			}
			if (disposeInvoker)
			{
				invoker?.Dispose();
			}
		}
	}

	private static HttpMessageInvoker SetupInvoker(ClientWebSocketOptions options, out bool disposeInvoker)
	{
		if (options.AreCompatibleWithCustomInvoker())
		{
			disposeInvoker = false;
			bool flag = options.Proxy != null;
			ref HttpMessageInvoker reference = ref flag ? ref s_defaultInvokerDefaultProxy : ref s_defaultInvokerNoProxy;
			if (reference == null)
			{
				HttpMessageInvoker httpMessageInvoker = new HttpMessageInvoker(new SocketsHttpHandler
				{
					PooledConnectionLifetime = TimeSpan.Zero,
					UseProxy = flag,
					UseCookies = false
				});
				if (Interlocked.CompareExchange(ref reference, httpMessageInvoker, null) != null)
				{
					httpMessageInvoker.Dispose();
				}
			}
			return reference;
		}
		disposeInvoker = true;
		SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();
		socketsHttpHandler.PooledConnectionLifetime = TimeSpan.Zero;
		socketsHttpHandler.CookieContainer = options.Cookies;
		socketsHttpHandler.UseCookies = options.Cookies != null;
		socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback;
		socketsHttpHandler.Credentials = (options.UseDefaultCredentials ? CredentialCache.DefaultCredentials : options.Credentials);
		if (options.Proxy == null)
		{
			socketsHttpHandler.UseProxy = false;
		}
		else if (options.Proxy != DefaultWebProxy.Instance)
		{
			socketsHttpHandler.Proxy = options.Proxy;
		}
		X509CertificateCollection clientCertificates = options._clientCertificates;
		if (clientCertificates != null && clientCertificates.Count > 0)
		{
			socketsHttpHandler.SslOptions.ClientCertificates = new X509Certificate2Collection();
			socketsHttpHandler.SslOptions.ClientCertificates.AddRange(options.ClientCertificates);
		}
		return new HttpMessageInvoker(socketsHttpHandler);
	}

	private static WebSocketDeflateOptions ParseDeflateOptions(ReadOnlySpan<char> extension, WebSocketDeflateOptions original)
	{
		WebSocketDeflateOptions webSocketDeflateOptions = new WebSocketDeflateOptions();
		while (true)
		{
			int num = extension.IndexOf(';');
			ReadOnlySpan<char> readOnlySpan = ((num >= 0) ? extension.Slice(0, num) : extension).Trim();
			if (readOnlySpan.Length > 0)
			{
				if (readOnlySpan.SequenceEqual("client_no_context_takeover"))
				{
					webSocketDeflateOptions.ClientContextTakeover = false;
				}
				else if (readOnlySpan.SequenceEqual("server_no_context_takeover"))
				{
					webSocketDeflateOptions.ServerContextTakeover = false;
				}
				else if (readOnlySpan.StartsWith("client_max_window_bits"))
				{
					webSocketDeflateOptions.ClientMaxWindowBits = ParseWindowBits(readOnlySpan);
				}
				else if (readOnlySpan.StartsWith("server_max_window_bits"))
				{
					webSocketDeflateOptions.ServerMaxWindowBits = ParseWindowBits(readOnlySpan);
				}
			}
			if (num < 0)
			{
				break;
			}
			int num2 = num + 1;
			extension = extension.Slice(num2, extension.Length - num2);
		}
		if (webSocketDeflateOptions.ClientMaxWindowBits > original.ClientMaxWindowBits)
		{
			throw new WebSocketException(System.SR.Format(System.SR.net_WebSockets_ClientWindowBitsNegotiationFailure, original.ClientMaxWindowBits, webSocketDeflateOptions.ClientMaxWindowBits));
		}
		if (webSocketDeflateOptions.ServerMaxWindowBits > original.ServerMaxWindowBits)
		{
			throw new WebSocketException(System.SR.Format(System.SR.net_WebSockets_ServerWindowBitsNegotiationFailure, original.ServerMaxWindowBits, webSocketDeflateOptions.ServerMaxWindowBits));
		}
		return webSocketDeflateOptions;
		static int ParseWindowBits(ReadOnlySpan<char> value)
		{
			int num3 = value.IndexOf('=');
			if (num3 < 0 || !int.TryParse(value.Slice(num3 + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < 9 || result > 15)
			{
				throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_InvalidResponseHeader, "permessage-deflate", value.ToString()));
			}
			return result;
		}
	}

	private static string AddWebSocketHeaders(HttpRequestMessage request, ClientWebSocketOptions options)
	{
		request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
		string result = null;
		if (request.Version == HttpVersion.Version11)
		{
			KeyValuePair<string, string> keyValuePair = CreateSecKeyAndSecWebSocketAccept();
			result = keyValuePair.Value;
			request.Headers.TryAddWithoutValidation("Connection", "Upgrade");
			request.Headers.TryAddWithoutValidation("Upgrade", "websocket");
			request.Headers.TryAddWithoutValidation("Sec-WebSocket-Key", keyValuePair.Key);
		}
		else if (request.Version == HttpVersion.Version20)
		{
			request.Headers.Protocol = "websocket";
		}
		request.Headers.TryAddWithoutValidation("Sec-WebSocket-Version", "13");
		List<string> requestedSubProtocols = options._requestedSubProtocols;
		if (requestedSubProtocols != null && requestedSubProtocols.Count > 0)
		{
			request.Headers.TryAddWithoutValidation("Sec-WebSocket-Protocol", string.Join(", ", options.RequestedSubProtocols));
		}
		if (options.DangerousDeflateOptions != null)
		{
			request.Headers.TryAddWithoutValidation("Sec-WebSocket-Extensions", GetDeflateOptions(options.DangerousDeflateOptions));
		}
		return result;
		static string GetDeflateOptions(WebSocketDeflateOptions options)
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			stringBuilder.Append("permessage-deflate").Append("; ");
			if (options.ClientMaxWindowBits != 15)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2, invariantCulture);
				handler.AppendFormatted("client_max_window_bits");
				handler.AppendLiteral("=");
				handler.AppendFormatted(options.ClientMaxWindowBits);
				stringBuilder3.Append(provider, ref handler);
			}
			else
			{
				stringBuilder.Append("client_max_window_bits");
			}
			if (!options.ClientContextTakeover)
			{
				stringBuilder.Append("; ").Append("client_no_context_takeover");
			}
			if (options.ServerMaxWindowBits != 15)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider2 = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2, invariantCulture);
				handler.AppendLiteral("; ");
				handler.AppendFormatted("server_max_window_bits");
				handler.AppendLiteral("=");
				handler.AppendFormatted(options.ServerMaxWindowBits);
				stringBuilder4.Append(provider2, ref handler);
			}
			if (!options.ServerContextTakeover)
			{
				stringBuilder.Append("; ").Append("server_no_context_takeover");
			}
			return stringBuilder.ToString();
		}
	}

	private static void ValidateResponse(HttpResponseMessage response, string secValue)
	{
		if (response.Version == HttpVersion.Version11)
		{
			if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
			{
				throw new WebSocketException(WebSocketError.NotAWebSocket, System.SR.Format(System.SR.net_WebSockets_ConnectStatusExpected, (int)response.StatusCode, 101));
			}
			ValidateHeader(response.Headers, "Connection", "Upgrade");
			ValidateHeader(response.Headers, "Upgrade", "websocket");
			ValidateHeader(response.Headers, "Sec-WebSocket-Accept", secValue);
		}
		else if (response.Version == HttpVersion.Version20 && response.StatusCode != HttpStatusCode.OK)
		{
			throw new WebSocketException(WebSocketError.NotAWebSocket, System.SR.Format(System.SR.net_WebSockets_ConnectStatusExpected, (int)response.StatusCode, 200));
		}
		if (response.Content == null)
		{
			throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
		}
	}

	private static KeyValuePair<string, string> CreateSecKeyAndSecWebSocketAccept()
	{
		ReadOnlySpan<byte> readOnlySpan = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"u8;
		Span<byte> span = stackalloc byte[24 + readOnlySpan.Length];
		bool flag = Guid.NewGuid().TryWriteBytes(span);
		string text = Convert.ToBase64String(span.Slice(0, 16));
		int bytes = Encoding.ASCII.GetBytes(text, span);
		readOnlySpan.CopyTo(span.Slice(bytes));
		SHA1.TryHashData(span, span, out var bytesWritten);
		return new KeyValuePair<string, string>(text, Convert.ToBase64String(span.Slice(0, bytesWritten)));
	}

	private static void ValidateHeader(HttpHeaders headers, string name, string expectedValue)
	{
		if (headers.NonValidated.TryGetValues(name, out var values))
		{
			if (values.Count == 1)
			{
				using HeaderStringValues.Enumerator enumerator = values.GetEnumerator();
				if (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					if (string.Equals(current, expectedValue, StringComparison.OrdinalIgnoreCase))
					{
						return;
					}
				}
			}
			throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_InvalidResponseHeader, name, values));
		}
		throw new WebSocketException(WebSocketError.Faulted, System.SR.Format(System.SR.net_WebSockets_MissingResponseHeader, name));
	}
}
