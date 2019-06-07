using System;
using LaunchDarkly.Xamarin;
using Android.App;
using Android.OS;

namespace LaunchDarkly.Xamarin.BackgroundDetection
{
    internal static partial class BackgroundDetection
    {
        private static ActivityLifecycleCallbacks _callbacks;
        private static Application _application;

        private static void StartListening()
        {
            _callbacks = new ActivityLifecycleCallbacks();
            _application = (Application)Application.Context;
            _application.RegisterActivityLifecycleCallbacks(_callbacks);
        }

        private static void StopListening()
        {
            _callbacks = null;
            _application = null;
        }

        private class ActivityLifecycleCallbacks : Java.Lang.Object, Application.IActivityLifecycleCallbacks
        {
            private IBackgroundingState _backgroundingState;

            public ActivityLifecycleCallbacks(IBackgroundingState backgroundingState)
            {
                _backgroundingState = backgroundingState;
            }

            public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
            {
            }

            public void OnActivityDestroyed(Activity activity)
            {
            }

            public void OnActivityPaused(Activity activity)
            {
                BackgroundDetection.UpdateBackgroundMode(true);
            }

            public void OnActivityResumed(Activity activity)
            {
                BackgroundDetection.UpdateBackgroundMode(false);
            }

            public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
            {
            }

            public void OnActivityStarted(Activity activity)
            {
            }

            public void OnActivityStopped(Activity activity)
            {
            }
        }
    }
}
