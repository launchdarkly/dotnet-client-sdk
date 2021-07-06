using System;

namespace LaunchDarkly.Sdk.Xamarin.Internal.Interfaces
{
    internal interface IConnectivityStateManager
    {
        bool IsConnected { get; set; }
        Action<bool> ConnectionChanged { get; set; }
    }
}
