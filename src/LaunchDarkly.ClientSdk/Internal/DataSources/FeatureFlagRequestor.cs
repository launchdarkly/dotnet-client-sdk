using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Http;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal struct WebResponse
    {
        public int statusCode { get; private set; }
        public string jsonResponse { get; private set; }
        public string errorMessage { get; private set; }

        public WebResponse(int code, string response, string error)
        {
            statusCode = code;
            jsonResponse = response;
            errorMessage = error;
        }
    }

    internal interface IFeatureFlagRequestor : IDisposable
    {
        Task<WebResponse> FeatureFlagsAsync();
    }

    internal sealed class FeatureFlagRequestor : IFeatureFlagRequestor
    {
        private static readonly HttpMethod ReportMethod = new HttpMethod("REPORT");

        private readonly Uri _baseUri;
        private readonly User _currentUser;
        private readonly bool _useReport;
        private readonly bool _withReasons;
        private readonly HttpClient _httpClient;
        private readonly HttpProperties _httpProperties;
        private readonly Logger _log;
        private volatile EntityTagHeaderValue _etag;

        internal FeatureFlagRequestor(
            Uri baseUri,
            User user,
            bool useReport,
            bool withReasons,
            HttpProperties httpProperties,
            Logger log
            )
        {
            this._baseUri = baseUri;
            this._httpProperties = httpProperties;
            this._httpClient = httpProperties.NewHttpClient();
            this._currentUser = user;
            this._useReport = useReport;
            this._withReasons = withReasons;
            this._log = log;
        }

        public async Task<WebResponse> FeatureFlagsAsync()
        {
            var requestMessage = _useReport ? ReportRequestMessage() : GetRequestMessage();
            return await MakeRequest(requestMessage);
        }

        private HttpRequestMessage GetRequestMessage()
        {
            var path = Constants.FLAG_REQUEST_PATH_GET + Base64.UrlSafeEncode(JsonUtil.EncodeJson(_currentUser));
            return new HttpRequestMessage(HttpMethod.Get, MakeRequestUriWithPath(path));
        }

        private HttpRequestMessage ReportRequestMessage()
        {
            var request = new HttpRequestMessage(ReportMethod, MakeRequestUriWithPath(Constants.FLAG_REQUEST_PATH_REPORT));
            request.Content = new StringContent(JsonUtil.EncodeJson(_currentUser), Encoding.UTF8, Constants.APPLICATION_JSON);
            return request;
        }

        private Uri MakeRequestUriWithPath(string path)
        {
            var uri = _baseUri.AddPath(path);
            return _withReasons ? uri.AddQuery("withReasons=true") : uri;
        }

        private async Task<WebResponse> MakeRequest(HttpRequestMessage request)
        {
            _httpProperties.AddHeaders(request);
            using (var cts = new CancellationTokenSource(_httpProperties.ConnectTimeout))
            {
                if (_etag != null)
                {
                    request.Headers.IfNoneMatch.Add(_etag);
                }

                try
                {
                    _log.Debug("Getting flags with uri: {0}", request.RequestUri.AbsoluteUri);
                    using (var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            _log.Debug("Get all flags returned 304: not modified");
                            return new WebResponse(304, null, "Get all flags returned 304: not modified");
                        }
                        _etag = response.Headers.ETag;
                        //We ensure the status code after checking for 304, because 304 isn't considered success
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new UnsuccessfulResponseException((int)response.StatusCode);
                        }

                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return new WebResponse(200, content, null);
                    }
                }
                catch (TaskCanceledException tce)
                {
                    if (tce.CancellationToken == cts.Token)
                    {
                        //Indicates the task was cancelled by something other than a request timeout
                        throw;
                    }
                    //Otherwise this was a request timeout.
                    throw new TimeoutException("Get item with URL: " + request.RequestUri +
                                                " timed out after : " + _httpProperties.ConnectTimeout);
                }
            }
        }

        // Sealed, non-derived class should implement Dispose() and finalize method, not Dispose(boolean)
        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        ~FeatureFlagRequestor()
        {
            Dispose();
        }
    }
}
