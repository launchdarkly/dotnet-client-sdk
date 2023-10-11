using System;
using System.Collections.Generic;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    /// <summary>
    /// This class can decorate a context by adding additional contexts to it using auto environment attributes
    /// gotten via the provided <see cref="IEnvironmentReporter"/>.
    /// </summary>
    internal class AutoEnvContextDecorator
    {
        internal const string LD_APPLICATION_KIND = "ld_application";
        internal const string LD_DEVICE_KIND = "ld_device";
        internal const string ATTR_ID = "id";
        internal const string ATTR_NAME = "name";
        internal const string ATTR_VERSION = "version";
        internal const string ATTR_VERSION_NAME = "versionName";
        internal const string ATTR_MANUFACTURER = "manufacturer";
        internal const string ATTR_MODEL = "model";
        internal const string ATTR_LOCALE = "locale";
        internal const string ATTR_OS = "os";
        internal const string ATTR_FAMILY = "family";
        internal const string ENV_ATTRIBUTES_VERSION = "envAttributesVersion";
        internal const string SPEC_VERSION = "1.0";

        private readonly PersistentDataStoreWrapper _persistentData;
        private readonly IEnvironmentReporter _environmentReporter;
        private readonly Logger _logger;

        /// <summary>
        /// Creates a <see cref="AutoEnvContextDecorator"/>.
        /// </summary>
        /// <param name="persistentData">the data source that will be used for retrieving/saving information related
        /// to the generated contexts.  Example data includes the stable key of the ld_device context kind.</param>
        /// <param name="environmentReporter">the environment reporter that will be used to source the
        /// environment attributes</param>
        /// <param name="logger">the humble logger</param>
        public AutoEnvContextDecorator(
            PersistentDataStoreWrapper persistentData,
            IEnvironmentReporter environmentReporter,
            Logger logger)
        {
            _persistentData = persistentData;
            _environmentReporter = environmentReporter;
            _logger = logger;
        }

        /// <summary>
        /// Decorates the provided context with additional contexts containing environment attributes.
        /// </summary>
        /// <param name="context">the context to be decorated</param>
        /// <returns>the decorated context</returns>
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
                { ATTR_VERSION_NAME, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationVersionName) },
                { ATTR_LOCALE, () => LdValue.Of(_environmentReporter.Locale) }
            };

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
