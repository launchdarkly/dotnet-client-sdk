using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using Common.Logging;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// A mutable object that uses the Builder pattern to specify properties for a <see cref="Configuration"/> object.
    /// </summary>
    /// <remarks>
    /// Obtain an instance of this class by calling <see cref="Configuration.Builder(string)"/>.
    /// 
    /// All of the builder methods for setting a configuration property return a reference to the same builder, so they can be
    /// chained together.
    /// </remarks>
    /// <example>
    /// <code>
    ///     var config = Configuration.Builder("my-mobile-key").AllAttributesPrivate(true).EventCapacity(1000).Build();
    /// </code>
    /// </example>
    public interface IConfigurationBuilder
    {
        /// <summary>        /// Creates a <see cref="Configuration"/> based on the properties that have been set on the builder.        /// Modifying the builder after this point does not affect the returned <c>Configuration</c>.        /// </summary>        /// <returns>the configured <c>Configuration</c> object</returns>        Configuration Build();        /// <summary>
        /// Sets whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server).
        /// </summary>
        /// <remarks>
        /// If this is true, all of the user attributes will be private, not just the attributes specified with
        /// <see cref="IUserBuilderCanMakeAttributePrivate.AsPrivateAttribute"/> on the <see cref="User"/> object.
        /// By default, this is false.
        /// </remarks>
        /// <param name="allAttributesPrivate">true if all attributes should be private</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder AllAttributesPrivate(bool allAttributesPrivate);        /// <summary>
        /// Sets the interval for background polling.
        /// </summary>
        /// <param name="backgroundPollingInterval">the background polling interval</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder BackgroundPollingInterval(TimeSpan backgroundPollingInterval);        /// <summary>
        /// Sets the base URI of the LaunchDarkly server.
        /// </summary>
        /// <param name="baseUri">the base URI</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder BaseUri(Uri baseUri);        /// <summary>        /// Set to true if LaunchDarkly should provide additional information about how flag values were
        /// calculated.
        /// </summary>
        /// <remarks>
        /// The additional information will then be available through the client's "detail"
        /// methods such as <see cref="ILdMobileClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to true.
        /// </remarks>
        /// <param name="evaluationReasons">True if evaluation reasons are desired.</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder EvaluationReasons(bool evaluationReasons);
                /// <summary>
        /// Sets the capacity of the events buffer.
        /// </summary>
        /// <remarks>
        /// The client buffers up to this many events in memory before flushing. If the capacity is exceeded
        /// before the buffer is flushed, events will be discarded. Increasing the capacity means that events
        /// are less likely to be discarded, at the cost of consuming more memory.
        /// </remarks>
        /// <param name="eventCapacity">the capacity of the events buffer</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder EventCapacity(int eventCapacity);        /// <summary>
        /// Sets the time between flushes of the event buffer.
        /// </summary>
        /// <remarks>
        /// Decreasing the flush interval means that the event buffer is less likely to reach capacity. The
        /// default value is 5 seconds.
        /// </remarks>
        /// <param name="eventflushInterval">the flush interval</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder EventFlushInterval(TimeSpan eventflushInterval);
        /// <summary>
        /// Enables event sampling if non-zero. 
        /// </summary>
        /// <remarks>
        /// When set to the default of zero, all analytics events are sent back to LaunchDarkly. When greater
        /// than zero, there is a 1 in <c>EventSamplingInterval</c> chance that events will be sent (example:
        /// if the interval is 20, on average 5% of events will be sent).
        /// </remarks>
        /// <param name="eventSamplingInterval">the sampling interval</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder EventSamplingInterval(int eventSamplingInterval);        /// <summary>
        /// Sets the base URL of the LaunchDarkly analytics event server.
        /// </summary>
        /// <param name="eventsUri">the events URI</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder EventsUri(Uri eventsUri);

        /// <summary>
        /// Sets the object to be used for sending HTTP requests. This is exposed for testing purposes.
        /// </summary>
        /// <param name="httpClientHandler">the <c>HttpClientHandler</c> to use</param>
        /// <returns>the same builder</returns>        IConfigurationBuilder HttpClientHandler(HttpClientHandler httpClientHandler);        /// <summary>
        /// Sets the connection timeout. The default value is 10 seconds.
        /// </summary>
        /// <param name="httpClientTimeout">the connection timeout</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder HttpClientTimeout(TimeSpan httpClientTimeout);

        /// <summary>
        /// Sets whether to include full user details in every analytics event.
        /// </summary>
        /// <remarks>
        /// The default is false: events will only include the user key, except for one "index" event that
        /// provides the full details for the user.
        /// </remarks>
        /// <param name="inlineUsersInEvents">true or false</param>
        /// <returns>the same builder</returns>        IConfigurationBuilder InlineUsersInEvents(bool inlineUsersInEvents);

        /// <summary>
        /// Sets whether or not the streaming API should be used to receive flag updates.
        /// </summary>
        /// <remarks>
        /// This is true by default. Streaming should only be disabled on the advice of LaunchDarkly support.
        /// </remarks>
        /// <param name="isStreamingEnabled">true if the streaming API should be used</param>
        /// <returns>the same builder</returns>        IConfigurationBuilder IsStreamingEnabled(bool isStreamingEnabled);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobileKey"></param>
        /// <returns>the same builder</returns>        IConfigurationBuilder MobileKey(string mobileKey);        /// <summary>
        /// Sets whether or not this client is offline. If true, no calls to Launchdarkly will be made.
        /// </summary>
        /// <param name="offline">true if the client should remain offline</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder Offline(bool offline);

        /// <summary>
        /// Sets whether the SDK should save flag values for each user in persistent storage, so they will be
        /// immediately available the next time the SDK is started for the same user. The default is <see langword="true"/>.
        /// </summary>
        /// <param name="persistFlagValues">true to save flag values</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        /// <returns>the same builder</returns>
        IConfigurationBuilder PersistFlagValues(bool persistFlagValues);        /// <summary>
        /// Sets the polling interval (when streaming is disabled).
        /// </summary>
        /// <remarks>
        /// Values less than the default of 30 seconds will be changed to the default.
        /// </remarks>
        /// <param name="pollingInterval">the rule update polling interval</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder PollingInterval(TimeSpan pollingInterval);

        /// <summary>
        /// Marks an attribute name as private.
        /// </summary>
        /// <remarks>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with this name
        /// removed, even if you did not use the <c>AndPrivate...</c> methods on the <see cref="User"/> object.
        /// You may call this method repeatedly to mark multiple attributes as private.
        /// </remarks>
        /// <param name="privateAtributeName">the attribute name</param>
        /// <returns>the same builder</returns>        IConfigurationBuilder PrivateAttribute(string privateAtributeName);        /// <summary>
        /// Sets the timeout when reading data from the streaming connection.
        /// </summary>
        /// <remarks>
        /// The default value is 5 minutes.
        /// </remarks>
        /// <param name="readTimeout">the read timeout</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder ReadTimeout(TimeSpan readTimeout);        /// <summary>
        /// Sets the reconnect base time for the streaming connection.
        /// </summary>
        /// <remarks>
        /// The streaming connection uses an exponential backoff algorithm (with jitter) for reconnects, but
        /// will start the backoff with a value near the value specified here. The default value is 1 second.
        /// </remarks>
        /// <param name="reconnectTime">the reconnect time base value</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder ReconnectTime(TimeSpan reconnectTime);        /// <summary>
        /// Sets the base URI of the LaunchDarkly streaming server.
        /// </summary>
        /// <param name="streamUri">the stream URI</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder StreamUri(Uri streamUri);

        /// <summary>
        /// Determines whether to use the REPORT method for networking requests.
        /// </summary>
        /// <param name="useReport">whether to use REPORT mode</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder UseReport(bool useReport);

        /// <summary>
        /// Sets the number of user keys that the event processor can remember at any one time.
        /// </summary>
        /// <remarks>
        /// The event processor keeps track of recently seen user keys so that duplicate user details will not
        /// be sent in analytics events.
        /// </remarks>
        /// <param name="userKeysCapacity">the user key cache capacity</param>
        /// <returns>the same builder</returns>        IConfigurationBuilder UserKeysCapacity(int userKeysCapacity);        /// <summary>
        /// Sets the interval at which the event processor will clear its cache of known user keys.
        /// </summary>
        /// <remarks>
        /// The default value is five minutes.
        /// </remarks>
        /// <param name="userKeysFlushInterval">the flush interval</param>
        /// <returns>the same builder</returns>
        IConfigurationBuilder UserKeysFlushInterval(TimeSpan userKeysFlushInterval);
    }

    internal class ConfigurationBuilder : IConfigurationBuilder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigurationBuilder));

        internal bool _allAttributesPrivate = false;
        internal TimeSpan _backgroundPollingInterval;
        internal Uri _baseUri = Configuration.DefaultUri;
        internal TimeSpan _connectionTimeout;
        internal bool _enableBackgroundUpdating;
        internal bool _evaluationReasons = false;
        internal int _eventCapacity = Configuration.DefaultEventCapacity;
        internal TimeSpan _eventFlushInterval = Configuration.DefaultEventFlushInterval;
        internal int _eventSamplingInterval = 0;
        internal Uri _eventsUri = Configuration.DefaultEventsUri;
        internal HttpClientHandler _httpClientHandler = new HttpClientHandler();
        internal TimeSpan _httpClientTimeout = Configuration.DefaultHttpClientTimeout;
        internal bool _inlineUsersInEvents = false;
        internal bool _isStreamingEnabled = true;
        internal string _mobileKey;
        internal bool _offline = false;
        internal bool _persistFlagValues = true;
        internal TimeSpan _pollingInterval = Configuration.DefaultPollingInterval;
        internal HashSet<string> _privateAttributeNames = null;
        internal TimeSpan _readTimeout = Configuration.DefaultReadTimeout;
        internal TimeSpan _reconnectTime = Configuration.DefaultReconnectTime;
        internal Uri _streamUri = Configuration.DefaultStreamUri;
        internal bool _useReport;
        internal int _userKeysCapacity = Configuration.DefaultUserKeysCapacity;
        internal TimeSpan _userKeysFlushInterval = Configuration.DefaultUserKeysFlushInterval;

        // Internal properties only settable for testing
        internal IConnectionManager _connectionManager;
        internal IDeviceInfo _deviceInfo;
        internal IEventProcessor _eventProcessor;
        internal IFlagCacheManager _flagCacheManager;
        internal IFlagChangedEventManager _flagChangedEventManager;
        internal IPersistentStorage _persistentStorage;
        internal Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> _updateProcessorFactory;

        internal ConfigurationBuilder(string mobileKey)        {            _mobileKey = mobileKey;
        }        internal ConfigurationBuilder(Configuration copyFrom)        {            _allAttributesPrivate = copyFrom.AllAttributesPrivate;            _backgroundPollingInterval = copyFrom.BackgroundPollingInterval;            _baseUri = copyFrom.BaseUri;            _connectionTimeout = copyFrom.ConnectionTimeout;            _enableBackgroundUpdating = copyFrom.EnableBackgroundUpdating;            _evaluationReasons = copyFrom.EvaluationReasons;            _eventCapacity = copyFrom.EventCapacity;            _eventFlushInterval = copyFrom.EventFlushInterval;            _eventSamplingInterval = copyFrom.EventSamplingInterval;            _eventsUri = copyFrom.EventsUri;            _httpClientHandler = copyFrom.HttpClientHandler;            _httpClientTimeout = copyFrom.HttpClientTimeout;            _inlineUsersInEvents = copyFrom.InlineUsersInEvents;            _isStreamingEnabled = copyFrom.IsStreamingEnabled;            _mobileKey = copyFrom.MobileKey;            _offline = copyFrom.Offline;            _persistFlagValues = copyFrom.PersistFlagValues;            _pollingInterval = copyFrom.PollingInterval;            _privateAttributeNames = copyFrom.PrivateAttributeNames is null ? null :                new HashSet<string>(copyFrom.PrivateAttributeNames);            _readTimeout = copyFrom.ReadTimeout;            _reconnectTime = copyFrom.ReconnectTime;            _streamUri = copyFrom.StreamUri;            _useReport = copyFrom.UseReport;            _userKeysCapacity = copyFrom.UserKeysCapacity;            _userKeysFlushInterval = copyFrom.UserKeysFlushInterval;        }        public Configuration Build()        {            return new Configuration(this);        }

        public IConfigurationBuilder AllAttributesPrivate(bool allAttributesPrivate)
        {
            _allAttributesPrivate = allAttributesPrivate;
            return this;
        }

        public IConfigurationBuilder BackgroundPollingInterval(TimeSpan backgroundPollingInterval)
        {
            if (backgroundPollingInterval.CompareTo(Configuration.MinimumBackgroundPollingInterval) < 0)
            {
                Log.WarnFormat("BackgroundPollingInterval cannot be less than {0}", Configuration.MinimumBackgroundPollingInterval);
                _backgroundPollingInterval = Configuration.MinimumBackgroundPollingInterval;
            }
            else
            {
                _backgroundPollingInterval = backgroundPollingInterval;
            }
            return this;
        }

        public IConfigurationBuilder BaseUri(Uri baseUri)
        {
            _baseUri = baseUri;
            return this;
        }

        public IConfigurationBuilder ConnectionTimeout(TimeSpan connectionTimeout)
        {
            _connectionTimeout = connectionTimeout;
            return this;
        }

        public IConfigurationBuilder EnableBackgroundUpdating(bool enableBackgroundUpdating)
        {
            _enableBackgroundUpdating = enableBackgroundUpdating;
            return this;
        }

        public IConfigurationBuilder EvaluationReasons(bool evaluationReasons)
        {
            _evaluationReasons = evaluationReasons;
            return this;
        }

        public IConfigurationBuilder EventCapacity(int eventCapacity)
        {
            _eventCapacity = eventCapacity;
            return this;
        }        public IConfigurationBuilder EventFlushInterval(TimeSpan eventflushInterval)
        {
            _eventFlushInterval = eventflushInterval;
            return this;
        }

        public IConfigurationBuilder EventSamplingInterval(int eventSamplingInterval)
        {
            _eventSamplingInterval = eventSamplingInterval;
            return this;
        }        public IConfigurationBuilder EventsUri(Uri eventsUri)
        {
            _eventsUri = eventsUri;
            return this;
        }

        public IConfigurationBuilder HttpClientHandler(HttpClientHandler httpClientHandler)
        {
            _httpClientHandler = httpClientHandler;
            return this;
        }        public IConfigurationBuilder HttpClientTimeout(TimeSpan httpClientTimeout)
        {
            _httpClientTimeout = httpClientTimeout;
            return this;
        }

        public IConfigurationBuilder InlineUsersInEvents(bool inlineUsersInEvents)
        {
            _inlineUsersInEvents = inlineUsersInEvents;
            return this;
        }

        public IConfigurationBuilder IsStreamingEnabled(bool isStreamingEnabled)
        {
            _isStreamingEnabled = isStreamingEnabled;
            return this;
        }

        public IConfigurationBuilder MobileKey(string mobileKey)
        {
            _mobileKey = mobileKey;
            return this;
        }        public IConfigurationBuilder Offline(bool offline)        {            _offline = offline;            return this;        }        public IConfigurationBuilder PersistFlagValues(bool persistFlagValues)
        {
            _persistFlagValues = persistFlagValues;
            return this;
        }        public IConfigurationBuilder PollingInterval(TimeSpan pollingInterval)
        {
            if (pollingInterval.CompareTo(Configuration.MinimumPollingInterval) < 0)
            {
                Log.WarnFormat("PollingInterval cannot be less than {0}", Configuration.MinimumPollingInterval);
                _pollingInterval = Configuration.MinimumPollingInterval;
            }
            else
            {
                _pollingInterval = pollingInterval;
            }
            return this;
        }

        public IConfigurationBuilder PrivateAttribute(string privateAtributeName)
        {
            if (_privateAttributeNames is null)
            {
                _privateAttributeNames = new HashSet<string>();
            }
            _privateAttributeNames.Add(privateAtributeName);
            return this;
        }        public IConfigurationBuilder ReadTimeout(TimeSpan readTimeout)
        {
            _readTimeout = readTimeout;
            return this;
        }        public IConfigurationBuilder ReconnectTime(TimeSpan reconnectTime)
        {
            _reconnectTime = reconnectTime;
            return this;
        }        public IConfigurationBuilder StreamUri(Uri streamUri)
        {
            _streamUri = streamUri;
            return this;
        }

        public IConfigurationBuilder UseReport(bool useReport)
        {
            _useReport = useReport;
            return this;
        }

        public  IConfigurationBuilder UserKeysCapacity(int userKeysCapacity)
        {
            _userKeysCapacity = userKeysCapacity;
            return this;
        }        public IConfigurationBuilder UserKeysFlushInterval(TimeSpan userKeysFlushInterval)
        {
            _userKeysFlushInterval = userKeysFlushInterval;
            return this;
        }

        // The following properties are internal and settable only for testing. They are not part
        // of the IConfigurationBuilder interface, so you must call the internal method
        // Configuration.BuilderInternal() which exposes the internal ConfigurationBuilder,
        // and then call these methods before you have called any of the public methods (since
        // only these methods return ConfigurationBuilder rather than IConfigurationBuilder).

        internal ConfigurationBuilder ConnectionManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
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
