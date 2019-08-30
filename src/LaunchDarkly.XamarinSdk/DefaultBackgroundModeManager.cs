using System;
using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
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
