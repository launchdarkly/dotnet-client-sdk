using System;
using Android.OS;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class AsyncScheduler
    {
        private static Handler handler;

        private static void PlatformScheduleAction(Action a)
        {
            // This is based on the Android implementation in Xamarin.Essentials:
            // https://github.com/xamarin/Essentials/blob/master/Xamarin.Essentials/MainThread/MainThread.android.cs
            if (handler?.Looper != Looper.MainLooper)
            {
                handler = new Handler(Looper.MainLooper);
            }
            handler.Post(a);
        }
    }
}
