using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.Subsystems
{
    internal class MaybeAppInfo: IProp<ApplicationInfo>
    {
        private readonly ApplicationInfo? _value;

        public MaybeAppInfo(ApplicationInfo? value)
        {
            _value = value;
        }

        public bool HasValue()
        {
            return _value.HasValue;
        }

        public ApplicationInfo GetValue() => _value.Value;
    }
    
    internal static class PlatformAttributes
    {
        internal static Layer Layer => new Layer { ApplicationInfo = new MaybeAppInfo(AppInfo.Get()) };
    }
}
