﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Json;
using Xunit;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
{
    public static class TestUtil
    {
        // Any tests that are going to access the static LdClient.Instance must hold this lock,
        // to avoid interfering with tests that use CreateClient.
        private static readonly SemaphoreSlim ClientInstanceLock = new SemaphoreSlim(1);

        private static ThreadLocal<bool> InClientLock = new ThreadLocal<bool>();

        public static LdClientContext SimpleContext => new LdClientContext(Configuration.Default("key"));

        public static Context Base64ContextFromUrlPath(string path, string pathPrefix)
        {
            Assert.StartsWith(pathPrefix, path);
            var base64String = path.Substring(pathPrefix.Length);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
            return LdJsonSerialization.DeserializeObject<Context>(decoded);
        }

        public static ContextBuilder BuildAutoContext() =>
            Context.Builder(Constants.AutoKeyMagicValue).Transient(true);

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
        public static LdClient CreateClient(Configuration config, Context context, TimeSpan? timeout = null)
        {
            return WithClientLock(() =>
            {
                ClearClient();
                LdClient client = LdClient.Init(config, context, timeout ?? TimeSpan.FromSeconds(1));
                client.DetachInstance();
                return client;
            });
        }

        // Calls LdClient.Init, but then sets LdClient.Instance to null so other tests can
        // instantiate their own independent clients. Application code cannot do this because
        // the LdClient.Instance setter has internal scope.
        public static async Task<LdClient> CreateClientAsync(Configuration config, Context context)
        {
            return await WithClientLockAsync(async () =>
            {
                ClearClient();
                LdClient client = await LdClient.InitAsync(config, context);
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

        internal static string MakeJsonData(FullDataSet data)
        {
            var w = JWriter.New();
            using (var ow = w.Object())
            {
                foreach (var item in data.Items)
                {
                    if (item.Value.Item != null)
                    {
                        FeatureFlagJsonConverter.WriteJsonValue(item.Value.Item, ow.Name(item.Key));
                    }
                }
            }
            return w.GetString();
        }
    }
}
