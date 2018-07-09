using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Common.Logging;

namespace LaunchDarkly.Xamarin
{
    internal class MobileStreamingProcessor : IMobileUpdateProcessor, IStreamProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MobileStreamingProcessor));

        private readonly IMobileConfiguration _configuration;
        private readonly IFlagCacheManager _cacheManager;
        private readonly User _user;
        private readonly StreamManager _streamManager;

        internal MobileStreamingProcessor(IMobileConfiguration configuration,
                                          IFlagCacheManager cacheManager,
                                          User user,
                                          StreamManager.EventSourceCreator eventSourceCreator)
        {
            this._configuration = configuration;
            this._cacheManager = cacheManager;
            this._user = user;

            StreamProperties streamProperties;
            if (_configuration.UseReport)
            {
                streamProperties = MakeStreamPropertiesForREPORT();
            }
            else
            {
                streamProperties = MakeStreamPropertiesForGET();
            }

            _streamManager = new StreamManager(this,
                                               streamProperties,
                                               _configuration,
                                               MobileClientEnvironment.Instance,
                                               eventSourceCreator);
        }

        #region IMobileUpdateProcessor

        bool IMobileUpdateProcessor.Initialized()
        {
            return _streamManager.Initialized;
        }

        Task<bool> IMobileUpdateProcessor.Start()
        {
            return _streamManager.Start();
        }

        #endregion

        private StreamProperties MakeStreamPropertiesForGET()
        {
            var userEncoded = _user.AsJson().Base64Encode();
            Uri uri = new Uri(_configuration.StreamUri, Constants.STREAM_REQUEST_PATH + userEncoded);
            return new StreamProperties(uri, HttpMethod.Get, null);
        }

        private StreamProperties MakeStreamPropertiesForREPORT()
        {
            var userEncoded = _user.AsJson();
            Uri uri = new Uri(_configuration.StreamUri, Constants.STREAM_REQUEST_PATH);
            var content = new StringContent(_user.AsJson(), Encoding.UTF8, Constants.APPLICATION_JSON);
            var method = new HttpMethod("REPORT");
            return new StreamProperties(uri, method, content);
        }

        #region IStreamProcessor

        Task IStreamProcessor.HandleMessage(StreamManager streamManager, string messageType, string messageData)
        {
            switch (messageType)
            {
                case Constants.PUT:
                    {
                        _cacheManager.CacheFlagsFromService(JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(messageData), _user);
                        streamManager.Initialized = true;
                        break;
                    }
                case Constants.PATCH:
                    {
                        try
                        {
                            var parsed = JsonConvert.DeserializeObject<JObject>(messageData);
                            var flagkey = (string)parsed[Constants.KEY];
                            var featureFlag = parsed.ToObject<FeatureFlag>();
                            PatchFeatureFlag(flagkey, featureFlag);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error parsing PATCH message {0}: '{1}'",
                                            ex, messageData, Util.ExceptionMessage(ex));
                        }
                        break;
                    }
                case Constants.DELETE:
                    {
                        try
                        {
                            var dictionary = JsonConvert.DeserializeObject<IDictionary<string, JToken>>(messageData);
                            int version = dictionary[Constants.VERSION].ToObject<int>();
                            string flagKey = dictionary[Constants.KEY].ToString();
                            DeleteFeatureFlag(flagKey, version);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error parsing DELETE message {0}: '{1}'",
                                            ex, messageData, Util.ExceptionMessage(ex));
                        }
                        break;
                    }
                case Constants.PING:
                    {
                        streamManager.Initialized = true;
                        break;
                    }
                default:
                    break;
            }

            return Task.CompletedTask;
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
                ((IDisposable)_streamManager).Dispose();
            }
        }
    }
}
