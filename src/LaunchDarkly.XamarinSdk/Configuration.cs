using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using LaunchDarkly.Client;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// This class exposes advanced configuration options for <see cref="LdClient"/>.
    /// </summary>
    /// <remarks>
    /// Instances of <c>Configuration</c> are immutable once created. They can be created with the factory method
    /// <see cref="Configuration.Default(string)"/>, or using a builder pattern with <see cref="Configuration.Builder(string)"/>
    /// or <see cref="Configuration.Builder(Configuration)"/>.
    /// </remarks>
    public sealed class Configuration
    {
        private readonly bool _allAttributesPrivate;
        private readonly TimeSpan _backgroundPollingInterval;
        private readonly Uri _baseUri;
        private readonly TimeSpan _connectionTimeout;
        private readonly bool _enableBackgroundUpdating;
        private readonly bool _evaluationReasons;
        private readonly TimeSpan _eventFlushInterval;
        private readonly int _eventCapacity;
        private readonly Uri _eventsUri;
        private readonly HttpMessageHandler _httpMessageHandler;
        private readonly TimeSpan _httpClientTimeout;
        private readonly bool _inlineUsersInEvents;
        private readonly bool _isStreamingEnabled;
        private readonly string _mobileKey;
        private readonly bool _offline;
        private readonly bool _persistFlagValues;
        private readonly TimeSpan _pollingInterval;
        private readonly ImmutableHashSet<string> _privateAttributeNames;
        private readonly TimeSpan _readTimeout;
        private readonly TimeSpan _reconnectTime;
        private readonly Uri _streamUri;
        private readonly bool _useReport;
        private readonly int _userKeysCapacity;
        private readonly TimeSpan _userKeysFlushInterval;

        // Settable only for testing
        internal readonly IConnectionManager _connectionManager;
        internal readonly IDeviceInfo _deviceInfo;
        internal readonly IEventProcessor _eventProcessor;
        internal readonly IFlagCacheManager _flagCacheManager;
        internal readonly IFlagChangedEventManager _flagChangedEventManager;
        internal readonly IPersistentStorage _persistentStorage;
        internal readonly Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> _updateProcessorFactory;

        /// <summary>
        /// Whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server).
        /// </summary>
        /// <remarks>
        /// If this is true, all of the user attributes will be private, not just the attributes specified with
        /// <see cref="ConfigurationBuilder.PrivateAttribute(string)"/> or with the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/>
        /// method in <see cref="UserBuilder"/>.
        ///
        /// By default, this is false.
        /// </remarks>
        public bool AllAttributesPrivate => _allAttributesPrivate;

        /// <summary>
        /// The interval between feature flag updates when the application is running in the background.
        /// </summary>
        /// <remarks>
        /// This is only relevant on mobile platforms.
        /// </remarks>
        public TimeSpan BackgroundPollingInterval => _backgroundPollingInterval;

        /// <summary>
        /// The base URI of the LaunchDarkly server.
        /// </summary>
        public Uri BaseUri => _baseUri;

        /// <summary>
        /// The connection timeout to the LaunchDarkly server.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; internal set; }

        /// <summary>
        /// Whether to enable feature flag updates when the application is running in the background.
        /// </summary>
        /// <remarks>
        /// This is only relevant on mobile platforms.
        /// </remarks>
        public bool EnableBackgroundUpdating => _enableBackgroundUpdating;

        /// <summary>
        /// True if LaunchDarkly should provide additional information about how flag values were
        /// calculated.
        /// </summary>
        /// <remarks>
        /// The additional information will then be available through the client's "detail"
        /// methods such as <see cref="ILdClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to true.
        /// </remarks>
        public bool EvaluationReasons => _evaluationReasons;

        /// <summary>
        /// The capacity of the events buffer.
        /// </summary>
        /// <remarks>
        /// The client buffers up to this many events in memory before flushing. If the capacity is exceeded
        /// before the buffer is flushed, events will be discarded. Increasing the capacity means that events
        /// are less likely to be discarded, at the cost of consuming more memory.
        /// </remarks>
        public int EventCapacity => _eventCapacity;

        /// <summary>
        /// The time between flushes of the event buffer.
        /// </summary>
        /// <remarks>
        /// Decreasing the flush interval means that the event buffer is less likely to reach capacity.
        /// </remarks>
        public TimeSpan EventFlushInterval => _eventFlushInterval;

        /// <summary>
        /// The base URL of the LaunchDarkly analytics event server.
        /// </summary>
        public Uri EventsUri => _eventsUri;
        
        /// <summary>
        /// The object to be used for sending HTTP requests, if a specific implementation is desired.
        /// </summary>
        public HttpMessageHandler HttpMessageHandler => _httpMessageHandler;

        /// <summary>
        /// The connection timeout. The default value is 10 seconds.
        /// </summary>
        public TimeSpan HttpClientTimeout => _httpClientTimeout;

        /// <summary>
        /// Sets whether to include full user details in every analytics event.
        /// </summary>
        /// <remarks>
        /// The default is false: events will only include the user key, except for one "index" event that
        /// provides the full details for the user.
        /// </remarks>
        public bool InlineUsersInEvents => _inlineUsersInEvents;

        /// <summary>
        /// Whether or not the streaming API should be used to receive flag updates.
        /// </summary>
        /// <remarks>
        /// This is true by default. Streaming should only be disabled on the advice of LaunchDarkly support.
        /// </remarks>
        public bool IsStreamingEnabled => _isStreamingEnabled;

        /// <summary>
        /// The key for your LaunchDarkly environment.
        /// </summary>
        /// <remarks>
        /// This should be the "mobile key" field for the environment on your LaunchDarkly dashboard.
        /// </remarks>
        public string MobileKey => _mobileKey;

        /// <summary>
        /// Whether or not this client is offline. If true, no calls to Launchdarkly will be made.
        /// </summary>
        public bool Offline => _offline;

        /// <summary>
        /// Whether the SDK should save flag values for each user in persistent storage, so they will be
        /// immediately available the next time the SDK is started for the same user.
        /// </summary>
        /// <remarks>
        /// The default is <c>true</c>.
        /// </remarks>
        public bool PersistFlagValues => _persistFlagValues;

        /// <summary>
        /// The polling interval (when streaming is disabled).
        /// </summary>
        public TimeSpan PollingInterval => _pollingInterval;

        /// <summary>
        /// Attribute names that have been marked as private for all users.
        /// </summary>
        /// <remarks>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with this name
        /// removed, even if you did not use the <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/>
        /// method in <see cref="UserBuilder"/>.
        /// </remarks>
        public IImmutableSet<string> PrivateAttributeNames => _privateAttributeNames;

        /// <summary>
        /// The timeout when reading data from the streaming connection.
        /// </summary>
        public TimeSpan ReadTimeout => _readTimeout;

        /// <summary>
        /// The reconnect base time for the streaming connection.
        /// </summary>
        /// <remarks>
        /// The streaming connection uses an exponential backoff algorithm (with jitter) for reconnects, but
        /// will start the backoff with a value near the value specified here. The default value is 1 second.
        /// </remarks>
        public TimeSpan ReconnectTime => _reconnectTime;

        /// <summary>
        /// The base URL of the LaunchDarkly streaming server.
        /// </summary>
        public Uri StreamUri => _streamUri;

        /// <summary>
        /// Whether to use the HTTP REPORT method for feature flag requests.
        /// </summary>
        /// <remarks>
        /// By default, polling and streaming connections are made with the GET method, witht the user data
        /// encoded into the request URI. Using REPORT allows the user data to be sent in the request body instead.
        /// However, some network gateways do not support REPORT.
        /// </remarks>
        internal bool UseReport => _useReport;
        // UseReport is currently disabled due to Android HTTP issues (ch47341), but it's still implemented internally

        /// <summary>
        /// The number of user keys that the event processor can remember at any one time.
        /// </summary>
        /// <remarks>
        /// The event processor keeps track of recently seen user keys so that duplicate user details will not
        /// be sent in analytics events.
        /// </remarks>
        public int UserKeysCapacity => _userKeysCapacity;

        /// <summary>
        /// The interval at which the event processor will reset its set of known user keys.
        /// </summary>
        public TimeSpan UserKeysFlushInterval => _userKeysFlushInterval;

        /// <summary>
        /// Default value for <see cref="PollingInterval"/>.
        /// </summary>
        public static TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Minimum value for <see cref="PollingInterval"/>.
        /// </summary>
        public static TimeSpan MinimumPollingInterval = TimeSpan.FromMinutes(5);

        internal static readonly Uri DefaultUri = new Uri("https://app.launchdarkly.com");
        internal static readonly Uri DefaultStreamUri = new Uri("https://clientstream.launchdarkly.com");
        internal static readonly Uri DefaultEventsUri = new Uri("https://mobile.launchdarkly.com");
        internal static readonly int DefaultEventCapacity = 100;
        internal static readonly TimeSpan DefaultEventFlushInterval = TimeSpan.FromSeconds(5);
        internal static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromMinutes(5);
        internal  static readonly TimeSpan DefaultReconnectTime = TimeSpan.FromSeconds(1);
        internal static readonly TimeSpan DefaultHttpClientTimeout = TimeSpan.FromSeconds(10);
        internal static readonly int DefaultUserKeysCapacity = 1000;
        internal static readonly TimeSpan DefaultUserKeysFlushInterval = TimeSpan.FromMinutes(5);
        internal static readonly TimeSpan DefaultBackgroundPollingInterval = TimeSpan.FromMinutes(60);
        internal static readonly TimeSpan MinimumBackgroundPollingInterval = TimeSpan.FromMinutes(15);
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Creates a configuration with all parameters set to the default. Use extension methods
        /// to set additional parameters.
        /// </summary>
        /// <param name="mobileKey">the SDK key for your LaunchDarkly environment</param>
        /// <returns>a <c>Configuration</c> instance</returns>
        public static Configuration Default(string mobileKey)
        {
            return Builder(mobileKey).Build();
        }

        /// <summary>
        /// Creates a <c>ConfigurationBuilder</c> for constructing a configuration object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// This is the only method for building a <c>Configuration</c> if you are setting properties
        /// besides the <c>MobileKey</c>. The <c>ConfigurationBuilder</c> has methods for setting any number of
        /// properties, after which you call <see cref="ConfigurationBuilder.Build"/> to get the resulting
        /// <c>Configuration</c> instance.
        /// </remarks>
        /// <example>
        /// <code><pre>
        ///     var config = Configuration.Builder("my-sdk-key")
        ///         .EventQueueFrequency(TimeSpan.FromSeconds(90))
        ///         .StartWaitTime(TimeSpan.FromSeconds(5))
        ///         .Build();
        /// </pre></code>
        /// </example>
        /// <param name="mobileKey">the mobile SDK key for your LaunchDarkly environment</param>
        /// <returns>a builder object</returns>
        public static IConfigurationBuilder Builder(string mobileKey)
        {
            if (String.IsNullOrEmpty(mobileKey))
            {
                throw new ArgumentOutOfRangeException(nameof(mobileKey), "key is required");
            }
            return new ConfigurationBuilder(mobileKey);
        }

        /// <summary>
        /// Exposed for test code that needs to access the internal methods of <c>ConfigurationBuilder</c> that
        /// are not in <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="mobileKey">the mobile SDK key</param>
        /// <returns>a builder object</returns>
        internal static ConfigurationBuilder BuilderInternal(string mobileKey)
        {
            return new ConfigurationBuilder(mobileKey);
        }

        /// <summary>
        /// Creates a <c>ConfigurationBuilder</c> starting with the properties of an existing <c>Configuration</c>.
        /// </summary>
        /// <param name="fromConfiguration">the configuration to copy</param>
        /// <returns>a builder object</returns>
        public static IConfigurationBuilder Builder(Configuration fromConfiguration)
        {
            return new ConfigurationBuilder(fromConfiguration);
        }

        internal Configuration(ConfigurationBuilder builder)
        {
            _allAttributesPrivate = builder._allAttributesPrivate;
            _backgroundPollingInterval = builder._backgroundPollingInterval;
            _baseUri = builder._baseUri;
            _connectionTimeout = builder._connectionTimeout;
            _enableBackgroundUpdating = builder._enableBackgroundUpdating;
            _evaluationReasons = builder._evaluationReasons;
            _eventFlushInterval = builder._eventFlushInterval;
            _eventCapacity = builder._eventCapacity;
            _eventsUri = builder._eventsUri;
            _httpMessageHandler = builder._httpMessageHandler;
            _httpClientTimeout = builder._httpClientTimeout;
            _inlineUsersInEvents = builder._inlineUsersInEvents;
            _isStreamingEnabled = builder._isStreamingEnabled;
            _mobileKey = builder._mobileKey;
            _offline = builder._offline;
            _persistFlagValues = builder._persistFlagValues;
            _pollingInterval = builder._pollingInterval;
            _privateAttributeNames = builder._privateAttributeNames is null ? null :
                builder._privateAttributeNames.ToImmutableHashSet();
            _readTimeout = builder._readTimeout;
            _reconnectTime = builder._reconnectTime;
            _streamUri = builder._streamUri;
            _useReport = builder._useReport;
            _userKeysCapacity = builder._userKeysCapacity;
            _userKeysFlushInterval = builder._userKeysFlushInterval;

            _connectionManager = builder._connectionManager;
            _deviceInfo = builder._deviceInfo;
            _eventProcessor = builder._eventProcessor;
            _flagCacheManager = builder._flagCacheManager;
            _flagChangedEventManager = builder._flagChangedEventManager;
            _persistentStorage = builder._persistentStorage;
            _updateProcessorFactory = builder._updateProcessorFactory;
        }

        internal IEventProcessorConfiguration EventProcessorConfiguration => new EventProcessorAdapter { Config = this };
        internal IHttpRequestConfiguration HttpRequestConfiguration => new HttpRequestAdapter { Config = this };
        internal IStreamManagerConfiguration StreamManagerConfiguration => new StreamManagerAdapter { Config = this };

        private class EventProcessorAdapter : IEventProcessorConfiguration
        {
            internal Configuration Config { get; set; }
            public bool AllAttributesPrivate => Config.AllAttributesPrivate;
            public int EventCapacity => Config.EventCapacity;
            public TimeSpan EventFlushInterval => Config.EventFlushInterval;
            public Uri EventsUri => Config.EventsUri;
            public TimeSpan HttpClientTimeout => Config.HttpClientTimeout;
            public bool InlineUsersInEvents => Config.InlineUsersInEvents;
            public IImmutableSet<string> PrivateAttributeNames => Config.PrivateAttributeNames;
            public TimeSpan ReadTimeout => Config.ReadTimeout;
            public TimeSpan ReconnectTime => Config.ReconnectTime;
            public int UserKeysCapacity => Config.UserKeysCapacity;
            public TimeSpan UserKeysFlushInterval => Config.UserKeysFlushInterval;
        }

        private class HttpRequestAdapter : IHttpRequestConfiguration
        {
            internal Configuration Config { get; set; }
            public string HttpAuthorizationKey => Config.MobileKey;
            public HttpMessageHandler HttpMessageHandler => Config.HttpMessageHandler;
        }

        private class StreamManagerAdapter : IStreamManagerConfiguration
        {
            internal Configuration Config { get; set; }
            public string HttpAuthorizationKey => Config.MobileKey;
            public HttpMessageHandler HttpMessageHandler => Config.HttpMessageHandler;
            public TimeSpan HttpClientTimeout => Config.HttpClientTimeout;
            public TimeSpan ReadTimeout => Config.ReadTimeout;
            public TimeSpan ReconnectTime => Config.ReconnectTime;
        }
    }
}
