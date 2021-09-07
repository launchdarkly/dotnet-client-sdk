using System.Collections.Immutable;

using static LaunchDarkly.Sdk.Client.DataModel;

namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Types that are used by the data store and related interfaces.
    /// </summary>
    public static class DataStoreTypes
    {
        /// <summary>
        /// Represents a full set of feature flag data received from LaunchDarkly.
        /// </summary>
        public struct FullDataSet
        {
            /// <summary>
            /// The feature flags, indexed by key.
            /// </summary>
            public IImmutableDictionary<string, FeatureFlag> Flags { get; }

            /// <summary>
            /// Creates a new instance.
            /// </summary>
            /// <param name="flags">the feature flags, indexed by key</param>
            public FullDataSet(IImmutableDictionary<string, FeatureFlag> flags)
            {
                Flags = flags ?? ImmutableDictionary.Create<string, FeatureFlag>();
            }
        }
    }
}
