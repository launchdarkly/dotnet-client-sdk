using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    internal sealed class MobilePollingProcessor : IMobileUpdateProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MobilePollingProcessor));

        private readonly IFeatureFlagRequestor _featureFlagRequestor;
        private readonly IFlagCacheManager _flagCacheManager;
        private readonly User _user;
        private readonly TimeSpan _pollingInterval;
        private readonly TimeSpan _initialDelay;
        private readonly TaskCompletionSource<bool> _startTask;
        private readonly TaskCompletionSource<bool> _stopTask;
        private const int UNINITIALIZED = 0;
        private const int INITIALIZED = 1;
        private int _initialized = UNINITIALIZED;
        private volatile bool _disposed;

        internal MobilePollingProcessor(IFeatureFlagRequestor featureFlagRequestor,
                                      IFlagCacheManager cacheManager,
                                      User user,
                                      TimeSpan pollingInterval,
                                      TimeSpan initialDelay)
        {
            this._featureFlagRequestor = featureFlagRequestor;
            this._flagCacheManager = cacheManager;
            this._user = user;
            this._pollingInterval = pollingInterval;
            this._initialDelay = initialDelay;
            _startTask = new TaskCompletionSource<bool>();
            _stopTask = new TaskCompletionSource<bool>();
        }

        Task<bool> IMobileUpdateProcessor.Start()
        {
            if (_pollingInterval.Equals(TimeSpan.Zero))
                throw new Exception("Timespan for polling can't be zero");

            if (_initialDelay > TimeSpan.Zero)
            {
                Log.InfoFormat("Starting LaunchDarkly PollingProcessor with interval: {0} (waiting {1} first)", _pollingInterval, _initialDelay);
            }
            else
            {
                Log.InfoFormat("Starting LaunchDarkly PollingProcessor with interval: {0}", _pollingInterval);
            }

            Task.Run(() => UpdateTaskLoopAsync());
            return _startTask.Task;
        }

        bool IMobileUpdateProcessor.Initialized()
        {
            return _initialized == INITIALIZED;
        }

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
                    var flagsDictionary = JsonUtil.DecodeJson<IDictionary<string, FeatureFlag>>(flagsAsJsonString);
                    _flagCacheManager.CacheFlagsFromService(flagsDictionary, _user);

                    // We can't use bool in CompareExchange because it is not a reference type.
                    if (Interlocked.CompareExchange(ref _initialized, INITIALIZED, UNINITIALIZED) == UNINITIALIZED)
                    {
                        _startTask.SetResult(true);
                        Log.Info("Initialized LaunchDarkly Polling Processor.");
                    }
                }
            }
            catch (UnsuccessfulResponseException ex) when (ex.StatusCode == 401)
            {
                Log.ErrorFormat("Error Updating features: '{0}'", Util.ExceptionMessage(ex));
                Log.Error("Received 401 error, no further polling requests will be made since SDK key is invalid");
                if (_initialized == UNINITIALIZED)
                {
                    _startTask.SetException(ex);
                }
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error Updating features: '{0}'", Util.ExceptionMessage(ex));
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
