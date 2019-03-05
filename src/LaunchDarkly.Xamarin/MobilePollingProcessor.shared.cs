using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin
{
    internal class MobilePollingProcessor : IMobileUpdateProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MobilePollingProcessor));

        private readonly IFeatureFlagRequestor _featureFlagRequestor;
        private readonly IFlagCacheManager _flagCacheManager;
        private readonly User user;
        private readonly TimeSpan pollingInterval;
        private readonly TaskCompletionSource<bool> _startTask;
        private readonly TaskCompletionSource<bool> _stopTask;
        private const int UNINITIALIZED = 0;
        private const int INITIALIZED = 1;
        private int _initialized = UNINITIALIZED;
        private volatile bool _disposed;

        internal MobilePollingProcessor(IFeatureFlagRequestor featureFlagRequestor,
                                      IFlagCacheManager cacheManager,
                                      User user,
                                      TimeSpan pollingInterval)
        {
            this._featureFlagRequestor = featureFlagRequestor;
            this._flagCacheManager = cacheManager;
            this.user = user;
            this.pollingInterval = pollingInterval;
            _startTask = new TaskCompletionSource<bool>();
            _stopTask = new TaskCompletionSource<bool>();
        }

        Task<bool> IMobileUpdateProcessor.Start()
        {
            if (pollingInterval.Equals(TimeSpan.Zero))
                throw new Exception("Timespan for polling can't be zero");

            Log.InfoFormat("Starting LaunchDarkly PollingProcessor with interval: {0} milliseconds",
                           pollingInterval);
            
            Task.Run(() => UpdateTaskLoopAsync());
            return _startTask.Task;
        }

        bool IMobileUpdateProcessor.Initialized()
        {
            return _initialized == INITIALIZED;
        }

        public async Task PingAndWait()
        {
            await UpdateTaskAsync();
        }

        private async Task UpdateTaskLoopAsync()
        {
            while (!_disposed)
            {
                await UpdateTaskAsync();
                await Task.Delay(pollingInterval);
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
                    var flagsDictionary = JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(flagsAsJsonString);
                    _flagCacheManager.CacheFlagsFromService(flagsDictionary, user);

                    //We can't use bool in CompareExchange because it is not a reference type.
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
                Log.ErrorFormat("Error Updating features: '{0}'",
                    ex,
                    Util.ExceptionMessage(ex));
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
