using System;
using LaunchDarkly.Xamarin;
using UIKit;

namespace LaunchDarkly.Xamarin.BackgroundAdapter
{
    public class BackgroundAdapter : UIApplicationDelegate, IPlatformAdapter
    {
        private IBackgroundingState _backgroundingState;

        public void EnableBackgrounding(IBackgroundingState backgroundingState)
        {
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
            _backgroundingState.ExitBackgroundAsync();
        }

        public override void DidEnterBackground(UIApplication application)
        {
            _backgroundingState.EnterBackgroundAsync();
        }
    }
}
