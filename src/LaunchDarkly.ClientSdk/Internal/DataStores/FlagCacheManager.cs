using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

using static LaunchDarkly.Sdk.Client.DataModel;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class FlagCacheManager : IFlagCacheManager
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

        public IImmutableDictionary<string, FeatureFlag> FlagsForUser(User user)
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

        public void CacheFlagsFromService(IImmutableDictionary<string, FeatureFlag> flags, User user)
        {
            List<Tuple<string, LdValue, LdValue>> changes = null;
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
                        if (!originalFlag.value.Equals(flag.Value.value))
                        {
                            if (changes == null)
                            {
                                changes = new List<Tuple<string, LdValue, LdValue>>();
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
            LdValue oldValue = LdValue.Null;
            bool existed = false;
            readWriteLock.EnterWriteLock();
            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                if (flagsForUser.TryGetValue(flagKey, out var flag))
                {
                    existed = true;
                    oldValue = flag.value;
                    var updatedFlags = flagsForUser.Remove(flagKey); // IImmutableDictionary.Remove() returns a new dictionary
                    deviceCache.CacheFlagsForUser(updatedFlags, user);
                    inMemoryCache.CacheFlagsForUser(updatedFlags, user);
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
            if (existed)
            {
                flagChangedEventManager.FlagWasDeleted(flagKey, oldValue);
            }
        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            bool changed = false;
            LdValue oldValue = LdValue.Null;
            readWriteLock.EnterWriteLock();
            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                if (flagsForUser.TryGetValue(flagKey, out var oldFlag))
                {
                    if (!oldFlag.value.Equals(featureFlag.value))
                    {
                        oldValue = oldFlag.value;
                        changed = true;
                    }
                }
                var updatedFlags = flagsForUser.SetItem(flagKey, featureFlag); // IImmutableDictionary.SetItem() returns a new dictionary
                deviceCache.CacheFlagsForUser(updatedFlags, user);
                inMemoryCache.CacheFlagsForUser(updatedFlags, user);
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
