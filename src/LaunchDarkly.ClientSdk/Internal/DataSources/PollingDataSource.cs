using System;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Http;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal sealed class PollingDataSource : IDataSource
    {
        private readonly IFeatureFlagRequestor _featureFlagRequestor;
        private readonly IDataSourceUpdateSink _updateSink;
        private readonly User _user;
        private readonly TimeSpan _pollingInterval;
        private readonly TimeSpan _initialDelay;
        private readonly Logger _log;
        private readonly TaskCompletionSource<bool> _startTask;
        private readonly TaskCompletionSource<bool> _stopTask;
        private readonly AtomicBoolean _initialized = new AtomicBoolean(false);
        private volatile bool _disposed;

        internal PollingDataSource(
            IDataSourceUpdateSink updateSink,
            User user,
            IFeatureFlagRequestor featureFlagRequestor,
            TimeSpan pollingInterval,
            TimeSpan initialDelay,
            Logger log)
        {
            this._featureFlagRequestor = featureFlagRequestor;
            this._updateSink = updateSink;
            this._user = user;
            this._pollingInterval = pollingInterval;
            this._initialDelay = initialDelay;
            this._log = log;
            _startTask = new TaskCompletionSource<bool>();
            _stopTask = new TaskCompletionSource<bool>();
        }

        public Task<bool> Start()
        {
            if (_pollingInterval.Equals(TimeSpan.Zero))
                throw new Exception("Timespan for polling can't be zero");

            if (_initialDelay > TimeSpan.Zero)
            {
                _log.Info("Starting LaunchDarkly PollingProcessor with interval: {0} (waiting {1} first)", _pollingInterval, _initialDelay);
            }
            else
            {
                _log.Info("Starting LaunchDarkly PollingProcessor with interval: {0}", _pollingInterval);
            }

            Task.Run(() => UpdateTaskLoopAsync());
            return _startTask.Task;
        }

        public bool Initialized => _initialized.Get();

        private async Task UpdateTaskLoopAsync()
        {
            if (_initialDelay > TimeSpan.Zero)
            {
                await Task.Delay(_initialDelay);
            }
            while (!_disposed)
            {
                await UpdateTaskAsync();
                await Task.Delay(_pollingInterval);
            }
        }

        private async Task UpdateTaskAsync()
        {
            try
            {
                var response = await _featureFlagRequestor.FeatureFlagsAsync();
                if (response.statusCode == 200)
                {
                    var flagsAsJsonString = response.jsonResponse;
                    var allData = DataModelSerialization.DeserializeV1Schema(flagsAsJsonString);
                    _updateSink.Init(allData, _user);

                    if (_initialized.GetAndSet(true) == false)
                    {
                        _startTask.SetResult(true);
                        _log.Info("Initialized LaunchDarkly Polling Processor.");
                    }
                }
            }
            catch (UnsuccessfulResponseException ex) when (ex.StatusCode == 401)
            {
                _log.Error("Error Updating features: '{0}'", LogValues.ExceptionSummary(ex));
                _log.Error("Received 401 error, no further polling requests will be made since SDK key is invalid");
                if (!_initialized.Get())
                {
                    _startTask.SetException(ex);
                }
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                LogHelpers.LogException(_log, "Error updating features", PlatformSpecific.Http.TranslateHttpException(ex));
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Log that the polling has stopped
                _disposed = true;
                _featureFlagRequestor.Dispose();
            }
        }
    }
}
