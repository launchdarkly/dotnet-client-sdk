using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    public interface ILdClient : IDisposable
    {
        /// <summary>
        /// Returns the current version number of the LaunchDarkly client.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Returns a boolean value indicating LaunchDarkly connection and flag state within the client.
        /// </summary>
        /// <remarks>
        /// When you first start the client, once Init or InitAsync has returned, 
        /// Initialized() should be true if and only if either 1. it connected to LaunchDarkly 
        /// and successfully retrieved flags, or 2. it started in offline mode so 
        /// there's no need to connect to LaunchDarkly. So if the client timed out trying to 
        /// connect to LD, then Initialized is false (even if we do have cached flags). 
        /// If the client connected and got a 401 error, Initialized is false. 
        /// This serves the purpose of letting the app know that 
        /// there was a problem of some kind. Initialized() will be temporarily false during the 
        /// time in between calling Identify and receiving the new user's flags. It will also be false 
        /// if you switch users with Identify and the client is unable 
        /// to get the new user's flags from LaunchDarkly. 
        /// </remarks>
        /// <returns>true if the client has connected to LaunchDarkly and has flags or if the config is set offline, or false if it couldn't connect.</returns>
        bool Initialized();

        /// <summary>
        /// Tests whether the client is being used in offline mode.
        /// </summary>
        /// <returns>true if the client is offline</returns>
        bool IsOffline();

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
        /// The <c>Reason</c> property in the result will also be included in analytics events, if you are
        /// capturing detailed event data for this flag.
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
        /// The <c>Reason</c> property in the result will also be included in analytics events, if you are
        /// capturing detailed event data for this flag.
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
        /// The <c>Reason</c> property in the result will also be included in analytics events, if you are
        /// capturing detailed event data for this flag.
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
        /// The <c>Reason</c> property in the result will also be included in analytics events, if you are
        /// capturing detailed event data for this flag.
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
        ImmutableJsonValue JsonVariation(string key, ImmutableJsonValue defaultValue);

        /// <summary>
        /// Returns the JSON value of a feature flag for a given flag key, in an object that also
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// The <c>Reason</c> property in the result will also be included in analytics events, if you are
        /// capturing detailed event data for this flag.
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <c>EvaluationDetail</c> object</returns>
        EvaluationDetail<ImmutableJsonValue> JsonVariationDetail(string key, ImmutableJsonValue defaultValue);

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
        void Track(string eventName, ImmutableJsonValue data);

        /// <summary>
        /// Tracks that the current user performed an event for the given event name, and associates it with a
        /// numeric metric value.
        /// </summary>
        /// <remarks>
        /// As of this version’s release date, the LaunchDarkly service does not support the <c>metricValue</c>
        /// parameter. As a result, calling this overload of <c>Track</c> will not yet produce any different
        /// behavior from calling <see cref="Track(String, ImmutableJsonValue)"/> without a <c>metricValue</c>.
        /// Refer to the <see cref="https://docs.launchdarkly.com/docs/xamarin-sdk-reference#section-track">SDK reference guide</a>
        /// for the latest status.
        /// </remarks>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON value containing additional data associated with the event; pass
        /// <see cref="ImmutableJsonValue.Null"/> if you do not need this value</param>
        /// <param name="metricValue">this value is used by the LaunchDarkly experimentation feature in
        /// numeric custom metrics, and will also be returned as part of the custom event for Data Export</param>
        void Track(string eventName, ImmutableJsonValue data, double metricValue);

        /// <summary>
        /// Gets or sets the online status of the client.
        /// </summary>
        /// <remarks>
        /// The setter is equivalent to calling <see cref="SetOnlineAsync(bool)"/>; if you are going from offline to
        /// online, it does <i>not</i> wait until the connection has been established. If you want to wait for the
        /// connection, call <see cref="SetOnlineAsync(bool)"/> and then use <c>await</c>.
        /// </remarks>
        /// <value><c>true</c> if online; otherwise, <c>false</c>.</value>
        bool Online { get; set; }

        /// <summary>
        /// Sets the client to be online or not.
        /// </summary>
        /// <returns>a Task</returns>
        /// <param name="value">true if the client should be online</param>
        Task SetOnlineAsync(bool value);

        /// <summary>
        /// Returns a map from feature flag keys to JSON feature flag values for the current user.
        /// </summary>
        /// <remarks>
        /// If the result of a flag's value would have returned the default variation, the value in the map will be
        /// <c>ImmutableJsonValue.Null</c>. If the client is offline or has not been initialized, a <c>null</c> map
        /// will be returned.
        /// 
        /// This method will not send analytics events back to LaunchDarkly.
        /// </remarks>
        /// <returns>a map from feature flag keys to values for the current user</returns>
        IDictionary<string, ImmutableJsonValue> AllFlags();

        /// <summary>
        /// This event is triggered when the client has received an updated value for a feature flag.
        /// </summary>
        /// <remarks>
        /// This could mean that the flag configuration was changed in LaunchDarkly, or that you have changed the current
        /// user and the flag values are different for this user than for the previous user. The event is only triggered
        /// if the newly received flag value is actually different from the previous one.
        ///
        /// The <see cref="FlagChangedEventArgs"/> properties will indicate the key of the feature flag, the new value,
        /// and the previous value.
        ///
        /// On platforms that have a main UI thread (such as iOS and Android), handlers for this event are guaranteed to
        /// be called on that thread; on other platforms, the SDK uses a thread pool. Either way, the handler is called
        /// called asynchronously after whichever SDK action triggered the flag change has already completed. This is to
        /// avoid deadlocks, in case the action was also on the main thread, or on a thread that was holding a lock on
        /// some application resource that the handler also uses.
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
        /// Changes the current user.
        /// </summary>
        /// <remarks>
        /// This both sets the current user for the purpose of flag evaluations and also generates an analytics event to
        /// tell LaunchDarkly about the user.
        /// 
        /// <c>Identify</c> waits and blocks the current thread until the SDK has received feature flag values for the
        /// new user from LaunchDarkly. If you do not want to wait, consider <see cref="IdentifyAsync(User)"/>.
        /// </remarks>
        /// <param name="user">the new user</param>
        void Identify(User user);

        /// <summary>
        /// Changes the current user.
        /// </summary>
        /// <remarks>
        /// This both sets the current user for the purpose of flag evaluations and also generates an analytics event to
        /// tell LaunchDarkly about the user.
        /// 
        /// <c>IdentifyAsync</c> is meant to be used from asynchronous code. It returns a Task that is resolved once the
        /// SDK has received feature flag values for the new user from LaunchDarkly.
        /// </remarks>
        /// <example>
        ///     // Within asynchronous code, use await to wait for the task to be resolved
        ///     await client.IdentifyAsync(user);
        ///
        ///     // Or, if you want to let the flag values be retrieved in the background instead of waiting:
        ///     Task.Run(() => client.IdentifyAsync(user));
        /// </example>
        /// <param name="user">the user to register</param>
        Task IdentifyAsync(User user);

        /// <summary>
        /// Flushes all pending events.
        /// </summary>
        void Flush();
    }
}
