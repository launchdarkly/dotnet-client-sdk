using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal interface IUserFlagCache
    {
        void CacheFlagsForUser(IDictionary<string, FeatureFlag> flags, User user);
        IDictionary<string, FeatureFlag> RetrieveFlags(User user);
    }
}
