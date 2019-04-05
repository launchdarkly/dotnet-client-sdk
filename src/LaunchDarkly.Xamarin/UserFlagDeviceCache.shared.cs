using System;
using System.Collections.Generic;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin
{
    internal class UserFlagDeviceCache : IUserFlagCache
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UserFlagDeviceCache));
        private readonly ISimplePersistance persister;

        public UserFlagDeviceCache(ISimplePersistance persister)
        {
            this.persister = persister;
        }

        void IUserFlagCache.CacheFlagsForUser(IDictionary<string, FeatureFlag> flags, User user)
        {
            var jsonString = JsonConvert.SerializeObject(flags);
            try
            {
                persister.Save(Constants.FLAGS_KEY_PREFIX + user.Key, jsonString);
            }
            catch (System.Exception ex)
            {
                Log.ErrorFormat("Couldn't set preferences on mobile device: '{0}'",
                    ex,
                    Util.ExceptionMessage(ex));
            }
        }

        IDictionary<string, FeatureFlag> IUserFlagCache.RetrieveFlags(User user)
        {
            try
            {
                var flagsAsJson = persister.GetValue(Constants.FLAGS_KEY_PREFIX + user.Key);
                if (flagsAsJson != null)
                {
                    return JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(flagsAsJson);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Couldn't get preferences on mobile device: '{0}'",
                    ex,
                    Util.ExceptionMessage(ex));
            }

            return new Dictionary<string, FeatureFlag>();
        }
    }
}
