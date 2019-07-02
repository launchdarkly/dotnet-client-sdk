using System;
using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
{
    internal class MobileConnectionManager : IConnectionManager
    {
        internal Action<bool> ConnectionChanged;

        internal MobileConnectionManager()
        {
            UpdateConnectedStatus();
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        bool isConnected;
        bool IConnectionManager.IsConnected
        {
            get { return isConnected; }
            set
            {
                isConnected = value;
            }
        }
        
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            UpdateConnectedStatus();
            ConnectionChanged?.Invoke(isConnected);
        }

        private void UpdateConnectedStatus()
        {
            isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;
        }
    }
}
