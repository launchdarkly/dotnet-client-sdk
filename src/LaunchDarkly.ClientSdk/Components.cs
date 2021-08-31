using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client
{
    /// <summary>
    /// Provides configurable factories for the standard implementations of LaunchDarkly component interfaces.
    /// </summary>
    /// <remarks>
    /// Some of the configuration options in <see cref="ConfigurationBuilder"/> affect the entire SDK,
    /// but others are specific to one area of functionality, such as how the SDK receives feature flag
    /// updates or processes analytics events. For the latter, the standard way to specify a configuration
    /// is to call one of the static methods in <see cref="Components"/> (such as <see cref="Logging()"/>),
    /// apply any desired configuration change to the object that that method returns (such as
    /// <see cref="LoggingConfigurationBuilder.BaseLoggerName(string)"/>), and then use the
    /// corresponding method in <see cref="ConfigurationBuilder"/> (such as
    /// <see cref="ConfigurationBuilder.Logging(ILoggingConfigurationFactory)"/>) to use that
    /// configured component in the SDK.
    /// </remarks>
    public static class Components
    {
        /// <summary>
        /// Returns a configuration builder for the SDK's logging configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Passing this to <see cref="ConfigurationBuilder.Logging(ILoggingConfigurationFactory)" />,
        /// after setting any desired properties on the builder, applies this configuration to the SDK.
        /// </para>
        /// <para>
        /// For a description of the default behavior, see <see cref="LoggingConfigurationBuilder"/>.
        /// </para>
        /// <para>
        /// For more about how logging works in the SDK, see the LaunchDarkly
        /// <a href="https://docs.launchdarkly.com/sdk/features/logging#net-client-side">feature guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var config = Configuration.Builder(mobileKey)
        ///         .Logging(Components.Logging().Level(LogLevel.Warn)))
        ///         .Build();
        /// </code>
        /// </example>
        /// <returns>a configurable factory object</returns>
        /// <seealso cref="ConfigurationBuilder.Logging(ILoggingConfigurationFactory)" />
        /// <seealso cref="Components.Logging(ILogAdapter) "/>
        /// <seealso cref="Components.NoLogging" />
        public static LoggingConfigurationBuilder Logging() =>
            new LoggingConfigurationBuilder();

        /// <summary>
        /// Returns a configuration builder for the SDK's logging configuration, specifying the logging implementation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a shortcut for calling <see cref="Logging()"/> and then
        /// <see cref="LoggingConfigurationBuilder.Adapter(ILogAdapter)"/>, to specify a logging implementation
        /// other than the default one. For details about the default implementation, see
        /// <see cref="Logging()"/>.
        /// </para>
        /// <para>
        /// By default, the minimum log level is <c>Info</c> (that is, <c>Debug</c> logging is
        /// disabled). This can be overridden with <see cref="LoggingConfigurationBuilder.Level(LogLevel)"/>.
        /// </para>
        /// <para>
        /// For more about log adapters, see <see cref="LoggingConfigurationBuilder.Adapter(ILogAdapter)"/>.
        /// </para>
        /// <para>
        /// For more about how logging works in the SDK, see the LaunchDarkly
        /// <a href="https://docs.launchdarkly.com/sdk/features/logging#net-client-side">feature guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var config = Configuration.Builder(mobileKey)
        ///         .Logging(Components.Logging(Logs.ToConsole))
        ///         .Build();
        /// </code>
        /// </example>
        /// <param name="adapter">an <c>ILogAdapter</c> for the desired logging implementation</param>
        /// <returns>a configurable factory object</returns>
        /// <seealso cref="ConfigurationBuilder.Logging(ILoggingConfigurationFactory)" />
        /// <seealso cref="LoggingConfigurationBuilder.Adapter(ILogAdapter)" />
        /// <seealso cref="Logging() "/>
        /// <seealso cref="NoLogging" />
        public static LoggingConfigurationBuilder Logging(ILogAdapter adapter) =>
            new LoggingConfigurationBuilder().Adapter(adapter);

        /// <summary>
        /// A configuration object that disables logging.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>Logging(LaunchDarkly.Logging.Logs.None)</c>.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var config = Configuration.Builder(sdkKey)
        ///         .Logging(Components.NoLogging)
        ///         .Build();
        /// </code>
        /// </example>
        public static LoggingConfigurationBuilder NoLogging =>
            new LoggingConfigurationBuilder().Adapter(Logs.None);
    }
}
