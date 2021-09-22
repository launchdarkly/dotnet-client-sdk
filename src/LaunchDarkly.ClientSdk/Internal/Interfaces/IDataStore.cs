using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.Interfaces
{
    /// <summary>
    /// Internal interface for the top-level component that reads and writes flag data.
    /// </summary>
    /// <remarks>
    /// This is non-public because applications should not need to customize storage at
    /// this level; they can customize the <see cref="IPersistentDataStore"/> piece.
    /// The standard implementations of <c>IDataStore</c> are
    /// <see cref="InMemoryDataStore"/> and <see cref="PersistentDataStoreWrapper"/>.
    /// </remarks>
    internal interface IDataStore : IDisposable
    {
        /// <summary>
        /// Tells the data store that we are changing the user properties and it should
        /// load any previously persisted flag data for the new user into memory. The
        /// store does not maintain a "current user" state, since each method has a user
        /// parameter, but <c>Preload</c> allows us to load a whole data set at once
        /// rather than doing so on individual cache misses.
        /// </summary>
        /// <param name="user">the new user properties</param>
        void Preload(User user);

        /// <summary>
        /// Overwrites the store's contents for a specific user with a serialized data set.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="allData"the data set></param>
        void Init(User user, FullDataSet allData);

        /// <summary>
        /// Retrieves an individual flag item for a specific user, if available.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="key">the flag key</param>
        /// <returns>an <see cref="ItemDescriptor"/> containing either the flag data or a
        /// deleted item tombstone, or <see langword="null"/>if the flag key is unknown</returns>
        ItemDescriptor? Get(User user, string key);

        /// <summary>
        /// Retrieves all items for a specific user, if available.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <returns>a <see cref="FullDataSet"/> containing all flags for the user, or
        /// <see langword="null"/>if the user key is unknown</returns>
        FullDataSet? GetAll(User user);

        /// <summary>
        /// Updates or inserts an item. For updates, the object will only be updated if the
        /// existing version is less than the new version.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="key">the flag key</param>
        /// <param name="data">the new flag data or deleted item tombstone</param>
        /// <returns>true if the item was updated; false if it was not updated because the
        /// store contains an equal or greater version</returns>
        bool Upsert(User user, string key, ItemDescriptor data);
    }
}
