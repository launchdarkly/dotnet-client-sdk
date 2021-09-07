using System;
using System.Collections.Immutable;
using System.Net.Http;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.Events;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Http;

namespace LaunchDarkly.Sdk.Client
{
    /// <summary>
    /// Configuration options for <see cref="LdClient"/>. 
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="Configuration"/> are immutable once created. They can be created with the factory method
    /// <see cref="Configuration.Default(string)"/>, or using a builder pattern with <see cref="Configuration.Builder(string)"/>
    /// or <see cref="Configuration.Builder(Configuration)"/>.
    /// </remarks>
    public sealed class Configuration
    {
        /// <summary>
        /// Default value for <see cref="PollingDataSourceBuilder.BackgroundPollInterval"/> and
        /// <see cref="StreamingDataSourceBuilder.BackgroundPollInterval"/>.
        /// </summary>
        public static readonly TimeSpan DefaultBackgroundPollInterval = TimeSpan.FromMinutes(60);

        /// <summary>
        /// Minimum value for <see cref="PollingDataSourceBuilder.BackgroundPollInterval"/> and
        /// <see cref="StreamingDataSourceBuilder.BackgroundPollInterval"/>.
        /// </summary>
        public static readonly TimeSpan MinimumBackgroundPollInterval = TimeSpan.FromMinutes(15);

        // Settable only for testing
        internal IBackgroundModeManager BackgroundModeManager { get; }
        internal IConnectivityStateManager ConnectivityStateManager { get; }
        internal IDeviceInfo DeviceInfo { get; }
        internal IEventProcessor EventProcessor { get; }
        internal IFlagCacheManager FlagCacheManager { get; }
        internal IFlagChangedEventManager FlagChangedEventManager { get; }
        internal IPersistentStorage PersistentStorage { get; }

        /// <summary>
        /// Whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server).
        /// </summary>
        /// <remarks>
        /// By default, this is <see langword="false"/>. If <see langword="true"/>, all of the user attributes
        /// will be private, not just the attributes specified with <see cref="ConfigurationBuilder.PrivateAttribute(UserAttribute)"/>
        /// or with the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/> method.
        /// </remarks>
        public bool AllAttributesPrivate { get; }

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
        public bool AutoAliasingOptOut { get; }

        /// <summary>
        /// The connection timeout to the LaunchDarkly server.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; }

        /// <summary>
        /// A factory object that creates an implementation of <see cref="IDataSource"/>, which will
        /// receive feature flag data.
        /// </summary>
        public IDataSourceFactory DataSourceFactory { get; }

        /// <summary>
        /// Whether to enable feature flag updates when the application is running in the background.
        /// </summary>
        /// <remarks>
        /// This is only relevant on mobile platforms.
        /// </remarks>
        public bool EnableBackgroundUpdating { get; }

        /// <summary>
        /// True if LaunchDarkly should provide additional information about how flag values were
        /// calculated.
        /// </summary>
        /// <remarks>
        /// The additional information will then be available through the client's "detail"
        /// methods such as <see cref="LdClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to <see langword="true"/>.
        /// </remarks>
        public bool EvaluationReasons { get; }

        /// <summary>
        /// The capacity of the event buffer.
        /// </summary>
        /// <remarks>
        /// The client buffers up to this many events in memory before flushing. If the capacity is exceeded
        /// before the buffer is flushed, events will be discarded. Increasing the capacity means that events
        /// are less likely to be discarded, at the cost of consuming more memory.
        /// </remarks>
        public int EventCapacity { get; }

        /// <summary>
        /// The time between flushes of the event buffer.
        /// </summary>
        /// <remarks>
        /// Decreasing the flush interval means that the event buffer is less likely to reach capacity.
        /// </remarks>
        public TimeSpan EventFlushInterval { get; }

        /// <summary>
        /// The base URL of the LaunchDarkly analytics event server.
        /// </summary>
        public Uri EventsUri { get; }

        /// <summary>
        /// The object to be used for sending HTTP requests, if a specific implementation is desired.
        /// </summary>
        public HttpMessageHandler HttpMessageHandler { get; }

        /// <summary>
        /// Sets whether to include full user details in every analytics event.
        /// </summary>
        /// <remarks>
        /// The default is <see langword="false"/>: events will only include the user key, except for one
        /// "index" event that provides the full details for the user.
        /// </remarks>
        public bool InlineUsersInEvents { get; }

        internal ILoggingConfigurationFactory LoggingConfigurationFactory { get; }

        /// <summary>
        /// The key for your LaunchDarkly environment.
        /// </summary>
        /// <remarks>
        /// This should be the "mobile key" field for the environment on your LaunchDarkly dashboard.
        /// </remarks>
        public string MobileKey { get; }

        /// <summary>
        /// Whether or not this client is offline. If <see langword="true"/>, no calls to LaunchDarkly will be made.
        /// </summary>
        public bool Offline { get; }

        /// <summary>
        /// Whether the SDK should save flag values for each user in persistent storage, so they will be
        /// immediately available the next time the SDK is started for the same user.
        /// </summary>
        /// <remarks>
        /// The default is <see langword="true"/>.
        /// </remarks>
        public bool PersistFlagValues { get; }

        /// <summary>
        /// Attribute names that have been marked as private for all users.
        /// </summary>
        /// <remarks>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with this name
        /// removed, even if you did not use the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/>
        /// method when building the user.
        /// </remarks>
        public IImmutableSet<UserAttribute> PrivateAttributeNames { get; }

        /// <summary>
        /// The timeout when reading data from the streaming connection.
        /// </summary>
        public TimeSpan ReadTimeout { get; }

        /// <summary>
        /// Whether to use the HTTP REPORT method for feature flag requests.
        /// </summary>
        /// <remarks>
        /// By default, polling and streaming connections are made with the GET method, witht the user data
        /// encoded into the request URI. Using REPORT allows the user data to be sent in the request body instead.
        /// However, some network gateways do not support REPORT.
        /// </remarks>
        internal bool UseReport { get; }
        // UseReport is currently disabled due to Android HTTP issues (ch47341), but it's still implemented internally

        internal static readonly Uri DefaultUri = new Uri("https://app.launchdarkly.com");
        internal static readonly Uri DefaultStreamUri = new Uri("https://clientstream.launchdarkly.com");
        internal static readonly Uri DefaultEventsUri = new Uri("https://mobile.launchdarkly.com");
        internal static readonly int DefaultEventCapacity = 100;
        internal static readonly TimeSpan DefaultEventFlushInterval = TimeSpan.FromSeconds(5);
        internal static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromMinutes(5);
        internal  static readonly TimeSpan DefaultReconnectTime = TimeSpan.FromSeconds(1);
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Creates a configuration with all parameters set to the default.
        /// </summary>
        /// <param name="mobileKey">the SDK key for your LaunchDarkly environment</param>
        /// <returns>a <see cref="Configuration"/> instance</returns>
        public static Configuration Default(string mobileKey)
        {
            return Builder(mobileKey).Build();
        }

        /// <summary>
        /// Creates a <see cref="ConfigurationBuilder"/> for constructing a configuration object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// This is the only method for building a <see cref="Configuration"/> if you are setting properties
        /// besides the <c>MobileKey</c>. The <see cref="ConfigurationBuilder"/> has methods for setting any number of
        /// properties, after which you call <see cref="ConfigurationBuilder.Build"/> to get the resulting
        /// <c>Configuration</c> instance.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var config = Configuration.Builder("my-sdk-key")
        ///         .EventFlushInterval(TimeSpan.FromSeconds(90))
        ///         .StartWaitTime(TimeSpan.FromSeconds(5))
        ///         .Build();
        /// </code>
        /// </example>
        /// <param name="mobileKey">the mobile SDK key for your LaunchDarkly environment</param>
        /// <returns>a builder object</returns>
        public static ConfigurationBuilder Builder(string mobileKey)
        {
            if (String.IsNullOrEmpty(mobileKey))
            {
                throw new ArgumentOutOfRangeException(nameof(mobileKey), "key is required");
            }
            return new ConfigurationBuilder(mobileKey);
        }

        /// <summary>
        /// Creates a <see cref="ConfigurationBuilder"/> starting with the properties of an existing <see cref="Configuration"/>.
        /// </summary>
        /// <param name="fromConfiguration">the configuration to copy</param>
        /// <returns>a builder object</returns>
        public static ConfigurationBuilder Builder(Configuration fromConfiguration)
        {
            return new ConfigurationBuilder(fromConfiguration);
        }

        internal Configuration(ConfigurationBuilder builder)
        {
            AllAttributesPrivate = builder._allAttributesPrivate;
            AutoAliasingOptOut = builder._autoAliasingOptOut;
            ConnectionTimeout = builder._connectionTimeout;
            DataSourceFactory = builder._dataSourceFactory;
            EnableBackgroundUpdating = builder._enableBackgroundUpdating;
            EvaluationReasons = builder._evaluationReasons;
            EventFlushInterval = builder._eventFlushInterval;
            EventCapacity = builder._eventCapacity;
            EventsUri = builder._eventsUri;
            HttpMessageHandler = object.ReferenceEquals(builder._httpMessageHandler, ConfigurationBuilder.DefaultHttpMessageHandlerInstance) ?
                PlatformSpecific.Http.CreateHttpMessageHandler(builder._connectionTimeout, builder._readTimeout) :
                builder._httpMessageHandler;
            InlineUsersInEvents = builder._inlineUsersInEvents;
            LoggingConfigurationFactory = builder._loggingConfigurationFactory;
            MobileKey = builder._mobileKey;
            Offline = builder._offline;
            PersistFlagValues = builder._persistFlagValues;
            PrivateAttributeNames = builder._privateAttributeNames is null ? null :
                builder._privateAttributeNames.ToImmutableHashSet();
            UseReport = builder._useReport;

            BackgroundModeManager = builder._backgroundModeManager;
            ConnectivityStateManager = builder._connectivityStateManager;
            DeviceInfo = builder._deviceInfo;
            EventProcessor = builder._eventProcessor;
            FlagCacheManager = builder._flagCacheManager;
            FlagChangedEventManager = builder._flagChangedEventManager;
            PersistentStorage = builder._persistentStorage;
        }

        internal HttpProperties HttpProperties => HttpProperties.Default
            .WithAuthorizationKey(this.MobileKey)
            .WithConnectTimeout(this.ConnectionTimeout)
            .WithHttpMessageHandlerFactory(_ => this.HttpMessageHandler)
            .WithReadTimeout(this.ReadTimeout)
            .WithUserAgent("XamarinClient/" + AssemblyVersions.GetAssemblyVersionStringForType(typeof(LdClient)));
    }
}
