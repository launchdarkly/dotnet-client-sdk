using System;
using System.Threading.Tasks;

namespace LaunchDarkly.Xamarin.Tests
{
    public class MockPollingProcessor : IMobileUpdateProcessor
    {
        public bool IsRunning
        {
            get;
            set;
        }

        public void Dispose()
        {
            IsRunning = false;
        }

        public bool Initialized()
        {
            return IsRunning;
        }

        public Task<bool> Start()
        {
            IsRunning = true;
            return Task.FromResult(true);
        }
    }
}
