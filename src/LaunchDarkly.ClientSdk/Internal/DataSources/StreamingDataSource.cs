using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Http;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal sealed class StreamingDataSource : IDataSource
    {
        // The read timeout for the stream is not the same read timeout that can be set in the SDK configuration.
        // It is a fixed value that is set to be slightly longer than the expected interval between heartbeats
        // from the LaunchDarkly streaming server. If this amount of time elapses with no new data, the connection
        // will be cycled.
        private static readonly TimeSpan LaunchDarklyStreamReadTimeout = TimeSpan.FromMinutes(5);

        private static readonly HttpMethod ReportMethod = new HttpMethod("REPORT");

        private readonly IDataSourceUpdateSink _updateSink;
        private readonly Uri _baseUri;
        private readonly User _user;
        private readonly bool _useReport;
        private readonly bool _withReasons;
        private readonly TimeSpan _initialReconnectDelay;
        private readonly IFeatureFlagRequestor _requestor;
        private readonly HttpProperties _httpProperties;
        private readonly EventSourceCreator _eventSourceCreator;
        private readonly TaskCompletionSource<bool> _initTask;
        private readonly AtomicBoolean _initialized = new AtomicBoolean(false);
        private readonly Logger _log;

        private volatile IEventSource _eventSource;

        internal delegate IEventSource EventSourceCreator(
            HttpProperties httpProperties,
            HttpMethod method,
            Uri uri,
            string jsonBody
            );

        internal StreamingDataSource(
            IDataSourceUpdateSink updateSink,
            User user,
            Uri baseUri,
            bool withReasons,
            TimeSpan initialReconnectDelay,
            IFeatureFlagRequestor requestor,
            HttpConfiguration httpConfig,
            Logger log,
            EventSourceCreator eventSourceCreator // used only in tests
            )
        {
            this._updateSink = updateSink;
            this._user = user;
            this._baseUri = baseUri;
            this._useReport = httpConfig.UseReport;
            this._withReasons = withReasons;
            this._initialReconnectDelay = initialReconnectDelay;
            this._requestor = requestor;
            this._httpProperties = httpConfig.HttpProperties;
            this._initTask = new TaskCompletionSource<bool>();
            this._log = log;
            this._eventSourceCreator = eventSourceCreator ?? CreateEventSource;
        }

        public bool Initialized => _initialized.Get();

        public Task<bool> Start()
        {
            if (_useReport)
            {
                _eventSource = _eventSourceCreator(
                    _httpProperties,
                    ReportMethod,
                    MakeRequestUriWithPath(Constants.STREAM_REQUEST_PATH),
                    JsonUtil.EncodeJson(_user)
                    );
            }
            else
            {
                _eventSource = _eventSourceCreator(
                    _httpProperties,
                    HttpMethod.Get,
                    MakeRequestUriWithPath(Constants.STREAM_REQUEST_PATH +
                        Base64.UrlSafeEncode(JsonUtil.EncodeJson(_user))),
                    null
                    );
            }

            _eventSource.MessageReceived += OnMessage;
            _eventSource.Error += OnError;
            _eventSource.Opened += OnOpen;

            _ = Task.Run(() => _eventSource.StartAsync());
            return _initTask.Task;
        }

        private IEventSource CreateEventSource(
            HttpProperties httpProperties,
            HttpMethod method,
            Uri uri,
            string jsonBody
            )
        {
            var configBuilder = EventSource.Configuration.Builder(uri)
                .Method(method)
                .HttpMessageHandler(httpProperties.HttpMessageHandlerFactory(httpProperties))
                .ResponseStartTimeout(httpProperties.ConnectTimeout)
                .InitialRetryDelay(_initialReconnectDelay)
                .ReadTimeout(LaunchDarklyStreamReadTimeout)
                .RequestHeaders(httpProperties.BaseHeaders.ToDictionary(kv => kv.Key, kv => kv.Value))
                .Logger(_log);
            return new EventSource.EventSource(configBuilder.Build());
        }

        private Uri MakeRequestUriWithPath(string path)
        {
            var uri = _baseUri.AddPath(path);
            return _withReasons ? uri.AddQuery("withReasons=true") : uri;
        }

        private void OnOpen(object sender, EventSource.StateChangedEventArgs e)
        {
            _log.Debug("EventSource Opened");
        }

        private void OnMessage(object sender, EventSource.MessageReceivedEventArgs e)
        {
            try
            {
                HandleMessage(e.EventName, e.Message.Data);
            }
            catch (JsonReadException ex)
            {
                _log.Error("LaunchDarkly service request failed or received invalid data: {0}",
                    LogValues.ExceptionSummary(ex));
            }
            catch (Exception ex)
            {
                LogHelpers.LogException(_log, "Unexpected error in stream processing", ex);
            }
        }

        private void OnError(object sender, EventSource.ExceptionEventArgs e)
        {
            var ex = e.Exception;
            LogHelpers.LogException(_log, "Encountered EventSource error", ex);
            if (ex is EventSource.EventSourceServiceUnsuccessfulResponseException respEx)
            {
                int status = respEx.StatusCode;
                _log.Error(HttpErrors.ErrorMessage(status, "streaming connection", "will retry"));
                if (!HttpErrors.IsRecoverable(status))
                {
                    _initTask.TrySetException(ex); // sends this exception to the client if we haven't already started up
                    Dispose(true);
                }
            }
        }

        void HandleMessage(string messageType, string messageData)
        {
            switch (messageType)
            {
                case Constants.PUT:
                    {
                        _updateSink.Init(new FullDataSet(JsonUtil.DeserializeFlags(messageData)), _user);
                        if (!_initialized.GetAndSet(true))
                        {
                            _initTask.SetResult(true);
                        }
                        break;
                    }
                case Constants.PATCH:
                    {
                        try
                        {
                            var parsed = LdValue.Parse(messageData);
                            var flagkey = parsed.Get(Constants.KEY).AsString;
                            var featureFlag = JsonUtil.DecodeJson<FeatureFlag>(messageData);
                            _updateSink.Upsert(flagkey, featureFlag.version, featureFlag, _user);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Error parsing PATCH message {0}: {1}", messageData, LogValues.ExceptionSummary(ex));
                        }
                        break;
                    }
                case Constants.DELETE:
                    {
                        try
                        {
                            var parsed = LdValue.Parse(messageData);
                            int version = parsed.Get(Constants.VERSION).AsInt;
                            string flagKey = parsed.Get(Constants.KEY).AsString;
                            _updateSink.Delete(flagKey, version, _user);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Error parsing DELETE message {0}: {1}", messageData, LogValues.ExceptionSummary(ex));
                        }
                        break;
                    }
                case Constants.PING:
                    {
                        try
                        {
                            Task.Run(async () =>
                            {
                                var response = await _requestor.FeatureFlagsAsync();
                                var flagsAsJsonString = response.jsonResponse;
                                var flagsDictionary = JsonUtil.DeserializeFlags(flagsAsJsonString);
                                _updateSink.Init(new FullDataSet(flagsDictionary), _user);
                                if (!_initialized.GetAndSet(true))
                                {
                                    _initTask.SetResult(true);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Error in handling PING message: {0}", LogValues.ExceptionSummary(ex));
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _eventSource?.Close();
                _requestor?.Dispose();
            }
        }
    }
}
