using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Interface that an implementation of <see cref="IDataSource"/> will use to push data into the SDK.
    /// </summary>
    /// <remarks>
    /// The data source interacts with this object, rather than manipulating the data store directly, so
    /// that the SDK can perform any other necessary operations that must happen when data is updated.
    /// </remarks>
    public interface IDataSourceUpdateSink
    {
        /// <summary>
        /// Completely overwrites the current contents of the data store with a set of items for each collection.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="data">the data set</param>
        /// <returns>true if the update succeeded, false if it failed</returns>
        void Init(User user, FullDataSet data);

        /// <summary>
        /// Updates or inserts an item. For updates, the object will only be updated if the existing
        /// version is less than the new version.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="key">the feature flag key</param>
        /// <param name="data">the item data</param>
        void Upsert(User user, string key, ItemDescriptor data);
    }
}
