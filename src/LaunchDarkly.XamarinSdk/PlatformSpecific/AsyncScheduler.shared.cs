using System;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    // This class and the rest of its partial class implementations are not derived from Xamarin Essentials.
    // It provides a method for asynchronously starting tasks, such as event handlers, using a mechanism
    // that may vary by platform.
    internal static partial class AsyncScheduler
    {
        // Queues a task to be executed asynchronously as soon as possible. On platforms that have a notion
        // of a "main thread" or "UI thread", the action is guaranteed to run on that thread; otherwise it
        // can be any thread.
        public static void ScheduleAction(Action a)
        {
            PlatformScheduleAction(a);
        }
    }
}
