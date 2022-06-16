using System;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class ContextDecorator
    {
        private readonly IDeviceInfo _deviceInfo;
        private readonly PersistentDataStoreWrapper _store;

        private string _cachedAnonUserKey = null;
        private object _anonUserKeyLock = new object();

        public ContextDecorator(
            IDeviceInfo deviceInfo,
            PersistentDataStoreWrapper store
            )
        {
            _deviceInfo = deviceInfo;
            _store = store;
        }

        public Context DecorateContext(Context context)
        {
            if (IsAutoContext(context))
            {
                var anonKey = GetOrCreateAutoContextKey();
                return Context.BuilderFromContext(context).Key(anonKey).Transient(true).Build();
            }
            return context;
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
