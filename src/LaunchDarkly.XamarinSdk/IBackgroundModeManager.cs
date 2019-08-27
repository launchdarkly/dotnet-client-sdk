using System;
using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
{
    internal interface IBackgroundModeManager
    {
        event EventHandler<BackgroundModeChangedEventArgs> BackgroundModeChanged;
    }
}
