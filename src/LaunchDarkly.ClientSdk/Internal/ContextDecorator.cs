using System;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class ContextDecorator
    {
        private readonly IDeviceInfo _deviceInfo;
        private readonly PersistentDataStoreWrapper _store;
        private readonly string _deviceName;
        private readonly string _osName;

        private string _cachedAnonUserKey = null;
        private object _anonUserKeyLock = new object();

        public ContextDecorator(
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

            Func<ContextBuilder> lazyBuilder = () =>
            {
                if (builder is null)
                {
                    builder = Context.BuilderFromContext(context);
                }
                return builder;
            };

            if (_deviceName != null)
            {
                lazyBuilder().Set("device", _deviceName);
            }
            if (_osName != null)
            {
                lazyBuilder().Set("os", _osName);
            }
            if (IsAutoContext(context))
            {
                var anonKey = GetOrCreateAutoContextKey();
                lazyBuilder().Key(anonKey).Transient(true);
            }
            return builder is null ? context : builder.Build();
        }

        private bool IsAutoContext(Context context) =>
            context.Transient && context.Key == Constants.AutoKeyMagicValue;
        // The use of a magic constant here is temporary because the current implementation of Context doesn't allow a null key

        private string GetOrCreateAutoContextKey()
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
