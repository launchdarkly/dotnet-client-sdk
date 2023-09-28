using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.Subsystems
{
    internal class ConcreteProp<T> : IProp<T>
    {
        private readonly T _value;

        public ConcreteProp(T value)
        {
            _value = value;
        }

        public bool HasValue()
        {
            return true;
        }

        public T GetValue()
        {
            return _value;
        }
    }
    
    internal static class PlatformAttributes
    {
        internal static Layer Layer => new Layer { ApplicationInfo = new ConcreteProp<ApplicationInfo>(new ApplicationInfo(
            AppInfo.Id,
            AppInfo.Name,
            AppInfo.Version,
            AppInfo.VersionName
        ))};
    }
}
