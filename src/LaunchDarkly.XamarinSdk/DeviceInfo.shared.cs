using System;
using Plugin.DeviceInfo;

namespace LaunchDarkly.Xamarin
{
    public class DeviceInfo : IDeviceInfo
    {
        public string UniqueDeviceId()
        {
            return CrossDeviceInfo.Current.Id;
        }
    }
}
