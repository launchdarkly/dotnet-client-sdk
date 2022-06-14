using System;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Client;

namespace TestService
{
    public class SdkClientEntity
    {
        private static HttpClient _httpClient = new HttpClient();

        private readonly LdClient _client;
        private readonly Logger _log;
        private readonly bool _evaluationReasons;

        public SdkClientEntity(
            SdkConfigParams sdkParams,
            ILogAdapter logAdapter,
            string tag
        )
        {
            if (sdkParams.ClientSide == null)
            {
                throw new Exception("test harness did not provide clientSide configuration");
            }

            _log = logAdapter.Logger(tag);
            Configuration config = BuildSdkConfig(sdkParams, logAdapter, tag);

            _evaluationReasons = sdkParams.ClientSide.EvaluationReasons ?? false;

            TimeSpan startWaitTime = TimeSpan.FromSeconds(5);
            if (sdkParams.StartWaitTimeMs.HasValue)
            {
                startWaitTime = TimeSpan.FromMilliseconds(sdkParams.StartWaitTimeMs.Value);
            }

            _client = LdClient.Init(config, sdkParams.ClientSide.InitialContext, startWaitTime);
            if (!_client.Initialized && !sdkParams.InitCanFail)
            {
                _client.Dispose();
                throw new Exception("Client initialization failed");
            }
        }
        
        public void Close()
        {
            _client.Dispose();
            _log.Info("Test ended");
        }

        public async Task<(bool, object)> DoCommand(CommandParams command)
        {
            _log.Info("Test harness sent command: {0}", command.Command);
            switch (command.Command)
            {
                case "evaluate":
                    return (true, DoEvaluate(command.Evaluate));

                case "evaluateAll":
                    return (true, DoEvaluateAll(command.EvaluateAll));

                case "identifyEvent":
                    await _client.IdentifyAsync(command.IdentifyEvent.Context);
                    return (true, null);

                case "customEvent":
                    var custom = command.CustomEvent;
                    if (custom.MetricValue.HasValue)
                    {
                        _client.Track(custom.EventKey, custom.Data, custom.MetricValue.Value);
                    }
                    else if (custom.OmitNullData && custom.Data.IsNull)
                    {
                        _client.Track(custom.EventKey);
                    }
                    else
                    {
                        _client.Track(custom.EventKey, custom.Data);
                    }
                    return (true, null);

                case "flushEvents":
                    _client.Flush();
                    return (true, null);

                default:
                    return (false, null);
            }
        }

        private object DoEvaluate(EvaluateFlagParams p)
        {
            var resp = new EvaluateFlagResponse();
            switch (p.ValueType)
            {
                case "bool":
                    if (p.Detail)
                    {
                        var detail = _client.BoolVariationDetail(p.FlagKey, p.DefaultValue.AsBool);
                        resp.Value = LdValue.Of(detail.Value);
                        resp.VariationIndex = detail.VariationIndex;
                        resp.Reason = detail.Reason;
                    }
                    else
                    {
                        resp.Value = LdValue.Of(_client.BoolVariation(p.FlagKey, p.DefaultValue.AsBool));
                    }
                    break;

                case "int":
                    if (p.Detail)
                    {
                        var detail = _client.IntVariationDetail(p.FlagKey, p.DefaultValue.AsInt);
                        resp.Value = LdValue.Of(detail.Value);
                        resp.VariationIndex = detail.VariationIndex;
                        resp.Reason = detail.Reason;
                    }
                    else
                    {
                        resp.Value = LdValue.Of(_client.IntVariation(p.FlagKey, p.DefaultValue.AsInt));
                    }
                    break;

                case "double":
                    if (p.Detail)
                    {
                        var detail = _client.DoubleVariationDetail(p.FlagKey, p.DefaultValue.AsDouble);
                        resp.Value = LdValue.Of(detail.Value);
                        resp.VariationIndex = detail.VariationIndex;
                        resp.Reason = detail.Reason;
                    }
                    else
                    {
                        resp.Value = LdValue.Of(_client.DoubleVariation(p.FlagKey, p.DefaultValue.AsDouble));
                    }
                    break;

                case "string":
                    if (p.Detail)
                    {
                        var detail = _client.StringVariationDetail(p.FlagKey, p.DefaultValue.AsString);
                        resp.Value = LdValue.Of(detail.Value);
                        resp.VariationIndex = detail.VariationIndex;
                        resp.Reason = detail.Reason;
                    }
                    else
                    {
                        resp.Value = LdValue.Of(_client.StringVariation(p.FlagKey, p.DefaultValue.AsString));
                    }
                    break;

                default:
                    if (p.Detail)
                    {
                        var detail = _client.JsonVariationDetail(p.FlagKey, p.DefaultValue);
                        resp.Value = detail.Value;
                        resp.VariationIndex = detail.VariationIndex;
                        resp.Reason = detail.Reason;
                    }
                    else
                    {
                        resp.Value = _client.JsonVariation(p.FlagKey, p.DefaultValue);
                    }
                    break;
            }

            if (p.Detail && !_evaluationReasons && resp.Reason.HasValue && resp.Reason.Value.Kind == EvaluationReasonKind.Off)
            {
                resp.Reason = null;
            }

            return resp;
        }

        private object DoEvaluateAll(EvaluateAllFlagsParams p)
        {
            return new EvaluateAllFlagsResponse
            {
                State = _client.AllFlags()
            };
        }

        private static Configuration BuildSdkConfig(SdkConfigParams sdkParams, ILogAdapter logAdapter, string tag)
        {
            var builder = Configuration.Builder(sdkParams.Credential);

            builder.Logging(Components.Logging(logAdapter).BaseLoggerName(tag + ".SDK"));

            var endpoints = Components.ServiceEndpoints();
            builder.ServiceEndpoints(endpoints);
            if (sdkParams.ServiceEndpoints != null)
            {
                if (sdkParams.ServiceEndpoints.Streaming != null)
                {
                    endpoints.Streaming(sdkParams.ServiceEndpoints.Streaming);
                }
                if (sdkParams.ServiceEndpoints.Polling != null)
                {
                    endpoints.Polling(sdkParams.ServiceEndpoints.Polling);
                }
                if (sdkParams.ServiceEndpoints.Events != null)
                {
                    endpoints.Events(sdkParams.ServiceEndpoints.Events);
                }
            }

            var streamingParams = sdkParams.Streaming;
            var pollingParams = sdkParams.Polling;
            if (streamingParams != null)
            {
                endpoints.Streaming(streamingParams.BaseUri);
                var dataSource = Components.StreamingDataSource();
                if (streamingParams.InitialRetryDelayMs.HasValue)
                {
                    dataSource.InitialReconnectDelay(TimeSpan.FromMilliseconds(streamingParams.InitialRetryDelayMs.Value));
                }
                if (pollingParams != null)
                {
                    endpoints.Polling(pollingParams.BaseUri);
                    if (pollingParams.PollIntervalMs.HasValue)
                    {
                        dataSource.BackgroundPollInterval(TimeSpan.FromMilliseconds(pollingParams.PollIntervalMs.Value));
                    }
                }
                builder.DataSource(dataSource);
            }
            else if (pollingParams != null)
            {
                endpoints.Polling(pollingParams.BaseUri);
                var dataSource = Components.PollingDataSource();
                if (pollingParams.PollIntervalMs.HasValue)
                {
                    dataSource.PollInterval(TimeSpan.FromMilliseconds(pollingParams.PollIntervalMs.Value));
                }
                builder.DataSource(dataSource);
            }

            var eventParams = sdkParams.Events;
            if (eventParams == null)
            {
                builder.Events(Components.NoEvents);
            }
            else
            {
                endpoints.Events(eventParams.BaseUri);
                var events = Components.SendEvents()
                    .AllAttributesPrivate(eventParams.AllAttributesPrivate);
                if (eventParams.Capacity.HasValue && eventParams.Capacity.Value > 0)
                {
                    events.Capacity(eventParams.Capacity.Value);
                }
                if (eventParams.FlushIntervalMs.HasValue && eventParams.FlushIntervalMs.Value > 0)
                {
                    events.FlushInterval(TimeSpan.FromMilliseconds(eventParams.FlushIntervalMs.Value));
                }
                if (eventParams.GlobalPrivateAttributes != null)
                {
                    events.PrivateAttributes(eventParams.GlobalPrivateAttributes);
                }
                builder.Events(events);
                builder.DiagnosticOptOut(!eventParams.EnableDiagnostics);
            }

            var http = Components.HttpConfiguration();
            if (sdkParams.ClientSide.UseReport.HasValue)
            {
                http.UseReport(sdkParams.ClientSide.UseReport.Value);
            }
            builder.Http(http);

            if (sdkParams.ClientSide.EvaluationReasons.HasValue)
            {
                builder.EvaluationReasons(sdkParams.ClientSide.EvaluationReasons.Value);
            }

            return builder.Build();
        }
    }
}
