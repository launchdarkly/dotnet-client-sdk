using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaunchDarkly.Sdk.Xamarin
{
    public static class TestUtil
    {
        // Any tests that are going to access the static LdClient.Instance must hold this lock,
        // to avoid interfering with tests that use CreateClient.
        private static readonly SemaphoreSlim ClientInstanceLock = new SemaphoreSlim(1);

        private static ThreadLocal<bool> InClientLock = new ThreadLocal<bool>();

        public static T WithClientLock<T>(Func<T> f)
        {
            // This cumbersome combination of a ThreadLocal and a SemaphoreSlim is simply because 1. we have to use
            // SemaphoreSlim (I think) since there's no way to wait on a regular lock in *async* code, and 2. SemaphoreSlim
            // is not reentrant, so we need to make sure a thread can't block itself.
            if (InClientLock.Value)
            {
                return f.Invoke();
            }
            ClientInstanceLock.Wait();
            try
            {
                InClientLock.Value = true;
                return f.Invoke();
            }
            finally
            {
                InClientLock.Value = false;
                ClientInstanceLock.Release();
            }
        }

        public static void WithClientLock(Action a)
        {
            if (InClientLock.Value)
            {
                a.Invoke();
                return;
            }
            ClientInstanceLock.Wait();
            try
            {
                InClientLock.Value = true;
                a.Invoke();
            }
            finally
            {
                InClientLock.Value = false;
                ClientInstanceLock.Release();
            }
        }

        public static async Task<T> WithClientLockAsync<T>(Func<Task<T>> f)
        {
            if (InClientLock.Value)
            {
                return await f.Invoke();
            }
            await ClientInstanceLock.WaitAsync();
            try
            {
                InClientLock.Value = true;
                return await f.Invoke();
            }
            finally
            {
                InClientLock.Value = false;
                ClientInstanceLock.Release();
            }
        }

        // Calls LdClient.Init, but then sets LdClient.Instance to null so other tests can
        // instantiate their own independent clients. Application code cannot do this because
        // the LdClient.Instance setter has internal scope.
        public static LdClient CreateClient(Configuration config, User user, TimeSpan? timeout = null)
        {
            return WithClientLock(() =>
            {
                ClearClient();
                LdClient client = LdClient.Init(config, user, timeout ?? TimeSpan.FromSeconds(1));
                client.DetachInstance();
                return client;
            });
        }

        // Calls LdClient.Init, but then sets LdClient.Instance to null so other tests can
        // instantiate their own independent clients. Application code cannot do this because
        // the LdClient.Instance setter has internal scope.
        public static async Task<LdClient> CreateClientAsync(Configuration config, User user)
        {
            return await WithClientLockAsync(async () =>
            {
                ClearClient();
                LdClient client = await LdClient.InitAsync(config, user);
                client.DetachInstance();
                return client;
            });
        }

        public static void ClearClient()
        {
            WithClientLock(() =>
            {
                LdClient.Instance?.Dispose();
            });
        }

        internal static IImmutableDictionary<string, FeatureFlag> MakeSingleFlagData(string flagKey, LdValue value, int? variation = null, EvaluationReason? reason = null)
        {
            var flag = new FeatureFlagBuilder().Value(value).Variation(variation).Reason(reason).Build();
            return ImmutableDictionary.Create<string, FeatureFlag>().SetItem(flagKey, flag);
        }

        internal static string JsonFlagsWithSingleFlag(string flagKey, LdValue value, int? variation = null, EvaluationReason? reason = null)
        {
            return JsonUtil.SerializeFlags(MakeSingleFlagData(flagKey, value, variation, reason));
        }

        internal static IImmutableDictionary<string, FeatureFlag> DecodeFlagsJson(string flagsJson)
        {
            return JsonUtil.DeserializeFlags(flagsJson);
        }

        internal static ConfigurationBuilder ConfigWithFlagsJson(User user, string appKey, string flagsJson)
        {
            var flags = DecodeFlagsJson(flagsJson);
            IUserFlagCache stubbedFlagCache = new UserFlagInMemoryCache();
            if (user != null && user.Key != null)
            {
                stubbedFlagCache.CacheFlagsForUser(flags, user);
            }

            return Configuration.BuilderInternal(appKey)
                                .FlagCacheManager(new MockFlagCacheManager(stubbedFlagCache))
                                .ConnectivityStateManager(new MockConnectivityStateManager(true))
                                .EventProcessor(new MockEventProcessor())
                                .UpdateProcessorFactory(MockPollingProcessor.Factory(null))
                                .PersistentStorage(new MockPersistentStorage())
                                .DeviceInfo(new MockDeviceInfo());
        }

        public static void AssertJsonEquals(LdValue expected, LdValue actual)
        {
            Assert.Equal(expected, actual);
        }

        public static LdValue NormalizeJsonUser(LdValue json)
        {
            // It's undefined whether a user with no custom attributes will have "custom":{} or not
            if (json.Type == LdValueType.Object && json.Get("custom").Type == LdValueType.Object)
            {
                if (json.Count == 0)
                {
                    var o1 = LdValue.BuildObject();
                    foreach (var kv in json.AsDictionary(LdValue.Convert.Json))
                    {
                        if (kv.Key != "custom")
                        {
                            o1.Add(kv.Key, kv.Value);
                        }
                    }
                    return o1.Build();
                }
            }
            return json;
        }
    }
}
