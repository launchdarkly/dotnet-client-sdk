using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Internal.Http;

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
        internal Configuration Configuration { get; }

        internal HttpProperties HttpProperties { get; }

        /// <summary>
        /// The configured logger for the SDK.
        /// </summary>
        public Logger BaseLogger { get; }

        /// <summary>
        /// True if evaluation reasons are enabled.
        /// </summary>
        public bool EvaluationReasons { get; }

        /// <summary>
        /// True if the HTTP REPORT method is enabled.
        /// </summary>
        public bool UseReport { get; }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="configuration">the SDK configuration</param>
        public LdClientContext(
            Configuration configuration
            )
        {
            var logConfig = (configuration.LoggingConfigurationFactory ?? Components.Logging())
                .CreateLoggingConfiguration();
            var logAdapter = logConfig.LogAdapter ?? Logs.None;
            this.BaseLogger = logAdapter.Logger(logConfig.BaseLoggerName ?? LogNames.Base);

            this.EvaluationReasons = configuration.EvaluationReasons;
            this.HttpProperties = configuration.HttpProperties;
            this.UseReport = configuration.UseReport;
        }
    }
}
