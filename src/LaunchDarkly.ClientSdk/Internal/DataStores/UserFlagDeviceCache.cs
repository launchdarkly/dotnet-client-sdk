using System;
using System.Collections.Immutable;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class UserFlagDeviceCache : IUserFlagCache
    {
        private readonly IPersistentStorage persister;
        private readonly Logger _log;

        public UserFlagDeviceCache(IPersistentStorage persister, Logger log)
        {
            this.persister = persister;
            _log = log;
        }

        void IUserFlagCache.CacheFlagsForUser(IImmutableDictionary<string, FeatureFlag> flags, User user)
        {
            var jsonString = JsonUtil.SerializeFlags(flags);
            try
            {
                persister.Save(Constants.FLAGS_KEY_PREFIX + user.Key, jsonString);
            }
            catch (System.Exception ex)
            {
                _log.Error("Couldn't set preferences on mobile device: {0}",
                    LogValues.ExceptionSummary(ex));
            }
        }

        IImmutableDictionary<string, FeatureFlag> IUserFlagCache.RetrieveFlags(User user)
        {
            try
            {
                var flagsAsJson = persister.GetValue(Constants.FLAGS_KEY_PREFIX + user.Key);
                if (flagsAsJson != null)
                {
                    return JsonUtil.DeserializeFlags(flagsAsJson);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Couldn't get preferences on mobile device: {0}",
                    LogValues.ExceptionSummary(ex));
            }

            return ImmutableDictionary.Create<string, FeatureFlag>();
        }
    }
}
