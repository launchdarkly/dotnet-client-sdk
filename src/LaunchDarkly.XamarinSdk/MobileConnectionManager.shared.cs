using System;

namespace LaunchDarkly.Xamarin
{
    internal class MobileConnectionManager : IConnectionManager
    {
        internal Action<bool> ConnectionChanged;

        internal MobileConnectionManager()
        {
            UpdateConnectedStatus();
            try
            {
                LaunchDarkly.Xamarin.Connectivity.Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            }
            catch (NotImplementedException)
            { }
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
            try
            {
                isConnected = LaunchDarkly.Xamarin.Connectivity.Connectivity.NetworkAccess == LaunchDarkly.Xamarin.Connectivity.NetworkAccess.Internet;
            }
            catch (NotImplementedException)
            {
                // .NET Standard has no way to detect network connectivity
                isConnected = true;
            }
        }
    }
}
