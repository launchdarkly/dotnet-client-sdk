using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// Interface for the standard SDK client methods and properties. The only implementation of this is <see cref="LdClient"/>.
    /// </summary>
    /// <remarks>
    /// See also <see cref="ILdClientExtensions"/>, which provides convenience methods that build upon
    /// this interface.
    /// </remarks>
    public interface ILdClient : IDisposable
    {
        /// <summary>
        /// Returns a boolean value indicating LaunchDarkly connection and flag state within the client.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When you first start the client, once <see cref="LdClient.Init(Configuration, User, TimeSpan)"/> or
        /// <see cref="LdClient.InitAsync(Configuration, User)"/> has returned, <see cref="Initialized"/> should be
        /// <see langword="true"/> if and only if either 1. it connected to LaunchDarkly and successfully retrieved
        /// flags, or 2. it started in offline mode so there's no need to connect to LaunchDarkly. If the client
        /// timed out trying to connect to LD, then <see cref="Initialized"/> is <see langword="false"/> (even if we
        /// do have cached flags).  If the client connected and got a 401 error, <see cref="Initialized"/> is
        /// <see langword="false"/>. This serves the purpose of letting the app know that there was a problem of some kind.
        /// </para>
        /// <para>
        /// If you call <see cref="Identify(User, TimeSpan)"/> or <see cref="IdentifyAsync(User)"/>, <see cref="Initialized"/>
        /// will become <see langword="false"/> until the SDK receives the new user's flags.
        /// </para>
        /// </remarks>
        bool Initialized { get; }

        /// <summary>
        /// Indicates whether the SDK is configured to be always offline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is initially <see langword="true"/> if you set it to <see langword="true"/> in the configuration with
        /// <see cref="IConfigurationBuilder.Offline(bool)"/>. However, you can change it at any time to allow the client
        /// to go online, or force it to go offline, using <see cref="SetOffline(bool, TimeSpan)"/> or
        /// <see cref="SetOfflineAsync(bool)"/>.
        /// </para>
        /// <para>
        /// When <see cref="Offline"/> is <see langword="false"/>, the SDK connects to LaunchDarkly if possible, but
        /// this does not guarantee that the connection is successful. There is currently no mechanism to detect whether
        /// the SDK is currently connected to LaunchDarkly.
        /// </para>
        /// </remarks>
        bool Offline { get; }

        /// <summary>
        /// Sets whether the SDK should be always offline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is equivalent to <see cref="SetOfflineAsync(bool)"/>, but as a synchronous method.
        /// </para>
        /// <para>
        /// If you set the property to <see langword="true"/>, any existing connection will be dropped, and the
        /// method immediately returns <see langword="false"/>.
        /// </para>
        /// <para>
        /// If you set it to <see langword="false"/> when it was previously <see langword="true"/>, but no connection can
        /// be made because the network is not available, the method immediately returns <see langword="false"/>, but the
        /// SDK will attempt to connect later if the network becomes available.
        /// </para>
        /// <para>
        /// If you set it to <see langword="false"/> when it was previously <see langword="true"/>, and the network is
        /// available, the SDK will attempt to connect to LaunchDarkly. If the connection succeeds within the interval
        /// <c>maxWaitTime</c>, the method returns <see langword="true"/>. If the connection permanently fails (e.g. if
        /// the mobile key is invalid), the method returns <see langword="false"/>. If the connection attempt is still in
        /// progress after <c>maxWaitTime</c> elapses, the method returns <see langword="false"/>, but the connection
        /// might succeed later.
        /// </para>
        /// </remarks>
        /// <param name="value">true if the client should be always offline</param>
        /// <param name="maxWaitTime">the maximum length of time to wait for a connection</param>
        /// <returns>true if a new connection was successfully made</returns>
        bool SetOffline(bool value, TimeSpan maxWaitTime);

        /// <summary>
        /// Sets whether the SDK should be always offline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is equivalent to <see cref="SetOffline(bool, TimeSpan)"/>, but as an asynchronous method.
        /// </para>
        /// <para>
        /// If you set the property to <see langword="true"/>, any existing connection will be dropped, and the
        /// task immediately yields <see langword="false"/>.
        /// </para>
        /// <para>
        /// If you set it to <see langword="false"/> when it was previously <see langword="true"/>, but no connection can
        /// be made because the network is not available, the task immediately yields <see langword="false"/>, but the
        /// SDK will attempt to connect later if the network becomes available.
        /// </para>
        /// <para>
        /// If you set it to <see langword="false"/> when it was previously <see langword="true"/>, and the network is
        /// available, the SDK will attempt to connect to LaunchDarkly. If and when the connection succeeds, the task
        /// yields <see langword="true"/>. If and when the connection permanently fails (e.g. if the mobile key is
        /// invalid), the task yields <see langword="false"/>.
        /// </para>
        /// </remarks>
        /// <param name="value">true if the client should be always offline</param>
        /// <returns>a task that yields true if a new connection was successfully made</returns>
        Task SetOfflineAsync(bool value);

        /// <summary>
        /// Returns the boolean value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        bool BoolVariation(string key, bool defaultValue = false);

        /// <summary>
        /// Returns the boolean value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included in analytics
        /// events, if you are capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<bool> BoolVariationDetail(string key, bool defaultValue = false);

        /// <summary>
        /// Returns the string value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        string StringVariation(string key, string defaultValue);

        /// <summary>
        /// Returns the string value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included in analytics
        /// events, if you are capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<string> StringVariationDetail(string key, string defaultValue);

        /// <summary>
        /// Returns the float value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        float FloatVariation(string key, float defaultValue = 0);

        /// <summary>
        /// Returns the float value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included in analytics
        /// events, if you are capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<float> FloatVariationDetail(string key, float defaultValue = 0);

        /// <summary>
        /// Returns the integer value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        int IntVariation(string key, int defaultValue = 0);

        /// <summary>
        /// Returns the integer value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included in analytics
        /// events, if you are capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<int> IntVariationDetail(string key, int defaultValue = 0);

        /// <summary>
        /// Returns the JSON value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        LdValue JsonVariation(string key, LdValue defaultValue);

        /// <summary>
        /// Returns the JSON value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included in analytics
        /// events, if you are capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<LdValue> JsonVariationDetail(string key, LdValue defaultValue);

        /// <summary>
        /// Tracks that current user performed an event for the given event name.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        void Track(string eventName);

        /// <summary>
        /// Tracks that the current user performed an event for the given event name, with additional JSON data.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON value containing additional data associated with the event</param>
        void Track(string eventName, LdValue data);

        /// <summary>
        /// Tracks that the current user performed an event for the given event name, and associates it with a
        /// numeric metric value.
        /// </summary>
        /// <remarks>
        /// Refer to the <a href="https://docs.launchdarkly.com/sdk/features/events#xamarin">SDK reference guide</a>
        /// for the latest status.
        /// </remarks>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON value containing additional data associated with the event; pass
        /// <see cref="LdValue.Null"/> if you do not need this value</param>
        /// <param name="metricValue">this value is used by the LaunchDarkly experimentation feature in
        /// numeric custom metrics, and will also be returned as part of the custom event for Data Export</param>
        void Track(string eventName, LdValue data, double metricValue);

        /// <summary>
        /// Returns a map from feature flag keys to <see cref="LdValue"/> feature flag values for the current user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the result of a flag's value would have returned the default variation, the value in the map will contain
        /// <see cref="LdValue.Null"/>. If the client is offline or has not been initialized, an empty
        /// map will be returned.
        /// </para>
        /// <para>
        /// This method will not send analytics events back to LaunchDarkly.
        /// </para>
        /// </remarks>
        /// <returns>a map from feature flag keys to values for the current user</returns>
        IDictionary<string, LdValue> AllFlags();

        /// <summary>
        /// This event is triggered when the client has received an updated value for a feature flag.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This could mean that the flag configuration was changed in LaunchDarkly, or that you have changed the current
        /// user and the flag values are different for this user than for the previous user. The event is only triggered
        /// if the newly received flag value is actually different from the previous one.
        /// </para>
        /// <para>
        /// The <see cref="FlagChangedEventArgs"/> properties will indicate the key of the feature flag, the new value,
        /// and the previous value.
        /// </para>
        /// <para>
        /// On platforms that have a main UI thread (such as iOS and Android), handlers for this event are guaranteed to
        /// be called on that thread; on other platforms, the SDK uses a thread pool. Either way, the handler is called
        /// called asynchronously after whichever SDK action triggered the flag change has already completed. This is to
        /// avoid deadlocks, in case the action was also on the main thread, or on a thread that was holding a lock on
        /// some application resource that the handler also uses.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     client.FlagChanged += (sender, eventArgs) => {
        ///         if (eventArgs.Key == "key-for-flag-i-am-watching") {
        ///             DoSomethingWithNewFlagValue(eventArgs.NewBoolValue);
        ///         }
        ///     };
        /// </code>
        /// </example>
        event EventHandler<FlagChangedEventArgs> FlagChanged;

        /// <summary>
        /// Changes the current user, requests flags for that user from LaunchDarkly if we are online, and generates
        /// an analytics event to tell LaunchDarkly about the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is equivalent to <see cref="IdentifyAsync(User)"/>, but as a synchronous method.
        /// </para>
        /// <para>
        /// If the SDK is online, <see cref="Identify"/> waits to receive feature flag values for the new user from
        /// LaunchDarkly. If it receives the new flag values before <c>maxWaitTime</c> has elapsed, it returns
        /// <see langword="true"/>. If the timeout elapses, it returns <see langword="false"/> (although the SDK might
        /// still receive the flag values later). If we do not need to request flags from LaunchDarkly because we are
        /// in offline mode, it returns <see langword="true"/>.
        /// </para>
        /// <para>
        /// If you do not want to wait, you can either set <c>maxWaitTime</c> to zero or call <see cref="IdentifyAsync(User)"/>.
        /// </para>
        /// </remarks>
        /// <param name="user">the new user</param>
        /// <param name="maxWaitTime">the maximum time to wait for the new flag values</param>
        /// <returns>true if new flag values were obtained</returns>
        bool Identify(User user, TimeSpan maxWaitTime);

        /// <summary>
        /// Changes the current user, requests flags for that user from LaunchDarkly if we are online, and generates
        /// an analytics event to tell LaunchDarkly about the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is equivalent to <see cref="Identify(User, TimeSpan)"/>, but as an asynchronous method.
        /// </para>
        /// <para>
        /// If the SDK is online, the returned task is completed once the SDK has received feature flag values for the
        /// new user from LaunchDarkly, or received an unrecoverable error; it yields <see langword="true"/> for success
        /// or <see langword="false"/> for an error. If the SDK is offline, the returned task is completed immediately
        /// and yields <see langword="true"/>.
        /// </para>
        /// </remarks>
        /// <param name="user">the new user</param>
        /// <returns>a task that yields true if new flag values were obtained</returns>
        Task<bool> IdentifyAsync(User user);

        /// <summary>
        /// Tells the client that all pending analytics events should be delivered as soon as possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the LaunchDarkly client generates analytics events (from flag evaluations, or from
        /// <see cref="Identify(User, TimeSpan)"/> or <see cref="Track(string)"/>), they are queued on a worker thread.
        /// The event thread normally sends all queued events to LaunchDarkly at regular intervals, controlled by the
        /// <see cref="IConfigurationBuilder.EventFlushInterval"/> option. Calling <see cref="Flush"/> triggers a send
        /// without waiting for the next interval.
        /// </para>
        /// <para>
        /// Flushing is asynchronous, so this method will return before it is complete. However, if you
        /// shut down the client with <see cref="IDisposable.Dispose()"/>, events are guaranteed to be
        /// sent before that method returns.
        /// </para>
        /// </remarks>
        void Flush();
    }
}
