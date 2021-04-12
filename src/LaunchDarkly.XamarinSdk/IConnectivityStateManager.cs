using System;

namespace LaunchDarkly.Sdk.Xamarin
{
    internal interface IConnectivityStateManager
    {
        bool IsConnected { get; set; }
        Action<bool> ConnectionChanged { get; set; }
    }
}
