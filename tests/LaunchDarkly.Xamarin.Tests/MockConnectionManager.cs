using System;

namespace LaunchDarkly.Xamarin.Tests
{
    internal class MockConnectionManager : IConnectionManager
    {
        public Action<bool> ConnectionChanged;

        public MockConnectionManager(bool isOnline)
        {
            isConnected = isOnline;
        }

        bool isConnected;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }

            set
            {
                isConnected = value;
            }
        }

        public void Connect(bool online)
        {
            IsConnected = online;
            ConnectionChanged?.Invoke(IsConnected);
        }
    }
}
