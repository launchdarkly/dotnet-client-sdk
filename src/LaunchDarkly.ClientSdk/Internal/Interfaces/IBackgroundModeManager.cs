using System;
using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Sdk.Xamarin.Internal.Interfaces
{
    internal interface IBackgroundModeManager
    {
        event EventHandler<BackgroundModeChangedEventArgs> BackgroundModeChanged;
    }
}
