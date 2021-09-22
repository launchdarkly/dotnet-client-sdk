
namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Interface for a factory that creates some implementation of <see cref="IPersistentDataStore"/>.
    /// </summary>
    /// <seealso cref="ConfigurationBuilder.Persistence(IPersistentDataStoreFactory)"/>
    public interface IPersistentDataStoreFactory
    {
        /// <summary>
        /// Called internally by the SDK to create an implementation instance. Applications do not need
        /// to call this method.
        /// </summary>
        /// <param name="context">configuration of the current client instance</param>
        /// <returns>an <c>IPersistentDataStore</c> instance</returns>
        IPersistentDataStore CreatePersistentDataStore(LdClientContext context);
    }
}
