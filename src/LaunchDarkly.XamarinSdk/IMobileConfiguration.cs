using System;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    public interface IMobileConfiguration : IBaseConfiguration
    {
        /// <summary>
        /// The interval between feature flag updates when the app is running in the background.
        /// </summary>
        TimeSpan BackgroundPollingInterval { get; }

        /// <summary>
        /// When streaming mode is disabled, this is the interval between feature flag updates.
        /// </summary>
        TimeSpan PollingInterval { get; }

        /// <summary>
        /// The connection timeout to the LaunchDarkly server.
        /// </summary>
        TimeSpan ConnectionTimeout { get; }

        /// <summary>
        /// Whether to enable feature flag updates when the app is running in the background.
        /// </summary>
        bool EnableBackgroundUpdating { get; }

        /// <summary>
        /// The time between flushes of the event buffer. Decreasing the flush interval means
        /// that the event buffer is less likely to reach capacity. The default value is 5 seconds.
        /// </summary>
        TimeSpan EventFlushInterval { get; }

        /// <summary>
        /// Whether to enable real-time streaming flag updates. When false, feature flags are updated via polling.
        /// </summary>
        bool IsStreamingEnabled { get; }

        /// <summary>
        /// Whether to use the REPORT HTTP verb when fetching flags from LaunchDarkly.
        /// </summary>
        bool UseReport { get; }

        /// <summary>
        /// True if LaunchDarkly should provide additional information about how flag values were
        /// calculated. The additional information will then be available through the client's "detail"
        /// methods such as <see cref="ILdMobileClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to true.
        /// </summary>
        bool EvaluationReasons { get; }

        /// <summary>
        /// True if the SDK should save flag values for each user in persistent storage, so they will be
        /// immediately available the next time the SDK is started for the same user. This is true by
        /// default; set it to false to disable this behavior.
        /// </summary>
        /// <remarks>
        /// The implementation of persistent storage depends on the target platform. In Android and iOS, it
        /// uses the standard user preferences mechanism. In .NET Standard, it uses the IsolatedStorageFile
        /// API, which stores file data under the current account's home directory at
        /// <c>~/.local/share/IsolateStorage/</c>.
        /// </remarks>
        bool PersistFlagValues { get; }
    }
}
