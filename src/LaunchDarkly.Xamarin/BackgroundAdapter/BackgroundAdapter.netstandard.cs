using System;
using LaunchDarkly.Xamarin;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    public class BackgroundAdapter : IPlatformAdapter
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
            throw new NotImplementedException();
        }
    }
}
