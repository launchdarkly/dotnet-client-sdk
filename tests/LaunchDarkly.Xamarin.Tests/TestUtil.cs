using System;
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
                LdClient client = LdClient.Init(config, user, TimeSpan.Zero);
                LdClient.Instance = null;
                return client;
            }
        }

        public static void ClearClient()
        {
            if (LdClient.Instance != null)
            {
                (LdClient.Instance as IDisposable).Dispose();
                LdClient.Instance = null;
            }
        }

        public static string JsonFlagsWithSingleFlag(string flagKey, JToken value, int? variation = null, EvaluationReason reason = null)
        {
            JObject fo = new JObject { { "value", value } };
            if (variation != null)
            {
                fo["variation"] = new JValue(variation.Value);
            }
            if (reason != null)
            {
                fo["reason"] = JToken.FromObject(reason);
            }
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

            Configuration configuration = Configuration.Default(appKey)
                                                       .WithFlagCacheManager(new MockFlagCacheManager(stubbedFlagCache))
                                                       .WithConnectionManager(new MockConnectionManager(true))
                                                       .WithEventProcessor(new MockEventProcessor())
                                                       .WithUpdateProcessor(new MockPollingProcessor())
                                                       .WithPersister(new MockPersister())
                                                       .WithDeviceInfo(new MockDeviceInfo(""))
                                                       .WithFeatureFlagListenerManager(new FeatureFlagListenerManager());
            return configuration;
        }
    }
}
