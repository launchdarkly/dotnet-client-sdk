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

        public Context DecorateContext(Context context)
        {
            ContextBuilder builder = null;

            if (_deviceName != null)
            {
                if (builder is null)
                {
                    builder = Context.BuilderFromContext(context);
                }
                builder.Set("device", _deviceName);
            }
            if (_osName != null)
            {
                if (builder is null)
                {
                    builder = Context.BuilderFromContext(context);
                }
                builder.Set("os", _osName);
            }
            // The use of a magic constant here is temporary because the current implementation of Context doesn't allow a null key
            if (context.Key == Constants.AutoKeyMagicValue)
            {
                if (builder is null)
                {
                    builder = Context.BuilderFromContext(context);
                }
                var anonUserKey = GetOrCreateAnonUserKey();
                builder.Key(anonUserKey).Transient(true);
            }
            return builder is null ? context : builder.Build();
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
