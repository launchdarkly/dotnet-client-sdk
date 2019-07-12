using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// Represents a callback listener for feature flag value changes.
    /// 
    /// You should have your ViewController, Activity or custom class implement this interface if you want to
    /// be notified of value changes for a given flag key.
    /// 
    /// Look at <see cref="ILdMobileClient.RegisterFeatureFlagListener(string, IFeatureFlagListener)"/> for
    /// usage of this interface.
    /// </summary>
    public interface IFeatureFlagListener
    {
        /// <summary>
        /// Tells the implementer of this interface that the feature flag for the given key
        /// was changed to the given value.
        /// 
        /// It is important to know that this will not be called on your main UI thread, so if you plan
        /// on updating the UI when this is called, you will need to use the appropriate pattern to post safely to the UI
        /// main thread.
        /// </summary>
        /// <param name="featureFlagKey">The feature flag key.</param>
        /// <param name="value">The feature flag value that changed.</param>
        void FeatureFlagChanged(string featureFlagKey, JToken value);

        /// <summary>
        /// Tells the implementer of this interface that the feature flag for the given key
        /// was deleted on the LaunchDarkly service side.
        /// 
        /// It is important to know that this will not be called on your main UI thread, so if you plan
        /// on updating the UI when this is called, you will need to use the appropriate pattern to post safely to the UI
        /// main thread.
        /// </summary>
        /// <param name="featureFlagKey">The feature flag key.</param>
        void FeatureFlagDeleted(string featureFlagKey);
    }
}

