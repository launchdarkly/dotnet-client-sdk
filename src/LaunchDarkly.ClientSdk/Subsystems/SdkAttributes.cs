using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.Subsystems
{
    internal static class SdkAttributes
    {
        internal static Layer Layer => new Layer
        {
            ApplicationInfo = new Props.Some<ApplicationInfo>(new ApplicationInfo(
                SdkPackage.Name, 
                SdkPackage.Name, 
                SdkPackage.Version, 
                SdkPackage.Version))
        };
    }
}
