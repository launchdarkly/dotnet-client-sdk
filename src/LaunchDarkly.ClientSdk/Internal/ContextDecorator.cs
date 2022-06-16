using System;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class ContextDecorator
    {
        private readonly PersistentDataStoreWrapper _store;

        private string _cachedAnonUserKey = null;
        private object _anonUserKeyLock = new object();

        public ContextDecorator(
            PersistentDataStoreWrapper store
            )
        {
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
                var uniqueId = _store?.GetAnonymousUserKey();
                if (uniqueId is null)
                {
                    uniqueId = Guid.NewGuid().ToString();
                    _store?.SetAnonymousUserKey(uniqueId);
                }
                _cachedAnonUserKey = uniqueId;
                return uniqueId;
            }
        }
    }
}
