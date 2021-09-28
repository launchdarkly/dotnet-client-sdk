using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.DataSources;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    /// <summary>
    /// Contains methods for configuring the polling data source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Polling is not the default behavior; by default, the SDK uses a streaming connection to receive feature flag
    /// data from LaunchDarkly. In polling mode, the SDK instead makes a new HTTP request to LaunchDarkly at regular
    /// intervals. HTTP caching allows it to avoid redundantly downloading data if there have been no changes, but
    /// polling is still less efficient than streaming and should only be used on the advice of LaunchDarkly support.
    /// </para>
    /// <para>
    /// To use polling mode, create a builder with <see cref="Components.PollingDataSource"/>, change its properties
    /// with the methods of this class, and pass it to <see cref="ConfigurationBuilder.DataSource(IDataSourceFactory)"/>.
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
    public sealed class PollingDataSourceBuilder : IDataSourceFactory
    {
        internal static readonly Uri DefaultBaseUri = new Uri("https://clientsdk.launchdarkly.com");

        /// <summary>
        /// The default value for <see cref="PollInterval(TimeSpan)"/>: 5 minutes.
        /// </summary>
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMinutes(5);

        internal TimeSpan _backgroundPollInterval = Configuration.DefaultBackgroundPollInterval;
        internal Uri _baseUri = null;
        internal TimeSpan _pollInterval = DefaultPollInterval;

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
        public PollingDataSourceBuilder BackgroundPollInterval(TimeSpan backgroundPollInterval)
        {
            _backgroundPollInterval = (backgroundPollInterval < Configuration.MinimumBackgroundPollInterval) ?
                Configuration.MinimumBackgroundPollInterval : backgroundPollInterval;
            return this;
        }

        /// <summary>
        /// Sets a custom base URI for the polling service.
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
        /// <param name="baseUri">the base URI of the polling service; null to use the default</param>
        /// <returns>the builder</returns>
        public PollingDataSourceBuilder BaseUri(Uri baseUri)
        {
            _baseUri = baseUri ?? DefaultBaseUri;
            return this;
        }

        /// <summary>
        /// Sets the interval at which the SDK will poll for feature flag updates.
        /// </summary>
        /// <remarks>
        /// The default and minimum value is <see cref="DefaultPollInterval"/>. Values less than this will
        /// be set to the default.
        /// </remarks>
        /// <param name="pollInterval">the polling interval</param>
        /// <returns>the builder</returns>
        public PollingDataSourceBuilder PollInterval(TimeSpan pollInterval)
        {
            _pollInterval = (pollInterval < DefaultPollInterval) ?
                DefaultPollInterval :
                pollInterval;
            return this;
        }

        // Exposed internally for testing
        internal PollingDataSourceBuilder PollIntervalNoMinimum(TimeSpan pollInterval)
        {
            _pollInterval = pollInterval;
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
            if (!inBackground)
            {
                context.BaseLogger.Warn("You should only disable the streaming API if instructed to do so by LaunchDarkly support");
            }

            var logger = context.BaseLogger.SubLogger(LogNames.DataSourceSubLog);
            var requestor = new FeatureFlagRequestor(
                _baseUri ?? DefaultBaseUri,
                currentUser,
                context.EvaluationReasons,
                context.Http,
                logger
                );

            return new PollingDataSource(
                updateSink,
                currentUser,
                requestor,
                _pollInterval,
                TimeSpan.Zero,
                context.TaskExecutor,
                logger
                );
        }
    }
}
