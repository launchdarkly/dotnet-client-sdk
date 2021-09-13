using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;

namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Encapsulates SDK client context when creating components.
    /// </summary>
    /// <remarks>
    /// Factory interfaces like <see cref="IDataSourceFactory"/> receive this class as a parameter.
    /// Its public properties provide information about the SDK configuration and environment. The SDK
    /// may also include non-public properties that are relevant only when creating one of the built-in
    /// component types and are not accessible to custom components.
    /// </remarks>
    public sealed class LdClientContext
    {
        /// <summary>
        /// The basic properties common to all components.
        /// </summary>
        public BasicConfiguration Basic { get; }

        /// <summary>
        /// The configured logger for the SDK.
        /// </summary>
        public Logger BaseLogger { get; }

        /// <summary>
        /// True if evaluation reasons are enabled.
        /// </summary>
        public bool EvaluationReasons { get; }

        /// <summary>
        /// The HTTP configuration properties.
        /// </summary>
        public HttpConfiguration Http { get; }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="configuration">the SDK configuration</param>
        public LdClientContext(
            Configuration configuration
            )
        {
            this.Basic = new BasicConfiguration(configuration.MobileKey);

            var logConfig = (configuration.LoggingConfigurationBuilder ?? Components.Logging())
                .CreateLoggingConfiguration();
            var logAdapter = logConfig.LogAdapter ?? Logs.None;
            this.BaseLogger = logAdapter.Logger(logConfig.BaseLoggerName ?? LogNames.Base);

            this.EvaluationReasons = configuration.EvaluationReasons;
            this.Http = (configuration.HttpConfigurationBuilder ?? Components.HttpConfiguration())
                .CreateHttpConfiguration(this.Basic);
        }
    }
}
