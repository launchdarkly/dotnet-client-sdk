using System;
using LaunchDarkly.Xamarin;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    // This is a stub implementation for .NET Standard where there's no such thing as backgrounding.

    internal class BackgroundAdapter : IPlatformAdapter
    {
        public void Dispose()
        {
        }

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
        }
    }
}
