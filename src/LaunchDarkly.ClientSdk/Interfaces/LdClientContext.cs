using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Events;

namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Encapsulates SDK client context when creating components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Factory interfaces like <see cref="IDataSourceFactory"/> receive this class as a parameter.
    /// Its public properties provide information about the SDK configuration and environment. The SDK
    /// may also include non-public properties that are relevant only when creating one of the built-in
    /// component types and are not accessible to custom components.
    /// </para>
    /// </remarks>
    public sealed class LdClientContext
    {
        /// <summary>
        /// The configured mobile key.
        /// </summary>
        public string MobileKey { get; }

        /// <summary>
        /// The configured logger for the SDK.
        /// </summary>
        public Logger BaseLogger { get; }

        /// <summary>
        /// Whether to enable feature flag updates when the application is running in the background.
        /// </summary>
        public bool EnableBackgroundUpdating { get; }

        /// <summary>
        /// True if evaluation reasons are enabled.
        /// </summary>
        public bool EvaluationReasons { get; }

        /// <summary>
        /// The HTTP configuration properties.
        /// </summary>
        public HttpConfiguration Http { get; }

        /// <summary>
        /// The configured service base URIs.
        /// </summary>
        public ServiceEndpoints ServiceEndpoints { get; }

        internal IDiagnosticDisabler DiagnosticDisabler { get; }

        internal IDiagnosticStore DiagnosticStore { get; }

        internal TaskExecutor TaskExecutor { get; }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="configuration">the SDK configuration</param>
        public LdClientContext(
            Configuration configuration
            ) : this(configuration, null) { }

        internal LdClientContext(
            string mobileKey,
            Logger baseLogger,
            bool enableBackgroundUpdating,
            bool evaluationReasons,
            HttpConfiguration http,
            ServiceEndpoints serviceEndpoints,
            IDiagnosticDisabler diagnosticDisabler,
            IDiagnosticStore diagnosticStore,
            TaskExecutor taskExecutor
            )
        {
            MobileKey = mobileKey;
            BaseLogger = baseLogger;
            EnableBackgroundUpdating = enableBackgroundUpdating;
            EvaluationReasons = evaluationReasons;
            Http = http;
            ServiceEndpoints = serviceEndpoints ?? Components.ServiceEndpoints().Build();
            DiagnosticDisabler = diagnosticDisabler;
            DiagnosticStore = diagnosticStore;
            TaskExecutor = taskExecutor ?? new TaskExecutor(null,
                PlatformSpecific.AsyncScheduler.ScheduleAction,
                baseLogger
                );
        }

        internal LdClientContext(
            Configuration configuration,
            object eventSender
            ) :
            this(
                configuration.MobileKey,
                MakeLogger(configuration),
                configuration.EnableBackgroundUpdating,
                configuration.EvaluationReasons,
                (configuration.HttpConfigurationBuilder ?? Components.HttpConfiguration())
                    .CreateHttpConfiguration(MakeMinimalContext(configuration.MobileKey)),
                configuration.ServiceEndpoints,
                null,
                null,
                new TaskExecutor(
                    eventSender,
                    PlatformSpecific.AsyncScheduler.ScheduleAction,
                    MakeLogger(configuration)
                )
            ) { }

        internal static Logger MakeLogger(Configuration configuration)
        {
            var logConfig = (configuration.LoggingConfigurationBuilder ?? Components.Logging())
                .CreateLoggingConfiguration();
            var logAdapter = logConfig.LogAdapter ?? Logs.None;
            return logAdapter.Logger(logConfig.BaseLoggerName ?? LogNames.Base);
        }

        internal static LdClientContext MakeMinimalContext(string mobileKey) =>
            new LdClientContext(
                mobileKey,
                Logs.None.Logger(""),
                false,
                false,
                HttpConfiguration.Default(),
                null,
                null,
                null,
                null
            );

        internal LdClientContext WithDiagnostics(
            IDiagnosticDisabler newDiagnosticDisabler,
            IDiagnosticStore newDiagnosticStore
            ) =>
            new LdClientContext(
                MobileKey,
                BaseLogger,
                EnableBackgroundUpdating,
                EvaluationReasons,
                Http,
                ServiceEndpoints,
                newDiagnosticDisabler,
                newDiagnosticStore,
                TaskExecutor);
    }
}
