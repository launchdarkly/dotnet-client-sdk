
namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// Interface for a factory that creates some implementation of <see cref="IDataSource"/>.
    /// </summary>
    /// <seealso cref="ConfigurationBuilder.DataSource"/>
    /// <seealso cref="Components"/>
    public interface IDataSourceFactory
    {
        /// <summary>
        /// Called internally by the SDK to create an implementation instance. Applications do not need
        /// to call this method.
        /// </summary>
        /// <param name="context">configuration of the current client instance</param>
        /// <param name="updateSink">the destination for pushing data and status updates</param>
        /// <param name="currentUser">the current user attributes</param>
        /// <param name="inBackground">true if the application is known to be in the background</param>
        /// <returns>an <see cref="IDataSource"/> instance</returns>
        IDataSource CreateDataSource(
            LdClientContext context,
            IDataSourceUpdateSink updateSink,
            User currentUser,
            bool inBackground
            );
    }
}
