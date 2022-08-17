using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal class ContextDecorator
    {
        private readonly PersistentDataStoreWrapper _store;

        private Dictionary<ContextKind, string> _cachedGeneratedKey = new Dictionary<ContextKind, string>();
        private object _generatedKeyLock = new object();

        public ContextDecorator(
            PersistentDataStoreWrapper store
            )
        {
            _store = store;
        }

        public Context DecorateContext(Context context)
        {
            if (context.Multiple)
            {
                if (context.MultiKindContexts.Any(ContextNeedsGeneratedKey))
                {
                    var builder = Context.MultiBuilder();
                    foreach (var c in context.MultiKindContexts)
                    {
                        builder.Add(ContextNeedsGeneratedKey(c) ? SingleKindContextWithGeneratedKey(c) : c);
                    }
                    return builder.Build();
                }
            }
            else if (ContextNeedsGeneratedKey(context))
            {
                return SingleKindContextWithGeneratedKey(context);
            }
            return context;
        }

        private Context SingleKindContextWithGeneratedKey(Context context) =>
            Context.BuilderFromContext(context).Key(GetOrCreateAutoContextKey(context.Kind)).Build();

        private bool ContextNeedsGeneratedKey(Context context) =>
            context.Anonymous && context.Key == Constants.AutoKeyMagicValue;
        // The use of a magic constant here is temporary because the current implementation of Context doesn't allow a null key

        private string GetOrCreateAutoContextKey(ContextKind contextKind)
        {
            lock (_generatedKeyLock)
            {
                if (_cachedGeneratedKey.TryGetValue(contextKind, out var key))
                {
                    return key;
                }
                var uniqueId = _store?.GetGeneratedContextKey(contextKind);
                if (uniqueId is null)
                {
                    uniqueId = Guid.NewGuid().ToString();
                    _store?.SetGeneratedContextKey(contextKind, uniqueId);
                }
                _cachedGeneratedKey[contextKind] = uniqueId;
                return uniqueId;
            }
        }
    }
}
