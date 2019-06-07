using System;
using LaunchDarkly.Xamarin;
using UIKit;
using Common.Logging;
using Foundation;

namespace LaunchDarkly.Xamarin.BackgroundDetection
{
    internal static partial class BackgroundDetection
    {
        private static NSObject _foregroundHandle;
        private static NSObject _backgroundHandle;
        private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundAdapter));

        private static void StartListening()
        {
            _foregroundHandle = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, HandleWillEnterForeground);
            _backgroundHandle = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, HandleWillEnterBackground);
        }

        private static void StopListening()
        {
            _foregroundHandle = null;
            _backgroundHandle = null;
        }

        private static void HandleWillEnterForeground(NSNotification notification)
        {
            UpdateBackgroundMode(false);
        }

        private static void HandleWillEnterBackground(NSNotification notification)
        {
            UpdateBackgroundMode(true);
        }
    }
}
