using System;

namespace LaunchDarkly.Xamarin
{
    internal class MobileConnectionManager : IConnectionManager
    {
        internal Action<bool> ConnectionChanged;

        internal MobileConnectionManager()
        {
            UpdateConnectedStatus();
            LaunchDarkly.Xamarin.Connectivity.Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
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
        
        void Connectivity_ConnectivityChanged(object sender, Connectivity.ConnectivityChangedEventArgs e)
        {
            UpdateConnectedStatus();
            ConnectionChanged?.Invoke(isConnected);
        }

        private void UpdateConnectedStatus()
        {
            isConnected = LaunchDarkly.Xamarin.Connectivity.Connectivity.NetworkAccess == LaunchDarkly.Xamarin.Connectivity.NetworkAccess.Internet;
        }
    }
}
