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
        /// <summary>
        /// The default value for <see cref="InitialReconnectDelay(TimeSpan)"/>: 1000 milliseconds.
        /// </summary>
        public static readonly TimeSpan DefaultInitialReconnectDelay = TimeSpan.FromSeconds(1);

        internal TimeSpan _backgroundPollInterval = Configuration.DefaultBackgroundPollInterval;
        internal TimeSpan _initialReconnectDelay = DefaultInitialReconnectDelay;

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
            var baseUri = ServiceEndpointsBuilder.SelectBaseUri(
                context.ServiceEndpoints.StreamingBaseUri,
                StandardEndpoints.DefaultStreamingBaseUri,
                "Streaming",
                context.BaseLogger
                );
            var pollingBaseUri = ServiceEndpointsBuilder.SelectBaseUri(
                context.ServiceEndpoints.PollingBaseUri,
                StandardEndpoints.DefaultPollingBaseUri,
                "Polling",
                context.BaseLogger
                );

            if (inBackground)
            {
                // When in the background, always use polling instead of streaming
                return new PollingDataSourceBuilder()
                    .BackgroundPollInterval(_backgroundPollInterval)
                    .CreateDataSource(context, updateSink, currentUser, true);
            }

            var logger = context.BaseLogger.SubLogger(LogNames.DataSourceSubLog);
            var requestor = new FeatureFlagRequestor(
                pollingBaseUri,
                currentUser,
                context.EvaluationReasons,
                context.Http,
                logger
                );

            return new StreamingDataSource(
                updateSink,
                currentUser,
                baseUri,
                context.EvaluationReasons,
                _initialReconnectDelay,
                requestor,
                context.Http,
                logger,
                _eventSourceCreator
                );
        }
    }
}
