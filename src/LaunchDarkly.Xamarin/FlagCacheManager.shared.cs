using System.Collections.Generic;
using System.Threading;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal class FlagCacheManager : IFlagCacheManager
    {
        private readonly IUserFlagCache inMemoryCache;
        private readonly IUserFlagCache deviceCache;
        private readonly IFlagListenerUpdater flagListenerUpdater;

        private ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public FlagCacheManager(IUserFlagCache inMemoryCache,
                                IUserFlagCache deviceCache,
                                IFlagListenerUpdater flagListenerUpdater,
                                User user)
        {
            this.inMemoryCache = inMemoryCache;
            this.deviceCache = deviceCache;
            this.flagListenerUpdater = flagListenerUpdater;

            var flagsFromDevice = deviceCache.RetrieveFlags(user);
            inMemoryCache.CacheFlagsForUser(flagsFromDevice, user);
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
            readWriteLock.EnterWriteLock();
            try
            {
                var previousFlags = inMemoryCache.RetrieveFlags(user);
                deviceCache.CacheFlagsForUser(flags, user);
                inMemoryCache.CacheFlagsForUser(flags, user);

                foreach (var flag in flags)
                {
                    bool flagAlreadyExisted = previousFlags.ContainsKey(flag.Key);
                    bool flagValueChanged = false;
                    if (flagAlreadyExisted)
                    {
                       var originalFlag = previousFlags[flag.Key];
                       flagValueChanged = originalFlag.value != flag.Value.value;
                    }

                    // only update the Listeners if the flag value changed
                    if (flagValueChanged)
                    {
                        flagListenerUpdater.FlagWasUpdated(flag.Key, flag.Value.value);
                    }
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public FeatureFlag FlagForUser(string flagKey, User user)
        {
            var flags = FlagsForUser(user);
            FeatureFlag featureFlag;
            if (flags.TryGetValue(flagKey, out featureFlag))
            {
                return featureFlag;
            }

            return null;
        }

        public void RemoveFlagForUser(string flagKey, User user)
        {
            readWriteLock.EnterWriteLock();

            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                flagsForUser.Remove(flagKey);
                deviceCache.CacheFlagsForUser(flagsForUser, user);
                inMemoryCache.CacheFlagsForUser(flagsForUser, user);
                flagListenerUpdater.FlagWasDeleted(flagKey);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }

        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            readWriteLock.EnterWriteLock();

            try
            {
                var flagsForUser = inMemoryCache.RetrieveFlags(user);
                flagsForUser[flagKey] = featureFlag;
                deviceCache.CacheFlagsForUser(flagsForUser, user);
                inMemoryCache.CacheFlagsForUser(flagsForUser, user);
                flagListenerUpdater.FlagWasUpdated(flagKey, featureFlag.value);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }
    }
}
