using System;
using System.Collections.Generic;
using System.Net.Http;
using LaunchDarkly.Logging;
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
        internal bool _allAttributesPrivate = false;
        internal TimeSpan _backgroundPollingInterval = Configuration.DefaultBackgroundPollingInterval;
        internal Uri _baseUri = Configuration.DefaultUri;
        internal TimeSpan _connectionTimeout = Configuration.DefaultConnectionTimeout;
        internal bool _enableBackgroundUpdating = true;
        internal bool _evaluationReasons = false;
        internal int _eventCapacity = Configuration.DefaultEventCapacity;
        internal TimeSpan _eventFlushInterval = Configuration.DefaultEventFlushInterval;
        internal Uri _eventsUri = Configuration.DefaultEventsUri;
        internal HttpMessageHandler _httpMessageHandler = DefaultHttpMessageHandlerInstance;
        internal bool _inlineUsersInEvents = false;
        internal bool _isStreamingEnabled = true;
        internal ILoggingConfigurationFactory _loggingConfigurationFactory = null;
        internal string _mobileKey;
        internal bool _offline = false;
        internal bool _persistFlagValues = true;
        internal TimeSpan _pollingInterval = Configuration.DefaultPollingInterval;
        internal HashSet<UserAttribute> _privateAttributeNames = null;
        internal TimeSpan _readTimeout = Configuration.DefaultReadTimeout;
        internal TimeSpan _reconnectTime = Configuration.DefaultReconnectTime;
        internal Uri _streamUri = Configuration.DefaultStreamUri;
        internal bool _useReport = false;
        internal int _userKeysCapacity = Configuration.DefaultUserKeysCapacity;
        internal TimeSpan _userKeysFlushInterval = Configuration.DefaultUserKeysFlushInterval;

        // Internal properties only settable for testing
        internal IBackgroundModeManager _backgroundModeManager;
        internal IConnectivityStateManager _connectivityStateManager;
        internal IDeviceInfo _deviceInfo;
        internal IEventProcessor _eventProcessor;
        internal IFlagCacheManager _flagCacheManager;
        internal IFlagChangedEventManager _flagChangedEventManager;
        internal IPersistentStorage _persistentStorage;
        internal Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> _updateProcessorFactory;

        internal ConfigurationBuilder(string mobileKey)
        {
            _mobileKey = mobileKey;
        }

        internal ConfigurationBuilder(Configuration copyFrom)
        {
            _allAttributesPrivate = copyFrom.AllAttributesPrivate;
            _autoAliasingOptOut = copyFrom.AutoAliasingOptOut;
            _backgroundPollingInterval = copyFrom.BackgroundPollingInterval;
            _baseUri = copyFrom.BaseUri;
            _connectionTimeout = copyFrom.ConnectionTimeout;
            _enableBackgroundUpdating = copyFrom.EnableBackgroundUpdating;
            _evaluationReasons = copyFrom.EvaluationReasons;
            _eventCapacity = copyFrom.EventCapacity;
            _eventFlushInterval = copyFrom.EventFlushInterval;
            _eventsUri = copyFrom.EventsUri;
            _httpMessageHandler = copyFrom.HttpMessageHandler;
            _inlineUsersInEvents = copyFrom.InlineUsersInEvents;
            _isStreamingEnabled = copyFrom.IsStreamingEnabled;
            _loggingConfigurationFactory = copyFrom.LoggingConfigurationFactory;
            _mobileKey = copyFrom.MobileKey;
            _offline = copyFrom.Offline;
            _persistFlagValues = copyFrom.PersistFlagValues;
            _pollingInterval = copyFrom.PollingInterval;
            _privateAttributeNames = copyFrom.PrivateAttributeNames is null ? null :
                new HashSet<UserAttribute>(copyFrom.PrivateAttributeNames);
            _readTimeout = copyFrom.ReadTimeout;
            _reconnectTime = copyFrom.ReconnectTime;
            _streamUri = copyFrom.StreamUri;
            _useReport = copyFrom.UseReport;
            _userKeysCapacity = copyFrom.UserKeysCapacity;
            _userKeysFlushInterval = copyFrom.UserKeysFlushInterval;
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
        /// Sets whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server).
        /// </summary>
        /// <remarks>
        /// By default, this is <see langword="false"/>. If <see langword="true"/>, all of the user attributes
        /// will be private, not just the attributes specified with <see cref="ConfigurationBuilder.PrivateAttribute(UserAttribute)"/>
        /// or with the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/> method.
        /// </remarks>
        /// <param name="allAttributesPrivate">true if all attributes should be private</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder AllAttributesPrivate(bool allAttributesPrivate)
        {
            _allAttributesPrivate = allAttributesPrivate;
            return this;
        }
        
        /// <summary>
        /// Whether to disable the automatic sending of an alias event when the current user is changed
        /// to a non-anonymous user andthe previous user was anonymous.
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
        /// Sets the interval between feature flag updates when the application is running in the background.
        /// </summary>
        /// <remarks>
        /// This is only relevant on mobile platforms. The default is <see cref="Configuration.DefaultBackgroundPollingInterval"/>;
        /// the minimum is <see cref="Configuration.MinimumPollingInterval"/>.
        /// </remarks>
        /// <param name="backgroundPollingInterval">the background polling interval</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder BackgroundPollingInterval(TimeSpan backgroundPollingInterval)
        {
            if (backgroundPollingInterval.CompareTo(Configuration.MinimumBackgroundPollingInterval) < 0)
            {
                _backgroundPollingInterval = Configuration.MinimumBackgroundPollingInterval;
            }
            else
            {
                _backgroundPollingInterval = backgroundPollingInterval;
            }
            return this;
        }

        /// <summary>
        /// Sets the base URI of the LaunchDarkly server.
        /// </summary>
        /// <param name="baseUri">the base URI</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder BaseUri(Uri baseUri)
        {
            _baseUri = baseUri;
            return this;
        }

        /// <summary>
        /// Sets the connection timeout for all HTTP requests.
        /// </summary>
        /// <remarks>
        /// The default value is 10 seconds.
        /// </remarks>
        /// <param name="connectionTimeout">the connection timeout</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder ConnectionTimeout(TimeSpan connectionTimeout)
        {
            _connectionTimeout = connectionTimeout;
            return this;
        }

        /// <summary>
        /// Sets whether to enable feature flag polling when the application is in the background.
        /// </summary>
        /// <remarks>
        /// By default, on Android and iOS the SDK can still receive feature flag updates when an application
        /// is in the background, but it will use polling rather than maintaining a streaming connection (and
        /// will use <see cref="BackgroundPollingInterval(TimeSpan)"/> rather than <see cref="PollingInterval(TimeSpan)"/>).
        /// If you set this property to false, it will not check for feature flag updates until the
        /// application returns to the foreground.
        /// </remarks>
        /// <param name="enableBackgroundUpdating"><see langword="true"/> if background updating should be allowed</param>
        /// <returns>the same builder</returns>
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
        /// Sets the capacity of the event buffer.
        /// </summary>
        /// <remarks>
        /// The client buffers up to this many events in memory before flushing. If the capacity is exceeded
        /// before the buffer is flushed, events will be discarded. Increasing the capacity means that events
        /// are less likely to be discarded, at the cost of consuming more memory.
        /// </remarks>
        /// <param name="eventCapacity">the capacity of the event buffer</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder EventCapacity(int eventCapacity)
        {
            _eventCapacity = eventCapacity;
            return this;
        }

        /// <summary>
        /// Sets the time between flushes of the event buffer.
        /// </summary>
        /// <remarks>
        /// Decreasing the flush interval means that the event buffer is less likely to reach capacity. The
        /// default value is 5 seconds.
        /// </remarks>
        /// <param name="eventflushInterval">the flush interval</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder EventFlushInterval(TimeSpan eventflushInterval)
        {
            _eventFlushInterval = eventflushInterval;
            return this;
        }

        /// <summary>
        /// Sets the base URL of the LaunchDarkly analytics event server.
        /// </summary>
        /// <param name="eventsUri">the events URI</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder EventsUri(Uri eventsUri)
        {
            _eventsUri = eventsUri;
            return this;
        }

        /// <summary>
        /// Sets the object to be used for sending HTTP requests, if a specific implementation is desired.
        /// </summary>
        /// <remarks>
        /// This is exposed mainly for testing purposes; you should not normally need to change it. The default
        /// value is an <see cref="System.Net.Http.HttpClientHandler"/>, but if you do not change this value,
        /// on mobile platforms it will be replaced by the appropriate native HTTP handler for the current
        /// current platform, if any (e.g. <c>Xamarin.Android.Net.AndroidClientHandler</c>). If you set it
        /// explicitly to <see langword="null"/>, the SDK will call the default <see cref="HttpClient"/>
        /// constructor without specifying a handler, which may or may not result in using a native HTTP handler
        /// (depending on your application configuration).
        /// </remarks>
        /// <param name="httpMessageHandler">the <see cref="System.Net.Http.HttpMessageHandler"/> to use</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder HttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            _httpMessageHandler = httpMessageHandler;
            return this;
        }

        /// <summary>
        /// Sets whether to include full user details in every analytics event.
        /// </summary>
        /// <remarks>
        /// The default is <see langword="false"/>: events will only include the user key, except for one
        /// "index" event that provides the full details for the user.
        /// </remarks>
        /// <param name="inlineUsersInEvents">true or false</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder InlineUsersInEvents(bool inlineUsersInEvents)
        {
            _inlineUsersInEvents = inlineUsersInEvents;
            return this;
        }

        /// <summary>
        /// Sets whether or not the streaming API should be used to receive flag updates.
        /// </summary>
        /// <remarks>
        /// This is <see langword="true"/> by default. Streaming should only be disabled on the advice of LaunchDarkly support.
        /// </remarks>
        /// <param name="isStreamingEnabled">true if the streaming API should be used</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder IsStreamingEnabled(bool isStreamingEnabled)
        {
            _isStreamingEnabled = isStreamingEnabled;
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
        public ConfigurationBuilder Logging(ILoggingConfigurationFactory loggingConfigurationFactory)
        {
            _loggingConfigurationFactory = loggingConfigurationFactory;
            return this;
        }

        /// <summary>
        /// Sets the SDK's logging configuration, using a factory object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This object is normally a configuration builder obtained from <see cref="Components.Logging()"/>
        /// which has methods for setting individual logging-related properties. As a shortcut for disabling
        /// logging, you may use <see cref="Components.NoLogging"/> instead. If all you want to do is to set
        /// the basic logging destination, and you do not need to set other logging properties, you can use
        /// <see cref="Logging(ILogAdapter)"/> instead.
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
        /// <param name="loggingConfigurationFactory">the factory object</param>
        /// <returns>the same builder</returns>
        /// <seealso cref="Components.Logging()" />
        /// <seealso cref="Components.Logging(ILogAdapter) "/>
        /// <seealso cref="Components.NoLogging" />
        /// <seealso cref="Logging(ILogAdapter)"/>
        public ConfigurationBuilder Logging(ILogAdapter logAdapter) =>
            Logging(Components.Logging(logAdapter));

        /// <summary>
        /// Sets the key for your LaunchDarkly environment.
        /// </summary>
        /// <remarks>
        /// This should be the "mobile key" field for the environment on your LaunchDarkly dashboard.
        /// </remarks>
        /// <param name="mobileKey"></param>
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
        /// Sets whether the SDK should save flag values for each user in persistent storage, so they will be
        /// immediately available the next time the SDK is started for the same user.
        /// </summary>
        /// <remarks>
        /// The default is <see langword="true"/>.
        /// </remarks>
        /// <param name="persistFlagValues"><see langword="true"/> to save flag values</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder PersistFlagValues(bool persistFlagValues)
        {
            _persistFlagValues = persistFlagValues;
            return this;
        }

        /// <summary>
        /// Sets the polling interval (when streaming is disabled).
        /// </summary>
        /// <remarks>
        /// The default is <see cref="Configuration.DefaultPollingInterval"/>; the minimum is
        /// <see cref="Configuration.MinimumPollingInterval"/>.
        /// </remarks>
        /// <param name="pollingInterval">the rule update polling interval</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder PollingInterval(TimeSpan pollingInterval)
        {
            if (pollingInterval.CompareTo(Configuration.MinimumPollingInterval) < 0)
            {
                _pollingInterval = Configuration.MinimumPollingInterval;
            }
            else
            {
                _pollingInterval = pollingInterval;
            }
            return this;
        }

        /// <summary>
        /// Marks an attribute name as private for all users.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with this name
        /// removed, even if you did not use the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/>
        /// method in <see cref="UserBuilder"/>.
        /// </para>
        /// <para>
        /// You may call this method repeatedly to mark multiple attributes as private.
        /// </para>
        /// </remarks>
        /// <param name="privateAttribute">the attribute</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder PrivateAttribute(UserAttribute privateAttribute)
        {
            if (_privateAttributeNames is null)
            {
                _privateAttributeNames = new HashSet<UserAttribute>();
            }
            _privateAttributeNames.Add(privateAttribute);
            return this;
        }

        /// <summary>
        /// Sets the timeout when reading data from the streaming connection.
        /// </summary>
        /// <remarks>
        /// The default value is 5 minutes.
        /// </remarks>
        /// <param name="readTimeout">the read timeout</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder ReadTimeout(TimeSpan readTimeout)
        {
            _readTimeout = readTimeout;
            return this;
        }

        /// <summary>
        /// Sets the reconnect base time for the streaming connection.
        /// </summary>
        /// <remarks>
        /// The streaming connection uses an exponential backoff algorithm (with jitter) for reconnects, but
        /// will start the backoff with a value near the value specified here. The default value is 1 second.
        /// </remarks>
        /// <param name="reconnectTime">the reconnect time base value</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder ReconnectTime(TimeSpan reconnectTime)
        {
            _reconnectTime = reconnectTime;
            return this;
        }

        /// <summary>
        /// Sets the base URI of the LaunchDarkly streaming server.
        /// </summary>
        /// <param name="streamUri">the stream URI</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder StreamUri(Uri streamUri)
        {
            _streamUri = streamUri;
            return this;
        }

        /// <summary>
        /// Sets the number of user keys that the event processor can remember at any one time.
        /// </summary>
        /// <remarks>
        /// The event processor keeps track of recently seen user keys so that duplicate user details will not
        /// be sent in analytics events.
        /// </remarks>
        /// <param name="userKeysCapacity">the user key cache capacity</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder UserKeysCapacity(int userKeysCapacity)
        {
            _userKeysCapacity = userKeysCapacity;
            return this;
        }

        /// <summary>
        /// Sets the interval at which the event processor will clear its cache of known user keys.
        /// </summary>
        /// <remarks>
        /// The default value is five minutes.
        /// </remarks>
        /// <param name="userKeysFlushInterval">the flush interval</param>
        /// <returns>the same builder</returns>
        public ConfigurationBuilder UserKeysFlushInterval(TimeSpan userKeysFlushInterval)
        {
            _userKeysFlushInterval = userKeysFlushInterval;
            return this;
        }

        // The following properties are internal and settable only for testing.

        internal ConfigurationBuilder BackgroundModeManager(IBackgroundModeManager backgroundModeManager)
        {
            _backgroundModeManager = backgroundModeManager;
            return this;
        }

        internal ConfigurationBuilder BackgroundPollingIntervalWithoutMinimum(TimeSpan backgroundPollingInterval)
        {
            _backgroundPollingInterval = backgroundPollingInterval;
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

        internal ConfigurationBuilder EventProcessor(IEventProcessor eventProcessor)
        {
            _eventProcessor = eventProcessor;
            return this;
        }

        internal ConfigurationBuilder FlagCacheManager(IFlagCacheManager flagCacheManager)
        {
            _flagCacheManager = flagCacheManager;
            return this;
        }

        internal ConfigurationBuilder FlagChangedEventManager(IFlagChangedEventManager flagChangedEventManager)
        {
            _flagChangedEventManager = flagChangedEventManager;
            return this;
        }

        internal ConfigurationBuilder PersistentStorage(IPersistentStorage persistentStorage)
        {
            _persistentStorage = persistentStorage;
            return this;
        }

        internal ConfigurationBuilder UpdateProcessorFactory(Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> updateProcessorFactory)
        {
            _updateProcessorFactory = updateProcessorFactory;
            return this;
        }
    }
}
