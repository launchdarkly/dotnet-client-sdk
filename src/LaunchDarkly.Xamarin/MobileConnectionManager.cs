using System;
using Xamarin.Essentials;

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
                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            }
            catch (NotImplementedInReferenceAssemblyException)
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
        
        void Connectivity_ConnectivityChanged(ConnectivityChangedEventArgs e)
        {
            UpdateConnectedStatus();
            ConnectionChanged?.Invoke(isConnected);
        }

        private void UpdateConnectedStatus()
        {
            try
            {
                isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;
            }
            catch (NotImplementedInReferenceAssemblyException)
            {
                // .NET Standard has no way to detect network connectivity
                isConnected = true;
            }
        }
    }
}
