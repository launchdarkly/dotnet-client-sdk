using System;

namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Interface for a data store that holds feature flag data in a serialized form.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface should be used for platform-specific integrations that store data somewhere
    /// other than in memory. The SDK will take care of converting between its own internal data model
    /// and a serialized string form; the data store interacts only with the serialized form. The data
    /// store should not make any assumptions about the format of the serialized data.
    /// </para>
    /// <para>
    /// Unlike server-side SDKs, the persistent data store in this SDK reads or writes the data for
    /// all flags at once for a given user, instead of one flag at a time. This is for two reasons:
    /// </para>
    /// <list type="bullet">
    /// <item> The SDK assumes that the persistent store cannot be written to by any other process,
    /// so it does not need to implement read-through behavior when getting individual flags, and can
    /// read flags only from the in-memory cache. It only needs to read the persistent store at
    /// startup time or when changing users, to get any last known data for all flags at once. </item>
    /// <item> On many platforms, reading or writing multiple separate keys may be inefficient or may
    /// not be possible to do atomically. </item>
    /// </list>
    /// <para>
    /// The SDK will also provide its own caching layer on top of the persistent data store; the data
    /// store implementation should not provide caching, but simply do every query or update that the
    /// SDK tells it to do.
    /// </para>
    /// <para>
    /// Implementations must be thread-safe.
    /// </para>
    /// <para>
    /// Error handling is defined as follows: if any data store operation encounters an I/O
    /// error, or is otherwise unable to complete its task, it should throw an exception to make
    /// the SDK aware of this.
    /// </para>
    /// </remarks>
    /// <seealso cref="IPersistentDataStoreFactory"/>
    public interface IPersistentDataStore : IDisposable
    {
        /// <summary>
        /// Overwrites the store's contents for a specific user with a serialized data set.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All previous data for the user should be discarded. This should not affect stored data for
        /// any other user. For efficiency, the store can assume that only the user key is significant.
        /// </para>
        /// </remarks>
        /// <param name="user">the current user</param>
        /// <param name="allData">a serialized data set represented as an opaque string</param>
        void Init(User user, string allData);

        /// <summary>
        /// Retrieves the serialized data for a specific user, if available.
        /// </summary>
        /// <param name="user">the current user</param>
        /// <returns>the serialized data for that user, or null if there is none</returns>
        string GetAll(User user);
    }
}
