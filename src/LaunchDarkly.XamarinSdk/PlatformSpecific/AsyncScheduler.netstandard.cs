using System;
using System.Threading.Tasks;

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    internal static partial class AsyncScheduler
    {
        private static void PlatformScheduleAction(Action a)
        {
            Task.Run(a);
        }
    }
}
