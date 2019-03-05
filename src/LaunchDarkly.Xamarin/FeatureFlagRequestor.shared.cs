using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin
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

    internal class FeatureFlagRequestor : IFeatureFlagRequestor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FeatureFlagRequestor));
        private readonly IMobileConfiguration _configuration;
        private readonly User _currentUser;
        private volatile HttpClient _httpClient;
        private volatile EntityTagHeaderValue _etag;

        internal FeatureFlagRequestor(IMobileConfiguration configuration, User user)
        {
            this._configuration = configuration;
            this._httpClient = Util.MakeHttpClient(configuration, MobileClientEnvironment.Instance);
            this._currentUser = user;
        }

        public async Task<WebResponse> FeatureFlagsAsync()
        {
            HttpRequestMessage requestMessage;
            if (_configuration.UseReport)
            {
                requestMessage = ReportRequestMessage();
            }
            else
            {
                requestMessage = GetRequestMessage();
            }

            return await MakeRequest(requestMessage);
        }

        private HttpRequestMessage GetRequestMessage()
        {
            var encodedUser = _currentUser.AsJson().Base64Encode();
            var requestUrlPath = _configuration.BaseUri + Constants.FLAG_REQUEST_PATH_GET + encodedUser;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrlPath);
            return request;
        }

        private HttpRequestMessage ReportRequestMessage()
        {
            var requestUrlPath = _configuration.BaseUri + Constants.FLAG_REQUEST_PATH_REPORT;
            var request = new HttpRequestMessage(new HttpMethod("REPORT"), requestUrlPath);
            request.Content = new StringContent(_currentUser.AsJson(), Encoding.UTF8, Constants.APPLICATION_JSON);

            return request;
        }

        private async Task<WebResponse> MakeRequest(HttpRequestMessage request)
        {
            var cts = new CancellationTokenSource(_configuration.HttpClientTimeout);

            if (_etag != null)
            {
                request.Headers.IfNoneMatch.Add(_etag);
            }

            try
            {
                Log.DebugFormat("Getting flags with uri: {0}", request.RequestUri.AbsoluteUri);
                using (var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        Log.Debug("Get all flags returned 304: not modified");
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
                                            " timed out after : " + _configuration.HttpClientTimeout);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
