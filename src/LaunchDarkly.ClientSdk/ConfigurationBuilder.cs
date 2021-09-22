using System;
using System.Collections.Generic;
using System.Net.Http;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.Events;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client
{
    /// <summary>
    /// A mutable object that uses the Builder pattern to specify properties for a <see cref="Configuration"/> object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Obtain an instance of this class by calling <see cref="Configuration.Builder(string)"/>.
    /// </para>
    /// <para>
    /// All of the builder methods for setting a configuration property return a reference to the same builder, so they can be
    /// chained together.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///     var config = Configuration.Builder("my-mobile-key").AllAttributesPrivate(true).EventCapacity(1000).Build();
    /// </code>
    /// </example>
    public sealed class ConfigurationBuilder
    {
        // This exists so that we can distinguish between leaving the HttpMessageHandler property unchanged
        // and explicitly setting it to null. If the property value is the exact same instance as this, we
        // will replace it with a platform-specific implementation.
        internal static readonly HttpMessageHandler DefaultHttpMessageHandlerInstance = new HttpClientHandler();

        internal bool _autoAliasingOptOut = false;
        internal IDataSourceFactory _dataSourceFactory = null;
        internal bool _enableBackgroundUpdating = true;
        internal bool _evaluationReasons = false;
        internal IEventProcessorFactory _eventProcessorFactory = null;
        internal HttpConfigurationBuilder _httpConfigurationBuilder = null;
        internal LoggingConfigurationBuilder _loggingConfigurationBuilder = null;
        internal string _mobileKey;
        internal bool _offline = false;
        internal IPersistentDataStoreFactory _persistentDataStoreFactory = null;

        // Internal properties only settable for testing
        internal IBackgroundModeManager _backgroundModeManager;
        internal IConnectivityStateManager _connectivityStateManager;
        internal IDeviceInfo _deviceInfo;
        internal IFlagChangedEventManager _flagChangedEventManager;

        internal ConfigurationBuilder(string mobileKey)
        {
            _mobileKey = mobileKey;
        }

        internal ConfigurationBuilder(Configuration copyFrom)
        {
            _autoAliasingOptOut = copyFrom.AutoAliasingOptOut;
            _dataSourceFactory = copyFrom.DataSourceFactory;
            _enableBackgroundUpdating = copyFrom.EnableBackgroundUpdating;
            _evaluationReasons = copyFrom.EvaluationReasons;
            _eventProcessorFactory = copyFrom.EventProcessorFactory;
            _httpConfigurationBuilder = copyFrom.HttpConfigurationBuilder;
            _loggingConfigurationBuilder = copyFrom.LoggingConfigurationBuilder;
            _mobileKey = copyFrom.MobileKey;
            _offline = copyFrom.Offline;
            _persistentDataStoreFactory = copyFrom.PersistentDataStoreFactory;
        }

        /// <summary>
        /// Creates a <see cref="Configuration"/> based on the properties that have been set on the builder.
        /// Modifying the builder after this point does not affect the returned <see cref="Configuration"/>.
        /// </summary>
        /// <returns>the configured <c>Configuration</c> object</returns>
        public Configuration Build()
        {
            return new Configuration(this);
        }

        /// <summary>
        /// Whether to disable the automatic sending of an alias event when the current user is changed
        /// to a non-anonymous user and the previous user was anonymous.
        /// </summary>
        /// <remarks>
        /// By default, if you call <see cref="LdClient.Identify(User, TimeSpan)"/> or
        /// <see cref="LdClient.IdentifyAsync(User)"/> with a non-anonymous user, and the current user
        /// (previously specified either with one of those methods or when creating the <see cref="LdClient"/>)
        /// was anonymous, the SDK assumes the two users should be correlated and sends an analytics
        /// event equivalent to calling <see cref="LdClient.Alias(User, User)"/>. Setting
        /// AutoAliasingOptOut to <see langword="true"/> disables this behavior.
        /// </remarks>
        /// <param name="autoAliasingOptOut">true to disable automatic user aliasing</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder AutoAliasingOptOut(bool autoAliasingOptOut)
        {
            _autoAliasingOptOut = autoAliasingOptOut;
            return this;
        }

        /// <summary>
        /// Sets the implementation of the component that receives feature flag data from LaunchDarkly,
        /// using a factory object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Depending on the implementation, the factory may be a builder that allows you to set other
        /// configuration options as well.
        /// </para>
        /// <para>
        /// The default is <see cref="Components.StreamingDataSource"/>. You may instead use
        /// <see cref="Components.PollingDataSource"/>. See those methods for details on how
        /// to configure them.
        /// </para>
        /// </remarks>
        /// <param name="dataSourceFactory">the factory object</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder DataSource(IDataSourceFactory dataSourceFactory)
        {
            _dataSourceFactory = dataSourceFactory;
            return this;
        }

        /// <summary>
        /// Sets whether to enable feature flag polling when the application is in the background.
        /// </summary>
        /// <remarks>
        /// By default, on Android and iOS the SDK can still receive feature flag updates when an application
        /// is in the background, but it will use polling rather than maintaining a streaming connection (and
        /// will use the background polling interval rather than the regular polling interval). If you set
        /// this property to false, it will not check for feature flag updates until the application returns
        /// to the foreground.
        /// </remarks>
        /// <param name="enableBackgroundUpdating"><see langword="true"/> if background updating should be allowed</param>
        /// <returns>the same builder</returns>
        /// <seealso cref="StreamingDataSourceBuilder.BackgroundPollInterval"/>
        /// <seealso cref="PollingDataSourceBuilder.BackgroundPollInterval"/>
        public ConfigurationBuilder EnableBackgroundUpdating(bool enableBackgroundUpdating)
        {
            _enableBackgroundUpdating = enableBackgroundUpdating;
            return this;
        }

        /// <summary>
        /// Set to <see langword="true"/> if LaunchDarkly should provide additional information about how flag values were
        /// calculated.
        /// </summary>
        /// <remarks>
        /// The additional information will then be available through the client's "detail"
        /// methods such as <see cref="LdClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to <see langword="true"/>.
        /// </remarks>
        /// <param name="evaluationReasons"><see langword="true"/> if evaluation reasons are desired</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder EvaluationReasons(bool evaluationReasons)
        {
            _evaluationReasons = evaluationReasons;
            return this;
        }

        /// <summary>
        /// Sets the implementation of the component that processes analytics events.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="Components.SendEvents"/>, but you may choose to set it to a customized
        /// <see cref="EventProcessorBuilder"/>, a custom implementation (for instance, a test fixture), or
        /// disable events with <see cref="Components.NoEvents"/>.
        /// </remarks>
        /// <param name="eventProcessorFactory">a builder/factory object for event configuration</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder Events(IEventProcessorFactory eventProcessorFactory)
        {
            _eventProcessorFactory = eventProcessorFactory;
            return this;
        }

        /// <summary>
        /// Sets the SDK's networking configuration, using a configuration builder obtained from
        /// <see cref="Components.HttpConfiguration()"/>. The builder has methods for setting
        /// individual HTTP-related properties.
        /// </summary>
        /// <param name="httpConfigurationBuilder">a builder for HTTP configuration</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder Http(HttpConfigurationBuilder httpConfigurationBuilder)
        {
            _httpConfigurationBuilder = httpConfigurationBuilder;
            return this;
        }

        /// <summary>
        /// Sets the SDK's logging destination.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a shortcut for <c>Logging(Components.Logging(logAdapter))</c>. You can use it when you
        /// only want to specify the basic logging destination, and do not need to set other log properties.
        /// </para>
        /// <para>
        /// For more about how logging works in the SDK, see the LaunchDarkly
        /// <a href="https://docs.launchdarkly.com/sdk/features/logging#net-client-side">feature guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        ///     var config = Configuration.Builder("my-sdk-key")
        ///         .Logging(Logs.ToWriter(Console.Out))
        ///         .Build();
        /// </example>
        /// <param name="logAdapter">an <c>ILogAdapter</c> for the desired logging implementation</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder Logging(ILogAdapter logAdapter) =>
            Logging(Components.Logging(logAdapter));

        /// <summary>
        /// Sets the SDK's logging configuration, using a configuration builder obtained from.
        /// <see cref="Components.Logging()"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As a shortcut for disabling logging, you may use <see cref="Components.NoLogging"/> instead.
        /// If all you want to do is to set the basic logging destination, and you do not need to set other
        /// logging properties, you can use <see cref="Logging(ILogAdapter)"/> instead.
        /// </para>
        /// <para>
        /// For more about how logging works in the SDK, see the LaunchDarkly
        /// <a href="https://docs.launchdarkly.com/sdk/features/logging#net-client-side">feature guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        ///     var config = Configuration.Builder("my-sdk-key")
        ///         .Logging(Components.Logging().Level(LogLevel.Warn)))
        ///         .Build();
        /// </example>
        /// <param name="loggingConfigurationBuilder">the builder object</param>
        /// <returns>the same builder</returns>
        /// <seealso cref="Components.Logging()" />
        /// <seealso cref="Components.Logging(ILogAdapter) "/>
        /// <seealso cref="Components.NoLogging" />
        /// <seealso cref="Logging(ILogAdapter)"/>
        public ConfigurationBuilder Logging(LoggingConfigurationBuilder loggingConfigurationBuilder)
        {
            _loggingConfigurationBuilder = loggingConfigurationBuilder;
            return this;
        }

        /// <summary>
        /// Sets the key for your LaunchDarkly environment.
        /// </summary>
        /// <remarks>
        /// This should be the "mobile key" field for the environment on your LaunchDarkly dashboard.
        /// </remarks>
        /// <param name="mobileKey">the mobile key</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder MobileKey(string mobileKey)
        {
            _mobileKey = mobileKey;
            return this;
        }

        /// <summary>
        /// Sets whether or not this client is offline. If <see langword="true"/>, no calls to LaunchDarkly will be made.
        /// </summary>
        /// <param name="offline"><see langword="true"/> if the client should remain offline</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder Offline(bool offline)
        {
            _offline = offline;
            return this;
        }

        /// <summary>
        /// Sets the implementation of the component that saves flag values for each user in persistent storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The persistent storage mechanism allows the SDK to immediately access the last known flag data
        /// for the user, if any, if it is offline or has not yet received data from LaunchDarkly.
        /// </para>
        /// <para>
        /// By default, the SDK uses a persistence mechanism that is specific to each platform: on Android and
        /// iOS it is the native preferences store, and in the .NET Standard implementation for desktop apps
        /// it is the <c>System.IO.IsolatedStorage</c> API. You may substitute a custom implementation, or
        /// pass <see cref="Components.NoPersistence"/> to disable persistent storage and use only in-memory
        /// storage.
        /// </para>
        /// </remarks>
        /// <param name="persistentDataStoreFactory">the factory object; null to use the default implementation</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder Persistence(IPersistentDataStoreFactory persistentDataStoreFactory)
        {
            _persistentDataStoreFactory = persistentDataStoreFactory;
            return this;
        }

        // The following properties are internal and settable only for testing.

        internal ConfigurationBuilder BackgroundModeManager(IBackgroundModeManager backgroundModeManager)
        {
            _backgroundModeManager = backgroundModeManager;
            return this;
        }

        internal ConfigurationBuilder ConnectivityStateManager(IConnectivityStateManager connectivityStateManager)
        {
            _connectivityStateManager = connectivityStateManager;
            return this;
        }

        internal ConfigurationBuilder DeviceInfo(IDeviceInfo deviceInfo)
        {
            _deviceInfo = deviceInfo;
            return this;
        }

        internal ConfigurationBuilder FlagChangedEventManager(IFlagChangedEventManager flagChangedEventManager)
        {
            _flagChangedEventManager = flagChangedEventManager;
            return this;
        }
    }
}
