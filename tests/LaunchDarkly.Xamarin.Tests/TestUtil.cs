using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin.Tests
{
    public class TestUtil
    {
        // Any tests that are going to access the static LdClient.Instance must hold this lock,
        // to avoid interfering with tests that use CreateClient.
        public static readonly object ClientInstanceLock = new object();
        
        // Calls LdClient.Init, but then sets LdClient.Instance to null so other tests can
        // instantiate their own independent clients. Application code cannot do this because
        // the LdClient.Instance setter has internal scope.
        public static LdClient CreateClient(Configuration config, User user)
        {
            lock (ClientInstanceLock)
            {
                LdClient client = LdClient.Init(config, user);
                LdClient.Instance = null;
                return client;
            }
        }

        public static string JsonFlagsWithSingleFlag(string flagKey, JToken value)
        {
            JObject fo = new JObject { { "value", value }  };
            JObject o = new JObject { { flagKey, fo } };
            return JsonConvert.SerializeObject(o);
        }

        public static IDictionary<string, FeatureFlag> DecodeFlagsJson(string flagsJson)
        {
            return JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(flagsJson);
        }

        public static Configuration ConfigWithFlagsJson(User user, string appKey, string flagsJson)
        {
            var flags = DecodeFlagsJson(flagsJson);
            IUserFlagCache stubbedFlagCache = new UserFlagInMemoryCache();
            if (user != null && user.Key != null)
            {
                stubbedFlagCache.CacheFlagsForUser(flags, user);
            }

            var mockOnlineConnectionManager = new MockConnectionManager(true);
            var mockFlagCacheManager = new MockFlagCacheManager(stubbedFlagCache);
            var mockPollingProcessor = new MockPollingProcessor();
            var mockPersister = new MockPersister();
            var mockDeviceInfo = new MockDeviceInfo();
            var featureFlagListener = new FeatureFlagListenerManager();

            Configuration configuration = Configuration.Default(appKey)
                                                       .WithFlagCacheManager(mockFlagCacheManager)
                                                       .WithConnectionManager(mockOnlineConnectionManager)
                                                       .WithUpdateProcessor(mockPollingProcessor)
                                                       .WithPersister(mockPersister)
                                                       .WithDeviceInfo(mockDeviceInfo)
                                                       .WithFeatureFlagListenerManager(featureFlagListener);
            return configuration;
        }
    }
}
