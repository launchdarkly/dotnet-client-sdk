using System;
using System.Collections.Generic;
using System.Net.Http;
using Common.Logging;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// This class exposes advanced configuration options for <see cref="Client.ILdCommonClient"/>.
    /// </summary>
    public class Configuration : IMobileConfiguration
    {
        /// <summary>
        /// The base URI of the LaunchDarkly server.
        /// </summary>
        public Uri BaseUri { get; internal set; }
        /// <summary>
        /// The base URL of the LaunchDarkly streaming server.
        /// </summary>
        public Uri StreamUri { get; internal set; }
        /// <summary>
        /// The base URL of the LaunchDarkly analytics event server.
        /// </summary>
        public Uri EventsUri { get; internal set; }
        /// <summary>
        /// The Mobile key for your LaunchDarkly environment.
        /// </summary>
        public string MobileKey { get; internal set; }
        /// <summary>
        /// The SDK key for your LaunchDarkly environment. This is the Mobile key.
        /// </summary>
        /// <value>Returns the Mobile Key.</value>
        public string SdkKey { get { return MobileKey; } }
        /// <summary>
        /// Whether or not the streaming API should be used to receive flag updates. This is true by default.
        /// Streaming should only be disabled on the advice of LaunchDarkly support.
        /// </summary>
        public bool IsStreamingEnabled { get; internal set; }
        /// <summary>
        /// The capacity of the events buffer. The client buffers up to this many events in
        /// memory before flushing. If the capacity is exceeded before the buffer is flushed,
        /// events will be discarded. Increasing the capacity means that events are less likely
        /// to be discarded, at the cost of consuming more memory.
        /// </summary>
        public int EventQueueCapacity { get; internal set; }
        /// <summary>
        /// The time between flushes of the event buffer. Decreasing the flush interval means
        /// that the event buffer is less likely to reach capacity. The default value is 5 seconds.
        /// </summary>
        public TimeSpan EventQueueFrequency { get; internal set; }
        /// <summary>
        /// Enables event sampling if non-zero. When set to the default of zero, all analytics events are
        /// sent back to LaunchDarkly. When greater than zero, there is a 1 in <c>EventSamplingInterval</c>
        /// chance that events will be sent (example: if the interval is 20, on average 5% of events will be sent).
        /// </summary>
        public int EventSamplingInterval { get; internal set; }
        /// <summary>
        /// Set the polling interval (when streaming is disabled). The default value is 30 seconds.
        /// </summary>
        public TimeSpan PollingInterval { get; internal set; }
        /// <summary>
        /// The timeout when reading data from the EventSource API. The default value is 5 minutes.
        /// </summary>
        public TimeSpan ReadTimeout { get; internal set; }
        /// <summary>
        /// The reconnect base time for the streaming connection.The streaming connection
        /// uses an exponential backoff algorithm (with jitter) for reconnects, but will start the
        /// backoff with a value near the value specified here. The default value is 1 second.
        /// </summary>
        public TimeSpan ReconnectTime { get; internal set; }
        /// <summary>
        /// The connection timeout. The default value is 10 seconds.
        /// </summary>
        public TimeSpan HttpClientTimeout { get; internal set; }
        /// <summary>
        /// The object to be used for sending HTTP requests. This is exposed for testing purposes.
        /// </summary>
        public HttpClientHandler HttpClientHandler { get; internal set; }
        /// <summary>
        /// Whether or not this client is offline. If true, no calls to Launchdarkly will be made.
        /// </summary>
        public bool Offline { get; internal set; }
        /// <summary>
        /// Whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server). If this is true, all of the user attributes will be private,
        /// not just the attributes specified with the <c>AndPrivate...</c> methods on the
        /// <see cref="Client.User"/> object. By default, this is false.
        /// </summary>
        public bool AllAttributesPrivate { get; internal set; }
        /// <summary>
        /// Marks a set of attribute names as private. Any users sent to LaunchDarkly with this
        /// configuration active will have attributes with these names removed, even if you did
        /// not use the <c>AndPrivate...</c> methods on the <see cref="Client.User"/> object.
        /// </summary>
        public ISet<string> PrivateAttributeNames { get; internal set; }
        /// <summary>
        /// The number of user keys that the event processor can remember at any one time, so that
        /// duplicate user details will not be sent in analytics events.
        /// </summary>
        public int UserKeysCapacity { get; internal set; }
        /// <summary>
        /// The interval at which the event processor will reset its set of known user keys. The
        /// default value is five minutes.
        /// </summary>
        public TimeSpan UserKeysFlushInterval { get; internal set; }
        /// <summary>
        /// True if full user details should be included in every analytics event. The default is false (events will
        /// only include the user key, except for one "index" event that provides the full details for the user).
        /// </summary>
        public bool InlineUsersInEvents { get; internal set; }
        /// <see cref="IMobileConfiguration.BackgroundPollingInterval"/>
        public TimeSpan BackgroundPollingInterval { get; internal set; }
        /// <see cref="IMobileConfiguration.ConnectionTimeout"/>
        public TimeSpan ConnectionTimeout { get; internal set; }
        /// <see cref="IMobileConfiguration.EnableBackgroundUpdating"/>
        public bool EnableBackgroundUpdating { get; internal set; }
        /// <see cref="IMobileConfiguration.UseReport"/>
        public bool UseReport { get; internal set; }

        internal IFlagCacheManager FlagCacheManager { get; set; }
        internal IConnectionManager ConnectionManager { get; set; }
        internal IEventProcessor EventProcessor { get; set; }
        internal IMobileUpdateProcessor MobileUpdateProcessor { get; set; }
        internal ISimplePersistance Persister { get; set; }
        internal IDeviceInfo DeviceInfo { get; set; }
        internal IFeatureFlagListenerManager FeatureFlagListenerManager { get; set; }
        internal IPlatformAdapter PlatformAdapter { get; set; }

        /// <summary>
        /// Default value for <see cref="PollingInterval"/>.
        /// </summary>
        public static TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Minimum value for <see cref="PollingInterval"/>.
        /// </summary>
        public static TimeSpan MinimumPollingInterval = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Default value for <see cref="BaseUri"/>.
        /// </summary>
        internal static readonly Uri DefaultUri = new Uri("https://app.launchdarkly.com");
        /// <summary>
        /// Default value for <see cref="StreamUri"/>.
        /// </summary>
        private static readonly Uri DefaultStreamUri = new Uri("https://clientstream.launchdarkly.com");
        /// <summary>
        /// Default value for <see cref="EventsUri"/>.
        /// </summary>
        private static readonly Uri DefaultEventsUri = new Uri("https://mobile.launchdarkly.com");
        /// <summary>
        /// Default value for <see cref="EventQueueCapacity"/>.
        /// </summary>
        private static readonly int DefaultEventQueueCapacity = 100;
        /// <summary>
        /// Default value for <see cref="EventQueueFrequency"/>.
        /// </summary>
        private static readonly TimeSpan DefaultEventQueueFrequency = TimeSpan.FromSeconds(5);
        /// <summary>
        /// Default value for <see cref="ReadTimeout"/>.
        /// </summary>
        private static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Default value for <see cref="ReconnectTime"/>.
        /// </summary>
        private static readonly TimeSpan DefaultReconnectTime = TimeSpan.FromSeconds(1);
        /// <summary>
        /// Default value for <see cref="HttpClientTimeout"/>.
        /// </summary>
        private static readonly TimeSpan DefaultHttpClientTimeout = TimeSpan.FromSeconds(10);
        /// <summary>
        /// Default value for <see cref="UserKeysCapacity"/>.
        /// </summary>
        private static readonly int DefaultUserKeysCapacity = 1000;
        /// <summary>
        /// Default value for <see cref="UserKeysFlushInterval"/>.
        /// </summary>
        private static readonly TimeSpan DefaultUserKeysFlushInterval = TimeSpan.FromMinutes(5);
        /// <summary>
        /// The default value for <see cref="BackgroundPollingInterval"/>.
        /// </summary>
        private static readonly TimeSpan DefaultBackgroundPollingInterval = TimeSpan.FromMinutes(60);
        /// <summary>
        /// The minimum value for <see cref="BackgroundPollingInterval"/>.
        /// </summary>
        public static readonly TimeSpan MinimumBackgroundPollingInterval = TimeSpan.FromMinutes(15);
        /// <summary>
        /// The default value for <see cref="ConnectionTimeout"/>.
        /// </summary>
        private static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Creates a configuration with all parameters set to the default. Use extension methods
        /// to set additional parameters.
        /// </summary>
        /// <param name="mobileKey">the SDK key for your LaunchDarkly environment</param>
        /// <returns>a <c>Configuration</c> instance</returns>
        public static Configuration Default(string mobileKey)
        {
            if (String.IsNullOrEmpty(mobileKey))
            {
                throw new ArgumentOutOfRangeException("mobileKey", "key is required");
            }
            var defaultConfiguration = new Configuration
            {
                BaseUri = DefaultUri,
                StreamUri = DefaultStreamUri,
                EventsUri = DefaultEventsUri,
                EventQueueCapacity = DefaultEventQueueCapacity,
                EventQueueFrequency = DefaultEventQueueFrequency,
                PollingInterval = DefaultPollingInterval,
                BackgroundPollingInterval = DefaultBackgroundPollingInterval,
                ReadTimeout = DefaultReadTimeout,
                ReconnectTime = DefaultReconnectTime,
                HttpClientTimeout = DefaultHttpClientTimeout,
                HttpClientHandler = new HttpClientHandler(),
                Offline = false,
                MobileKey = mobileKey,
                IsStreamingEnabled = true,
                AllAttributesPrivate = false,
                PrivateAttributeNames = null,
                UserKeysCapacity = DefaultUserKeysCapacity,
                UserKeysFlushInterval = DefaultUserKeysFlushInterval,
                InlineUsersInEvents = false,
                EnableBackgroundUpdating = true,               
                UseReport = true
            };

            return defaultConfiguration;
        }
    }

    /// <summary>
    /// Extension methods that can be called on a <see cref="Configuration"/> to add to its properties.
    /// </summary>
    public static class ConfigurationExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigurationExtensions));

        /// <summary>
        /// Sets the base URI of the LaunchDarkly server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the base URI as a string</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithBaseUri(this Configuration configuration, string uri)
        {
            if (uri != null)
                configuration.BaseUri = new Uri(uri);

            return configuration;
        }

        /// <summary>
        /// Sets the base URI of the LaunchDarkly server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the base URI</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithBaseUri(this Configuration configuration, Uri uri)
        {
            if (uri != null)
                configuration.BaseUri = uri;

            return configuration;
        }

        /// <summary>
        /// Sets the base URL of the LaunchDarkly streaming server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the stream URI as a string</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithStreamUri(this Configuration configuration, string uri)
        {
            if (uri != null)
                configuration.StreamUri = new Uri(uri);

            return configuration;
        }

        /// <summary>
        /// Sets the base URL of the LaunchDarkly streaming server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the stream URI</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithStreamUri(this Configuration configuration, Uri uri)
        {
            if (uri != null)
                configuration.StreamUri = uri;

            return configuration;
        }

        /// <summary>
        /// Sets the base URL of the LaunchDarkly analytics event server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the events URI as a string</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventsUri(this Configuration configuration, string uri)
        {
            if (uri != null)
                configuration.EventsUri = new Uri(uri);

            return configuration;
        }

        /// <summary>
        /// Sets the base URL of the LaunchDarkly analytics event server for this configuration.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="uri">the events URI</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventsUri(this Configuration configuration, Uri uri)
        {
            if (uri != null)
                configuration.EventsUri = uri;

            return configuration;
        }

        /// <summary>
        /// Sets the capacity of the events buffer. The client buffers up to this many events in
        /// memory before flushing. If the capacity is exceeded before the buffer is flushed,
        /// events will be discarded. Increasing the capacity means that events are less likely
        /// to be discarded, at the cost of consuming more memory.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="eventQueueCapacity"></param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventQueueCapacity(this Configuration configuration, int eventQueueCapacity)
        {
            configuration.EventQueueCapacity = eventQueueCapacity;
            return configuration;
        }

        /// <summary>
        /// Sets the time between flushes of the event buffer. Decreasing the flush interval means
        /// that the event buffer is less likely to reach capacity. The default value is 5 seconds.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="frequency">the flush interval</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventQueueFrequency(this Configuration configuration, TimeSpan frequency)
        {
            configuration.EventQueueFrequency = frequency;
            return configuration;
        }

        /// <summary>
        /// Enables event sampling if non-zero. When set to the default of zero, all analytics events are
        /// sent back to LaunchDarkly. When greater than zero, there is a 1 in <c>EventSamplingInterval</c>
        /// chance that events will be sent (example: if the interval is 20, on average 5% of events will be sent).
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="interval">the sampling interval</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventSamplingInterval(this Configuration configuration, int interval)
        {
            if (interval < 0)
            {
                Log.Warn("EventSamplingInterval cannot be less than zero.");
                interval = 0;
            }
            configuration.EventSamplingInterval = interval;
            return configuration;
        }

        /// <summary>
        /// Sets the polling interval (when streaming is disabled). Values less than the default of
        /// 30 seconds will be changed to the default.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="pollingInterval">the rule update polling interval</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithPollingInterval(this Configuration configuration, TimeSpan pollingInterval)
        {
            if (pollingInterval.CompareTo(Configuration.MinimumPollingInterval) < 0)
            {
                Log.WarnFormat("PollingInterval cannot be less than the default of {0}.");
                pollingInterval = Configuration.MinimumPollingInterval;
            }
            configuration.PollingInterval = pollingInterval;
            return configuration;
        }

        /// <summary>
        /// Sets whether or not this client is offline. If true, no calls to Launchdarkly will be made.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="offline">true if the client should remain offline</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithOffline(this Configuration configuration, bool offline)
        {
            configuration.Offline = offline;
            return configuration;
        }

        /// <summary>
        /// Sets the connection timeout. The default value is 10 seconds.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="timeSpan">the connection timeout</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithHttpClientTimeout(this Configuration configuration, TimeSpan timeSpan)
        {
            configuration.HttpClientTimeout = timeSpan;
            return configuration;
        }

        /// <summary>
        /// Sets the timeout when reading data from the EventSource API. The default value is 5 minutes.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="timeSpan">the read timeout</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithReadTimeout(this Configuration configuration, TimeSpan timeSpan)
        {
            configuration.ReadTimeout = timeSpan;
            return configuration;
        }

        /// <summary>
        /// Sets the reconnect base time for the streaming connection. The streaming connection
        /// uses an exponential backoff algorithm (with jitter) for reconnects, but will start the
        /// backoff with a value near the value specified here. The default value is 1 second.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="timeSpan">the reconnect time base value</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithReconnectTime(this Configuration configuration, TimeSpan timeSpan)
        {
            configuration.ReconnectTime = timeSpan;
            return configuration;
        }

        /// <summary>
        /// Sets the object to be used for sending HTTP requests. This is exposed for testing purposes.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="httpClientHandler">the <c>HttpClientHandler</c> to use</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithHttpClientHandler(this Configuration configuration, HttpClientHandler httpClientHandler)
        {
            configuration.HttpClientHandler = httpClientHandler;
            return configuration;
        }

        /// <summary>
        /// Sets whether or not the streaming API should be used to receive flag updates. This
        /// is true by default. Streaming should only be disabled on the advice of LaunchDarkly
        /// support.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="enableStream">true if the streaming API should be used</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithIsStreamingEnabled(this Configuration configuration, bool enableStream)
        {
            configuration.IsStreamingEnabled = enableStream;
            return configuration;
        }

        /// <summary>
        /// Sets whether or not user attributes (other than the key) should be private (not sent to
        /// the LaunchDarkly server). If this is true, all of the user attributes will be private,
        /// not just the attributes specified with the <c>AndPrivate...</c> methods on the
        /// <see cref="Client.User"/> object. By default, this is false.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="allAttributesPrivate">true if all attributes should be private</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithAllAttributesPrivate(this Configuration configuration, bool allAttributesPrivate)
        {
            configuration.AllAttributesPrivate = allAttributesPrivate;
            return configuration;
        }

        /// <summary>
        /// Marks an attribute name as private. Any users sent to LaunchDarkly with this
        /// configuration active will have attributes with this name removed, even if you did
        /// not use the <c>AndPrivate...</c> methods on the <see cref="Client.User"/> object. You may
        /// call this method repeatedly to mark multiple attributes as private.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="attributeName">the attribute name</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithPrivateAttributeName(this Configuration configuration, string attributeName)
        {
            if (configuration.PrivateAttributeNames == null)
            {
                configuration.PrivateAttributeNames = new HashSet<string>();
            }
            configuration.PrivateAttributeNames.Add(attributeName);
            return configuration;
        }

        /// <summary>Configuration
        /// Sets the number of user keys that the event processor can remember at any one time, so that
        /// duplicate user details will not be sent in analytics events.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="capacity">the user key cache capacity</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithUserKeysCapacity(this Configuration configuration, int capacity)
        {
            configuration.UserKeysCapacity = capacity;
            return configuration;
        }

        /// <summary>
        /// Sets the interval at which the event processor will reset its set of known user keys. The
        /// default value is five minutes.
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="flushInterval">the flush interval</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithUserKeysFlushInterval(this Configuration configuration, TimeSpan flushInterval)
        {
            configuration.UserKeysFlushInterval = flushInterval;
            return configuration;
        }

        /// <summary>
        /// Sets whether to include full user details in every analytics event. The default is false (events will
        /// only include the user key, except for one "index" event that provides the full details for the user).
        /// </summary>
        /// <param name="configuration">the configuration</param>
        /// <param name="inlineUsers">true or false</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithInlineUsersInEvents(this Configuration configuration, bool inlineUsers)
        {
            configuration.InlineUsersInEvents = inlineUsers;
            return configuration;
        }

        /// <summary>
        /// Sets the IFlagCacheManager instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="flagCacheManager">FlagCacheManager.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        internal static Configuration WithFlagCacheManager(this Configuration configuration, IFlagCacheManager flagCacheManager)
        {
            configuration.FlagCacheManager = flagCacheManager;
            return configuration;
        }

        /// <summary>
        /// Sets the IConnectionManager instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="connectionManager">Connection manager.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithConnectionManager(this Configuration configuration, IConnectionManager connectionManager)
        {
            configuration.ConnectionManager = connectionManager;
            return configuration;
        }

        /// <summary>
        /// Sets the IEventProcessor instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="eventProcessor">Event processor.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEventProcessor(this Configuration configuration, IEventProcessor eventProcessor)
        {
            configuration.EventProcessor = eventProcessor;
            return configuration;
        }

        /// <summary>
        /// Determines whether to use the Report method for networking requests
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="useReport">If set to <c>true</c> use report.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithUseReport(this Configuration configuration, bool useReport)
        {
            configuration.UseReport = useReport;
            return configuration;
        }

        /// <summary>
        /// Sets the IMobileUpdateProcessor instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="mobileUpdateProcessor">Mobile update processor.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        internal static Configuration WithUpdateProcessor(this Configuration configuration, IMobileUpdateProcessor mobileUpdateProcessor)
        {
            configuration.MobileUpdateProcessor = mobileUpdateProcessor;
            return configuration;
        }

        /// <summary>
        /// Sets the ISimplePersistance instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="persister">Persister.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithPersister(this Configuration configuration, ISimplePersistance persister)
        {
            configuration.Persister = persister;
            return configuration;
        }

        /// <summary>
        /// Sets the IDeviceInfo instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="deviceInfo">Device info.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithDeviceInfo(this Configuration configuration, IDeviceInfo deviceInfo)
        {
            configuration.DeviceInfo = deviceInfo;
            return configuration;
        }

        /// <summary>
        /// Sets the IFeatureFlagListenerManager instance, used internally for stubbing mock instances.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="featureFlagListenerManager">Feature flag listener manager.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        internal static Configuration WithFeatureFlagListenerManager(this Configuration configuration, IFeatureFlagListenerManager featureFlagListenerManager)
        {
            configuration.FeatureFlagListenerManager = featureFlagListenerManager;
            return configuration;
        }

        /// <summary>
        /// Sets whether to enable background polling.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="enableBackgroundUpdating">If set to <c>true</c> enable background updating.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithEnableBackgroundUpdating(this Configuration configuration, bool enableBackgroundUpdating)
        {
            configuration.EnableBackgroundUpdating = enableBackgroundUpdating;
            return configuration;
        }

        /// <summary>
        /// Sets the interval for background polling.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="backgroundPollingInternal">Background polling internal.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithBackgroundPollingInterval(this Configuration configuration, TimeSpan backgroundPollingInternal)
        {
            if (backgroundPollingInternal.CompareTo(Configuration.MinimumBackgroundPollingInterval) < 0)
            {
                Log.WarnFormat("BackgroundPollingInterval cannot be less than the default of {0}.", Configuration.MinimumBackgroundPollingInterval);
                backgroundPollingInternal = Configuration.MinimumBackgroundPollingInterval;
            }
            configuration.BackgroundPollingInterval = backgroundPollingInternal;
            return configuration;
        }

        /// <summary>
        /// Specifies a component that provides special functionality for the current mobile platform.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="adapter">An implementation of <see cref="IPlatformAdapter"/>.</param>
        /// <returns>the same <c>Configuration</c> instance</returns>
        public static Configuration WithPlatformAdapter(this Configuration configuration, IPlatformAdapter adapter)
        {
            configuration.PlatformAdapter = adapter;
            return configuration;
        }
    }
}
