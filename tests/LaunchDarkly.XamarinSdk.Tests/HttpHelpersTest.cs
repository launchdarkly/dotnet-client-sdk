using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaunchDarkly.Sdk.Xamarin.HttpHelpers
{
    public class HttpHelpersTest
    {
        [Fact]
        public async Task ServerWithSimpleStatusHandler()
        {
            await WithServerAndClient(Handlers.Status(419), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
            });
        }

        [Fact]
        public async Task ServerCapturesRequestWithoutBody()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, server.Uri.ToString() + "request/path?a=b");
                req.Headers.Add("header-name", "header-value");

                var resp = await client.SendAsync(req);
                Assert.Equal(200, (int)resp.StatusCode);

                var received = server.Recorder.RequireRequest();
                Assert.Equal("GET", received.Method);
                Assert.Equal("/request/path", received.Path);
                Assert.Equal("?a=b", received.Query);
                Assert.Equal("header-value", received.Headers["header-name"]);
                Assert.Null(received.Body);
            });
        }

        [Fact]
        public async Task ServerCapturesRequestWithBody()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, server.Uri.ToString() + "request/path");
                req.Content = new StringContent("hello", Encoding.UTF8, "text/plain");

                var resp = await client.SendAsync(req);
                Assert.Equal(200, (int)resp.StatusCode);

                var received = server.Recorder.RequireRequest();
                Assert.Equal("POST", received.Method);
                Assert.Equal("/request/path", received.Path);
                Assert.Equal("hello", received.Body);
            });
        }

        [Fact]
        public async Task CustomAsyncHandler()
        {
            Handler handler = async ctx =>
            {
                await ctx.WriteFullResponseAsync("text/plain", Encoding.UTF8.GetBytes("hello"));
            };
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal("hello", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task CustomSyncHandler()
        {
            Action<RequestContext> action = ctx =>
            {
                ctx.SetStatus(419);
            };
            await WithServerAndClient(Handlers.Sync(action), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
            });
        }

        [Fact]
        public async Task ResponseHandlerWithoutBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            await WithServerAndClient(Handlers.Response(419, headers), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal("", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task ResponseHandlerWithBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            byte[] data = new byte[] { 1, 2, 3 };
            await WithServerAndClient(Handlers.Response(200, headers, "weird", data), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("weird", resp.Content.Headers.GetValues("content-type").First());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal(data, await resp.Content.ReadAsByteArrayAsync());
            });
        }

        [Fact]
        public async Task StringResponseHandlerWithBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            string body = "hello";
            await WithServerAndClient(Handlers.StringResponse(200, headers, "weird", body), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("weird; charset=utf-8", resp.Content.Headers.GetValues("content-type").First());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal(body, await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task JsonResponseHandler()
        {
            await WithServerAndClient(Handlers.JsonResponse("true"), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("application/json; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                Assert.Equal("true", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task JsonResponseHandlerWithHeaders()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            await WithServerAndClient(Handlers.JsonResponse("true", headers), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("application/json; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal("true", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task SwitchableDelegatorHandler()
        {
            var switchable = Handlers.DelegateTo(Handlers.Status(200));
            await WithServerAndClient(switchable, async (server, client) =>
            {
                var resp1 = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp1.StatusCode);

                switchable.Target = Handlers.Status(400);

                var resp2 = await client.GetAsync(server.Uri);
                Assert.Equal(400, (int)resp2.StatusCode);
            });
        }

        [Fact]
        public async Task ChunkedResponse()
        {
            Handler handler = async ctx =>
            {
                ctx.SetHeader("Content-Type", "text/plain; charset=utf-8");
                await ctx.WriteChunkedDataAsync(Encoding.UTF8.GetBytes("chunk1,"));
                await ctx.WriteChunkedDataAsync(Encoding.UTF8.GetBytes("chunk2"));
                await Task.Delay(Timeout.Infinite, ctx.CancellationToken);
            };
            var expected = "chunk1,chunk2";
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("text/plain; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                var stream = await resp.Content.ReadAsStreamAsync();
                var received = new StringBuilder();
                while (true)
                {
                    var buf = new byte[100];
                    int n = await stream.ReadAsync(buf, 0, buf.Length);
                    received.Append(Encoding.UTF8.GetString(buf, 0, n));
                    if (received.Length >= expected.Length)
                    {
                        Assert.Equal(expected, received.ToString());
                        break;
                    }
                }
            });
        }

        private static async Task WithServerAndClient(Handler handler, Func<TestHttpServer, HttpClient, Task> action)
        {
            using (var server = TestHttpServer.Start(handler))
            {
                using (var client = new HttpClient())
                {
                    await action(server, client);
                }
            }
        }
    }
}
