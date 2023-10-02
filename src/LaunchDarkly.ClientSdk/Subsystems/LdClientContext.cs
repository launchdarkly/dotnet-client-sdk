using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Events;

namespace LaunchDarkly.Sdk.Client.Subsystems
{
    /// <summary>
    /// Encapsulates SDK client context when creating components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The component factory interface <see cref="IComponentConfigurer{T}"/> receives this class as a parameter.
    /// Its public properties provide information about the SDK configuration and environment. The SDK
    /// may also include non-public properties that are relevant only when creating one of the built-in
    /// component types and are not accessible to custom components.
    /// </para>
    /// </remarks>
    public sealed class LdClientContext
    {
        internal IEnvironmentReporter EnvironmentReporter { get; }

        /// <summary>
        /// The configured mobile key.
        /// </summary>
        public string MobileKey { get; }

        /// <summary>
        /// The configured logger for the SDK.
        /// </summary>
        public Logger BaseLogger { get; }

        /// <summary>
        /// The current evaluation context.
        /// </summary>
        public Context CurrentContext { get; }

        /// <summary>
        /// A component that <see cref="IDataSource"/> implementations use to deliver status updates to
        /// the SDK.
        /// </summary>
        /// <remarks>
        /// This property is only set when the SDK is calling an <see cref="IDataSource"/> factory.
        /// Otherwise it is null.
        /// </remarks>
        public IDataSourceUpdateSink DataSourceUpdateSink { get; }

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
        /// True if the application is currently in a background state.
        /// </summary>
        public bool InBackground { get; }

        /// <summary>
        /// The configured service base URIs.
        /// </summary>
        public ServiceEndpoints ServiceEndpoints { get; }

        /// <summary>
        /// TODO
        /// </summary>

        internal IDiagnosticDisabler DiagnosticDisabler { get; }

        internal IDiagnosticStore DiagnosticStore { get; }

        internal TaskExecutor TaskExecutor { get; }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="configuration">the SDK configuration</param>
        /// <param name="currentContext">the current evaluation context</param>
        public LdClientContext(
            Configuration configuration,
            Context currentContext
        ) : this(configuration, currentContext, null)
        {
        }

        internal LdClientContext(Configuration configuration, Context currentContext, object eventSender) : this(
            configuration.MobileKey,
            MakeLogger(configuration),
            currentContext,
            null,
            configuration.EnableBackgroundUpdating,
            configuration.EvaluationReasons,
            (configuration.HttpConfigurationBuilder ?? Components.HttpConfiguration())
            .CreateHttpConfiguration(configuration.MobileKey,
                MakeEnvironmentReporter(configuration.ApplicationInfo).ApplicationInfo), // looks silly, but handles invalid info
            false,
            configuration.ServiceEndpoints,
            MakeEnvironmentReporter(configuration.ApplicationInfo),
            null,
            null,
            new TaskExecutor(
                eventSender,
                PlatformSpecific.AsyncScheduler.ScheduleAction,
                MakeLogger(configuration)
            )
        )
        {
        }

        internal LdClientContext(
            string mobileKey,
            Logger baseLogger,
            Context currentContext,
            IDataSourceUpdateSink dataSourceUpdateSink,
            bool enableBackgroundUpdating,
            bool evaluationReasons,
            HttpConfiguration http,
            bool inBackground,
            ServiceEndpoints serviceEndpoints,
            IEnvironmentReporter environmentReporter,
            IDiagnosticDisabler diagnosticDisabler,
            IDiagnosticStore diagnosticStore,
            TaskExecutor taskExecutor
        )
        {
            MobileKey = mobileKey;
            BaseLogger = baseLogger;
            CurrentContext = currentContext;
            DataSourceUpdateSink = dataSourceUpdateSink;
            EnableBackgroundUpdating = enableBackgroundUpdating;
            EvaluationReasons = evaluationReasons;
            Http = http;
            InBackground = inBackground;
            ServiceEndpoints = serviceEndpoints ?? Components.ServiceEndpoints().Build();
            EnvironmentReporter = environmentReporter;
            DiagnosticDisabler = diagnosticDisabler;
            DiagnosticStore = diagnosticStore;
            TaskExecutor = taskExecutor ?? new TaskExecutor(null,
                PlatformSpecific.AsyncScheduler.ScheduleAction,
                baseLogger
            );
        }

        internal static Logger MakeLogger(Configuration configuration)
        {
            var logConfig = (configuration.LoggingConfigurationBuilder ?? Components.Logging())
                .CreateLoggingConfiguration();
            var logAdapter = logConfig.LogAdapter ?? Logs.None;
            return logAdapter.Logger(logConfig.BaseLoggerName ?? LogNames.Base);
        }

        internal static IEnvironmentReporter MakeEnvironmentReporter(ApplicationInfoBuilder applicationInfoBuilder)
        {
            var builder = new EnvironmentReporterBuilder();
            if (applicationInfoBuilder != null)
            {
                var applicationInfo = applicationInfoBuilder.Build();
                
                // If AppInfo is provided by the user, then the Config layer has first priority in the environment reporter.
                builder.SetConfigLayer(new ConfigLayerBuilder().SetAppInfo(applicationInfo).Build());
            }

            // The platform layer has second priority if properties aren't set by the Config layer.
            builder.SetPlatformLayer(PlatformAttributes.Layer);
            
            // The SDK layer has third priority if properties aren't set by the Platform layer.
            builder.SetSdkLayer(SdkAttributes.Layer);

            return builder.Build();
        }

        internal LdClientContext WithContextAndBackgroundState(
            Context newCurrentContext,
            bool newInBackground
        ) =>
            new LdClientContext(
                MobileKey,
                BaseLogger,
                newCurrentContext,
                DataSourceUpdateSink,
                EnableBackgroundUpdating,
                EvaluationReasons,
                Http,
                newInBackground,
                ServiceEndpoints,
                EnvironmentReporter,
                DiagnosticDisabler,
                DiagnosticStore,
                TaskExecutor);

        internal LdClientContext WithDataSourceUpdateSink(
            IDataSourceUpdateSink newDataSourceUpdateSink
        ) =>
            new LdClientContext(
                MobileKey,
                BaseLogger,
                CurrentContext,
                newDataSourceUpdateSink,
                EnableBackgroundUpdating,
                EvaluationReasons,
                Http,
                InBackground,
                ServiceEndpoints,
                EnvironmentReporter,
                DiagnosticDisabler,
                DiagnosticStore,
                TaskExecutor);

        internal LdClientContext WithDiagnostics(
            IDiagnosticDisabler newDiagnosticDisabler,
            IDiagnosticStore newDiagnosticStore
        ) =>
            new LdClientContext(
                MobileKey,
                BaseLogger,
                CurrentContext,
                DataSourceUpdateSink,
                EnableBackgroundUpdating,
                EvaluationReasons,
                Http,
                InBackground,
                ServiceEndpoints,
                EnvironmentReporter,
                newDiagnosticDisabler,
                newDiagnosticStore,
                TaskExecutor);
    }
}
