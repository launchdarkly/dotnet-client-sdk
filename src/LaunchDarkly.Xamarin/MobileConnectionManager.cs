using System;
using Xamarin.Essentials;

namespace LaunchDarkly.Xamarin
{
    internal class MobileConnectionManager : IConnectionManager
    {
        internal Action<bool> ConnectionChanged;

        internal MobileConnectionManager()
        {
            isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;
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


        void Connectivity_ConnectivityChanged(ConnectivityChangedEventArgs e)
        {
            isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;
            ConnectionChanged?.Invoke(isConnected);
        }
    }
}
