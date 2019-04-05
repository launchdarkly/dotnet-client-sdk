using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// An interface that is used internally by implementations of <see cref="IBackgroundingManagerFactory"/>
    /// to update the state of the LaunchDarkly client in background mode. Application code does not need
    /// to interact with this interface.
    /// </summary>
    public interface IBackgroundingState
    {
        /// <summary>
        /// Tells the LaunchDarkly client that the application is entering background mode. The client will
        /// suspend the regular streaming or polling process, except when <see cref="BackgroundPollAsync"/>
        /// is called.
        /// </summary>
        Task EnterBackgroundAsync();

        /// <summary>
        /// Tells the LaunchDarkly client that the application is exiting background mode. The client will
        /// resume the regular streaming or polling process.
        /// </summary>
        Task ExitBackgroundAsync();
    }
}
