using System;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Xamarin.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Xamarin.Internal.DataSources
{
    /// <summary>
    /// Manages our connection to LaunchDarkly, if any, and encapsulates all of the state that
    /// determines whether we should have a connection or not.
    /// </summary>
    /// <remarks>
    /// Whenever the state of this object is modified by <see cref="SetForceOffline(bool)"/>,
    /// <see cref="SetNetworkEnabled(bool)"/>, <see cref="SetUpdateProcessorFactory(Func{IMobileUpdateProcessor}, bool)"/>,
    /// or <see cref="Start"/>, it will decide whether to make a new connection, drop an existing
    /// connection, both, or neither. If the caller wants to know when a new connection (if any) is
    /// ready, it should <c>await</c> the returned task.
    ///
    /// The object begins in a non-started state, so regardless of what properties are set, it will not
    /// make a connection until after <see cref="Start"/> has been called.
    /// </remarks>
    internal sealed class ConnectionManager : IDisposable
    {
        private readonly Logger _log;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private bool _disposed = false;
        private bool _started = false;
        private bool _initialized = false;
        private bool _forceOffline = false;
        private bool _networkEnabled = false;
        private IMobileUpdateProcessor _updateProcessor = null;
        private Func<IMobileUpdateProcessor> _updateProcessorFactory = null;

        // Note that these properties do not have simple setter methods, because the setters all
        // need to return Tasks.

        /// <summary>
        /// True if we are in offline mode (<see cref="SetForceOffline(bool)"/> was set to true).
        /// </summary>
        public bool ForceOffline => LockUtils.WithReadLock(_lock, () => _forceOffline);

        /// <summary>
        /// True if we have been told there is network connectivity (<see cref="SetNetworkEnabled(bool)"/>
        /// was set to true).
        /// </summary>
        public bool NetworkEnabled => LockUtils.WithReadLock(_lock, () => _networkEnabled);

        /// <summary>
        /// True if we made a successful LaunchDarkly connection or do not need to make one (see
        /// <see cref="LdClient.Initialized"/>).
        /// </summary>
        public bool Initialized => LockUtils.WithReadLock(_lock, () => _initialized);

        internal ConnectionManager(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Sets whether the client should always be offline, and attempts to connect if appropriate.
        /// </summary>
        /// <remarks>
        /// Besides updating the value of the <see cref="ForceOffline"/> property, we do the
        /// following:
        /// 
        /// If <c>forceOffline</c> is true, we drop our current connection (if any), and we will not
        /// make any connections no matter what other properties are changed as long as this property is
        /// still true.
        ///
        /// If <c>forceOffline</c> is false and we already have a connection, nothing happens.
        ///
        /// If <c>forceOffline</c> is false and we have no connection, but other conditions disallow
        /// making a connection (or we do not have an update processor factory), nothing happens.
        ///
        /// If <c>forceOffline</c> is false, and we do not yet have a connection, and no other
        /// conditions disallow making a connection, and we have an update processor factory,
        /// we create an update processor and tell it to start.
        ///
        /// The returned task is immediately completed unless we are making a new connection, in which
        /// case it is completed when the update processor signals success or failure. The task yields
        /// a true result if we successfully made a connection <i>or</i> if we decided not to connect
        /// because we are in offline mode. In other words, the result is true if
        /// <see cref="Initialized"/> is true.
        /// </remarks>
        /// <param name="forceOffline">true if the client should always be offline</param>
        /// <returns>a task as described above</returns>
        public Task<bool> SetForceOffline(bool forceOffline)
        {
            return LockUtils.WithWriteLock(_lock, () =>
            {
                if (_disposed || _forceOffline == forceOffline)
                {
                    return Task.FromResult(false);
                }
                _forceOffline = forceOffline;
                _log.Info("Offline mode is now {0}", forceOffline);
                return OpenOrCloseConnectionIfNecessary(); // not awaiting
            });
        }

        /// <summary>
        /// Sets whether we should be able to make network connections, and attempts to connect if appropriate.
        /// </summary>
        /// <remarks>
        /// Besides updating the value of the <see cref="NetworkEnabled"/> property, we do the
        /// following:
        /// 
        /// If <c>networkEnabled</c> is false, we drop our current connection (if any), and we will not
        /// make any connections no matter what other properties are changed as long as this property is
        /// still true.
        ///
        /// If <c>networkEnabled</c> is true and we already have a connection, nothing happens.
        ///
        /// If <c>networkEnabled</c> is true and we have no connection, but other conditions disallow
        /// making a connection (or we do not have an update processor factory), nothing happens.
        ///
        /// If <c>networkEnabled</c> is true, and we do not yet have a connection, and no other
        /// conditions disallow making a connection, and we have an update processor factory,
        /// we create an update processor and tell it to start.
        ///
        /// The returned task is immediately completed unless we are making a new connection, in which
        /// case it is completed when the update processor signals success or failure. The task yields
        /// a true result if we successfully made a connection <i>or</i> if we decided not to connect
        /// because we are in offline mode. In other words, the result is true if
        /// <see cref="Initialized"/> is true.
        /// </remarks>
        /// <param name="networkEnabled">true if we think we can make network connections</param>
        /// <returns>a task as described above</returns>
        public Task<bool> SetNetworkEnabled(bool networkEnabled)
        {
            return LockUtils.WithWriteLock(_lock, () =>
            {
                if (_disposed || _networkEnabled == networkEnabled)
                {
                    return Task.FromResult(false);
                }
                _networkEnabled = networkEnabled;
                _log.Info("Network availability is now {0}", networkEnabled);
                return OpenOrCloseConnectionIfNecessary(); // not awaiting
            });
        }

        /// <summary>
        /// Sets the factory function for creating an update processor, and attempts to connect if
        /// appropriate.
        /// </summary>
        /// <remarks>
        /// The factory function encapsulates all the information that <see cref="LdClient"/> takes into
        /// account when making a connection, i.e. whether we are in streaming or polling mode, the
        /// polling interval, and the curent user. <c>ConnectionManager</c> itself has no knowledge of
        /// those things.
        /// 
        /// Besides updating the private factory function field, we do the following:
        /// 
        /// If the function is null, we drop our current connection (if any), and we will not make
        /// any connections no matter what other properties are changed as long as it is still null.
        ///
        /// If it is non-null and we already have the same factory function, nothing happens.
        ///
        /// If it is non-null and we do not already have the same factory function, but other conditions
        /// disallow making a connection, nothing happens.
        ///
        /// If it is non-null and we do not already have the same factory function, and no other
        /// conditions disallow making a connection, we create an update processor and tell it to start.
        /// In this case, we also reset <see cref="Initialized"/> to false if <c>resetInitialized</c> is
        /// true.
        ///
        /// The returned task is immediately completed unless we are making a new connection, in which
        /// case it is completed when the update processor signals success or failure. The task yields
        /// a true result if we successfully made a connection <i>or</i> if we decided not to connect
        /// because we are in offline mode. In other words, the result is true if
        /// <see cref="Initialized"/> is true.
        /// </remarks>
        /// <param name="updateProcessorFactory">a factory function or null</param>
        /// <param name="resetInitialized">true if we should reset the initialized state (e.g. if we
        /// are switching users</param>
        /// <returns>a task as described above</returns>
        public Task<bool> SetUpdateProcessorFactory(Func<IMobileUpdateProcessor> updateProcessorFactory, bool resetInitialized)
        {
            return LockUtils.WithWriteLock(_lock, () =>
            {
                if (_disposed || _updateProcessorFactory == updateProcessorFactory)
                {
                    return Task.FromResult(false);
                }
                _updateProcessorFactory = updateProcessorFactory;
                _updateProcessor?.Dispose();
                _updateProcessor = null;
                if (resetInitialized)
                {
                    _initialized = false;
                }
                return OpenOrCloseConnectionIfNecessary(); // not awaiting
            });
        }

        /// <summary>
        /// Tells the <c>ConnectionManager</c> that it can go ahead and connect if appropriate.
        /// </summary>
        /// <returns>a task which will yield true if this method results in a successful connection, or
        /// if we are in offline mode and don't need to make a connection</returns>
        public Task<bool> Start()
        {
            return LockUtils.WithWriteLock(_lock, () =>
            {
                if (_started)
                {
                    return Task.FromResult(_initialized);
                }
                _started = true;
                return OpenOrCloseConnectionIfNecessary(); // not awaiting
            });
        }

        public void Dispose()
        {
            IMobileUpdateProcessor processor = null;
            LockUtils.WithWriteLock(_lock, () =>
            {
                if (_disposed)
                {
                    return;
                }
                processor = _updateProcessor;
                _updateProcessor = null;
                _updateProcessorFactory = null;
                _disposed = true;
            });
            processor?.Dispose();
        }

        // This method is called while _lock is being held. If we're starting up a new connection, we do
        // *not* wait for it to succeed; we return a Task that will be completed once it succeeds. In all
        // other cases we return an immediately-completed Task.

        private Task<bool> OpenOrCloseConnectionIfNecessary()
        {
            if (!_started)
            {
                return Task.FromResult(false);
            }
            if (_networkEnabled && !_forceOffline)
            {
                if (_updateProcessor == null && _updateProcessorFactory != null)
                {
                    _updateProcessor = _updateProcessorFactory();
                    return _updateProcessor.Start()
                        .ContinueWith(SetInitializedIfUpdateProcessorStartedSuccessfully);
                }
            }
            else
            {
                _updateProcessor?.Dispose();
                _updateProcessor = null;
                _initialized = true;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // When this method is called, we are no longer holding the lock.

        private bool SetInitializedIfUpdateProcessorStartedSuccessfully(Task<bool> task)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    // Don't let exceptions from the update processor propagate up into the SDK. Just say we didn't initialize.
                    LogHelpers.LogException(_log, "Failed to initialize LaunchDarkly connection", task.Exception);
                    return false;
                }
                var success = task.Result;
                if (success)
                {
                    LockUtils.WithWriteLock(_lock, () =>
                    {
                        _initialized = true;
                    });
                    return true;
                }
            }
            return false;
        }
    }
}
