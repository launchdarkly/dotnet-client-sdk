using System;
using LaunchDarkly.Xamarin;
using Android.App;
using Android.OS;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    internal class BackgroundAdapter : IPlatformAdapter
    {
        private static ActivityLifecycleCallbacks _callbacks;
        private Application application;

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
            if (_callbacks == null)
            {
                _callbacks = new ActivityLifecycleCallbacks(backgroundingState);
                application = (Application)Application.Context;
                application.RegisterActivityLifecycleCallbacks(_callbacks);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void _Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                application = null;
                _callbacks = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

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
                _backgroundingState.EnterBackgroundAsync();
            }

            public void OnActivityResumed(Activity activity)
            {
                _backgroundingState.ExitBackgroundAsync();
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
