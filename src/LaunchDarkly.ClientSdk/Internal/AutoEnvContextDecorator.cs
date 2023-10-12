using System;
using System.Collections.Generic;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    /// <summary>
    /// This class can decorate a context by adding additional contexts to it using auto environment attributes
    /// provided by <see cref="IEnvironmentReporter"/>.
    /// </summary>
    internal class AutoEnvContextDecorator
    {
        internal const string LdApplicationKind = "ld_application";
        internal const string LdDeviceKind = "ld_device";
        internal const string AttrId = "id";
        internal const string AttrName = "name";
        internal const string AttrVersion = "version";
        internal const string AttrVersionName = "versionName";
        internal const string AttrManufacturer = "manufacturer";
        internal const string AttrModel = "model";
        internal const string AttrLocale = "locale";
        internal const string AttrOs = "os";
        internal const string AttrFamily = "family";
        internal const string EnvAttributesVersion = "envAttributesVersion";
        internal const string SpecVersion = "1.0";

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
        /// <param name="logger">the logger</param>
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

            foreach (var recipe in MakeRecipeList())
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

        private readonly struct ContextRecipe
        {
            public ContextKind Kind { get; }
            public Func<string> KeyCallable { get; }
            public Dictionary<string, Func<LdValue>> AttributeCallables { get; }

            public ContextRecipe(ContextKind kind, Func<string> keyCallable,
                Dictionary<string, Func<LdValue>> attributeCallables)
            {
                Kind = kind;
                KeyCallable = keyCallable;
                AttributeCallables = attributeCallables;
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
            var ldApplicationKind = ContextKind.Of(LdApplicationKind);
            var applicationCallables = new Dictionary<string, Func<LdValue>>
            {
                { EnvAttributesVersion, () => LdValue.Of(SpecVersion) },
                { AttrId, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationId) },
                { AttrName, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationName) },
                { AttrVersion, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationVersion) },
                { AttrVersionName, () => LdValue.Of(_environmentReporter.ApplicationInfo.ApplicationVersionName) },
                { AttrLocale, () => LdValue.Of(_environmentReporter.Locale) }
            };

            var ldDeviceKind = ContextKind.Of(LdDeviceKind);
            var deviceCallables = new Dictionary<string, Func<LdValue>>
            {
                { EnvAttributesVersion, () => LdValue.Of(SpecVersion) },
                { AttrManufacturer, () => LdValue.Of(_environmentReporter.DeviceInfo.Manufacturer) },
                { AttrModel, () => LdValue.Of(_environmentReporter.DeviceInfo.Model) },
                {
                    AttrOs, () =>
                        LdValue.BuildObject()
                            .Add(AttrFamily, _environmentReporter.OsInfo.Family)
                            .Add(AttrName, _environmentReporter.OsInfo.Name)
                            .Add(AttrVersion, _environmentReporter.OsInfo.Version)
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
