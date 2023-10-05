using System;
using System.Collections.Generic;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    /// <summary>
    /// TODO
    /// </summary>
    internal class AutoEnvContextDecorator
    {
        private const string LD_APPLICATION_KIND = "ld_application";
        private const string LD_DEVICE_KIND = "ld_device";
        private const string ATTR_ID = "id";
        private const string ATTR_NAME = "name";
        private const string ATTR_VERSION = "version";
        private const string ATTR_VERSION_NAME = "versionName";
        private const string ATTR_MANUFACTURER = "manufacturer";
        private const string ATTR_MODEL = "model";
        private const string ATTR_LOCALE = "locale";
        private const string ATTR_OS = "os";
        private const string ATTR_FAMILY = "family";
        private const string ENV_ATTRIBUTES_VERSION = "envAttributesVersion";
        private const string SPEC_VERSION = "1.0";

        private readonly PersistentDataStoreWrapper _persistentData;
        private readonly IEnvironmentReporter _environmentReporter;
        private readonly Logger _logger;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="persistentData"></param>
        /// <param name="environmentReporter"></param>
        /// <param name="logger"></param>
        public AutoEnvContextDecorator(
            PersistentDataStoreWrapper persistentData,
            IEnvironmentReporter environmentReporter,
            Logger logger)
        {
            _persistentData = persistentData;
            _environmentReporter = environmentReporter;
            _logger = logger;
        }

        public Context DecorateContext(Context context)
        {
            var builder = Context.MultiBuilder();
            builder.Add(context);

            foreach (ContextRecipe recipe in MakeRecipeList())
            {
                if (!context.TryGetContextByKind(recipe.Kind, out _))
                {
                    // only add contexts for recipe Kinds not already in context to avoid overwriting data.
                    builder.Add(MakeLdContextFromRecipe(recipe));
                }
                else
                {
                    _logger.Warn("Unable to automatically add environment attributes for kind:{0}. {1} already exists.",
                        recipe.Kind, recipe.Kind);
                }
            }

            return builder.Build();
        }

        private class ContextRecipe
        {
            public ContextKind Kind;
            public Func<string> KeyCallable;
            public Dictionary<string, Func<LdValue>> AttributeCallables;

            public ContextRecipe(ContextKind kind, Func<string> keyCallable,
                Dictionary<string, Func<LdValue>> attributeCallables)
            {
                this.Kind = kind;
                this.KeyCallable = keyCallable;
                this.AttributeCallables = attributeCallables;
            }
        }

        private static Context MakeLdContextFromRecipe(ContextRecipe recipe)
        {
            var builder = Context.Builder(recipe.Kind, recipe.KeyCallable.Invoke());
            foreach (var entry in recipe.AttributeCallables)
            {
                builder.Set(entry.Key, entry.Value.Invoke());
            }

            return builder.Build();
        }

        private List<ContextRecipe> MakeRecipeList()
        {
            var ldApplicationKind = ContextKind.Of(LD_APPLICATION_KIND);
            var applicationCallables = new Dictionary<string, Func<LdValue>>
            {
                { ENV_ATTRIBUTES_VERSION, () => LdValue.Of(SPEC_VERSION) },
                { ATTR_ID, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationId) },
                { ATTR_NAME, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationName) },
                { ATTR_VERSION, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationVersion) },
                { ATTR_VERSION_NAME, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationVersionName) }
            };

            // TODO: missing locale in environment reporter implementation
            // applicationCallables.Add(ATTR_LOCALE, () => LDValue.Of(environmentReporter.GetLocale()));

            var ldDeviceKind = ContextKind.Of(LD_DEVICE_KIND);
            var deviceCallables = new Dictionary<string, Func<LdValue>>
            {
                { ENV_ATTRIBUTES_VERSION, () => LdValue.Of(SPEC_VERSION) },
                { ATTR_MANUFACTURER, () => LdValue.Of(_environmentReporter.DeviceInfo.Manufacturer) },
                { ATTR_MODEL, () => LdValue.Of(_environmentReporter.DeviceInfo.Model) },
                {
                    ATTR_OS, () =>
                        LdValue.BuildObject()
                            .Add(ATTR_FAMILY, _environmentReporter.OsInfo.Family)
                            .Add(ATTR_NAME, _environmentReporter.OsInfo.Name)
                            .Add(ATTR_VERSION, _environmentReporter.OsInfo.Version)
                            .Build()
                }
            };

            return new List<ContextRecipe>
            {
                new ContextRecipe(
                    ldApplicationKind,
                    () => Base64.UrlSafeSha256Hash(
                        _environmentReporter.ApplicationInfo.ApplicationId ?? ""
                    ),
                    applicationCallables
                ),
                new ContextRecipe(
                    ldDeviceKind,
                    () => GetOrCreateAutoContextKey(_persistentData, ldDeviceKind),
                    deviceCallables
                )
            };
        }

        // TODO: commonize this with duplicate implementation in AnonymousKeyContextDecorator
        private string GetOrCreateAutoContextKey(PersistentDataStoreWrapper store, ContextKind contextKind)
        {
            var uniqueId = store.GetGeneratedContextKey(contextKind);
            if (uniqueId is null)
            {
                uniqueId = Guid.NewGuid().ToString();
                store.SetGeneratedContextKey(contextKind, uniqueId);
            }
            return uniqueId;
        }
    }
}
