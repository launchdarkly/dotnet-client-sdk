using System;
using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Sdk.Xamarin
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
