using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.DataSources;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    /// <summary>
    /// Contains methods for configuring the streaming data source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, the SDK uses a streaming connection to receive feature flag data from LaunchDarkly. If you want
    /// to customize the behavior of the connection, create a builder with <see cref="Components.StreamingDataSource"/>,
    /// change its properties with the methods of this class, and pass it to
    /// <see cref="ConfigurationBuilder.DataSource(IDataSourceFactory)"/>.
    /// </para>
    /// <para>
    /// Setting <see cref="ConfigurationBuilder.Offline(bool)"/> to <see langword="true"/> will supersede this
    /// setting and completely disable network requests.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///     var config = Configuration.Builder(sdkKey)
    ///         .DataSource(Components.PollingDataSource()
    ///             .PollInterval(TimeSpan.FromSeconds(45)))
    ///         .Build();
    /// </code>
    /// </example>
    public sealed class StreamingDataSourceBuilder : IDataSourceFactory
    {
        internal static readonly Uri DefaultBaseUri = new Uri("https://clientstream.launchdarkly.com");

        /// <summary>
        /// The default value for <see cref="InitialReconnectDelay(TimeSpan)"/>: 1000 milliseconds.
        /// </summary>
        public static readonly TimeSpan DefaultInitialReconnectDelay = TimeSpan.FromSeconds(1);

        internal TimeSpan _backgroundPollInterval = Configuration.DefaultBackgroundPollInterval;
        internal Uri _baseUri = null;
        internal TimeSpan _initialReconnectDelay = DefaultInitialReconnectDelay;
        internal Uri _pollingBaseUri = null;

        internal StreamingDataSource.EventSourceCreator _eventSourceCreator = null; // used only in testing

        /// <summary>
        /// Sets the interval between feature flag updates when the application is running in the background.
        /// </summary>
        /// <remarks>
        /// This is only relevant on mobile platforms. The default is <see cref="Configuration.DefaultBackgroundPollInterval"/>;
        /// the minimum is <see cref="Configuration.MinimumBackgroundPollInterval"/>.
        /// </remarks>
        /// <param name="backgroundPollInterval">the background polling interval</param>
        /// <returns>the same builder</returns>
        /// <seealso cref="ConfigurationBuilder.EnableBackgroundUpdating(bool)"/>
        public StreamingDataSourceBuilder BackgroundPollInterval(TimeSpan backgroundPollInterval)
        {
            _backgroundPollInterval = (backgroundPollInterval < Configuration.MinimumBackgroundPollInterval) ?
                Configuration.MinimumBackgroundPollInterval : backgroundPollInterval;
            return this;
        }

        internal StreamingDataSourceBuilder BackgroundPollingIntervalWithoutMinimum(TimeSpan backgroundPollInterval)
        {
            _backgroundPollInterval = backgroundPollInterval;
            return this;
        }

        /// <summary>
        /// Sets a custom base URI for the streaming service.
        /// </summary>
        /// <remarks>
        /// You will only need to change this value in the following cases:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// You are using the <a href="https://docs.launchdarkly.com/home/advanced/relay-proxy">Relay Proxy</a>.
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
        /// <param name="baseUri">the base URI of the streaming service; null to use the default</param>
        /// <returns>the builder</returns>
        public StreamingDataSourceBuilder BaseUri(Uri baseUri)
        {
            _baseUri = baseUri;
            return this;
        }

        /// <summary>
        /// Sets the initial reconnect delay for the streaming connection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The streaming service uses a backoff algorithm (with jitter) every time the connection needs
        /// to be reestablished.The delay for the first reconnection will start near this value, and then
        /// increase exponentially for any subsequent connection failures.
        /// </para>
        /// <para>
        /// The default value is <see cref="DefaultInitialReconnectDelay"/>.
        /// </para>
        /// </remarks>
        /// <param name="initialReconnectDelay">the reconnect time base value</param>
        /// <returns>the builder</returns>
        public StreamingDataSourceBuilder InitialReconnectDelay(TimeSpan initialReconnectDelay)
        {
            _initialReconnectDelay = initialReconnectDelay;
            return this;
        }

        /// <summary>
        /// Sets a custom base URI for the polling service.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The SDK may need the polling service even if you are using streaming mode, under certain
        /// circumstances: if the application has been put in the background on a mobile device, or if
        /// the streaming endpoint cannot deliver an update itself and instead tells the SDK to re-poll
        /// to get the update.
        /// </para>
        /// <para>
        /// You will only need to change this value if you are connecting to a test server or a
        /// nonstandard endpoint for the LaunchDarkly service.
        /// </para>
        /// <para>
        /// If you are using the <a href="https://docs.launchdarkly.com/home/advanced/relay-proxy">Relay Proxy</a>.
        /// you only need to set <see cref="BaseUri(Uri)"/>; it will automatically default to using the
        /// same value for <c>PollingBaseUri</c>.
        /// </para>
        /// </remarks>
        /// <param name="pollingBaseUri">the base URI of the polling service; null to use the default</param>
        /// <returns>the builder</returns>
        public StreamingDataSourceBuilder PollingBaseUri(Uri pollingBaseUri)
        {
            _pollingBaseUri = pollingBaseUri;
            return this;
        }

        internal StreamingDataSourceBuilder EventSourceCreator(StreamingDataSource.EventSourceCreator fn)
        {
            _eventSourceCreator = fn;
            return this;
        }

        /// <inheritdoc/>
        public IDataSource CreateDataSource(
            LdClientContext context,
            IDataSourceUpdateSink updateSink,
            User currentUser,
            bool inBackground
            )
        {
            var baseUri = _baseUri ?? DefaultBaseUri;
            Uri pollingBaseUri;
            if (_pollingBaseUri is null)
            {
                // If they specified a nonstandard BaseUri but did *not* specify PollingBaseUri,
                // we assume it's a Relay Proxy instance and we set both to the same.
                pollingBaseUri = (_baseUri is null || _baseUri == DefaultBaseUri) ?
                    PollingDataSourceBuilder.DefaultBaseUri :
                    _baseUri;
            }
            else
            {
                pollingBaseUri = _pollingBaseUri;
            }

            if (inBackground)
            {
                // When in the background, always use polling instead of streaming
                return new PollingDataSourceBuilder()
                    .BaseUri(pollingBaseUri)
                    .BackgroundPollInterval(_backgroundPollInterval)
                    .CreateDataSource(context, updateSink, currentUser, true);
            }

            var logger = context.BaseLogger.SubLogger(LogNames.DataSourceSubLog);
            var requestor = new FeatureFlagRequestor(
                pollingBaseUri,
                currentUser,
                context.UseReport,
                context.EvaluationReasons,
                context.HttpProperties,
                logger
                );

            return new StreamingDataSource(
                updateSink,
                currentUser,
                baseUri,
                context.UseReport,
                context.EvaluationReasons,
                _initialReconnectDelay,
                requestor,
                context.HttpProperties,
                logger,
                _eventSourceCreator
                );
        }
    }
}
