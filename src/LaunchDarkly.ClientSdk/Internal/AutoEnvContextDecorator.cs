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
                    recipe.TryWrite(builder);
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
            public ContextKind Kind { get; }
            public Func<string> KeyCallable { get; }
            public List<IRecipeNode> RecipeNodes { get; }

            public ContextRecipe(ContextKind kind, Func<string> keyCallable, List<IRecipeNode> recipeNodes)
            {
                Kind = kind;
                KeyCallable = keyCallable;
                RecipeNodes = recipeNodes;
            }

            public void TryWrite(ContextMultiBuilder multiBuilder)
            {
                var contextBuilder = Context.Builder(Kind, KeyCallable.Invoke());
                var adaptedBuilder = new ContextBuilderAdapter(contextBuilder);
                var wrote = false;
                RecipeNodes.ForEach(it => { wrote |= it.TryWrite(adaptedBuilder); });
                if (wrote)
                {
                    contextBuilder.Set(ENV_ATTRIBUTES_VERSION, SPEC_VERSION);
                    multiBuilder.Add(contextBuilder.Build());
                }
            }
        }

        private List<ContextRecipe> MakeRecipeList()
        {
            var ldApplicationKind = ContextKind.Of(LD_APPLICATION_KIND);
            var applicationNodes = new List<IRecipeNode>
            {
                new ConcreteRecipeNode(ATTR_ID, () => LdValue.Of(_environmentReporter.ApplicationInfo?.ApplicationId)),
                new ConcreteRecipeNode(ATTR_NAME,
                    () => LdValue.Of(_environmentReporter.ApplicationInfo?.ApplicationName)),
                new ConcreteRecipeNode(ATTR_VERSION,
                    () => LdValue.Of(_environmentReporter.ApplicationInfo?.ApplicationVersion)),
                new ConcreteRecipeNode(ATTR_VERSION_NAME,
                    () => LdValue.Of(_environmentReporter.ApplicationInfo?.ApplicationVersionName)),
                new ConcreteRecipeNode(ATTR_LOCALE, () => LdValue.Of(_environmentReporter.Locale)),
            };

            var ldDeviceKind = ContextKind.Of(LD_DEVICE_KIND);
            var deviceNodes = new List<IRecipeNode>
            {
                new ConcreteRecipeNode(ATTR_MANUFACTURER,
                    () => LdValue.Of(_environmentReporter.DeviceInfo?.Manufacturer)),
                new ConcreteRecipeNode(ATTR_MODEL, () => LdValue.Of(_environmentReporter.DeviceInfo?.Model)),
                new CompositeRecipeNode(ATTR_OS, new List<IRecipeNode>
                {
                    new ConcreteRecipeNode(ATTR_FAMILY, () => LdValue.Of(_environmentReporter.OsInfo?.Family)),
                    new ConcreteRecipeNode(ATTR_NAME, () => LdValue.Of(_environmentReporter.OsInfo?.Name)),
                    new ConcreteRecipeNode(ATTR_VERSION, () => LdValue.Of(_environmentReporter.OsInfo?.Version)),
                })
            };

            return new List<ContextRecipe>
            {
                new ContextRecipe(
                    ldApplicationKind,
                    () => Base64.UrlSafeSha256Hash(
                        _environmentReporter.ApplicationInfo?.ApplicationId ?? ""
                    ),
                    applicationNodes
                ),
                new ContextRecipe(
                    ldDeviceKind,
                    () => GetOrCreateAutoContextKey(_persistentData, ldDeviceKind),
                    deviceNodes
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

        private interface ISettableMap
        {
            void Set(string attributeName, LdValue value);
        }

        private interface IRecipeNode
        {
            bool TryWrite(ISettableMap settableMap);
        }

        private class CompositeRecipeNode : IRecipeNode
        {
            private readonly string _name;
            private readonly List<IRecipeNode> _nodes;

            public CompositeRecipeNode(string name, List<IRecipeNode> nodes)
            {
                _name = name;
                _nodes = nodes;
            }

            public bool TryWrite(ISettableMap map)
            {
                var wrote = false;
                var ldValueBuilder = LdValue.BuildObject();
                var adaptedBuilder = new ObjectBuilderAdapter(ldValueBuilder);
                _nodes.ForEach(it => { wrote |= it.TryWrite(adaptedBuilder); });
                if (wrote)
                {
                    map.Set(_name, ldValueBuilder.Build());
                }

                return wrote;
            }
        }

        private class ConcreteRecipeNode : IRecipeNode
        {
            private readonly string _name;
            private readonly Func<LdValue?> _valueFunc;

            public ConcreteRecipeNode(string name, Func<LdValue?> valueFunc)
            {
                _name = name;
                _valueFunc = valueFunc;
            }

            public bool TryWrite(ISettableMap map)
            {
                var result = _valueFunc.Invoke();
                if (!result.HasValue || result.Value == LdValue.Null) return false;
                map.Set(_name, result.Value);
                return true;
            }
        }

        private class ObjectBuilderAdapter : ISettableMap
        {
            private readonly LdValue.ObjectBuilder _underlyingBuilder;

            public ObjectBuilderAdapter(LdValue.ObjectBuilder builder)
            {
                _underlyingBuilder = builder;
            }

            public void Set(string attributeName, LdValue value)
            {
                _underlyingBuilder.Set(attributeName, value);
            }
        }

        private class ContextBuilderAdapter : ISettableMap
        {
            private readonly ContextBuilder _underlyingBuilder;

            public ContextBuilderAdapter(ContextBuilder builder)
            {
                _underlyingBuilder = builder;
            }

            public void Set(string attributeName, LdValue value)
            {
                _underlyingBuilder.Set(attributeName, value);
            }
        }
    }
}
