using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Logging;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    internal class FeatureFlagListenerManager : IFeatureFlagListenerManager, IFlagListenerUpdater
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FeatureFlagListenerManager));

        private readonly IDictionary<string, List<IFeatureFlagListener>> _map = 
            new Dictionary<string, List<IFeatureFlagListener>>();

        private ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public void RegisterListener(IFeatureFlagListener listener, string flagKey)
        {
            readWriteLock.EnterWriteLock();
            try
            {
                if (!_map.TryGetValue(flagKey, out var list))
                {
                    list = new List<IFeatureFlagListener>();
                    _map[flagKey] = list;
                }
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
                if (_map.TryGetValue(flagKey, out var list))
                {
                    list.Remove(listener);
                }
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        public bool IsListenerRegistered(IFeatureFlagListener listener, string flagKey)
        {
            readWriteLock.EnterReadLock();
            try
            {
                if (_map.TryGetValue(flagKey, out var list))
                {
                    return list.Contains(listener);
                }
                return false;
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public void FlagWasDeleted(string flagKey)
        {
            FireAction(flagKey, (listener) => listener.FeatureFlagDeleted(flagKey));
        }

        public void FlagWasUpdated(string flagKey, JToken value)
        {
            FireAction(flagKey, (listener) => listener.FeatureFlagChanged(flagKey, value));
        }

        private void FireAction(string flagKey, Action<IFeatureFlagListener> a)
        {
            IFeatureFlagListener[] listeners = null;
            readWriteLock.EnterReadLock();
            try
            {
                if (_map.TryGetValue(flagKey, out var mutableListOfListeners))
                {
                    listeners = mutableListOfListeners.ToArray(); // this copies the list
                }
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
            if (listeners != null)
            {
                foreach (var l in listeners)
                {
                    // Note, this schedules the listeners separately, rather than scheduling a single task that runs them all.
                    PlatformSpecific.AsyncScheduler.ScheduleAction(() =>
                    {
                        try
                        {
                            a(l);
                        }
                        catch (Exception e)
                        {
                            Log.Warn("Unexpected exception from feature flag listener", e);
                        }
                    });
                }
            }
        }
    }
}
