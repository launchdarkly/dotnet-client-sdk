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
        /// Tracks that the current user performed an event for the given event name, with additional JSON data.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON value containing additional data associated with the event</param>
        void Track(string eventName, ImmutableJsonValue data);

        /// <summary>
        /// Tracks that the current user performed an event for the given event name.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        void Track(string eventName);

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
        /// Indicates which platform the SDK is built for.
        /// </summary>
        /// <remarks>
        /// This property is mainly useful for debugging. It does not indicate which platform you are actually running on,
        /// but rather which variant of the SDK is currently in use.
        /// 
        /// The <c>LaunchDarkly.XamarinSdk</c> package contains assemblies for multiple target platforms. In an Android
        /// or iOS application, you will normally be using the Android or iOS variant of the SDK; that is done
        /// automatically when you install the NuGet package. On all other platforms, you will get the .NET Standard
        /// variant.
        ///
        /// The basic features of the SDK are the same in all of these variants; the difference is in platform-specific
        /// behavior such as detecting when an application has gone into the background, detecting network connectivity,
        /// and ensuring that code is executed on the UI thread if applicable for that platform. Therefore, if you find
        /// that these platform-specific behaviors are not working correctly, you may want to check this property to
        /// make sure you are not for some reason running the .NET Standard SDK on a phone.
        /// </remarks>
        PlatformType PlatformType { get; }
    }
}
