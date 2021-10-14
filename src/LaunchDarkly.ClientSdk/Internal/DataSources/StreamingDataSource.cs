﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Concurrent;
using LaunchDarkly.Sdk.Internal.Http;

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
                    MakeRequestUriWithPath(StandardEndpoints.StreamingReportRequestPath),
                    DataModelSerialization.SerializeUser(_user)
                    );
            }
            else
            {
                _eventSource = _eventSourceCreator(
                    _httpProperties,
                    HttpMethod.Get,
                    MakeRequestUriWithPath(StandardEndpoints.StreamingGetRequestPath(
                        Base64.UrlSafeEncode(DataModelSerialization.SerializeUser(_user)))),
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

                var errorInfo = new DataSourceStatus.ErrorInfo
                {
                    Kind = DataSourceStatus.ErrorKind.InvalidData,
                    Message = ex.Message,
                    Time = DateTime.Now
                };
                _updateSink.UpdateStatus(DataSourceState.Interrupted, errorInfo);

                _eventSource.Restart(false);
            }
            catch (Exception ex)
            {
                LogHelpers.LogException(_log, "Unexpected error in stream processing", ex);
            }
        }

        private void OnError(object sender, EventSource.ExceptionEventArgs e)
        {
            var ex = e.Exception;
            var recoverable = true;
            DataSourceStatus.ErrorInfo errorInfo;

            if (ex is EventSourceServiceUnsuccessfulResponseException respEx)
            {
                int status = respEx.StatusCode;
                errorInfo = DataSourceStatus.ErrorInfo.FromHttpError(status);
                if (!HttpErrors.IsRecoverable(status))
                {
                    recoverable = false;
                    _log.Error(HttpErrors.ErrorMessage(status, "streaming connection", ""));
                }
                else
                {
                    _log.Warn(HttpErrors.ErrorMessage(status, "streaming connection", "will retry"));
                }
            }
            else
            {
                errorInfo = DataSourceStatus.ErrorInfo.FromException(ex);
                _log.Warn("Encountered EventSource error: {0}", LogValues.ExceptionSummary(ex));
                _log.Debug(LogValues.ExceptionTrace(ex));
            }

            _updateSink.UpdateStatus(recoverable ? DataSourceState.Interrupted : DataSourceState.Shutdown,
                errorInfo);

            if (!recoverable)
            {
                // Make _initTask complete to tell the client to stop waiting for initialization. We use
                // TrySetResult rather than SetResult here because it might have already been completed
                // (if for instance the stream started successfully, then restarted and got a 401).
                _initTask.TrySetResult(false);
                ((IDisposable)this).Dispose();
            }
        }

        void HandleMessage(string messageType, string messageData)
        {
            _log.Debug("Event '{0}': {1}", messageType, messageData);
            switch (messageType)
            {
                case Constants.PUT:
                    {
                        var allData = DataModelSerialization.DeserializeV1Schema(messageData);
                        _updateSink.Init(_user, allData);
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
                            var featureFlag = DataModelSerialization.DeserializeFlag(messageData);
                            _updateSink.Upsert(_user, flagkey, featureFlag.ToItemDescriptor());
                        }
                        catch (Exception ex)
                        {
                            LogHelpers.LogException(_log, "Error parsing PATCH message", ex);
                            _log.Debug("Message data follows: {0}", messageData);
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
                            var deletedItem = new ItemDescriptor(version, null);
                            _updateSink.Upsert(_user, flagKey, deletedItem);
                        }
                        catch (Exception ex)
                        {
                            LogHelpers.LogException(_log, "Error parsing DELETE message", ex);
                            _log.Debug("Message data follows: {0}", messageData);
                        }
                        break;
                    }
                case Constants.PING:
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var response = await _requestor.FeatureFlagsAsync();
                                var flagsAsJsonString = response.jsonResponse;
                                var allData = DataModelSerialization.DeserializeV1Schema(flagsAsJsonString);
                                _updateSink.Init(_user, allData);
                                if (!_initialized.GetAndSet(true))
                                {
                                    _initTask.SetResult(true);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelpers.LogException(_log, "Error in handling PING message", ex);
                            }
                        });
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
