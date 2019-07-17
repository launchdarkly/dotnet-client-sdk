using System;
using System.Collections.Generic;
using System.Threading;
using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    internal class FlagCacheManager : IFlagCacheManager
    {
        private readonly IUserFlagCache inMemoryCache;
        private readonly IUserFlagCache deviceCache;
        private readonly IFlagChangedEventManager flagChangedEventManager;

        private ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public FlagCacheManager(IUserFlagCache inMemoryCache,
                                IUserFlagCache deviceCache,
                                IFlagChangedEventManager flagChangedEventManager,
                                User user)
        {
            this.inMemoryCache = inMemoryCache;
            this.deviceCache = deviceCache;
            this.flagChangedEventManager = flagChangedEventManager;

            var flagsFromDevice = deviceCache.RetrieveFlags(user);
            if (flagsFromDevice != null)
            {
                inMemoryCache.CacheFlagsForUser(flagsFromDevice, user);
            }
        }

        public IDictionary<string, FeatureFlag> FlagsForUser(User user)
        {
            readWriteLock.EnterReadLock();
            try
            {
                return inMemoryCache.RetrieveFlags(user);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public void CacheFlagsFromService(IDictionary<string, FeatureFlag> flags, User user)
        {
            List<Tuple<string, JToken, JToken>> changes = null;
            readWriteLock.EnterWriteLock();
            try
            {
                var previousFlags = inMemoryCache.RetrieveFlags(user);
                deviceCache.CacheFlagsForUser(flags, user);
                inMemoryCache.CacheFlagsForUser(flags, user);

                foreach (var flag in flags)
                {
                    if (previousFlags.TryGetValue(flag.Key, out var originalFlag))
                    {
                        if (!JToken.DeepEquals(originalFlag.value, flag.Value.value))
                        {
                            if (changes == null)
                            {
                                changes = new List<Tuple<string, JToken, JToken>>();
                            }
                            changes.Add(Tuple.Create(flag.Key, flag.Value.value, originalFlag.value));
                        }
                    }
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
            if (changes != null)
            {
                foreach (var c in changes)
                {
                    flagChangedEventManager.FlagWasUpdated(c.Item1, c.Item2, c.Item3);
                }
            }
        }

        public FeatureFlag FlagForUser(string flagKey, User user)
        {
            var flags = FlagsForUser(user);
            if (flags.TryGetValue(flagKey, out var featureFlag))
            {
                return featureFlag;
            }
            return null;
        }

        public void RemoveFlagForUser(string flagKey, User user)
        {
            JToken oldValue = null;
            readWriteLock.EnterWriteLock();
            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                if (flagsForUser.TryGetValue(flagKey, out var flag))
                {
                    oldValue = flag.value;
                    flagsForUser.Remove(flagKey);
                    deviceCache.CacheFlagsForUser(flagsForUser, user);
                    inMemoryCache.CacheFlagsForUser(flagsForUser, user);
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
            if (oldValue != null)
            {
                flagChangedEventManager.FlagWasDeleted(flagKey, oldValue);
            }
        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            bool changed = false;
            JToken oldValue = null;
            readWriteLock.EnterWriteLock();
            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                if (flagsForUser.TryGetValue(flagKey, out var oldFlag))
                {
                    if (!JToken.DeepEquals(oldFlag.value, featureFlag.value))
                    {
                        oldValue = oldFlag.value;
                        changed = true;
                    }
                }
                flagsForUser[flagKey] = featureFlag;
                deviceCache.CacheFlagsForUser(flagsForUser, user);
                inMemoryCache.CacheFlagsForUser(flagsForUser, user);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
            if (changed)
            {
                flagChangedEventManager.FlagWasUpdated(flagKey, featureFlag.value, oldValue);
            }
        }
    }
}
