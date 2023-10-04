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
    internal class AutoEnvContextModifier
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

        private readonly PersistentDataStoreWrapper persistentData;
        private readonly IEnvironmentReporter environmentReporter;
        private readonly Logger logger;

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="persistentData"></param>
        /// <param name="environmentReporter"></param>
        /// <param name="logger"></param>
        public AutoEnvContextModifier(
            PersistentDataStoreWrapper persistentData,
            IEnvironmentReporter environmentReporter,
            Logger logger)
        {
            this.persistentData = persistentData;
            this.environmentReporter = environmentReporter;
            this.logger = logger;
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
                    logger.Warn("Unable to automatically add environment attributes for kind:{0}. {1} already exists.",
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
                { ATTR_ID, () => LdValue.Of(environmentReporter.ApplicationInfo.ApplicationId) },
                { ATTR_NAME, () => LdValue.Of(environmentReporter.ApplicationInfo.ApplicationName) },
                { ATTR_VERSION, () => LdValue.Of(environmentReporter.ApplicationInfo.ApplicationVersion) },
                { ATTR_VERSION_NAME, () => LdValue.Of(environmentReporter.ApplicationInfo.ApplicationVersionName) }
            };

            // TODO: missing locale in environment reporter implementation
            // applicationCallables.Add(ATTR_LOCALE, () => LDValue.Of(environmentReporter.GetLocale()));

            var ldDeviceKind = ContextKind.Of(LD_DEVICE_KIND);
            var deviceCallables = new Dictionary<string, Func<LdValue>>
            {
                { ENV_ATTRIBUTES_VERSION, () => LdValue.Of(SPEC_VERSION) },
                { ATTR_MANUFACTURER, () => LdValue.Of(environmentReporter.DeviceInfo.Manufacturer) },
                { ATTR_MODEL, () => LdValue.Of(environmentReporter.DeviceInfo.Model) },
                {
                    ATTR_OS, () =>
                        LdValue.BuildObject()
                            .Add(ATTR_FAMILY, environmentReporter.OsInfo.Family)
                            .Add(ATTR_NAME, environmentReporter.OsInfo.Name)
                            .Add(ATTR_VERSION, environmentReporter.OsInfo.Version)
                            .Build()
                }
            };

            return new List<ContextRecipe>
            {
                new ContextRecipe(
                    ldApplicationKind,
                    () => Base64.UrlSafeSha256Hash(
                        environmentReporter.ApplicationInfo.ApplicationId ?? ""
                    ),
                    applicationCallables
                ),
                new ContextRecipe(
                    ldDeviceKind,
                    () => persistentData.GetGeneratedContextKey(ldDeviceKind),
                    deviceCallables
                )
            };
        }
    }
}
