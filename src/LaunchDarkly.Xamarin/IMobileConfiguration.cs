using System;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    public interface IMobileConfiguration : IBaseConfiguration
    {
        /// <summary>
        /// The interval between feature flag updates when the app is running in the background.
        /// </summary>
        /// <value>The background polling interval.</value>
        TimeSpan BackgroundPollingInterval { get; }

        /// <summary>
        /// When streaming mode is disabled, this is the interval between feature flag updates.
        /// </summary>
        /// <value>The feature flag polling interval.</value>
        TimeSpan PollingInterval { get; }

        /// <summary>
        /// Gets the connection timeout to the LaunchDarkly server
        /// </summary>
        /// <value>The connection timeout.</value>
        TimeSpan ConnectionTimeout { get; }

        /// <summary>
        /// Whether to enable feature flag updates when the app is running in the background.
        /// </summary>
        /// <value><c>true</c> if disable background updating; otherwise, <c>false</c>.</value>
        bool EnableBackgroundUpdating { get; }

        /// <summary>
        /// Whether to enable real-time streaming flag updates. When false, 
        /// feature flags are updated via polling.
        /// </summary>
        /// <value><c>true</c> if streaming; otherwise, <c>false</c>.</value>
        bool IsStreamingEnabled { get; }

        /// <summary>
        /// Whether to use the REPORT HTTP verb when fetching flags from LaunchDarkly.
        /// </summary>
        /// <value><c>true</c> if use report; otherwise, <c>false</c>.</value>
        bool UseReport { get; }

        /// <summary>
        /// True if LaunchDarkly should provide additional information about how flag values were
        /// calculated. The additional information will then be available through the client's "detail"
        /// methods such as <see cref="ILdMobileClient.BoolVariationDetail(string, bool)"/>. Since this
        /// increases the size of network requests, such information is not sent unless you set this option
        /// to true.
        /// </summary>
        /// <value><c>true</c> if evaluation reasons are desired.</value>
        bool EvaluationReasons { get; }
    }
}
