using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Concurrent;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    /// <summary>
    /// A facade over some implementation of <see cref="IPersistentDataStore"/>, which adds
    /// behavior that should be the same for all implementations, such as the specific data
    /// keys we use, the logging of errors, and how data is serialized and deserialized. This
    /// allows FlagDataManager (and other parts of the SDK that may need to access persistent
    /// storage) to be written in a clearer way without embedding many implementation details.
    /// </summary>
    internal sealed class PersistentDataStoreWrapper : IDisposable
    {
        private const string NamespacePrefix = "LaunchDarkly";
        private const string GlobalAnonUserKey = "anonUser";
        private const string EnvironmentMetadataKey = "index";
        private const string EnvironmentUserDataKeyPrefix = "flags:";

        private readonly IPersistentDataStore _persistentStore;
        private readonly string _globalNamespace;
        private readonly string _environmentNamespace;

        private readonly Logger _log;
        private readonly object _storeLock = new object();
        private readonly AtomicBoolean _loggedStorageError = new AtomicBoolean(false);

        public PersistentDataStoreWrapper(
            IPersistentDataStore persistentStore,
            string mobileKey,
            Logger log
            )
        {
            _persistentStore = persistentStore;
            _log = log;

            _globalNamespace = NamespacePrefix;
            _environmentNamespace = NamespacePrefix + ":" + Base64.Sha256Hash(mobileKey);
        }

        public FullDataSet? GetUserData(string userId)
        {
            var serializedData = HandleErrorsAndLock(() => _persistentStore.GetValue(_environmentNamespace, KeyForUserId(userId)));
            if (serializedData is null)
            {
                return null;
            }
            try
            {
                return DataModelSerialization.DeserializeAll(serializedData);
            }
            catch (Exception e)
            {
                LogHelpers.LogException(_log, "Failed to deserialize data from persistent store", e);
                return null;
            }
        }

        public void SetUserData(string userId, FullDataSet data) =>
            HandleErrorsAndLock(() => _persistentStore.SetValue(_environmentNamespace, KeyForUserId(userId),
                DataModelSerialization.SerializeAll(data)));

        public void RemoveUserData(string userId) =>
            HandleErrorsAndLock(() => _persistentStore.SetValue(_environmentNamespace, KeyForUserId(userId), null));

        public UserIndex GetIndex()
        {
            string data = HandleErrorsAndLock(() => _persistentStore.GetValue(_environmentNamespace, EnvironmentMetadataKey));
            if (data is null)
            {
                return new UserIndex();
            }
            try
            {
                return UserIndex.Deserialize(data);
            }
            catch (Exception)
            {
                _log.Warn("Discarding invalid data from persistent store index");
                return new UserIndex();
            }
        }

        public void SetIndex(UserIndex index) =>
            HandleErrorsAndLock(() => _persistentStore.SetValue(_environmentNamespace, EnvironmentMetadataKey, index.Serialize()));

        public string GetAnonymousUserKey() =>
            HandleErrorsAndLock(() => _persistentStore.GetValue(_globalNamespace, GlobalAnonUserKey));

        public void SetAnonymousUserKey(string value) =>
            HandleErrorsAndLock(() => _persistentStore.SetValue(_globalNamespace, GlobalAnonUserKey, value));

        public void Dispose() =>
            _persistentStore.Dispose();

        private static string KeyForUserId(string userId) => EnvironmentUserDataKeyPrefix + userId;

        private void MaybeLogStoreError(Exception e)
        {
            if (!_loggedStorageError.GetAndSet(true))
            {
                LogHelpers.LogException(_log, "Failure in persistent data store", e);
            }
        }

        private T HandleErrorsAndLock<T>(Func<T> action)
        {
            try
            {
                lock (_storeLock)
                {
                    return action();
                }
            }
            catch (Exception e)
            {
                MaybeLogStoreError(e);
                return default(T);
            }
        }

        private void HandleErrorsAndLock(Action action)
        {
            _ = HandleErrorsAndLock<bool>(() => { action(); return true; });
        }
    }
}
