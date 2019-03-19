using System;
using LaunchDarkly.Xamarin;
using UIKit;
using Common.Logging;
using Foundation;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    public class BackgroundAdapter : IPlatformAdapter
    {
        private IBackgroundingState _backgroundingState;
        private NSObject _foregroundHandle;
        private NSObject _backgroundHandle;
        private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundAdapter));

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
            Log.Debug("Enable Backgrounding");
            _foregroundHandle = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, HandleWillEnterForeground);
            _backgroundHandle = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, HandleWillEnterBackground);
            _backgroundingState = backgroundingState;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected void _Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                _backgroundingState = null;
                _foregroundHandle = null;

                disposedValue = true;
            }
        }

        public new void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private void HandleWillEnterForeground(NSNotification notification)
        {
            Log.Debug("Entering Foreground");
            _backgroundingState.ExitBackgroundAsync();
        }

        private void HandleWillEnterBackground(NSNotification notification)
        {
            Log.Debug("Entering Background");
            _backgroundingState.EnterBackgroundAsync();
        }
    }
}
