using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;

namespace LaunchDarkly.Xamarin.Tests.HttpHelpers
{
    /// <summary>
    /// An abstraction used by <see cref="Handler"/> implementations to hide the details of the
    /// underlying HTTP server framework.
    /// </summary>
    public sealed class RequestContext
    {
        public RequestInfo RequestInfo { get; }
        public CancellationToken CancellationToken { get; }
        private IHttpContext InternalContext { get; }
        private volatile bool _chunked;

        private RequestContext(IHttpContext context, RequestInfo requestInfo)
        {
            InternalContext = context;
            RequestInfo = requestInfo;
            CancellationToken = context.CancellationToken;
        }

        internal static async Task<RequestContext> FromHttpContext(IHttpContext ctx)
        {
            var requestInfo = new RequestInfo
            {
                Method = ctx.Request.HttpMethod,
                Path = ctx.RequestedPath,
                Query = ctx.Request.Url.Query,
                Headers = ctx.Request.Headers
            };

            if (ctx.Request.HasEntityBody)
            {
                requestInfo.Body = await ctx.GetRequestBodyAsStringAsync();
            }
            return new RequestContext(ctx, requestInfo);
        }

        /// <summary>
        /// Sets the response status.
        /// </summary>
        /// <param name="statusCode">the HTTP status</param>
        public void SetStatus(int statusCode) => InternalContext.Response.StatusCode = statusCode;

        /// <summary>
        /// Sets a response header.
        /// </summary>
        /// <param name="name">the header name</param>
        /// <param name="value">the header value</param>
        public void SetHeader(string name, string value)
        {
            if (name.ToLower() == "content-type")
            {
                InternalContext.Response.ContentType = value;
            }
            else
            {
                InternalContext.Response.Headers.Set(name, value);
            }
        }

        /// <summary>
        /// Adds a response header, allowing multiple values.
        /// </summary>
        /// <param name="name">the header name</param>
        /// <param name="value">the header value</param>
        public void AddHeader(string name, string value)
        {
            if (name.ToLower() == "content-type")
            {
                InternalContext.Response.ContentType = value;
            }
            else
            {
                InternalContext.Response.Headers.Add(name, value);
            }
        }

        /// <summary>
        /// Writes a chunk of data in a chunked response.
        /// </summary>
        public async Task WriteChunkedDataAsync(byte[] data)
        {
            if (!_chunked)
            {
                _chunked = true;
                InternalContext.Response.SendChunked = true;
            }
            await InternalContext.Response.OutputStream.WriteAsync(data, 0, data.Length);
        }

        /// <summary>
        /// Writes a complete response body.
        /// </summary>
        /// <param name="contentType">the Content-Type header value</param>
        /// <param name="data">the data</param>
        /// <returns>the asynchronous task </returns>
        public async Task WriteFullResponseAsync(string contentType, byte[] data)
        {
            InternalContext.Response.ContentLength64 = data.Length;
            InternalContext.Response.ContentType = contentType;
            await InternalContext.Response.OutputStream.WriteAsync(data, 0, data.Length,
                InternalContext.CancellationToken);
        }
    }

    /// <summary>
    /// Properties of a request received by a <see cref="TestHttpServer"/>.
    /// </summary>
    public struct RequestInfo
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Query { get; set; }
        public NameValueCollection Headers { get; set; }
        public string Body { get; set; }
    }

    /// <summary>
    /// An asynchronous function that handles HTTP requests to a <see cref="TestHttpServer"/>.
    /// </summary>
    /// <remarks>
    /// Use the factory methods in <see cref="Handlers"/> to create standard implementations.
    /// </remarks>
    /// <param name="context">the request context</param>
    /// <returns>the asynchronous task</returns>
    public delegate Task Handler(RequestContext context);

    /// <summary>
    /// A simplified system for setting up embedded test HTTP servers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstraction is designed to allow writing test code that does not need to know anything
    /// about the underlying implementation details of the HTTP framework, so that if a different
    /// library needs to be used for compatibility reasons, it can be substituted without disrupting
    /// the tests.
    /// </para>
    /// <example>
    /// <code>
    ///     // Start a server that returns a 200 status for all requests
    ///     using (var server = StartServer(Handlers.Status(200)))
    ///     {
    ///         DoSomethingThatMakesARequestTo(server.Uri);
    ///
    ///         var req = server.RequireRequest();
    ///         // Check for expected properties of the request
    ///     }
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class TestHttpServer : IDisposable
    {
        private static int nextPort = 10000;

        /// <summary>
        /// The base URI of the server.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Returns the <see cref="RequestRecorder"/> that captures all requests to this server.
        /// </summary>
        public RequestRecorder Recorder => _baseHandler;

        private readonly WebServer _server;
        private readonly RequestRecorder _baseHandler;

        private TestHttpServer(WebServer server, RequestRecorder baseHandler, Uri uri)
        {
            _server = server;
            _baseHandler = baseHandler;
            Uri = uri;
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public void Dispose() => _server.Dispose();

        /// <summary>
        /// Starts a new test server.
        /// </summary>
        /// <remarks>
        /// Make sure to close this when done, by calling <c>Dispose</c> or with a <c>using</c>
        /// statement.
        /// </remarks>
        /// <param name="handler">A function that will handle all requests to this server. Use
        /// the factory methods in <see cref="Handlers"/> for standard handlers. If you will need
        /// to change the behavior of the handler during the lifetime of the server, use
        /// <see cref="Handlers.Changeable(Handler)"/>.</param>
        /// <returns></returns>
        public static TestHttpServer Start(Handler handler)
        {
            var recorder = Handlers.RecordAndDelegateTo(handler);
            var server = StartWebServerOnAvailablePort(out var uri, recorder);
            return new TestHttpServer(server, recorder, uri);
        }

        private static WebServer StartWebServerOnAvailablePort(out Uri serverUri, Handler handler)
        {
            while (true)
            {
                var port = nextPort++;
                var options = new WebServerOptions()
                    .WithUrlPrefix($"http://*:{port}")
                    .WithMode(HttpListenerMode.Microsoft);
                var server = new WebServer(options);
                server.Listener.IgnoreWriteExceptions = true;
                server.OnAny("/", async internalContext =>
                {
                    var ctx = await RequestContext.FromHttpContext(internalContext);
                    await handler(ctx);
                });
                try
                {
                    _ = server.RunAsync();
                }
                catch (HttpListenerException)
                {
                    continue;
                }
                serverUri = new Uri(string.Format("http://localhost:{0}", port));
                return server;
            }
        }
    }

    /// <summary>
    /// Factory methods for standard <see cref="Handler"/> implementations.
    /// </summary>
    public static class Handlers
    {
        /// <summary>
        /// Creates a <see cref="Handler"/> that sleeps for the specified amount of time
        /// before passing the request to the target handler.
        /// </summary>
        /// <param name="delay">how long to delay</param>
        /// <param name="target">the handler to call after the delay</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler DelayBefore(TimeSpan delay, Handler target) =>
            async ctx =>
            {
                await Task.Delay(delay, ctx.CancellationToken);
                if (!ctx.CancellationToken.IsCancellationRequested)
                {
                    await target(ctx);
                }
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that sends a 200 response with a JSON content type.
        /// </summary>
        /// <param name="jsonBody">the JSON data</param>
        /// <param name="headers">additional headers (may be null)</param>
        /// <returns></returns>
        public static Handler JsonResponse(
            string jsonBody,
            NameValueCollection headers = null
            ) =>
            StringResponse(200, headers, "application/json", jsonBody);

        /// <summary>
        /// Creates a <see cref="RequestRecorder"/> that captures requests while delegating to
        /// another handler
        /// </summary>
        /// <param name="target">the handler to delegate to</param>
        /// <returns>a <see cref="RequestRecorder"/></returns>
        public static RequestRecorder RecordAndDelegateTo(Handler target) =>
            new RequestRecorder(target);

        /// <summary>
        /// Creates a <see cref="Handler"/> that always sends the same response,
        /// specifying the response body (if any) as a byte array.
        /// </summary>
        /// <param name="statusCode">the HTTP status code</param>
        /// <param name="headers">response headers (may be null)</param>
        /// <param name="contentType">response content type (used only if body is not null)</param>
        /// <param name="body">response body (may be null)</param>
        /// <returns></returns>
        public static Handler Response(
            int statusCode,
            NameValueCollection headers,
            string contentType = null,
            byte[] body = null
            ) =>
            async ctx =>
            {
                ctx.SetStatus(statusCode);
                if (headers != null)
                {
                    foreach (var k in headers.AllKeys)
                    {
                        foreach (var v in headers.GetValues(k))
                        {
                            ctx.AddHeader(k, v);
                        }
                    }
                }
                if (body != null)
                {
                    await ctx.WriteFullResponseAsync(contentType, body);
                }
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that always sends the same response,
        /// specifying the response body (if any) as a string.
        /// </summary>
        /// <param name="statusCode">the HTTP status code</param>
        /// <param name="headers">response headers (may be null)</param>
        /// <param name="contentType">response content type (used only if body is not null)</param>
        /// <param name="body">response body (may be null)</param>
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
        /// <returns></returns>
        public static Handler StringResponse(
            int statusCode,
            NameValueCollection headers,
            string contentType = null,
            string body = null,
            Encoding encoding = null
            ) =>
            Response(
                statusCode,
                headers,
                contentType is null || contentType.Contains("charset=") ? contentType :
                    contentType + "; charset=" + (encoding ?? Encoding.UTF8).WebName,
                body == null ? null : (encoding ?? Encoding.UTF8).GetBytes(body)
                );

        /// <summary>
        /// Creates a <see cref="Handler"/> that always returns the specified HTTP
        /// response status, with no custom headers and no body.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Status(int statusCode) =>
            Sync(ctx => ctx.SetStatus(statusCode));

        /// <summary>
        /// Creates a <see cref="HandlerDelegator"/> for changing handler behavior
        /// dynamically.
        /// </summary>
        /// <param name="target">the initial <see cref="Handler"/> to delegate to</param>
        /// <returns>a <see cref="HandlerDelegator"/></returns>
        public static HandlerDelegator DelegateTo(Handler target) =>
            new HandlerDelegator(target);

        /// <summary>
        /// Wraps a synchronous action in an asynchronous <see cref="Handler"/>.
        /// </summary>
        /// <param name="action">the action to run</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Sync(Action<RequestContext> action) =>
            ctx =>
            {
                action(ctx);
                return Task.CompletedTask;
            };
    }

    /// <summary>
    /// A delegator that forwards requests to another handler, which can be changed at any time.
    /// </summary>
    /// <remarks>
    /// There is an implicit conversion allowing a <c>HandlerDelegator</c> to be used as a
    /// <see cref="Handler"/>.
    /// </remarks>
    public sealed class HandlerDelegator
    {
        private volatile Handler _target;

        /// <summary>
        /// The handler that will actually handle the request.
        /// </summary>
        public Handler Target
        {
            get => _target;
            set
            {
                _target = value;
            }
        }

        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>HandlerDelegator</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => ctx => _target(ctx);

        internal HandlerDelegator(Handler target)
        {
            _target = target;
        }

        public static implicit operator Handler(HandlerDelegator me) => me.Handler;
    }

    /// <summary>
    /// An object that delegates requests to another handler while recording all requests.
    /// </summary>
    /// <remarks>
    /// Normally you won't need to use this class directly, because <see cref="TestHttpServer"/>
    /// has a built-in instance that captures all requests. You can use it if you need to
    /// capture only a subset of requests.
    /// </remarks>
    public class RequestRecorder
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly BlockingCollection<RequestInfo> _requests = new BlockingCollection<RequestInfo>();
        private readonly Handler _target;

        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>RequestRecorder</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => DoRequestAsync;

        /// <summary>
        /// The number of requests currently in the queue.
        /// </summary>
        public int Count => _requests.Count;

        internal RequestRecorder(Handler target)
        {
            _target = target;
        }

        /// <summary>
        /// Consumes and returns the first request in the queue, blocking until one is available.
        /// Throws an exception if the timeout expires.
        /// </summary>
        /// <param name="timeout">the maximum length of time to wait</param>
        /// <returns>the request information</returns>
        public RequestInfo RequireRequest(TimeSpan timeout)
        {
            if (!_requests.TryTake(out var req, timeout))
            {
                throw new TimeoutException("timed out waiting for request");
            }
            return req;
        }

        /// <summary>
        /// Returns the first request in the queue, blocking until one is available,
        /// using <see cref="DefaultTimeout"/>.
        /// </summary>
        /// <returns>the request information</returns>
        public RequestInfo RequireRequest() => RequireRequest(DefaultTimeout);

        public void RequireNoRequests(TimeSpan timeout)
        {
            if (_requests.TryTake(out var _, timeout))
            {
                throw new Exception("received an unexpected request");
            }
        }

        private async Task DoRequestAsync(RequestContext ctx)
        {
            _requests.Add(ctx.RequestInfo);
            await _target(ctx);
        }

        public static implicit operator Handler(RequestRecorder me) => me.Handler;
    }
}