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
        /// Returns the string value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        string StringVariation(string key, string defaultValue);

        /// <summary>
        /// Returns the float value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        float FloatVariation(string key, float defaultValue = 0);

        /// <summary>
        /// Returns the integer value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        int IntVariation(string key, int defaultValue = 0);

        /// <summary>
        /// Returns the JToken value of a feature flag for a given flag key.
        /// </summary>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the selected user, or <c>defaultValue</c> if the flag is
        /// disabled in the LaunchDarkly control panel</returns>
        JToken JsonVariation(string key, JToken defaultValue);

        /// <summary>
        /// Tracks that current user performed an event for the given JToken value and given event name.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        /// <param name="data">a JSON string containing additional data associated with the event</param>
        void Track(string eventName, JToken data);

        /// <summary>
        /// Tracks that current user performed an event for the given event name.
        /// </summary>
        /// <param name="eventName">the name of the event</param>
        void Track(string eventName);

        /// <summary>
        /// Gets or sets the online status of the client.
        /// 
        /// The setter will block and wait on the current thread for the Update processor
        /// to either be stopped or started.
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
        /// Registers an instance of <see cref="IFeatureFlagListener"/> for a given flag key to observe 
        /// flag value changes.
        /// 
        /// It is important to note that this callback scheme will need to be handled on the Main UI thread,
        /// if you plan on updating UI components with flag values.
        /// 
        /// </summary>
        /// <param name="flagKey">The flag key you want to observe changes for.</param>
        /// <param name="listener">The instance of the IFeatureFlagListener.</param>
        void RegisterFeatureFlagListener(string flagKey, IFeatureFlagListener listener);

        /// <summary>
        /// Unregisters an instance of <see cref="IFeatureFlagListener"/> for a given flag key to stop observing
        /// flag value changes.
        /// 
        /// </summary>
        /// <param name="flagKey">The flag key you want to observe changes for.</param>
        /// <param name="listener">The instance of the IFeatureFlagListener.</param>
        void UnregisterFeatureFlagListener(string flagKey, IFeatureFlagListener listener);

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
