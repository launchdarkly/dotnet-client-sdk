using System;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Client.PlatformSpecific;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal sealed class DefaultConnectivityStateManager : IConnectivityStateManager
    {
        public Action<bool> ConnectionChanged { get; set; }

        internal DefaultConnectivityStateManager()
        {
            UpdateConnectedStatus();
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        bool isConnected;
        bool IConnectivityStateManager.IsConnected
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
