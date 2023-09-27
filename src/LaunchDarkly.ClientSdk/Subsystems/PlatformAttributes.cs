using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
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
    
    internal static partial class PlatformAttributes
    {
        internal static Layer Layer => new Layer { ApplicationInfo = new ConcreteProp<ApplicationInfo>(new ApplicationInfo(
            "",
            AppInfo.Name,
            AppInfo.Version.ToString(),
            ""
        ))};
    }
}