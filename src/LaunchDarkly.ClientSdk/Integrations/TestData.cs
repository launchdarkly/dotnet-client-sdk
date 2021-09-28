﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    /// <summary>
    /// A mechanism for providing dynamically updatable feature flag state in a simplified form to an SDK
    /// client in test scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mechanism does not use any external resources. It provides only the data that the application
    /// has put into it using the <see cref="Update(TestData.FlagBuilder)"/> method.
    /// </para>
    /// <para>
    /// The example code below uses a simple boolean flag, but more complex configurations are possible using
    /// the methods of the <see cref="FlagBuilder"/> that is returned by <see cref="Flag(string)"/>.
    /// </para>
    /// <para>
    /// If the same <see cref="TestData"/> instance is used to configure multiple <see cref="LdClient"/>
    /// instances, any changes made to the data will propagate to all of the <see cref="LdClient"/>s.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///     var td = TestData.DataSource();
    ///     td.Update(td.Flag("flag-key-1").BooleanFlag().Variation(true));
    ///
    ///     var config = Configuration.Builder("sdk-key")
    ///         .DataSource(td)
    ///         .Build();
    ///     var client = new LdClient(config);
    ///
    ///     // flags can be updated at any time:
    ///     td.update(testData.flag("flag-key-2")
    ///         .VariationForUser("some-user-key", false));
    /// </code>
    /// </example>
    public sealed class TestData : IDataSourceFactory
    {
        #region Private fields

        private readonly object _lock = new object();
        private readonly Dictionary<string, int> _currentFlagVersions =
            new Dictionary<string, int>();
        private readonly Dictionary<string, FlagBuilder> _currentBuilders =
            new Dictionary<string, FlagBuilder>();
        private readonly List<DataSourceImpl> _instances = new List<DataSourceImpl>();

        #endregion

        #region Private constructor

        private TestData() { }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a new instance of the test data source.
        /// </summary>
        /// <remarks>
        /// See <see cref="TestData"/> for details.
        /// </remarks>
        /// <returns>a new configurable test data source</returns>
        public static TestData DataSource() => new TestData();

        /// <summary>
        /// Creates or copies a <see cref="FlagBuilder"/> for building a test flag configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this flag key has already been defined in this <see cref="TestData"/> instance, then
        /// the builder starts with the same configuration that was last provided for this flag.
        /// </para>
        /// <para>
        /// Otherwise, it starts with a new default configuration in which the flag has <c>true</c>
        /// and <c>false</c> variations, and is <c>true</c> by default for all users. You can change
        /// any of those properties, and provide more complex behavior, using the
        /// <see cref="FlagBuilder"/> methods.
        /// </para>
        /// <para>
        /// Once you have set the desired configuration, pass the builder to
        /// <see cref="Update(FlagBuilder)"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the flag key</param>
        /// <returns>a flag configuration builder</returns>
        /// <seealso cref="Update(FlagBuilder)"/>
        public FlagBuilder Flag(string key)
        {
            FlagBuilder existingBuilder;
            lock (_lock)
            {
                _currentBuilders.TryGetValue(key, out existingBuilder);
            }
            if (existingBuilder != null)
            {
                return new FlagBuilder(existingBuilder);
            }
            return new FlagBuilder(key).BooleanFlag();
        }

        /// <summary>
        /// Updates the test data with the specified flag configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This has the same effect as if a flag were added or modified on the LaunchDarkly dashboard.
        /// It immediately propagates the flag change to any <see cref="LdClient"/> instance(s) that
        /// you have already configured to use this <see cref="TestData"/>. If no <see cref="LdClient"/>
        /// has been started yet, it simply adds this flag to the test data which will be provided to any
        /// <see cref="LdClient"/> that you subsequently configure.
        /// </para>
        /// <para>
        /// Any subsequent changes to this <see cref="FlagBuilder"/> instance do not affect the test data,
        /// unless you call <see cref="Update(FlagBuilder)"/> again.
        /// </para>
        /// </remarks>
        /// <param name="flagBuilder">a flag configuration builder</param>
        /// <returns>the same <see cref="TestData"/> instance</returns>
        /// <seealso cref="Flag(string)"/>
        public TestData Update(FlagBuilder flagBuilder)
        {
            var key = flagBuilder._key;
            var clonedBuilder = new FlagBuilder(flagBuilder);
            UpdateInternal(key, clonedBuilder);
            return this;
        }

        private void UpdateInternal(string key, FlagBuilder builder)
        {
            DataSourceImpl[] instances;
            int newVersion;

            lock (_lock)
            {
                if (!_currentFlagVersions.TryGetValue(key, out var oldVersion))
                {
                    oldVersion = 0;
                }
                newVersion = oldVersion + 1;
                _currentFlagVersions[key] = newVersion;
                if (builder is null)
                {
                    _currentBuilders.Remove(key);
                }
                else
                {
                    _currentBuilders[key] = builder;
                }
                instances = _instances.ToArray();
            }

            foreach (var instance in instances)
            {
                instance.DoUpdate(key, builder.CreateFlag(newVersion, instance.User));
            }
        }

        /// <inheritdoc/>
        public IDataSource CreateDataSource(
            LdClientContext context,
            IDataSourceUpdateSink updateSink,
            User currentUser,
            bool inBackground
            )
        {
            var instance = new DataSourceImpl(
                this,
                updateSink,
                currentUser,
                context.BaseLogger.SubLogger("DataSource.TestData")
                );
            lock (_lock)
            {
                _instances.Add(instance);
            }
            return instance;
        }

        internal FullDataSet MakeInitData(User user)
        {
            lock (_lock)
            {
                var b = ImmutableList.CreateBuilder<KeyValuePair<string, ItemDescriptor>>();
                foreach (var fb in _currentBuilders)
                {
                    if (!_currentFlagVersions.TryGetValue(fb.Key, out var version))
                    {
                        version = 1;
                        _currentFlagVersions[fb.Key] = version;
                    }
                    b.Add(new KeyValuePair<string, ItemDescriptor>(fb.Key,
                        fb.Value.CreateFlag(version, user)));
                }
                return new FullDataSet(b.ToImmutable());
            }
        }

        internal void ClosedInstance(DataSourceImpl instance)
        {
            lock (_lock)
            {
                _instances.Remove(instance);
            }
        }

        #endregion

        #region Public inner types

        /// <summary>
        /// A builder for feature flag configurations to be used with <see cref="TestData"/>.
        /// </summary>
        /// <seealso cref="TestData.Flag(string)"/>
        /// <seealso cref="TestData.Update(FlagBuilder)"/>
        public sealed class FlagBuilder
        {
            #region Private/internal fields

            private const int TrueVariationForBoolean = 0;
            private const int FalseVariationForBoolean = 1;

            internal readonly string _key;
            private List<LdValue> _variations;
            private int _defaultVariation;
            private Dictionary<string, int> _variationByUserKey;
            private Func<User, int?> _variationFunc;
            private FeatureFlag _preconfiguredFlag;

            #endregion

            #region Internal constructors

            internal FlagBuilder(string key)
            {
                _key = key;
                _variations = new List<LdValue>();
                _defaultVariation = 0;
                _variationByUserKey = new Dictionary<string, int>();
            }

            internal FlagBuilder(FlagBuilder from)
            {
                _key = from._key;
                _variations = new List<LdValue>(from._variations);
                _defaultVariation = from._defaultVariation;
                _variationFunc = from._variationFunc;
                _variationByUserKey = new Dictionary<string, int>(from._variationByUserKey);
                _preconfiguredFlag = from._preconfiguredFlag;
            }

            #endregion

            #region Public methods

            /// <summary>
            /// A shortcut for setting the flag to use the standard boolean configuration.
            /// </summary>
            /// <remarks>
            /// This is the default for all new flags created with <see cref="TestData.Flag(string)"/>.
            /// The flag will have two variations, <c>true</c> and <c>false</c> (in that order). When
            /// using evaluation reasons, the reason will be set to <see cref="EvaluationReason.FallthroughReason"/>
            /// whenever the value is <c>true</c>, and <see cref="EvaluationReason.OffReason"/> whenever the
            /// value is <c>false</c>.
            /// </remarks>
            /// <returns>the builder</returns>
            public FlagBuilder BooleanFlag() =>
                IsBooleanFlag ? this : Variations(LdValue.Of(true), LdValue.Of(false));

            /// <summary>
            /// Sets the flag to return the specified boolean variation for all users by default.
            /// </summary>
            /// <remarks>
            /// The flag's variations are set to <c>true</c> and <c>false</c> if they are not already
            /// (equivalent to calling <see cref="BooleanFlag"/>).
            /// </remarks>
            /// <param name="variation">the desired true/false variation to be returned for all users</param>
            /// <returns>the builder</returns>
            public FlagBuilder Variation(bool variation) =>
                BooleanFlag().Variation(VariationForBoolean(variation));

            /// <summary>
            /// Sets the flag to return the specified variation for all users by default.
            /// </summary>
            /// <remarks>
            /// The variation is specified by number, out of whatever variation values have already been
            /// defined.
            /// </remarks>
            /// <param name="variationIndex">the desired variation: 0 for the first, 1 for the second, etc.</param>
            /// <returns>the builder</returns>
            public FlagBuilder Variation(int variationIndex)
            {
                _defaultVariation = variationIndex;
                return this;
            }

            /// <summary>
            /// Sets the flag to return the specified variation value for all users by default.
            /// </summary>
            /// <remarks>
            /// The value may be of any JSON type, as defined by <see cref="LdValue"/>. If the value
            /// matches one of the values previously specified with <see cref="Variations(LdValue[])"/>,
            /// then the variation index is set to the index of that value. Otherwise, the value is
            /// added to the variation list.
            /// </remarks>
            /// <param name="value">the desired value to be returned for all users</param>
            /// <returns>the builder</returns>
            public FlagBuilder Variation(LdValue value)
            {
                AddVariationIfNotDefined(value);
                _defaultVariation = _variations.IndexOf(value);
                _variationFunc = null;
                return this;
            }

            /// <summary>
            /// Sets the flag to return the specified boolean variation for a specific user key,
            /// overriding any other defaults.
            /// </summary>
            /// <remarks>
            /// The flag's variations are set to <c>true</c> and <c>false</c> if they are not already
            /// (equivalent to calling <see cref="BooleanFlag"/>).
            /// </remarks>
            /// <param name="userKey">a user key</param>
            /// <param name="variation">the desired true/false variation to be returned for this user</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationForUser(string userKey, bool variation) =>
                BooleanFlag().VariationForUser(userKey, VariationForBoolean(variation));

            /// <summary>
            /// Sets the flag to return the specified variation for a specific user key, overriding
            /// any other defaults.
            /// </summary>
            /// <remarks>
            /// The variation is specified by number, out of whatever variation values have already been
            /// defined.
            /// </remarks>
            /// <param name="userKey">a user key</param>
            /// <param name="variationIndex">the desired variation to be returned for this user when
            /// targeting is on: 0 for the first, 1 for the second, etc.</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationForUser(string userKey, int variationIndex)
            {
                _variationByUserKey[userKey] = variationIndex;
                return this;
            }

            /// <summary>
            /// Sets the flag to return the specified variation value for a specific user key, overriding
            /// any other defaults.
            /// </summary>
            /// <remarks>
            /// The value may be of any JSON type, as defined by <see cref="LdValue"/>. If the value
            /// matches one of the values previously specified with <see cref="Variations(LdValue[])"/>,
            /// then the variation index is set to the index of that value. Otherwise, the value is
            /// added to the variation list.
            /// </remarks>
            /// <param name="userKey">a user key</param>
            /// <param name="value">the desired value to be returned for this user</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationForUser(string userKey, LdValue value)
            {
                AddVariationIfNotDefined(value);
                _variationByUserKey[userKey] = _variations.IndexOf(value);
                return this;
            }

            /// <summary>
            /// Sets the flag to use a function to determine whether to return true or false for
            /// any given user.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The function takes a user and returns <see langword="true"/>, <see langword="false"/>,
            /// or <see langword="null"/>. A <see langword="null"/> result means that the flag will
            /// fall back to its default variation for all users.
            /// </para>
            /// <para>
            /// The flag's variations are set to <c>true</c> and <c>false</c> if they are not already
            /// (equivalent to calling <see cref="BooleanFlag"/>).
            /// </para>
            /// <para>
            /// The function is only called for users who were not already specified by
            /// <see cref="VariationForUser(string, bool)"/>.
            /// </para>
            /// </remarks>
            /// <param name="variationFunc">a function to determine the variation</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationFunc(Func<User, bool?> variationFunc) =>
                BooleanFlag().VariationFunc(user =>
                    {
                        var b = variationFunc(user);
                        return b.HasValue ? VariationForBoolean(b.Value) : (int?)null;
                    });

            /// <summary>
            /// Sets the flag to use a function to determine the variation index to return for
            /// any given user.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The function takes a user and returns an integer variation index or <see langword="null"/>.
            /// A <see langword="null"/> result means that the flag will fall back to its default
            /// variation for all users.
            /// </para>
            /// <para>
            /// The function is only called for users who were not already specified by
            /// <see cref="VariationForUser(string, int)"/>.
            /// </para>
            /// </remarks>
            /// <param name="variationFunc">a function to determine the variation</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationFunc(Func<User, int?> variationFunc)
            {
                _variationFunc = variationFunc;
                return this;
            }

            /// <summary>
            /// Sets the flag to use a function to determine the variation value to return for
            /// any given user.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The function takes a user and returns an <see cref="LdValue"/> or <see langword="null"/>.
            /// A <see langword="null"/> result means that the flag will fall back to its default
            /// variation for all users.
            /// </para>
            /// <para>
            /// The value returned by the function must be one of the values previously specified
            /// with <see cref="Variations(LdValue[])"/>; otherwise it will be ignored.
            /// </para>
            /// <para>
            /// The function is only called for users who were not already specified by
            /// <see cref="VariationForUser(string, LdValue)"/>.
            /// </para>
            /// </remarks>
            /// <param name="variationFunc">a function to determine the variation</param>
            /// <returns>the builder</returns>
            public FlagBuilder VariationFunc(Func<User, LdValue?> variationFunc) =>
                VariationFunc(user =>
                    {
                        var v = variationFunc(user);
                        if (!v.HasValue || !_variations.Contains(v.Value))
                        {
                            return null;
                        }
                        return _variations.IndexOf(v.Value);
                    });

            /// <summary>
            /// Changes the allowable variation values for the flag.
            /// </summary>
            /// <remarks>
            /// The value may be of any JSON type, as defined by <see cref="LdValue"/>. For instance, a
            /// boolean flag normally has <c>LdValue.Of(true), LdValue.Of(false)</c>; a string-valued
            /// flag might have <c>LdValue.Of("red"), LdValue.Of("green"), LdValue.Of("blue")</c>; etc.
            /// </remarks>
            /// <param name="values">the desired variations</param>
            /// <returns>the builder</returns>
            public FlagBuilder Variations(params LdValue[] values)
            {
                _variations.Clear();
                _variations.AddRange(values);
                return this;
            }

            // For testing only
            internal FlagBuilder PreconfiguredFlag(FeatureFlag preconfiguredFlag)
            {
                _preconfiguredFlag = preconfiguredFlag;
                return this;
            }

            #endregion

            #region Internal methods

            internal ItemDescriptor CreateFlag(int version, User user)
            {
                if (_preconfiguredFlag != null)
                {
                    return new ItemDescriptor(version, new FeatureFlag(
                        _preconfiguredFlag.Value,
                        _preconfiguredFlag.Variation,
                        _preconfiguredFlag.Reason,
                        _preconfiguredFlag.Version > version ? _preconfiguredFlag.Version : version,
                        _preconfiguredFlag.FlagVersion,
                        _preconfiguredFlag.TrackEvents,
                        _preconfiguredFlag.TrackReason,
                        _preconfiguredFlag.DebugEventsUntilDate));
                }
                int variation;
                if (!_variationByUserKey.TryGetValue(user.Key, out variation))
                {
                    variation = _variationFunc?.Invoke(user) ?? _defaultVariation;
                }
                var value = (variation < 0 || variation >= _variations.Count) ? LdValue.Null :
                    _variations[variation];
                var reason = variation == 0 ? EvaluationReason.FallthroughReason :
                    EvaluationReason.OffReason;
                var flag = new FeatureFlag(
                    value,
                    variation,
                    reason,
                    version,
                    null,
                    false,
                    false,
                    null
                    );
                return new ItemDescriptor(version, flag);
            }

            internal bool IsBooleanFlag =>
                _variations.Count == 2 &&
                    _variations[TrueVariationForBoolean] == LdValue.Of(true) &&
                    _variations[FalseVariationForBoolean] == LdValue.Of(false);

            internal void AddVariationIfNotDefined(LdValue value)
            {
                if (!_variations.Contains(value))
                {
                    _variations.Add(value);
                }
            }

            internal static int VariationForBoolean(bool value) =>
                value ? TrueVariationForBoolean : FalseVariationForBoolean;

            #endregion
        }

        #endregion

        #region Internal inner type

        internal class DataSourceImpl : IDataSource
        {
            private readonly TestData _parent;
            private readonly IDataSourceUpdateSink _updateSink;
            private readonly Logger _log;

            internal readonly User User;

            internal DataSourceImpl(TestData parent, IDataSourceUpdateSink updateSink, User user, Logger log)
            {
                _parent = parent;
                _updateSink = updateSink;
                User = user;
                _log = log;
            }

            public Task<bool> Start()
            {
                _updateSink.Init(User, _parent.MakeInitData(User));
                return Task.FromResult(true);
            }

            public bool Initialized => true;

            public void Dispose() =>
                _parent.ClosedInstance(this);

            internal void DoUpdate(string key, ItemDescriptor item)
            {
                _log.Debug("updating \"{0}\" to {1}", key, LogValues.Defer(() =>
                    item.Item is null ? "<null>" : DataModelSerialization.SerializeFlag(item.Item)));
                _updateSink.Upsert(User, key, item);
            }
        }

        #endregion
    }
}
