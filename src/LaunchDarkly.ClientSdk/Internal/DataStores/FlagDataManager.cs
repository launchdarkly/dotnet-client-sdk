using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    /// <summary>
    /// The component that maintains the state of last known flag values, and manages
    /// persistent storage if enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state of the <see cref="FlagDataManager"/> consists of all the
    /// <see cref="DataModel.FeatureFlag"/>s for a specific user, plus optionally persistent
    /// storage for some number of other users. 
    /// </para>
    /// <para>
    /// This is not a pluggable component - there can only be one implementation. The only
    /// piece of behavior that is platform-dependent and customizable is the implementation
    /// of persistent storage, which is represented by the <see cref="IPersistentDataStore"/>
    /// interface.
    /// </para>
    /// </remarks>
    internal sealed class FlagDataManager : IDisposable
    {
        private readonly int _maxCachedUsers;
        private readonly PersistentDataStoreWrapper _persistentStore;
        private readonly object _writerLock = new object();
        private readonly Logger _log;

        private volatile ImmutableDictionary<string, ItemDescriptor> _flags =
            ImmutableDictionary<string, ItemDescriptor>.Empty;
        private volatile UserIndex _storeIndex = null;
        private volatile string _currentUserId = null;

        public PersistentDataStoreWrapper PersistentStore => _persistentStore;

        public FlagDataManager(
            string mobileKey,
            PersistenceConfiguration persistenceConfiguration,
            Logger log
            )
        {
            _log = log;

            if (persistenceConfiguration is null || persistenceConfiguration.MaxCachedUsers == 0
                || persistenceConfiguration.PersistentDataStore is NullPersistentDataStore)
            {
                _persistentStore = null;
                _maxCachedUsers = 0;
            }
            else
            {
                _persistentStore = new PersistentDataStoreWrapper(
                    persistenceConfiguration.PersistentDataStore,
                    mobileKey,
                    log
                    );
                _maxCachedUsers = persistenceConfiguration.MaxCachedUsers;
                _storeIndex = _persistentStore.GetIndex();
            }
        }

        /// <summary>
        /// Attempts to retrieve cached data for the specified user, if any. This does not
        /// affect the current user/flags state.
        /// </summary>
        /// <param name="user">a user</param>
        /// <returns>that user's data from the persistent store, or null if none</returns>
        public FullDataSet? GetCachedData(User user) =>
            _persistentStore is null ? null : _persistentStore.GetUserData(UserIdFor(user));

        /// <summary>
        /// Replaces the current flag data and updates the current-user state, optionally
        /// updating persistent storage as well.
        /// </summary>
        /// <param name="user">the user who should become the current user</param>
        /// <param name="data">the full flag data</param>
        /// <param name="updatePersistentStorage">true to also update the flag data in
        /// persistent storage (if persistent storage is enabled)</param>
        public void Init(User user, FullDataSet data, bool updatePersistentStorage)
        {
            var newFlags = data.Items.ToImmutableDictionary();
            IEnumerable<string> removedUserIds = null;
            var userId = UserIdFor(user);
            var updatedIndex = _storeIndex;

            lock (_writerLock)
            {
                _flags = newFlags;

                if (_storeIndex != null)
                {
                    updatedIndex = _storeIndex.UpdateTimestamp(userId, UnixMillisecondTime.Now)
                        .Prune(_maxCachedUsers, out removedUserIds);
                    _storeIndex = updatedIndex;
                }

                _currentUserId = userId;
            }

            if (_persistentStore != null)
            {
                try
                {
                    if (removedUserIds != null)
                    {
                        foreach (var oldId in removedUserIds)
                        {
                            _persistentStore.RemoveUserData(oldId);
                        }
                    }
                    if (updatePersistentStorage)
                    {
                        _persistentStore.SetUserData(userId, data);
                    }
                    _persistentStore.SetIndex(updatedIndex);
                }
                catch (Exception e)
                {
                    LogHelpers.LogException(_log, "Failed to write to persistent store", e);
                }
            }
        }

        /// <summary>
        /// Attempts to get a flag by key from the current flags. This always uses the
        /// in-memory cache, not persistent storage.
        /// </summary>
        /// <param name="key">the flag key</param>
        /// <returns>the flag descriptor, or null if not found</returns>
        public ItemDescriptor? Get(string key) =>
            _flags.TryGetValue(key, out var item) ? item : (ItemDescriptor?)null;

        /// <summary>
        /// Returns all current flags. This always uses the in-memory cache, not
        /// persistent storage.
        /// </summary>
        /// <returns>the data set</returns>
        public FullDataSet? GetAll() =>
            new FullDataSet(_flags);

        /// <summary>
        /// Attempts to update or insert a flag.
        /// </summary>
        /// <remarks>
        /// This implements the usual versioning logic for updates: the update only succeeds if
        /// <c>data.Version</c> is greater than the version of any current data for the same key.
        /// If successful, and if persistent storage is enabled, it also updates persistent storage.
        /// Therefore <see cref="IPersistentDataStore"/> implementations do not need to implement
        /// their own version checking.
        /// </remarks>
        /// <param name="key">the flag key</param>
        /// <param name="data">the updated flag data, or a tombstone for a deleted flag</param>
        /// <returns>true if the update was done; false if it was not done due to a too-low
        /// version number</returns>
        public bool Upsert(string key, ItemDescriptor data)
        {
            var updatedFlags = _flags;
            string userId = null;

            lock (_writerLock)
            {
                if (_flags.TryGetValue(key, out var oldItem) && oldItem.Version >= data.Version)
                {
                    return false;
                }
                updatedFlags = _flags.SetItem(key, data);
                _flags = updatedFlags;
                userId = _currentUserId;
            }

            _persistentStore?.SetUserData(userId, new FullDataSet(updatedFlags));
            return true;
        }

        public void Dispose() => _persistentStore?.Dispose();

        internal static string UserIdFor(User user) => Base64.Sha256Hash(user.Key);
    }
}
