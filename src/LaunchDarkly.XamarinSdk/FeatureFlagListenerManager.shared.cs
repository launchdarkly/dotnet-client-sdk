using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    internal class FeatureFlagListenerManager : IFeatureFlagListenerManager, IFlagListenerUpdater
    {
        private readonly IDictionary<string, List<IFeatureFlagListener>> _map = 
            new Dictionary<string, List<IFeatureFlagListener>>();

        private ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public void RegisterListener(IFeatureFlagListener listener, string flagKey)
        {
            readWriteLock.EnterWriteLock();
            try
            {
                if (!_map.ContainsKey(flagKey))
                {
                    _map[flagKey] = new List<IFeatureFlagListener>();
                }

                var list = _map[flagKey];
                list.Add(listener);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public void UnregisterListener(IFeatureFlagListener listener, string flagKey)
        {
            readWriteLock.EnterWriteLock();
            try
            {
                if (_map.ContainsKey(flagKey))
                {
                    var listOfListeners = _map[flagKey];
                    listOfListeners.Remove(listener);
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public void FlagWasDeleted(string flagKey)
        {
            readWriteLock.EnterReadLock();
            try
            {
                if (_map.ContainsKey(flagKey))
                {
                    var listeners = _map[flagKey];
                    listeners.ForEach((listener) => listener.FeatureFlagDeleted(flagKey));
                }
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public void FlagWasUpdated(string flagKey, JToken value)
        {
            readWriteLock.EnterReadLock();
            try
            {
                if (_map.ContainsKey(flagKey))
                {
                    var listeners = _map[flagKey];
                    listeners.ForEach((listener) => listener.FeatureFlagChanged(flagKey, value));
                }
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }
    }
}
