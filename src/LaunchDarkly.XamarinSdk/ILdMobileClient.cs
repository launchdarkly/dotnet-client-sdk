using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LaunchDarkly.Common;
using System.Threading.Tasks;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    public interface ILdMobileClient : ILdCommonClient
    {
        /// <summary>
        /// Tests whether the client is ready to be used.
        /// </summary>
        /// <returns>true if the client is ready, or false if it is still initializing</returns>
        bool Initialized();

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
        /// describes the way the value was determined. The <c>Reason</c> property in the result will
        /// also be included in analytics events, if you are capturing detailed event data for this flag.
        /// </summary>
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
        /// describes the way the value was determined. The <c>Reason</c> property in the result will
        /// also be included in analytics events, if you are capturing detailed event data for this flag.
        /// </summary>
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
        /// describes the way the value was determined. The <c>Reason</c> property in the result will
        /// also be included in analytics events, if you are capturing detailed event data for this flag.
        /// </summary>
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
        /// describes the way the value was determined. The <c>Reason</c> property in the result will
        /// also be included in analytics events, if you are capturing detailed event data for this flag.
        /// </summary>
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
        /// describes the way the value was determined. The <c>Reason</c> property in the result will
        /// also be included in analytics events, if you are capturing detailed event data for this flag.
        /// </summary>
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
        /// Tracks that current user performed an event for the given JToken value and given event name.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON value containing additional data associated with the event</param>
        void Track(string eventName, ImmutableJsonValue data);

        /// <summary>
        /// Tracks that current user performed an event for the given event name, and associates it with a
        /// numeric metric value.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON string containing additional data associated with the event</param>
        /// <param name="metricValue">This value is used by the LaunchDarkly experimentation feature in
        /// numeric custom metrics, and will also be returned as part of the custom event for Data Export.</param>
        void Track(string eventName, JToken data, double metricValue);

        /// <summary>
        /// Gets or sets the online status of the client.
        /// 
        /// The setter is equivalent to calling <see cref="SetOnlineAsync(bool)"/>. If you are going from offline
        /// to online, and you want to wait until the connection has been established, call
        /// <see cref="SetOnlineAsync(bool)"/> and then use <c>await</c> or call <c>Wait()</c> on
        /// its return value.
        /// </summary>
        /// <value><c>true</c> if online; otherwise, <c>false</c>.</value>
        bool Online { get; set; }

        /// <summary>
        /// Sets the client to be online or not.
        /// </summary>
        /// <returns>a Task</returns>
        /// <param name="value">If set to <c>true</c> value.</param>
        Task SetOnlineAsync(bool value);

        /// <summary>
        /// Returns a map from feature flag keys to <see cref="JToken"/> feature flag values for the current user.
        /// If the result of a flag's value would have returned the default variation, it will have a
        /// null entry in the map. If the client is offline or has not been initialized, a <c>null</c> map will be returned. 
        /// This method will not send analytics events back to LaunchDarkly.
        /// </summary>
        /// <returns>a map from feature flag keys to {@code JToken} for the current user</returns>
        IDictionary<string, JToken> AllFlags();

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
        /// Registers the user.
        /// 
        /// This method will wait and block the current thread until the update processor has finished
        /// initializing and received a response from the LaunchDarkly service.
        /// </summary>
        /// <param name="user">the user to register</param>
        void Identify(User user);

        /// <summary>
        /// Registers the user.
        /// </summary>
        /// <param name="user">the user to register</param>
        Task IdentifyAsync(User user);
    }
}
