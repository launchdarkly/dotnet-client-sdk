using System;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class UserDecorator
    {
        private readonly IDeviceInfo _deviceInfo;
        private readonly PersistentDataStoreWrapper _store;
        private readonly string _deviceName;
        private readonly string _osName;

        private string _cachedAnonUserKey = null;
        private object _anonUserKeyLock = new object();

        public UserDecorator(
            IDeviceInfo deviceInfo,
            PersistentDataStoreWrapper store
            )
        {
            _deviceInfo = deviceInfo;
            _store = store;

            // Store platform-specific values in static fields to avoid having to compute them repeatedly
            _deviceName = PlatformSpecific.UserMetadata.DeviceName;
            _osName = PlatformSpecific.UserMetadata.OSName;
        }

        public User DecorateUser(User user)
        {
            IUserBuilder buildUser = null;

            if (_deviceName != null)
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                buildUser.Custom("device", _deviceName);
            }
            if (_osName != null)
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                buildUser.Custom("os", _osName);
            }
            // If you pass in a user with a null or blank key, one will be assigned to them.
            if (String.IsNullOrEmpty(user.Key))
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                var anonUserKey = GetOrCreateAnonUserKey();
                buildUser.Key(anonUserKey).Anonymous(true);
            }
            return buildUser is null ? user : buildUser.Build();
        }

        private string GetOrCreateAnonUserKey()
        {
            lock (_anonUserKeyLock)
            {
                if (_cachedAnonUserKey != null)
                {
                    return _cachedAnonUserKey;
                }
                var deviceId = _deviceInfo.UniqueDeviceId();
                if (deviceId is null)
                {
                    deviceId = _store?.GetAnonymousUserKey();
                    if (deviceId is null)
                    {
                        deviceId = Guid.NewGuid().ToString();
                        _store?.SetAnonymousUserKey(deviceId);
                    }
                }
                _cachedAnonUserKey = deviceId;
                return deviceId;
            }
        }
    }
}
