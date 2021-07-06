using System;
using System.Threading.Tasks;

namespace LaunchDarkly.Sdk.Client.Internal.Interfaces
{
    /// <summary>
    /// Interface for an object that receives updates to feature flags, user segments, and anything
    /// else that might come from LaunchDarkly.
    /// </summary>
    internal interface IMobileUpdateProcessor : IDisposable
    {
        /// <summary>
        /// Initializes the processor. This is called once from the <see cref="LdClient"/> constructor.
        /// </summary>
        /// <returns>a <c>Task</c> which is completed once the processor has finished starting up</returns>
        Task<bool> Start();

        /// <summary>
        /// Checks whether the processor has finished initializing.
        /// </summary>
        /// <returns>true if fully initialized</returns>
        bool Initialized();
    }

    /// <summary>
    /// Used when the client is offline or in LDD mode.
    /// </summary>
    internal sealed class NullUpdateProcessor : IMobileUpdateProcessor
    {
        Task<bool> IMobileUpdateProcessor.Start()
        {
            return Task.FromResult(true);
        }

        bool IMobileUpdateProcessor.Initialized()
        {
            return true;
        }

        void IDisposable.Dispose() { }
    }
}