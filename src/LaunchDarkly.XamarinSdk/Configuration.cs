using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using LaunchDarkly.Client;

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
    public class Configuration : IMobileConfiguration
    {
        private readonly bool _allAttributesPrivate;
        private readonly TimeSpan _backgroundPollingInterval;
        private readonly Uri _baseUri;
        private readonly TimeSpan _connectionTimeout;
        private readonly bool _enableBackgroundUpdating;
        private readonly bool _evaluationReasons;
        private readonly TimeSpan _eventFlushInterval;
        private readonly int _eventCapacity;
        private readonly int _eventSamplingInterval;
        private readonly Uri _eventsUri;
        private readonly HttpClientHandler _httpClientHandler;
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
        /// the LaunchDarkly server). If this is true, all of the user attributes will be private,
        /// not just the attributes specified with the <c>AndPrivate...</c> methods on the
        /// <see cref="Client.User"/> object. By default, this is false.
        /// </summary>
        public bool AllAttributesPrivate => _allAttributesPrivate;

        /// <see cref="IMobileConfiguration.BackgroundPollingInterval"/>
        public TimeSpan BackgroundPollingInterval => _backgroundPollingInterval;

        /// <summary>
        /// The base URI of the LaunchDarkly server.
        /// </summary>
        public Uri BaseUri => _baseUri;

        /// <see cref="IMobileConfiguration.ConnectionTimeout"/>
        public TimeSpan ConnectionTimeout { get; internal set; }

        /// <see cref="IMobileConfiguration.EnableBackgroundUpdating"/>
        public bool EnableBackgroundUpdating => _enableBackgroundUpdating;

        /// <see cref="IMobileConfiguration.EvaluationReasons"/>
        public bool EvaluationReasons => _evaluationReasons;

        /// <summary>
        /// The time between flushes of the event buffer. Decreasing the flush interval means
        /// that the event buffer is less likely to reach capacity. The default value is 5 seconds.
        /// </summary>
        public TimeSpan EventFlushInterval => _eventFlushInterval;

        /// <summary>
        /// The capacity of the events buffer. The client buffers up to this many events in
        /// memory before flushing. If the capacity is exceeded before the buffer is flushed,
        /// events will be discarded. Increasing the capacity means that events are less likely
        /// to be discarded, at the cost of consuming more memory.
        /// </summary>
        public int EventCapacity => _eventCapacity;

        /// <summary>
        /// Deprecated name for <see cref="EventCapacity"/>.
        /// </summary>
        [Obsolete]
        public int EventQueueCapacity => EventCapacity;

        /// <summary>
        /// Deprecated name for <see cref="EventFlushInterval"/>.
        /// </summary>
        [Obsolete]
        public TimeSpan EventQueueFrequency => EventFlushInterval;

        /// <summary>
        /// Enables event sampling if non-zero. When set to the default of zero, all analytics events are
        /// sent back to LaunchDarkly. When greater than zero, there is a 1 in <c>EventSamplingInterval</c>
        /// chance that events will be sent (example: if the interval is 20, on average 5% of events will be sent).
        /// </summary>
        public int EventSamplingInterval => _eventSamplingInterval;

        /// <summary>
        /// The base URL of the LaunchDarkly analytics event server.
        /// </summary>
        public Uri EventsUri => _eventsUri;
        
        /// <summary>
        /// The object to be used for sending HTTP requests. This is exposed for testing purposes.
        /// </summary>
        public HttpClientHandler HttpClientHandler => _httpClientHandler;

        /// <summary>
        /// The connection timeout. The default value is 10 seconds.
        /// </summary>
        public TimeSpan HttpClientTimeout => _httpClientTimeout;

        /// <summary>
        /// True if full user details should be included in every analytics event. The default is false (events will
        /// only include the user key, except for one "index" event that provides the full details for the user).
        /// </summary>
        public bool InlineUsersInEvents => _inlineUsersInEvents;

        /// <summary>
        /// Whether or not the streaming API should be used to receive flag updates. This is true by default.
        /// Streaming should only be disabled on the advice of LaunchDarkly support.
        /// </summary>
        public bool IsStreamingEnabled => _isStreamingEnabled;

        /// <summary>
        /// The Mobile key for your LaunchDarkly environment.
        /// </summary>
        public string MobileKey => _mobileKey;

        /// <summary>
        /// Whether or not this client is offline. If true, no calls to Launchdarkly will be made.
        /// </summary>
        public bool Offline => _offline;

        /// <see cref="IMobileConfiguration.PersistFlagValues"/>
        public bool PersistFlagValues => _persistFlagValues;

        /// <summary>
        /// Set the polling interval (when streaming is disabled). The default value is 30 seconds.
        /// </summary>
        public TimeSpan PollingInterval => _pollingInterval;

        /// <summary>
        /// Marks a set of attribute names as private. Any users sent to LaunchDarkly with this
        /// configuration active will have attributes with these names removed, even if you did
        /// not use the <c>AndPrivate...</c> methods on the <see cref="Client.User"/> object.
        /// </summary>
        public ISet<string> PrivateAttributeNames => _privateAttributeNames;

        /// <summary>
        /// The timeout when reading data from the EventSource API. The default value is 5 minutes.
        /// </summary>
        public TimeSpan ReadTimeout => _readTimeout;

        /// <summary>
        /// The reconnect base time for the streaming connection.The streaming connection
        /// uses an exponential backoff algorithm (with jitter) for reconnects, but will start the
        /// backoff with a value near the value specified here. The default value is 1 second.
        /// </summary>
        public TimeSpan ReconnectTime => _reconnectTime;

        /// <summary>
        /// Alternate name for <see cref="MobileKey"/>.
        /// </summary>
        public string SdkKey => MobileKey;

        /// <summary>
        /// The base URL of the LaunchDarkly streaming server.
        /// </summary>
        public Uri StreamUri => _streamUri;

        /// <see cref="IMobileConfiguration.UseReport"/>
        public bool UseReport => _useReport;

        /// <summary>
        /// The number of user keys that the event processor can remember at any one time, so that
        /// duplicate user details will not be sent in analytics events.
        /// </summary>
        public int UserKeysCapacity => _userKeysCapacity;

        /// <summary>
        /// The interval at which the event processor will reset its set of known user keys. The
        /// default value is five minutes.
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

        /// <summary>        /// Creates a <see cref="ConfigurationBuilder"/> for constructing a configuration object using a fluent syntax.        /// </summary>        /// <remarks>        /// This is the only method for building a <c>Configuration</c> if you are setting properties        /// besides the <c>MobileKey</c>. The <c>ConfigurationBuilder</c> has methods for setting any number of        /// properties, after which you call <see cref="ConfigurationBuilder.Build"/> to get the resulting        /// <c>Configuration</c> instance.        /// </remarks>        /// <example>        /// <code>        ///     var config = Configuration.Builder("my-sdk-key")        ///         .EventQueueFrequency(TimeSpan.FromSeconds(90))        ///         .StartWaitTime(TimeSpan.FromSeconds(5))        ///         .Build();        /// </code>        /// </example>        /// <param name="mobileKey">the SDK key for your LaunchDarkly environment</param>
        /// <returns>a builder object</returns>        public static IConfigurationBuilder Builder(string mobileKey)        {
            if (String.IsNullOrEmpty(mobileKey))
            {
                throw new ArgumentOutOfRangeException(nameof(mobileKey), "key is required");
            }
            return new ConfigurationBuilder(mobileKey);        }

        internal static ConfigurationBuilder BuilderInternal(string mobileKey)        {
            return new ConfigurationBuilder(mobileKey);        }

        public static IConfigurationBuilder Builder(Configuration fromConfiguration)
        {
            return new ConfigurationBuilder(fromConfiguration);
        }

        internal Configuration(ConfigurationBuilder builder)
        {
            _allAttributesPrivate = builder._allAttributesPrivate;            _backgroundPollingInterval = builder._backgroundPollingInterval;            _baseUri = builder._baseUri;            _connectionTimeout = builder._connectionTimeout;            _enableBackgroundUpdating = builder._enableBackgroundUpdating;            _evaluationReasons = builder._evaluationReasons;            _eventFlushInterval = builder._eventFlushInterval;            _eventCapacity = builder._eventCapacity;            _eventSamplingInterval = builder._eventSamplingInterval;            _eventsUri = builder._eventsUri;            _httpClientHandler = builder._httpClientHandler;            _httpClientTimeout = builder._httpClientTimeout;            _inlineUsersInEvents = builder._inlineUsersInEvents;            _isStreamingEnabled = builder._isStreamingEnabled;            _mobileKey = builder._mobileKey;            _offline = builder._offline;            _persistFlagValues = builder._persistFlagValues;            _pollingInterval = builder._pollingInterval;            _privateAttributeNames = builder._privateAttributeNames is null ? null :                builder._privateAttributeNames.ToImmutableHashSet();            _readTimeout = builder._readTimeout;            _reconnectTime = builder._reconnectTime;            _streamUri = builder._streamUri;            _useReport = builder._useReport;            _userKeysCapacity = builder._userKeysCapacity;            _userKeysFlushInterval = builder._userKeysFlushInterval;

            _connectionManager = builder._connectionManager;
            _deviceInfo = builder._deviceInfo;
            _eventProcessor = builder._eventProcessor;
            _flagCacheManager = builder._flagCacheManager;
            _flagChangedEventManager = builder._flagChangedEventManager;
            _persistentStorage = builder._persistentStorage;
            _updateProcessorFactory = builder._updateProcessorFactory;
        }
    }
}
