using System;
using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Sdk.Xamarin
{
    internal interface IBackgroundModeManager
    {
        event EventHandler<BackgroundModeChangedEventArgs> BackgroundModeChanged;
    }
}
