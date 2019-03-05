using System;

namespace LaunchDarkly.Xamarin
{
    public interface IConnectionManager
    {
        bool IsConnected { get; set; }
    }
}
