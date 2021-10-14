using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.Events;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Events;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    /// <summary>
    /// Contains methods for configuring delivery of analytics events.
    /// </summary>
    /// <remarks>
    /// The SDK normally buffers analytics events and sends them to LaunchDarkly at intervals. If you want
    /// to customize this behavior, create a builder with <see cref="Components.SendEvents"/>, change its
    /// properties with the methods of this class, and pass it to <see cref="ConfigurationBuilder.Events(IEventProcessorFactory)"/>.
    /// </remarks>
    /// <example>
    /// <code>
    ///     var config = Configuration.Builder(sdkKey)
    ///         .Events(
    ///             Components.SendEvents().Capacity(5000).FlushInterval(TimeSpan.FromSeconds(2))
    ///         )
    ///         .Build();
    /// </code>
    /// </example>
    public sealed class EventProcessorBuilder : IEventProcessorFactory
    {
        /// <summary>
        /// The default value for <see cref="Capacity(int)"/>.
        /// </summary>
        public const int DefaultCapacity = 100;

        /// <summary>
        /// The default value for <see cref="FlushInterval(TimeSpan)"/>.
        /// </summary>
        /// <remarks>
        /// For Android and iOS, this is 30 seconds. For all other platforms, it is 5 seconds. The
        /// difference is because the extra HTTP requests for sending events more frequently are
        /// undesirable in a mobile application as opposed to a desktop application.
        /// </remarks>
        public static readonly TimeSpan DefaultFlushInterval =
#if NETSTANDARD
            TimeSpan.FromSeconds(5);
#else
            TimeSpan.FromSeconds(30);
#endif

        internal static readonly Uri DefaultBaseUri = new Uri("https://mobile.launchdarkly.com");

        internal bool _allAttributesPrivate = false;
        internal Uri _baseUri = null;
        internal int _capacity = DefaultCapacity;
        internal TimeSpan _flushInterval = DefaultFlushInterval;
        internal bool _inlineUsersInEvents = false;
        internal HashSet<UserAttribute> _privateAttributes = new HashSet<UserAttribute>();
        internal IEventSender _eventSender = null; // used in testing

        /// <summary>
        /// Sets whether or not all optional user attributes should be hidden from LaunchDarkly.
        /// </summary>
        /// <remarks>
        /// If this is <see langword="true"/>, all user attribute values (other than the key) will be private, not just
        /// the attributes specified in <see cref="PrivateAttributes(UserAttribute[])"/> or on a per-user basis with
        /// <see cref="UserBuilder"/> methods. By default, it is <see langword="false"/>.
        /// </remarks>
        /// <param name="allAttributesPrivate">true if all user attributes should be private</param>
        /// <returns>the builder</returns>
        public EventProcessorBuilder AllAttributesPrivate(bool allAttributesPrivate)
        {
            _allAttributesPrivate = allAttributesPrivate;
            return this;
        }

        /// <summary>
        /// Sets a custom base URI for the events service.
        /// </summary>
        /// <remarks>
        /// You will only need to change this value in the following cases:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// You are using the <a href="https://docs.launchdarkly.com/docs/the-relay-proxy">Relay Proxy</a>.
        /// Set <c>BaseUri</c> to the base URI of the Relay Proxy instance.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// You are connecting to a test server or a nonstandard endpoint for the LaunchDarkly service.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="baseUri">the base URI of the events service; null to use the default</param>
        /// <returns>the builder</returns>
        public EventProcessorBuilder BaseUri(Uri baseUri)
        {
            _baseUri = baseUri;
            return this;
        }

        /// <summary>
        /// Sets the capacity of the events buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The client buffers up to this many events in memory before flushing. If the capacity is exceeded before
        /// the buffer is flushed (see <see cref="FlushInterval(TimeSpan)"/>), events will be discarded. Increasing the
        /// capacity means that events are less likely to be discarded, at the cost of consuming more memory.
        /// </para>
        /// <para>
        /// The default value is <see cref="DefaultCapacity"/>. A zero or negative value will be changed to the default.
        /// </para>
        /// </remarks>
        /// <param name="capacity">the capacity of the event buffer</param>
        /// <returns>the builder</returns>
        public EventProcessorBuilder Capacity(int capacity)
        {
            _capacity = (capacity <= 0) ? DefaultCapacity : capacity;
            return this;
        }

        // Used only in testing
        internal EventProcessorBuilder EventSender(IEventSender eventSender)
        {
            _eventSender = eventSender;
            return this;
        }

        /// <summary>
        /// Sets the interval between flushes of the event buffer.
        /// </summary>
        /// <remarks>
        /// Decreasing the flush interval means that the event buffer is less likely to reach capacity.
        /// The default value is <see cref="DefaultFlushInterval"/>. A zero or negative value will be changed to
        /// the default.
        /// </remarks>
        /// <param name="flushInterval">the flush interval</param>
        /// <returns>the builder</returns>
        public EventProcessorBuilder FlushInterval(TimeSpan flushInterval)
        {
            _flushInterval = (flushInterval.CompareTo(TimeSpan.Zero) <= 0) ?
                DefaultFlushInterval : flushInterval;
            return this;
        }

        /// <summary>
        /// Sets whether to include full user details in every analytics event.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>: events will only include the user key, except for one
        /// "identify" event that provides the full details for the user.
        /// </remarks>
        /// <param name="inlineUsersInEvents">true if you want full user details in each event</param>
        /// <returns>the builder</returns>
        public EventProcessorBuilder InlineUsersInEvents(bool inlineUsersInEvents)
        {
            _inlineUsersInEvents = inlineUsersInEvents;
            return this;
        }

        /// <summary>
        /// Marks a set of attribute names as private.
        /// </summary>
        /// <remarks>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with these
        /// names removed. This is in addition to any attributes that were marked as private for an
        /// individual user with <see cref="UserBuilder"/> methods.
        /// </remarks>
        /// <param name="attributes">a set of attributes that will be removed from user data set to LaunchDarkly</param>
        /// <returns>the builder</returns>
        /// <seealso cref="PrivateAttributeNames(string[])"/>
        public EventProcessorBuilder PrivateAttributes(params UserAttribute[] attributes)
        {
            foreach (var a in attributes)
            {
                _privateAttributes.Add(a);
            }
            return this;
        }

        /// <summary>
        /// Marks a set of attribute names as private.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Any users sent to LaunchDarkly with this configuration active will have attributes with these
        /// names removed. This is in addition to any attributes that were marked as private for an
        /// individual user with <see cref="UserBuilder"/> methods.
        /// </para>
        /// <para>
        /// Using <see cref="PrivateAttributes(UserAttribute[])"/> is preferable to avoid the possibility of
        /// misspelling a built-in attribute.
        /// </para>
        /// </remarks>
        /// <param name="attributes">a set of names that will be removed from user data set to LaunchDarkly</param>
        /// <returns>the builder</returns>
        /// <seealso cref="PrivateAttributes(UserAttribute[])"/>
        public EventProcessorBuilder PrivateAttributeNames(params string[] attributes)
        {
            foreach (var a in attributes)
            {
                _privateAttributes.Add(UserAttribute.ForName(a));
            }
            return this;
        }

        /// <inheritdoc/>
        public IEventProcessor CreateEventProcessor(LdClientContext context)
        {
            var uri = _baseUri ?? DefaultBaseUri;
            var eventsConfig = new EventsConfiguration
            {
                AllAttributesPrivate = _allAttributesPrivate,
                EventCapacity = _capacity,
                EventFlushInterval = _flushInterval,
                EventsUri = uri.AddPath(Constants.EVENTS_PATH),
                //DiagnosticUri = uri.AddPath("diagnostic"), // no diagnostic events yet
                InlineUsersInEvents = _inlineUsersInEvents,
                PrivateAttributeNames = _privateAttributes.ToImmutableHashSet(),
                RetryInterval = TimeSpan.FromSeconds(1)
            };
            var logger = context.BaseLogger.SubLogger(LogNames.EventsSubLog);
            var eventSender = _eventSender ??
                new DefaultEventSender(
                    context.Http.HttpProperties,
                    eventsConfig,
                    logger
                    );
            return new DefaultEventProcessorWrapper(
                new EventProcessor(
                    eventsConfig,
                    eventSender,
                    null, // no user deduplicator, because the client-side SDK doesn't send index events
                    null, // diagnostic store would go here, but we haven't implemented diagnostic events
                    null,
                    logger,
                    null
                    ));
        }
    }
}
