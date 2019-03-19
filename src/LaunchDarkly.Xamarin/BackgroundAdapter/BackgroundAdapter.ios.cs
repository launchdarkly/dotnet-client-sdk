using System;
using LaunchDarkly.Xamarin;
using UIKit;
using Common.Logging;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    public class BackgroundAdapter : UIApplicationDelegate, IPlatformAdapter
    {
        private IBackgroundingState _backgroundingState;
        private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundAdapter));

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
            Log.Debug("Enable Backgrounding");
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

                disposedValue = true;
            }
        }

        public new void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public override void WillEnterForeground(UIApplication application)
        {
            Log.Debug("Entering Foreground");
            _backgroundingState.ExitBackgroundAsync();
        }

        public override void DidEnterBackground(UIApplication application)
        {
            Log.Debug("Entering Background");
            _backgroundingState.EnterBackgroundAsync();
        }
    }
}
