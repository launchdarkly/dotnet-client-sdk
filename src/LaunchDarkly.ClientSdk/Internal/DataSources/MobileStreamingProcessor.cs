using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using LaunchDarkly.EventSource;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Http;
using LaunchDarkly.Sdk.Xamarin.Internal;
using LaunchDarkly.Sdk.Xamarin.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Xamarin.Internal.DataSources
{
    internal sealed class MobileStreamingProcessor : IMobileUpdateProcessor
    {
        // The read timeout for the stream is not the same read timeout that can be set in the SDK configuration.
        // It is a fixed value that is set to be slightly longer than the expected interval between heartbeats
        // from the LaunchDarkly streaming server. If this amount of time elapses with no new data, the connection
        // will be cycled.
        private static readonly TimeSpan LaunchDarklyStreamReadTimeout = TimeSpan.FromMinutes(5);

        private static readonly HttpMethod ReportMethod = new HttpMethod("REPORT");

        private readonly Configuration _configuration;
        private readonly IFlagCacheManager _cacheManager;
        private readonly User _user;
        private readonly EventSourceCreator _eventSourceCreator;
        private readonly IFeatureFlagRequestor _requestor;
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

        internal MobileStreamingProcessor(Configuration configuration,
                                          IFlagCacheManager cacheManager,
                                          IFeatureFlagRequestor requestor,
                                          User user,
                                          EventSourceCreator eventSourceCreator,
                                          Logger log)
        {
            this._configuration = configuration;
            this._cacheManager = cacheManager;
            this._requestor = requestor;
            this._user = user;
            this._eventSourceCreator = eventSourceCreator ?? CreateEventSource;
            this._initTask = new TaskCompletionSource<bool>();
            this._log = log;
        }

        #region IMobileUpdateProcessor

        bool IMobileUpdateProcessor.Initialized() => _initialized.Get();

        Task<bool> IMobileUpdateProcessor.Start()
        {
            if (_configuration.UseReport)
            {
                _eventSource = _eventSourceCreator(
                    _configuration.HttpProperties,
                    ReportMethod,
                    MakeRequestUriWithPath(Constants.STREAM_REQUEST_PATH),
                    JsonUtil.EncodeJson(_user)
                    );
            }
            else
            {
                _eventSource = _eventSourceCreator(
                    _configuration.HttpProperties,
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

        #endregion

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
                .ConnectionTimeout(httpProperties.ConnectTimeout)
                .InitialRetryDelay(_configuration.ReconnectTime)
                .ReadTimeout(LaunchDarklyStreamReadTimeout)
                .RequestHeaders(httpProperties.BaseHeaders.ToDictionary(kv => kv.Key, kv => kv.Value))
                .Logger(_log);
            return new EventSource.EventSource(configBuilder.Build());
        }

        private Uri MakeRequestUriWithPath(string path)
        {
            var uri = _configuration.StreamUri.AddPath(path);
            return _configuration.EvaluationReasons ? uri.AddQuery("withReasons=true") : uri;
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
                    ((IDisposable)this).Dispose();
                }
            }
        }

        #region IStreamProcessor

        void HandleMessage(string messageType, string messageData)
        {
            switch (messageType)
            {
                case Constants.PUT:
                    {
                        _cacheManager.CacheFlagsFromService(JsonUtil.DeserializeFlags(messageData), _user);
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
                            PatchFeatureFlag(flagkey, featureFlag);
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
                            DeleteFeatureFlag(flagKey, version);
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
                                _cacheManager.CacheFlagsFromService(flagsDictionary, _user);
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

        void PatchFeatureFlag(string flagKey, FeatureFlag featureFlag)
        {
            if (FeatureFlagShouldBeDeletedOrPatched(flagKey, featureFlag.version))
            {
                _cacheManager.UpdateFlagForUser(flagKey, featureFlag, _user);
            }
        }

        void DeleteFeatureFlag(string flagKey, int version)
        {
            if (FeatureFlagShouldBeDeletedOrPatched(flagKey, version))
            {
                _cacheManager.RemoveFlagForUser(flagKey, _user);
            }
        }

        bool FeatureFlagShouldBeDeletedOrPatched(string flagKey, int version)
        {
            var oldFlag = _cacheManager.FlagForUser(flagKey, _user);
            if (oldFlag != null)
            {
                return oldFlag.version < version;
            }

            return true;
        }

        #endregion

        void IDisposable.Dispose()
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
