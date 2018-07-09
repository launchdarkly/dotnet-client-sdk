using System;

namespace LaunchDarkly.Xamarin
{
    internal interface IConnectionManager
    {
        bool IsConnected { get; set; }
    }
}
