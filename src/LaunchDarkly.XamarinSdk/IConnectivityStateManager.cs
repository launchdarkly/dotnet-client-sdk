using System;

namespace LaunchDarkly.Xamarin
{
    internal interface IConnectivityStateManager
    {
        bool IsConnected { get; set; }
        Action<bool> ConnectionChanged { get; set; }
    }
}
