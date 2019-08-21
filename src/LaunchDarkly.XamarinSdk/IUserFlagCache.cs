using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal interface IUserFlagCache
    {
        void CacheFlagsForUser(IDictionary<string, FeatureFlag> flags, User user);
        IDictionary<string, FeatureFlag> RetrieveFlags(User user);
    }

    internal sealed class NullUserFlagCache : IUserFlagCache
    {
        public void CacheFlagsForUser(IDictionary<string, FeatureFlag> flags, User user) { }
        public IDictionary<string, FeatureFlag> RetrieveFlags(User user) => null;
    }
}
