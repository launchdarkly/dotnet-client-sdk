using System;
using LaunchDarkly.Sdk.Xamarin.Internal.Interfaces;
using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Sdk.Xamarin.Internal.DataSources
{
    internal class DefaultBackgroundModeManager : IBackgroundModeManager
    {
        public event EventHandler<BackgroundModeChangedEventArgs> BackgroundModeChanged
        {
            add
            {
                BackgroundDetection.BackgroundModeChanged += value;
            }
            remove
            {
                BackgroundDetection.BackgroundModeChanged -= value;
            }
        }
    }
}
